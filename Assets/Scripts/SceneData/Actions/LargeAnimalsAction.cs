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

		public bool skipNormalGrowthLogic = false;
		public bool skipNormalDecreaseLogic = false;
		public bool skipNormalLandUseLogic = false;

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
			writer.WriteAttributeString ("skipgrowth", skipNormalGrowthLogic.ToString().ToLower());
			writer.WriteAttributeString ("skipdecrease", skipNormalDecreaseLogic.ToString().ToLower());
			writer.WriteAttributeString ("skiplanduse", skipNormalLandUseLogic.ToString().ToLower());
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
						animals.Add (at as LargeAnimalType);
					}
				}
				if (animals.Count > 0)
				{
					activeThreads = animals.Count * 1;//animals[0].models.Count; // TODO: Make this number correct

					for (int i = 0; i < animals.Count; i++)
					{
						// Temp disable unreachable code warning
						#pragma warning disable 162
						if (GameSettings.ANIMALS_LOGIC_MULTITHREADED) 
						{
							ThreadPool.QueueUserWorkItem (ProcessAnimal, animals[i]);
						}
						else {
							//ProcessSlice (y);
						}
						#pragma warning restore 162
					}
				}
				else {
					activeThreads = 0;
					this.finishedProcessing = true;
				}
			}
		}

		#region Land Use

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
		}

		protected class AnimalSuccessionData
		{
			public LargeAnimalType animal { get; private set; }

			public System.Random rnd { get; private set; }
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
				this.walkDistance = walkMin + (int)((walkMax - walkMin) * rnd.NextDouble());

				this.prevDir = new Direction (Vector2.zero, 0f);
				this.currPos = startPos;
				this.hasDied = false;
			}
		}

		protected void ProcessAnimal (object arguments)
		{
			LargeAnimalType animal = arguments as LargeAnimalType;
			ProcessAnimalMovement (animal);
			ProcessAnimalDecrease (animal);
		}

		protected void ProcessAnimalMovement (LargeAnimalType animal)
		{
			if (animal.landUseModel != null)
			{
				try 
				{
					// Temp vars
					List<Direction> dirs = new List<Direction> ();

					System.Random rnd = new System.Random (); // When multithreading, you need a random generator per thread

					// Loop through all nests
					List<AnimalStartPopulationModel.Nests.Nest> nests = new List<AnimalStartPopulationModel.Nests.Nest>(animal.startPopModel.nests.nests);

					while (nests.Count > 0)
					{
						// Choose random nest
						AnimalStartPopulationModel.Nests.Nest nest = nests[Mathf.FloorToInt((float)rnd.NextDouble() * (float)(nests.Count - 1))];
						nests.Remove (nest);

						Debug.Log (nest.x + "," + nest.y);

						// Process movement
						for (int n = 0; n < nest.males; n++) 
						{
							AnimalSuccessionData data = new AnimalSuccessionData (animal, rnd, new Vector2 (nest.x, nest.y));

							// Process walking
							while (data.walkDistance > 0) {
								MoveAnimal (data);
							}

							// TODO: Check if the animal has died or is still alive
						}

						// TODO: Check nest growth and 
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
				    (newPos.x < this.scene.width) && (newPos.y < this.scene.height)) 
				{
					// Direction is valid, check the chance
					Vector2 dir = Direction.directions[d];
					Data movePrefData = data.animal.landUseModel.movement.movePrefData;
					float chance = (float)movePrefData.Get ((int)newPos.x, (int)newPos.y) / movePrefData.GetMax();
					possibleDirs.Add (new Direction (dir, chance));
				}
			}
			
			// Sort the possible directions by chance
			possibleDirs.Sort (delegate(Direction a, Direction b) 
			                   {
				if (a.chance > b.chance) return -1;
				else if (a.chance < b.chance) return 1;
				else return 0; // TODO: Make this random
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

		protected void ProcessAnimalOnTile (AnimalSuccessionData data)
		{
			Coordinate coord = new Coordinate ((int)data.currPos.x, (int)data.currPos.y);

			// Decrease model
			ProcessAnimalDecrease (data, coord);
			if (data.hasDied) return;

			// Check if the animal has found food
			ProcessAnimalFoodDiscovery (data, coord);
		}

		protected void ProcessAnimalDecrease (AnimalSuccessionData data, Coordinate coord)
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
						if (de.fixedChance.min != 0f || de.fixedChance.max != 1f){
							deathChance = de.fixedChance.min + ((de.fixedChance.max - de.fixedChance.min) * dataChance);
						}

						// Check death
						if (data.rnd.NextDouble () <= deathChance)
						{
							data.hasDied = true;
							break;
						}
					}
					break;
					}
				}
			}
		}

		protected void ProcessAnimalDecrease (AnimalSuccessionData data)
		{
			// TODO: Process the non area dependant death rates
		}

		protected void ProcessAnimalFoodDiscovery (AnimalSuccessionData data, Coordinate coord)
		{
			// TODO: Check if the animal has found food
			AnimalPopulationLandUseModel.Food food = data.animal.landUseModel.food;
			if (food.use)
			{
				// TODO: Check if there's food in this area and make sure it's saved between successions (copy it)
				int foodCount = food.data.Get (coord);
				if (foodCount > 0)
				{
					// Decrease the amount of food
					int foundFood = Mathf.Min (foodCount, (food.foodCarryCapacity - data.carriedFood));
					food.data.Set (coord, foodCount - foundFood);

					// TODO: Decrease the amount of walk distance
					data.carriedFood += foundFood;
				}
			}
		}

		// TODO: Fix and add code
		protected void ProcessAnimalGrowth (LargeAnimalType animal, AnimalStartPopulationModel.Nests.Nest nest, System.Random rnd)
		{
			AnimalPopulationGrowthModel growth = animal.growthModel;

			// Fixed number
			if (growth.fixedNumber.use)
			{
				int litterSize = growth.fixedNumber.minLitterSize + ((growth.fixedNumber.maxLitterSize - growth.fixedNumber.minLitterSize) * rnd.NextDouble());

				switch (growth.fixedNumber.type)
				{
				case AnimalPopulationGrowthModel.FixedNumber.Types.PerPair :
				{
					// TODO:
				}
				break;

				case AnimalPopulationGrowthModel.FixedNumber.Types.PerFemale :
				{
					// P[1] = M[0] + V[0] +(M[0]/M[0] * V[0] * W) 

					// TODO:
					int m = nest.males;
					int f = nest.females;
					int w = litterSize;
					int offspring = m + f + (m / m * f * w);

					// TODO: Check if we have males or females
					int offspringM = offspring * 0.5f;
					int offspringF = offspring * 0.5f;

					nest.males = Mathf.Clamp (0, nest.malesCapacity, nest.males + offspringM);
					nest.females = Mathf.Clamp (0, nest.femalesCapacity, nest.females + offspringF);

					int total = nest.males + nest.females;
					int surplus = total - nest.totalCapacity;
					if (surplus > 0)
					{
						// TODO: Handle surplus
					}
				}
				break;
				}
			}
		}

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