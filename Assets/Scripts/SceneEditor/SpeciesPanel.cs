using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor
{
	public class SpeciesPanel : Panel
	{
		#region Plants

		public class PlantState
		{
			public PlantState (PlantType plant)
			{
				this.plant = plant;
				this.isFoldedOpen = false;
				this.newMaxPerTile = this.plant.maxPerTile;
			}
			
			public bool isFoldedOpen;
			public PlantType plant;
			public int newMaxPerTile;
		}

		/// <summary>
		/// Deletes the plant type.
		/// </summary>
		public void DeletePlant (PlantState plantState)
		{
			List<PlantType> plantTypes = new List<PlantType>(scene.plantTypes);
			plantTypes.Remove (plantState.plant);
			scene.progression.DeleteData (plantState.plant.dataName);

			scene.plantTypes = plantTypes.ToArray();
			scene.UpdateReferences();

			plants.Remove (plantState);
		}

		#endregion Plants

		#region Animals

		public class AnimalState
		{
			public AnimalState (AnimalType animal)
			{
				this.animal = animal;
				this.isFoldedOpen = false;
			}

			public bool isFoldedOpen;
			public bool nestsListFoldedOpen;
			public AnimalType animal;
		}

		/// <summary>
		/// Deletes the animal type.
		/// </summary>
		public void DeleteAnimal (AnimalState animalState)
		{
			List<AnimalType> animalTypes = new List<AnimalType>(scene.animalTypes);
			animalTypes.Remove (animalState.animal);
			scene.progression.DeleteData (animalState.animal.dataName);

			scene.animalTypes = animalTypes.ToArray();
			scene.UpdateReferences ();

			animals.Remove (animalState);
		}

		private void DeleteNest (AnimalType animal, AnimalType.Nest nest)
		{
			List<AnimalType.Nest> nests = new List<AnimalType.Nest>(animal.nests);
			nests.Remove (nest);
			animal.nests = nests.ToArray ();
		}

		#endregion Animals

		private enum Tabs 
		{
			Plants,
			Animals
		}

		private Tabs currentTab;
		private ExtraPanel extraPanel;
		private Vector2 scrollPos;
		private Vector2 scrollPosExtra;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;

		private string newPlantName;
		private string newAnimalName;

		private Scene scene;
		private EditorCtrl ctrl;

		private List<PlantState> plants;
		private List<AnimalState> animals;

		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			if (scene == null)
				return;

			plants = new List<PlantState>();
			for (int i = 0; i < scene.plantTypes.Length; i++) {
				plants.Add(new PlantState (scene.plantTypes[i]));
			}

			animals = new List<AnimalState>();
			for (int i = 0; i < scene.animalTypes.Length; i++) {
				animals.Add(new AnimalState (scene.animalTypes[i]));
			}

			newPlantName = "New plant";
			newAnimalName = "New animal";

			extraPanel = null;

			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
		}
		
		public bool Render (int mx, int my)
		{
			if (scene == null)
				return false;

			GUILayout.BeginHorizontal ();
			{
				//float width = 60f;
				GUILayout.Label ("Type:", GUILayout.Width (40));
				if (GUILayout.Button ("Plants", (currentTab == Tabs.Plants) ? tabSelected : tabNormal))//, GUILayout.Width (width))) 
				{
					currentTab = Tabs.Plants;
					scrollPos = scrollPosExtra = Vector2.zero;
					DisposeExtraPanel ();
				}
				if (GUILayout.Button ("Animals", (currentTab == Tabs.Animals) ? tabSelected : tabNormal))//, GUILayout.Width (width))) 
				{
					currentTab = Tabs.Animals;
					scrollPos = scrollPosExtra= Vector2.zero;
					DisposeExtraPanel ();
				}
				//GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal ();

			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			{
				GUILayout.BeginVertical ();
				{
					switch (currentTab)
					{
						case Tabs.Plants : RenderPlants(); break;
						case Tabs.Animals : RenderAnimals(); break;
					}

					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndVertical ();
			}
			GUILayout.EndScrollView ();
			return (extraPanel != null);
		}

		private void RenderAnimals ()
		{
			int index = 0;
			foreach (AnimalState ast in animals)
			{
				GUILayout.BeginVertical (ctrl.skin.box);
				{
					// Header
					GUILayout.BeginHorizontal ();
					{
						if (GUILayout.Button (ast.isFoldedOpen ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
						{
							ast.isFoldedOpen = !ast.isFoldedOpen;
						}
						
						GUILayout.Label (index.ToString(), GUILayout.Width (40));
						ast.animal.name = GUILayout.TextField (ast.animal.name);
						
						if (GUILayout.Button ("-", GUILayout.Width (20)))
						{
							AnimalState tmp = ast;
							ctrl.StartDialog (string.Format("Delete animal '{0}'?", tmp.animal.name), newVal => { 
								DeleteAnimal (ast);
							}, null);
						}
					}
					GUILayout.EndHorizontal(); // ~Header

					// Animal body
					if (ast.isFoldedOpen) 
					{
						GUILayout.Space (8f);

						GUILayoutOption layout = GUILayout.Width (120f);

						AnimalType tmp = ast.animal;
						RenderParameterSelectionButton ("Food parameter:", ref tmp.foodParamName, layout, delegate (int i, string s) { tmp.foodParamName = s; });
						RenderParameterSelectionButton ("Food overrule parameter:", ref tmp.foodOverruleParamName, layout, delegate (int i, string s) { tmp.foodOverruleParamName = s; });
						RenderParameterSelectionButton ("Danger parameter:", ref tmp.dataName, layout, delegate (int i, string s) { tmp.dataName = s; });

						GUILayout.Space (8f);

						GUILayout.BeginHorizontal ();
						{
							GUILayout.Label ("Walk distance:", layout);
							EcoGUI.skipHorizontal = true;
							EcoGUI.IntField ("M", ref ast.animal.moveDistanceMale, 10, 0);
							EcoGUI.IntField ("F", ref ast.animal.moveDistanceFemale, 10, 0);
							EcoGUI.skipHorizontal = false;
						}
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal ();
						{
							GUILayout.Label ("Wander factor:", layout);
							EcoGUI.skipHorizontal = true;
							EcoGUI.FloatField ("M", ref ast.animal.wanderMale, 2, 10, 0);
							EcoGUI.FloatField ("F", ref ast.animal.wanderFemale, 2, 10, 0);
							EcoGUI.skipHorizontal = false;
						}
						GUILayout.EndHorizontal();

						GUILayout.Space (8f);

						GUILayout.BeginHorizontal ();
						{
							if (GUILayout.Button ("Open Nests Editor"))
							{
								if (extraPanel != null && extraPanel is AnimalNestsExtraPanel) {
									((AnimalNestsExtraPanel)extraPanel).Dispose ();
									extraPanel = null;
								}

								extraPanel = new AnimalNestsExtraPanel (ctrl, ast.animal);
							}

							// TODO: Open extra panel for Animal rules
							/*if (GUILayout.Button ("Open Rules"))
							{
							}*/
						}
						GUILayout.EndHorizontal();

						// Nests
						if (ast.animal.nests.Length > 0)
						{
							// Nests list
							GUILayout.BeginHorizontal ();
							{
								if (GUILayout.Button (ast.nestsListFoldedOpen ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) {
									ast.nestsListFoldedOpen = !ast.nestsListFoldedOpen;
								}
								GUILayout.Label ("Nests", GUILayout.Width (40));
							}
							GUILayout.EndHorizontal ();
							
							if (ast.nestsListFoldedOpen)
							{
								int idx = 0;
								foreach (AnimalType.Nest n in ast.animal.nests)
								{
									GUILayout.BeginHorizontal (ctrl.skin.box);
									{
										GUILayout.Label (string.Format(" Nest #{0} ({1},{2})", idx++, n.x, n.y));

										AnimalNestsExtraPanel animExtraPanel = (AnimalNestsExtraPanel)extraPanel;
										if (animExtraPanel != null)
										{
											if (GUILayout.Button ("Edit", GUILayout.Width(50))) 
											{
												animExtraPanel.SetAnimal (ast.animal);
												animExtraPanel.EditNest (n);
											}
											GUILayout.Space (2);
											if (GUILayout.Button ("Focus", GUILayout.Width(50))) 
											{
												animExtraPanel.SetAnimal (ast.animal);
												animExtraPanel.FocusOnNest (n);
											}
										}

										GUILayout.Space (2);
										if (GUILayout.Button ("-", GUILayout.Width (20))) 
										{
											if (animExtraPanel != null) {
												animExtraPanel.DeleteNest (n);
											}

											DeleteNest (ast.animal, n);
										}
									}
									GUILayout.EndHorizontal ();
								}
								//GUILayout.FlexibleSpace ();
							}
						}
					}
				}
				GUILayout.EndVertical(); // ~Animal body
				index++;
			} // ~AnimalState foreach

			if (RenderAddButton ("New animal:", ref newAnimalName))
			{
				// Add new animal
				AnimalType t = new AnimalType (scene, newAnimalName);
				AnimalState state = new AnimalState (t);
				animals.Add (state);
			}
		}

		private void RenderPlants ()
		{
			int index = 0;
			foreach (PlantState ps in plants) 
			{
				GUILayout.BeginVertical (ctrl.skin.box);
				{
					// Header
					GUILayout.BeginHorizontal ();
					{
						if (GUILayout.Button (ps.isFoldedOpen ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
						{
							ps.isFoldedOpen = !ps.isFoldedOpen;
						}
						
						GUILayout.Label (index.ToString(), GUILayout.Width (40));
						ps.plant.name = GUILayout.TextField (ps.plant.name);
						
						if (GUILayout.Button ("-", GUILayout.Width (20)))
						{
							PlantState tmpPS = ps;
							ctrl.StartDialog (string.Format("Delete plant '{0}'?", tmpPS.plant.name), newVal => { 
								DeletePlant (tmpPS);
							}, null);
						}
					}
					GUILayout.EndHorizontal(); // ~Header
					GUILayout.Space (5f);

					// Plant body
					if (ps.isFoldedOpen) 
					{
						GUILayout.BeginVertical (ctrl.skin.box);
						
						// Parameter name
						GUILayout.BeginHorizontal ();
						GUILayout.Label (string.Format(" Parameter name: '{0}'", ps.plant.dataName), GUILayout.Width (260));
						GUILayout.EndHorizontal ();

						GUILayoutOption labelLayout = GUILayout.Width (140);
						GUILayoutOption fieldLayout = GUILayout.Width (40);

						EcoGUI.IntField (" # Spawn seeds attempts", ref ps.plant.spawnCount, labelLayout, fieldLayout); 
						EcoGUI.IntField (" Spawn seeds dispersion", ref ps.plant.spawnRadius, labelLayout, fieldLayout);

						GUILayout.BeginHorizontal ();
						{
							EcoGUI.skipHorizontal = true;
							EcoGUI.IntField (" Maximum per tile", ref ps.newMaxPerTile, labelLayout, fieldLayout);
							EcoGUI.skipHorizontal = false;

							if (ps.newMaxPerTile != ps.plant.maxPerTile)
							{
								GUILayout.Label ("was " + ps.plant.maxPerTile.ToString(), GUILayout.Width (40));
								if (GUILayout.Button ("Update", GUILayout.Width(60))) 
								{
									// Update the current plants data parameter map because we have changed the max per tile
									Data plantData = scene.progression.GetData (ps.plant.dataName);
									int prevMaxPerTile = ps.plant.maxPerTile;
									ps.plant.maxPerTile = ps.newMaxPerTile;
									plantData.ProcessNotZero (delegate(int x, int y, int val, object data) {
										// Get the current value's percentage to calculate the new value according the new max (per tile) value
										float perc = (float)val / prevMaxPerTile;
										int newVal = Mathf.RoundToInt(perc * (float)ps.plant.maxPerTile);
										plantData.Set (x, y, newVal);
									}, null);
								}
								if (GUILayout.Button ("?", GUILayout.Width (20))) {
									string message = 
										@"You need to explicitly say to update the 'Maximum per tile' value, because changing this value will affect the currently placed amounts of the plant on the terrain.

The current values will be converted by their percentage of the current maximum per tile value, like this:

[new value] = ([current value]/[previous max]) * [new max].";
									ctrl.StartOkDialog (message, null, 300, 150);
								}
							}
						}
						GUILayout.EndHorizontal ();
						
						// Rules
						GUILayout.Space (2);
						GUILayout.BeginHorizontal(); // Rules
						{
							if (GUILayout.Button ("Open Rules", GUILayout.Width (120)))
							{
								DisposeExtraPanel ();
								extraPanel = new PlantRulesExtraPanel (ctrl, ps.plant);
							}
						}
						GUILayout.EndHorizontal(); // ~Rules
						
						GUILayout.EndVertical ();
					}
				}
				GUILayout.EndVertical(); // ~Plant body
				index++;
			} // ~PlantState foreach

			if (RenderAddButton ("New plant:", ref newPlantName))
			{
				// Add new plant
				PlantType t = new PlantType (scene, newPlantName);
				PlantState state = new PlantState (t);
				plants.Add (state);
			}
		}

		private void DisposeExtraPanel ()
		{
			if (extraPanel != null && extraPanel is AnimalNestsExtraPanel) {
				((AnimalNestsExtraPanel)extraPanel).Dispose ();
			}
			
			extraPanel = null;
		}

		#region Wrapper Methods

		private void RenderParameterSelectionButton (string label, ref string paramName, GUILayoutOption labelLayout, EditorCtrl.itemSelectedResult itemSelected)
		{
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label (label, labelLayout);
				if (GUILayout.Button (paramName))
				{
					// Get all param names
					List<string> dataNames = scene.progression.GetAllDataNames (false);
					int idx = Mathf.Clamp(dataNames.IndexOf (paramName), 0, dataNames.Count);
					ctrl.StartSelection (dataNames.ToArray(), idx, itemSelected);
				}
			}
			GUILayout.EndHorizontal ();
		}

		private bool RenderAddButton (string label, ref string newName)
		{
			bool createNew = false;
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label (label, GUILayout.Width (60));
				newName = GUILayout.TextField (newName, GUILayout.Width (200));
				if (GUILayout.Button ("Create")) {
					createNew = true;
				}
				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal (); //~ Add button
			return createNew;
		}

		#endregion Wrapper Methods

		public void RenderExtra (int mx, int my)
		{
			if (extraPanel != null) {
				bool keepRendering = extraPanel.Render (mx, my);
				if (!keepRendering) {
					extraPanel = null;
				}
			}
		}
		
		public void RenderSide (int mx, int my)
		{
			if (extraPanel != null) {
				extraPanel.RenderSide (mx, my);
			}
		}
		
		public bool NeedSidePanel ()
		{
			// Make this better when we also have animals
			if (extraPanel != null) 
			{
				if (extraPanel is PlantRulesExtraPanel)
				{
					return true;
				}
				else if (extraPanel is AnimalNestsExtraPanel)
				{
					return false;
				}
			}
			return false;
		}
		
		public bool IsAvailable ()
		{
			return (scene != null);
		}
		
		public void Activate ()
		{
		}
		
		public void Deactivate ()
		{
			DisposeExtraPanel ();
		}
		
		public void Update ()
		{
		}
	}
}