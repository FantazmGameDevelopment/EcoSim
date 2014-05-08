using System;
using System.Threading;
using System.Collections.Generic;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.PlantRules;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class PlantsAction : BasicAction
	{
		private class Spawn {
			public Spawn (PlantType plantType, int x, int y) {
				this.plantType = plantType;
				this.x = x;
				this.y = y;
			}

			public PlantType plantType;
			public int x;
			public int y;
		}

		public const string XML_ELEMENT = "plants";
		private const int SLICE_SIZE = 128;
		private volatile int activeThreads;

		//public string successionAreaName = null; // if not null, progression data with this name is used to determine if tiles needs calculations
		//private Data successionArea = null;

		private List<Spawn> spawnList;
		
		public PlantsAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public PlantsAction (Scene scene) : base(scene, scene.actions.lastId)
		{
		}
		
		public override string GetDescription ()
		{
			return "Handle Plants";
		}

		/**
		 * ProcessSlice is started in new thread, arguments should be of type int and is startY position
		 * of the slice.
		 */
		void ProcessSlice (object arguments)
		{
			List<Spawn> tmpSpawnList = new List<Spawn>();
			int startY = (int)arguments;
			try 
			{
				System.Random rnd = new System.Random (); // when multithreading, you need a random generator per thread
				Progression progress = scene.progression;

				foreach (PlantType plantType in scene.plantTypes)
				{
					Data plantData = progress.GetData (plantType.dataName);
					if (plantData == null) continue; // TODO: Check if this is possible?

					// Loop through the slice TODO: We should limit this to the "beheersgebied"!
					int p = scene.width * startY;
					for (int y = startY; y < startY + SLICE_SIZE; y++) 
					{
						for (int x = 0; x < scene.width; x++) 
						{
							int populationValue = plantData.Get (x, y);
							if (populationValue > 0) 
							{
								VegetationType vegType = progress.vegetation.GetVegetationType (x, y);
								int cummDelta = 0;

								foreach (PlantRule plantRule in plantType.rules) // Rules
								{
									// Check if the rule applies
									if (plantRule.chance >= rnd.NextDouble ()) 
									{
										// Check if the rule contains the vegetation type of the current tile
										foreach (PlantRule.VegetationCondition vegCondition in plantRule.vegetationConditions)
										{
											// First check if the succession and vegetation indices match
											bool correctVegetation = vegCondition.IsCompatible (vegType.successionType.index, vegType.index);

											if (correctVegetation)
											{
												// Check if the parameter ranges match
												bool paramsMatch = true;
												foreach (ParameterRange paramRange in plantRule.parameterConditions)
												{
													int val = paramRange.data.Get (x, y);
													if (val < paramRange.lowRange || val > paramRange.highRange) {
														paramsMatch = false;
														break;
													}
												}

												if (paramsMatch) {
													cummDelta += plantRule.delta;
												}
											}
										} // ~VegetationCondition foreach
									}
								} // ~Rules foreach

								// Update the plants population if it's changed
								int newPopulationValue = UnityEngine.Mathf.Clamp (populationValue + cummDelta, 0, plantType.maxPerTile);
								if (newPopulationValue != populationValue) {
									plantData.Set (x, y, newPopulationValue);
								}

								// Check if we are going to spawn seedlings
								if (newPopulationValue > 0)
								{
									int spawnCount = plantType.spawnCount + (newPopulationValue * plantType.spawnMultiplier);
									for (int i = 0; i < spawnCount; i++)
									{
										// Spawn on a random tile
										float angle = RndUtil.RndRange (ref rnd, 0f, 360f);
										float range = RndUtil.RndRange (ref rnd, 1f, plantType.spawnRadius);
										int targetX = Mathf.RoundToInt (Mathf.Sin (angle) * range) + x;
										int targetY = Mathf.RoundToInt (Mathf.Cos (angle) * range) + y;
										// Check if it's inside the terrain
										if ((targetX >= 0) && (targetY >= 0) && 
										    (targetX < this.scene.width) && (targetY < this.scene.height)) 
										{
											// New spawn
											tmpSpawnList.Add (new Spawn (plantType, targetX, targetY));
										}
									}
								} // ~Spawn seedlings check
							}
						}
					}
				} // ~PlantType foreach
			}catch (Exception e) {
				UnityEngine.Debug.LogException (e);
			}

			// Add the temp spawn list to the total list
			if (tmpSpawnList.Count > 0) {
				lock (spawnList) {
					spawnList.AddRange (tmpSpawnList);
				}
			}

			// Deduct the amount of active threads
			activeThreads--;
			if (activeThreads == 0) {
				finishedProcessing = true;
			}
		}

		void HandleSpawns()
		{
			System.Random rnd = new System.Random ();

			foreach (Spawn spawn in spawnList)
			{
				int x = spawn.x;
				int y = spawn.y;
				PlantType plantType = spawn.plantType;
				Data plantData = scene.progression.GetData (plantType.dataName);

				// Check if the plant type already exists on the tile TODO: We should limit this to the "beheersgebied"?
				if (plantData.Get (x, y) == 0)
				{
					VegetationType vegType = scene.progression.vegetation.GetVegetationType (x, y);
					bool doSpawn = false;
					foreach (PlantRule r in plantType.rules)
					{
						// Check if we can spawn and if the chance is correct
						if (r.canSpawn && r.chance >= rnd.NextDouble ())
						{
							bool rightVeg = (r.vegetationConditions.Length == 0);
							foreach (PlantRule.VegetationCondition vc in r.vegetationConditions)
							{
								if (vc.IsCompatible (vegType.successionType.index, vegType.index))
								{
									rightVeg = true;
									break;
								}
							}
								
							if (rightVeg)
							{
								doSpawn = true;
								foreach (ParameterRange pr in r.parameterConditions)
								{
									int val = pr.data.Get (x, y);
									if (val < pr.lowRange || val > pr.highRange) {
										doSpawn = false;
										break;
									}
								}
							}

							if (!doSpawn) break;
						}
					}
					if (doSpawn) {
						plantData.Set (x, y, 1); // TODO: Which number must this become?
					}
				}
			}
			spawnList.Clear ();
		}
		
		public override void DoSuccession ()
		{
			base.DoSuccession ();

			// Get the succession area
			/*successionArea = null;
			if (successionAreaName != null) {
				successionArea = scene.progression.GetData (successionAreaName);
			}*/

			spawnList = new List<Spawn>();

			// Handle the plants logic
			activeThreads = scene.height / SLICE_SIZE;
			for (int y = 0; y < scene.height; y += SLICE_SIZE) 
			{
				// Temp disable unreachable code warning
				#pragma warning disable 162
				if (GameSettings.PLANTS_LOGIC_MULTITHREADED) {
					ThreadPool.QueueUserWorkItem (ProcessSlice, y);
				}
				else {
					ProcessSlice (y);
				}
				#pragma warning restore 162
			}

			// Handle spawns
			HandleSpawns ();
		}
		
		public static PlantsAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			PlantsAction action = new PlantsAction (scene, id);

			if (!reader.IsEmptyElement) 
			{
			}

			return action;
			/*int id = int.Parse (reader.GetAttribute ("id"));
			SuccessionAction action = new SuccessionAction (scene, id);
			string sns = reader.GetAttribute ("skipnormalsuccession");
			action.skipNormalSuccession = (sns != null) && (sns == "true");
			action.successionAreaName = reader.GetAttribute ("successionarea");
			
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteraction.XML_ELEMENT)) {
						action.uiList.Add (UserInteraction.Load (action, reader));
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			return action;*/
		}
		
		public override void Save (XmlTextWriter writer)
		{
			/*writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("id", id.ToString ());
			writer.WriteAttributeString ("skipnormalsuccession", skipNormalSuccession ? "true" : "false");
			if (successionAreaName != null) {
				writer.WriteAttributeString ("successionarea", successionAreaName);
			}
			
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();*/
		}		
	}
}
