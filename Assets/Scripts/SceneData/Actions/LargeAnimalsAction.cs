using System;
using System.Threading;
using System.Collections.Generic;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneData.Rules;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class LargeAnimalsAction : AnimalsAction
	{
		// TODO: EcoBase linkage

		public const string XML_ELEMENT = "largeanimals";

		public LargeAnimalsAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public LargeAnimalsAction (Scene scene) : base(scene)
		{
		}

		public static LargeAnimalsAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			LargeAnimalsAction action = new LargeAnimalsAction (scene, id);
			LoadBase (action, scene, reader);
			return action;
		}

		public override void Save (XmlTextWriter writer)
		{
			writer.WriteStartElement (XML_ELEMENT);
			SaveBase (this, writer);
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}

		public override string GetDescription ()
		{
			return "Handle Large Animals Logic";
		}

		public override void DoSuccession ()
		{
			base.DoSuccession ();
			
			if (successionArea == null) {
				successionArea = scene.progression.successionArea;
			}
			
			// Handle the default logic
			if (!skipNormalAnimalsLogic)
			{
				List<LargeAnimalType> animals = new List<LargeAnimalType>();
				foreach (AnimalType at in scene.animalTypes) {
					if (at is LargeAnimalType) {
						LargeAnimalType a = at as LargeAnimalType;
						animals.Add (a);

						a.movementMap = new BitMap8 (scene);
						a.deathMap = new BitMap8 (scene);
						a.fouragingMap = new BitMap8 (scene);
					}
				}
				if (animals.Count > 0)
				{
					activeThreads = animals.Count;

					for (int i = 0; i < animals.Count; i++)
					{
						// Temp disable unreachable code warning
						#pragma warning disable 162
						if (GameSettings.ANIMALS_LOGIC_MULTITHREADED) 
						{
							ThreadPool.QueueUserWorkItem (ProcessAnimal, animals[i]);
						}
						else {
							ProcessAnimal (animals[i]);
						}
						#pragma warning restore 162
					}
				}
				else {
					activeThreads = 0;
					this.finishedProcessing = true;
				}
			} else {
				activeThreads = 0;
				this.finishedProcessing = true;
			}
		}

		protected void ProcessAnimal (object arguments)
		{
			System.Random rnd = new System.Random (); // When multithreading, you need a random generator per thread

			LargeAnimalType animal = arguments as LargeAnimalType;
			ProcessAnimalMovement (animal, rnd);
			ProcessAnimalPopulationDecrease (animal, rnd);
		}

		protected void ProcessAnimalOnTile (AnimalSuccessionData data)
		{
			Coordinate coord = new Coordinate ((int)data.currPos.x, (int)data.currPos.y);
			
			// Decrease model
			ProcessAnimalDecreaseOnTile (data, coord);
			if (data.hasDied) return;
			
			// Check if the animal has found food
			ProcessAnimalFouraging (data, coord);
		}

		#region Movement

		protected class Direction
		{
			public static Vector2[] directions = new Vector2[] 
			{
				new Vector2 (0f, 1f), // up
				new Vector2 (0f, -1f), // down
				new Vector2 (1f, 0f), // right
				new Vector2 (-1f, 0f) // left
			};

			public Vector2 dir;
			public float chance;

			public Direction (Vector2 dir, float chance)
			{
				this.dir = dir;
				this.chance = chance;
			}

			public bool CheckChance (System.Random rnd)
			{
				return (float)rnd.NextDouble() <= this.chance;
			}

			public override string ToString ()
			{
				return string.Format ("[Direction] {0}, {1}", dir, chance.ToString ("0.000"));
			}
		}

		protected class AnimalSuccessionData
		{
			public LargeAnimalType animal { get; private set; }

			public System.Random rnd;
			public Vector2 currPos;
			public Direction prevDir;

			public int walkDistance;
			public int carriedFood;

			private bool _hasDied;
			public bool hasDied
			{
				get { return _hasDied; }
				set {
					_hasDied = value;
					if (_hasDied) walkDistance = 0;
				}
			}

			public AnimalSuccessionData (LargeAnimalType animal, System.Random rnd, Vector2 startPos)
			{
				this.animal = animal;
				this.rnd = rnd;

				int walkMin = animal.landUseModel.movement.minWalkDistance;
				int walkMax = animal.landUseModel.movement.maxWalkDistance;
				this.walkDistance = RndUtil.RndRange (ref rnd, walkMin, walkMax);

				this.prevDir = new Direction (Vector2.zero, 0f);
				this.currPos = startPos;
				this.hasDied = false;
			}

			public Coordinate GetCurrentCoordinate ()
			{
				return new Coordinate ((int)currPos.x, (int)currPos.y);
			}
		}

		protected void ProcessAnimalMovement (LargeAnimalType animal, System.Random rnd)
		{
			// Land use
			if (animal.landUseModel != null)
			{
				try 
				{
					// Temp vars
					List<Direction> dirs = new List<Direction> ();

					// Loop through all nests
					List<AnimalStartPopulationModel.Nests.Nest> nests = new List<AnimalStartPopulationModel.Nests.Nest>(animal.startPopModel.nests.nests);

					while (nests.Count > 0)
					{
						// Choose random nest
						AnimalStartPopulationModel.Nests.Nest nest = nests[rnd.Next (nests.Count)];
						nests.Remove (nest);

						// Process movement
						int survived = 0;
						int foundFood = 0;

						for (int i = 0; i < 2; i++)
						{
							bool processMales = (i == 0);
							int total = (processMales) ? nest.males : nest.females;
							for (int n = 0; n < total; n++) 
							{
								AnimalSuccessionData data = new AnimalSuccessionData (animal, rnd, new Vector2 (nest.x, nest.y));

								// Process walking
								while (data.walkDistance > 0) {
									MoveAnimal (data);
								}

								// Check if the animal has survived
								if (!data.hasDied) 
								{
									survived++;
									foundFood += data.carriedFood;
								}
							}

							// Update the nests male population
							if (processMales) nest.males = survived;
							else nest.females = survived;
							nest.currentFood += foundFood;
						}

						// Check nest decrease
						ProcessAnimalNestDecrease (animal, nest, rnd);

						// Check nest growth
						ProcessAnimalGrowth (animal, nest, rnd);
					}

					// Process land use
				} catch (Exception e) {
					UnityEngine.Debug.LogException (e);
				}
			}

			ThreadFinished ();
		}

		protected void MoveAnimal (AnimalSuccessionData data)
		{
			// Check the new direction
			Direction newDir = ChooseMoveDirection (data);
			if (newDir != null)
			{
				data.currPos += newDir.dir;
				data.prevDir = newDir;
			}

			// Save the moment in the map
			Coordinate c = data.GetCurrentCoordinate ();
			int val = data.animal.movementMap.Get (c);
			data.animal.movementMap.Set (c, val + 1);

			// Process tile
			data.walkDistance--;
			ProcessAnimalOnTile (data);
		}

		protected Direction ChooseMoveDirection (AnimalSuccessionData data)
		{
			// Possible directions (and their chance etc)
			List<Direction> possibleDirs = new List<Direction> ();
			
			// Get all possible directions and their probability chance
			for (int d = 0; d < Direction.directions.Length; d++) 
			{
				Vector2 newPos = data.currPos + Direction.directions[d];
				if ((newPos.x >= 0) && (newPos.y >= 0) && 
				    (newPos.x < this.scene.width) && (newPos.y < this.scene.height) &&
				    scene.progression.successionArea.Get ((int)newPos.x, (int)newPos.y) > 0) 
				{
					// Direction is valid, check the chance
					Vector2 dir = Direction.directions[d];
					Data movePrefData = data.animal.landUseModel.movement.movePrefData;

					float val = (float)movePrefData.Get ((int)newPos.x, (int)newPos.y);
					float chance = val / (float)movePrefData.GetMax();
					possibleDirs.Add (new Direction (dir, chance));
				}
			}
			
			// Sort the possible directions by chance
			possibleDirs.Sort (delegate(Direction a, Direction b) 
			{
				if (a.chance > b.chance) return -1;
				else if (a.chance < b.chance) return 1;
				else return (data.rnd.NextDouble () < 0.5f) ? 0 : 1;
			});

			// Place the direction with the same dir as the previous direction at the bottom,
			// we want to check this one for last
			foreach (Direction d in possibleDirs) 
			{
				if (d.dir == data.prevDir.dir) 
				{
					possibleDirs.Remove (d);
					possibleDirs.Add (d);
					break;
				}
			}
			
			// Check all directions, from the highest chance to the lowest
			Direction newDir = null;
			for (int i = 0; i < possibleDirs.Count; i++) 
			{
				if (possibleDirs[i].CheckChance (data.rnd))
				{
					newDir = possibleDirs[i];
					break;
				}
			}
			return newDir;
		}

		#endregion Land use

		#region Decrease

		protected void ProcessAnimalDecreaseOnTile (AnimalSuccessionData data, Coordinate coord)
		{
			AnimalPopulationDecreaseModel.SpecifiedNumber.ArtificialDeath death = data.animal.decreaseModel.specifiedNumber.artificialDeath;

			// Process artificial death on tile
			if (death.use)
			{
				foreach (AnimalPopulationDecreaseModel.SpecifiedNumber.ArtificialDeath.ArtificialDeathEntry de in death.entries)
				{
					switch (de.type)
					{
					// Fixed chance
					case AnimalPopulationDecreaseModel.SpecifiedNumber.ArtificialDeath.ArtificialDeathEntry.Types.FixedChance :
					{
						// Calculate the chance
						float dataChance = (float)de.data.Get (coord) / (float)de.data.GetMax ();
						float deathChance = dataChance; 
						/*if (de.fixedChance.min != 0f || de.fixedChance.max != 1f){
							deathChance = RndUtil.RndRange (ref data.rnd, de.fixedChance.min, de.fixedChance.max);
						}*/ // TODO: Fix?

						// Check death
						if (data.rnd.NextDouble () <= deathChance)
						{
							data.hasDied = true;

							// Save the death in the map
							Coordinate c = data.GetCurrentCoordinate ();
							int val = data.animal.deathMap.Get (c);
							data.animal.deathMap.Set (c, val + 1);
							break;
						}
					}
					break;
					}
				}
			}
		}

		protected void ProcessAnimalNestDecrease (LargeAnimalType animal, AnimalStartPopulationModel.Nests.Nest nest, System.Random rnd)
		{
			// Specified number
			AnimalPopulationDecreaseModel.SpecifiedNumber spec = animal.decreaseModel.specifiedNumber;
			if (spec.use)
			{
				// Starvation
				AnimalPopulationDecreaseModel.SpecifiedNumber.Starvation starvation = spec.starvation;
				if (starvation.use)
				{
					// Check for each animal in the nest if they will starve
					int reqFood = starvation.foodRequiredPerAnimal;
					int max = (nest.males > nest.females) ? nest.males : nest.females;

					// We will use this for loop so we check 1 of every gender before checking the next
					// Or else one gender would always have a higher chance to starve
					int starvedMales = 0;
					int starvedFemales = 0;

					for (int i = 0; i < max; i++) 
					{
						// Males, check if we have some left to check
						if (i < nest.males) {
							if (ProcessAnimalStarvation (animal, nest, rnd))
								starvedMales++;
						}

						// Females,  check if we have some left to check
						if (i < nest.females) {
							if (ProcessAnimalStarvation (animal, nest, rnd))
								starvedFemales++;
						}
					}

					// Update the nests population
					nest.males -= starvedMales;
					nest.females -= starvedFemales;
				}

				// Natural death rate
				AnimalPopulationDecreaseModel.SpecifiedNumber.NaturalDeathRate naturalDeath = spec.naturalDeathRate;
				if (naturalDeath.use)
				{
					if (naturalDeath.minDeathRate >= 0f && naturalDeath.maxDeathRate > 0f)
					{
						int maleDeaths = 0;
						int femaleDeaths = 0;

						// Males
						for (int i = 0; i < nest.males; i++) {
							float deathRate = RndUtil.RndRange (ref rnd, naturalDeath.minDeathRate, naturalDeath.maxDeathRate);
							if (deathRate > 0f && rnd.NextDouble () <= deathRate) {
								maleDeaths++;
							}
						}

						// Females
						for (int i = 0; i < nest.females; i++) {
							float deathRate = RndUtil.RndRange (ref rnd, naturalDeath.minDeathRate, naturalDeath.maxDeathRate);
							if (deathRate > 0f && rnd.NextDouble () <= deathRate) {
								femaleDeaths++;
							}
						}

						// Update the nests population
						nest.males -= maleDeaths;
						nest.females -= femaleDeaths;
					}
				}
			}
		}

		/// <summary>
		/// Checks if the animal starves with the given parameters. It returns whether it did starve or not.
		/// </summary>
		protected bool ProcessAnimalStarvation (LargeAnimalType animal, AnimalStartPopulationModel.Nests.Nest nest, System.Random rnd)
		{
			AnimalPopulationDecreaseModel.SpecifiedNumber.Starvation starvation = animal.decreaseModel.specifiedNumber.starvation;

			// Formula : (available food / req food) * starve rate
			float starveRate = RndUtil.RndRange (ref rnd, starvation.minStarveRate, starvation.maxStarveRate);
			if (starveRate > 0f) 
			{
				// Get the amount of available food for the animal.
				int requiredFood = starvation.foodRequiredPerAnimal;
				int availableFood = Mathf.Clamp (requiredFood, 0, nest.currentFood);
				nest.currentFood -= availableFood;

				// Check if the animal will starve
				float starveChance = (1f - ((float)availableFood / (float)requiredFood)) * starveRate;
				return (rnd.NextDouble () <= starveChance);
			}
			return false;
		}

		protected void ProcessAnimalPopulationDecrease (LargeAnimalType animal, System.Random rnd)
		{
			// Fixed
			AnimalPopulationDecreaseModel.FixedNumber fixedNumber = animal.decreaseModel.fixedNumber;
			if (fixedNumber.use)
			{
				// Check type
				switch (fixedNumber.type)
				{
				case AnimalPopulationDecreaseModel.FixedNumber.Types.Absolute :
				{
					int totalDeaths = fixedNumber.absolute;
					DecreaseAnimalPopulation (animal, totalDeaths, rnd);
				}
				break;

				case AnimalPopulationDecreaseModel.FixedNumber.Types.Relative :
				{
					if (fixedNumber.relative > 0f)
					{
						// Total deaths
						int totalPopulation = CountTotalAnimalPopulation (animal);
						int totalDeaths = Mathf.RoundToInt ((float)totalPopulation * fixedNumber.relative);
						DecreaseAnimalPopulation (animal, totalDeaths, rnd);
					}
				}
				break;
				}
			}
		}

		protected int CountTotalAnimalPopulation (LargeAnimalType animal)
		{
			// Get the total population
			int totalPopulation = 0;
			AnimalStartPopulationModel.Nests.Nest[] nests = animal.startPopModel.nests.nests;
			foreach (AnimalStartPopulationModel.Nests.Nest n in nests)
			{
				totalPopulation += n.males;
				totalPopulation += n.females;
			}
			return totalPopulation;
		}

		protected void DecreaseAnimalPopulation (LargeAnimalType animal, int totalDeaths, System.Random rnd)
		{
			// Randomly kill animals throughout all nests
			AnimalStartPopulationModel.Nests.Nest[] nests = animal.startPopModel.nests.nests; 
			List<AnimalStartPopulationModel.Nests.Nest> nestsWithPopulation = new List<AnimalStartPopulationModel.Nests.Nest>(nests);
			while (totalDeaths > 0 && nestsWithPopulation.Count > 0) 
			{
				// Choose random nest
				AnimalStartPopulationModel.Nests.Nest n = nestsWithPopulation[rnd.Next (nestsWithPopulation.Count)];
				
				// Check if nest has a population
				if (n.males <= 0 && n.females <= 0) {
					nestsWithPopulation.Remove (n);
					continue;
				}
				
				// Choose the males or females
				bool killMale = true;
				if (n.males <= 0 && n.females > 0) killMale = false;
				else if (n.males > 0 && n.females <= 0) killMale = true;
				else killMale = (rnd.Next (0, 2) == 1);
				
				// Kill a male of female and decrease the total deaths (left)
				if (killMale) n.males--;
				else n.females--;
				totalDeaths--;
			}
		}

		#endregion Decrease 

		#region Food

		protected void ProcessAnimalFouraging (AnimalSuccessionData data, Coordinate coord)
		{
			// Check if the animal has found food
			AnimalPopulationLandUseModel.Food food = data.animal.landUseModel.food;
			if (food.use)
			{
				// Check if there's food on this tile
				int foodCount = food.foodArea.Get (coord);
				if (foodCount > 0)
				{
					// Decrease the amount of food
					int foundFood = Mathf.Min (foodCount, (food.foodCarryCapacity - data.carriedFood));
					food.foodArea.Set (coord, foodCount - foundFood);
					data.carriedFood += foundFood;

					// Save the fouraging in the map
					Coordinate c = data.GetCurrentCoordinate ();
					int val = data.animal.fouragingMap.Get (c);
					data.animal.fouragingMap.Set (c, val + foundFood);

					// We don't need to walk any further if we can't carry any more food
					if (data.carriedFood >= food.foodCarryCapacity) {
						data.walkDistance = 0;
					}
				}
			}
		}

		#endregion Food

		#region Growth

		protected void ProcessAnimalGrowth (LargeAnimalType animal, AnimalStartPopulationModel.Nests.Nest nest, System.Random rnd)
		{
			AnimalPopulationGrowthModel growth = animal.growthModel;

			// Fixed number
			if (growth.fixedNumber.use)
			{
				int litterSize = RndUtil.RndRange (ref rnd, growth.fixedNumber.minLitterSize, growth.fixedNumber.maxLitterSize);

				switch (growth.fixedNumber.type)
				{
				/*case AnimalPopulationGrowthModel.FixedNumber.Types.PerPair : { } break;*/

				case AnimalPopulationGrowthModel.FixedNumber.Types.PerFemale :
				{
					// Formula : P[1] = M[0] + V[0] +(M[0]/M[0] * V[0] * W) 

					// Calculate offspring
					float m = (float)nest.males;
					float f = (float)nest.females;
					float w = (float)litterSize;
					int offspring = (int)(m + f);
					if (m != 0f && f != 0f && w != 0) {
						offspring = (int)(m + f + (m / m * f * w));
					}

					// TODO: Check if we have males or females, for now it's just 50/50
					int offspringM = Mathf.CeilToInt ((float)offspring * 0.5f);
					int offspringF = Mathf.CeilToInt ((float)offspring * 0.5f);
					
					// TODO: Should we check the amount of room per gender when determining the m/f offspring count?
					nest.males = Mathf.Clamp (nest.males + offspringM, 0, nest.malesCapacity);
					nest.females = Mathf.Clamp (nest.females + offspringF, 0, nest.femalesCapacity);

					// TODO: Check if surplus logic is correct and if we want to use it
					int total = nest.males + nest.females;
					int surplus = total - nest.totalCapacity;
					while (surplus > 0)
					{
						// Start by decreasing the gender with the highest population
						bool decreaseMales = (nest.males >= nest.females);
						for (int i = 0; i < 2; i++) 
						{
							if (decreaseMales) {
								// Decrease males
								if (nest.males > 0) {
									nest.males--;
									surplus--;
								}
							} else {
								// Decrease females
								if (nest.females > 0) {
									nest.females--;
									surplus--;
								}
							}
							// Make sure the next for loop decreases the other gender
							decreaseMales = !decreaseMales;
							if (surplus <= 0) break;
						}
					}
				}
				break;
				}
			}
		}

		#endregion Growth

		protected void ThreadFinished ()
		{
			// Deduct the amount of active threads
			activeThreads--;
			if (activeThreads == 0) {
				finishedProcessing = true;
			}
		}
	}
}