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
				this.maxPerTileStr = this.plant.maxPerTile.ToString();
				this.isFoldedOpen = false;
			}
			
			public bool isFoldedOpen;
			public PlantType plant;
			public string maxPerTileStr;
		}

		/// <summary>
		/// Deletes the plant.
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
			public AnimalType animal;
		}

		#endregion Animals

		ExtraPanel extraPanel;
		Vector2 scrollPos;
		Vector2 scrollPosExtra;

		string newPlantName;
		string newAnimalName;

		Scene scene;
		EditorCtrl ctrl;

		List<PlantState> plants;
		List<AnimalState> animals;

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
		}
		
		public bool Render (int mx, int my)
		{
			if (scene == null)
				return false;

			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			{
				GUILayout.BeginVertical ();
				{
					// Plants
					RenderPlants ();
					RenderAnimals ();

					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndVertical ();
			}
			GUILayout.EndScrollView ();
			return (extraPanel != null);
		}

		private void RenderAnimals ()
		{

		}

		private void RenderPlants ()
		{
			int plantIndex = 0;
			foreach (PlantState ps in plants) 
			{
				GUILayout.BeginVertical (ctrl.skin.box);
				
				// Header
				GUILayout.BeginHorizontal ();
				{
					if (GUILayout.Button (ps.isFoldedOpen ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
					{
						ps.isFoldedOpen = !ps.isFoldedOpen;
					}
					
					GUILayout.Label (plantIndex.ToString(), GUILayout.Width (40));
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
				
				// Plant body
				if (ps.isFoldedOpen) 
				{
					GUILayout.BeginVertical (ctrl.skin.box);
					
					// Parameter name
					GUILayout.BeginHorizontal ();
					GUILayout.Label (string.Format(" Parameter name: '{0}'", ps.plant.dataName), GUILayout.Width (260));
					GUILayout.EndHorizontal ();
					
					// Spread attempts
					GUILayout.BeginHorizontal ();
					GUILayout.Label (" # Spawn seeds attempts", GUILayout.Width (140));
					string spawnAttempts = GUILayout.TextField (ps.plant.spawnCount.ToString(), GUILayout.Width (40));
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
					
					// Multiplier
					GUILayout.BeginHorizontal ();
					GUILayout.Label (" Spawn seeds multiplier", GUILayout.Width (140));
					string spawnMultiplier = GUILayout.TextField (ps.plant.spawnMultiplier.ToString(), GUILayout.Width (40));
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
					
					// Dispersion
					GUILayout.BeginHorizontal ();
					GUILayout.Label (" Spawn seeds dispersion", GUILayout.Width (140));
					string spawnRadius = GUILayout.TextField (ps.plant.spawnRadius.ToString(), GUILayout.Width (40));
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
					
					// Max per tile
					GUILayout.BeginHorizontal ();
					GUILayout.Label (" Maximum per tile", GUILayout.Width (140));
					ps.maxPerTileStr = GUILayout.TextField (ps.maxPerTileStr, GUILayout.Width (40));
					
					// Format the string for only digits
					string str = "";
					foreach (char c in ps.maxPerTileStr)
						if (char.IsDigit (c)) str += c.ToString();
					ps.maxPerTileStr = str;
					if (ps.maxPerTileStr.Length == 0) ps.maxPerTileStr = "0";
					
					// Check for a different value
					int maxPerTile = int.Parse (ps.maxPerTileStr);
					if (maxPerTile != ps.plant.maxPerTile) 
					{
						GUILayout.Label ("was " + ps.plant.maxPerTile.ToString(), GUILayout.Width (40));
						if (GUILayout.Button ("Update", GUILayout.Width(60))) {
							// Update the current plants data parameter map because we have changed the max per tile
							Data plantData = scene.progression.GetData (ps.plant.dataName);
							int prevMaxPerTile = ps.plant.maxPerTile;
							ps.plant.maxPerTile = maxPerTile;
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
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
					
					int outNr;
					if (int.TryParse (spawnAttempts, out outNr))
						ps.plant.spawnCount = outNr;
					if (int.TryParse (spawnMultiplier, out outNr))
						ps.plant.spawnMultiplier = outNr;
					if (int.TryParse (spawnRadius, out outNr))
						ps.plant.spawnRadius = outNr;
					
					// Rules
					GUILayout.Space (2);
					GUILayout.BeginHorizontal(); // Rules
					{
						if (GUILayout.Button ("Rules"))
						{
							extraPanel = new PlantRulesExtraPanel (ctrl, ps.plant);
						}
					}
					GUILayout.EndHorizontal(); // ~Rules
					
					GUILayout.EndVertical ();
				}
				GUILayout.EndVertical(); // ~Plant body
				plantIndex++;
			} // ~PlantState foreach
			
			// Add button
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label ("New plant:", GUILayout.Width (60));
				newPlantName = GUILayout.TextField (newPlantName, GUILayout.Width (200));
				if (GUILayout.Button ("Create")) {
					// Check plant name
					bool uniqueName = true;
					foreach (PlantState ps in plants) {
						if (ps.plant.name == newPlantName) {
							uniqueName = false;
							break;
						}
					}
					
					if (uniqueName)
					{
						// Add new plant
						PlantType t = new PlantType (scene, newPlantName);
						PlantState state = new PlantState (t);
						plants.Add (state);
					}
					else
					{
						ctrl.StartOkDialog ("Name is already taken, please choose another and try again.", null);
					}
				}
				
				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal (); //~ Add button
		}

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
			// TODO: Make this better when we also have animals
			if (extraPanel != null) {
				return true;
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
		}
		
		public void Update ()
		{
		}
	}
}