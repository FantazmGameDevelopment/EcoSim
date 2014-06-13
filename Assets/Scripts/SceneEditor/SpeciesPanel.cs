using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneEditor.Helpers;
using Ecosim.SceneEditor.Helpers.AnimalPopulationModel;

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
			public List<IAnimalPopulationModelHelper> modelHelpers;
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

		#endregion Animals

		private enum Tabs 
		{
			Plants,
			Animals
		}

		private Tabs currentTab;
		private Vector2 scrollPos;
		private Vector2 scrollPosExtra;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;

		public ExtraPanel extraPanel;

		public Scene scene;
		public EditorCtrl ctrl;

		public List<PlantState> plants;
		public List<AnimalState> animals;

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

			extraPanel = null;

			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;

			currentTab = Tabs.Plants;
			SpeciesPanelPlantsHelper.Setup (this);
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
					if (currentTab != Tabs.Plants)
					{
						currentTab = Tabs.Plants;
						scrollPos = scrollPosExtra = Vector2.zero;
						SpeciesPanelPlantsHelper.Setup (this);
						DisposeExtraPanel ();
					}
				}
				if (GUILayout.Button ("Animals", (currentTab == Tabs.Animals) ? tabSelected : tabNormal))//, GUILayout.Width (width))) 
				{
					if (currentTab != Tabs.Animals)
					{
						currentTab = Tabs.Animals;
						scrollPos = scrollPosExtra = Vector2.zero;
						SpeciesPanelAnimalsHelper.Setup (this);
						DisposeExtraPanel ();
					}
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
					case Tabs.Plants : SpeciesPanelPlantsHelper.Render (mx, my); break;
					case Tabs.Animals : SpeciesPanelAnimalsHelper.Render (mx, my); break;
					}

					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndVertical ();
			}
			GUILayout.EndScrollView ();
			return (extraPanel != null);
		}

		public void DisposeExtraPanel ()
		{
			if (extraPanel != null) {
				extraPanel.Dispose ();
			}
			extraPanel = null;
		}

		#region Wrapper Methods

		public void RenderParameterSelectionButton (string label, ref string paramName, GUILayoutOption labelLayout, EditorCtrl.itemSelectedResult itemSelected)
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

		public bool RenderAddButton (string label, ref string newName)
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