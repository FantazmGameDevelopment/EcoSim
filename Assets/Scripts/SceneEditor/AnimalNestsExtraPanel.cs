using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.PlantRules;
using Ecosim.SceneData.Rules;
using UnityEngine;

namespace Ecosim.SceneEditor
{
	public class AnimalNestsExtraPanel : ExtraPanel
	{
		public AnimalType animal { get; private set; }
		private EditorCtrl ctrl;

		private Data nestsData;
		private Vector2 editNestScrollPos;
		private AnimalType.Nest editNest;

		private EditData edit = null;
		private GridTextureSettings areaGrid = null;
		private Material areaMat = null;

		private bool updateData = false;

		private class NestStates
		{
			public const int None = 0;
			public const int Active = 1;
			public const int Editable = 2;
			public const int Hover = 3;
		}

		public AnimalNestsExtraPanel (EditorCtrl ctrl, AnimalType animal) 
		{
			this.ctrl = ctrl;

			areaMat = new Material (EcoTerrainElements.GetMaterial ("MapAnimalNestsGrid"));
			areaGrid = new GridTextureSettings (true, 0, 2, areaMat, true, areaMat);

			SetAnimal (animal);
		}

		public bool Render (int mx, int my)
		{
			if (updateData)
			{
				updateData = false;
				edit.CopyData (nestsData);
			}

			bool keepOpen = true;
			GUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button (ctrl.foldedOpenSmall)) 
				{
					keepOpen = false;
				}
				
				GUILayout.Label ("Animal Nests", GUILayout.Width (100));
				GUILayout.Label (animal.name);
				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal ();

			// TODO: Buttons?
			/*GUILayout.BeginHorizontal (); // Buttons
			{

			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~Buttons*/

			// Nest to edit
			if (editNest != null)
			{
				editNestScrollPos = GUILayout.BeginScrollView (editNestScrollPos, false, false);
				{
					// Header
					int editIdx = 0;
					foreach (AnimalType.Nest n in animal.nests) {
						editIdx++;
						if (n == editNest) break;
					}
					GUILayout.Label (string.Format("Nest #{0} ({1},{2})", editIdx, editNest.x, editNest.y));

					// Variables
					float labelWidth = 100f;
					EcoGUI.IntField ("Males:", ref editNest.males, labelWidth);
					EcoGUI.IntField ("Females:", ref editNest.females, labelWidth);
					EcoGUI.IntField ("Max males:", ref editNest.malesCapacity, labelWidth);
					EcoGUI.IntField ("Max females:", ref editNest.femalesCapacity, labelWidth);
					EcoGUI.IntField ("Max animals:", ref editNest.totalCapacity, labelWidth);
				}
				GUILayout.EndScrollView ();
			}
			return keepOpen;
		}

		public bool RenderSide (int mx, int my)
		{
			return false;
		}

		public void Dispose ()
		{
			if (edit != null) 
			{
				edit.Delete ();
				edit = null;
			}
		}

		public void SetAnimal (AnimalType animal)
		{
			if (this.animal != animal)
			{
				if (edit != null) {
					edit.Delete ();
					edit = null;
				}

				this.animal = animal;

				nestsData = new BitMap8 (ctrl.scene);

				foreach (AnimalType.Nest nest in animal.nests) {
					nestsData.Set (new Coordinate(nest.x, nest.y), 1);
				}

				SetupEditData ();
			}
		}

		private void SetupEditData ()
		{
			// TODO: We should make an toggle like option so we can create new nests

			edit = EditData.CreateEditData ("nests", nestsData, this.ctrl.scene.progression.managedArea, 
			delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
				return NestStates.Hover;
			}, areaGrid);
			
			edit.SetFinalBrushFunction (HandleClick);
			edit.SetModeBrush (0);
		}
		
		private int HandleClick (int x, int y, int currentVal, float strength, bool shift, bool ctrl) 
		{
			// Check the state, 0 = nothing, 1 = mouse selection, 2 = nest, 3 = selected nest
			int newVal = 0;
			switch (currentVal)
			{
			case NestStates.None : 
				newVal = NestStates.Active; 
				CreateNewNestAt (x, y);
				break;

			case NestStates.Active : 
				newVal = NestStates.Editable;
				EditNest (GetNestAt (x, y));
				break;

			case NestStates.Editable : 
				newVal = NestStates.Editable; 
				break;
			}

			if (newVal != currentVal) updateData = true;
			return newVal;
		}

		public void EditNest (AnimalType.Nest nest)
		{
			// Get the current data
			edit.CopyData (nestsData);

			// Update the values
			if (editNest != null) {
				nestsData.Set (new Coordinate(editNest.x, editNest.y), NestStates.Active);
				editNest = null;
			}
			if (nest != null) {
				editNest = nest;
				nestsData.Set (new Coordinate(editNest.x, editNest.y), NestStates.Editable);
			}

			// Update it
			edit.SetData (nestsData);
		}

		private void CreateNewNestAt (int x, int y)
		{
			AnimalType.Nest newNest = new AnimalType.Nest ();
			newNest.x = x;
			newNest.y = y;

			List<AnimalType.Nest> nests = new List<AnimalType.Nest>(animal.nests);
			nests.Add (newNest);
			animal.nests = nests.ToArray();

			EditNest (newNest);
		}

		public void FocusOnNest (AnimalType.Nest nest)
		{
			HeightMap heights = ctrl.scene.progression.GetData <HeightMap> (Progression.HEIGHTMAP_ID);
			float x = nest.x * TerrainMgr.TERRAIN_SCALE;
			float y = nest.y * TerrainMgr.TERRAIN_SCALE;
			float h = heights.GetInterpolatedHeight (x,y) + 1f;
			Vector3 pos = new Vector3 (x,h,y);
			CameraControl.FocusOnPosition (pos);
		}

		public void DeleteNest (AnimalType.Nest nest)
		{
			if (editNest != null && editNest == nest) {
				editNest = null;
			}
			nestsData.Set (new Coordinate(nest.x, nest.y), NestStates.None);
			updateData = true;
		}

		private AnimalType.Nest GetNestAt (int x, int y)
		{
			foreach (AnimalType.Nest n in animal.nests) {
				if (n.x == x && n.y == y) {
					return n;
				}
			}
			return null;
		}
	}
}