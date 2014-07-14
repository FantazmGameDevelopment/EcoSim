using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.PlantRules;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.AnimalPopulationModel;

namespace Ecosim.SceneEditor
{
	public class AnimalNestsExtraPanel : ExtraPanel
	{
		public LargeAnimalType animal { get; private set; }
		private EditorCtrl ctrl;

		private Data nestsData;
		private Vector2 editNestScrollPos;
		private AnimalStartPopulationModel startPopModel;
		private AnimalStartPopulationModel.Nests.Nest editNest;

		private EditData edit = null;
		private GridTextureSettings areaGrid = null;
		private Material areaMat = null;

		private bool canCreateNests = false;
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

			SetAnimal (animal);
			if (this.startPopModel.nests.nests.Length > 0)
				EditNest (this.startPopModel.nests.nests[0]);
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
				if (GUILayout.Button (ctrl.foldedOpenSmall, GUILayout.Width (20))) 
				{
					keepOpen = false;
				}
				
				GUILayout.Label ("Animal Nests", GUILayout.Width (100));
				GUILayout.Label ("'" + animal.name + "'");

				if (this.startPopModel.nests.nests.Length > 0)
				{
					if (GUILayout.Button (canCreateNests ? "Stop creating new nests" : "Start creating new nests", GUILayout.Width (160)))
					{
						canCreateNests = !canCreateNests;
						SetupEditData (canCreateNests);
					}
				}

				//GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal ();

			/*GUILayout.BeginHorizontal (); // Buttons
			{

			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~Buttons*/

			if (this.startPopModel.nests.nests.Length == 0)
			{
				GUILayout.Label ("No nests. Click on the terrain to add new nests.");
			}

			// Nest to edit
			if (editNest != null)
			{
				editNestScrollPos = GUILayout.BeginScrollView (editNestScrollPos, false, false);
				{
					// Header
					int editIdx = 0;
					foreach (AnimalStartPopulationModel.Nests.Nest n in this.startPopModel.nests.nests) {
						editIdx++;						
						if (n == editNest) break;
					}
					GUILayout.Label (string.Format(" Nest #{0} ({1},{2})", editIdx, editNest.x, editNest.y));

					// Variables
					float labelWidth = 100f;
					editNest.males = EcoGUI.IntField ("Males:", editNest.males, labelWidth);
					editNest.females = EcoGUI.IntField ("Females:", editNest.females, labelWidth);
					EcoGUI.IntField ("Max males:", ref editNest.malesCapacity, labelWidth);
					EcoGUI.IntField ("Max females:", ref editNest.femalesCapacity, labelWidth);
					EcoGUI.IntField ("Max animals:", ref editNest.totalCapacity, labelWidth);
				}
				GUILayout.EndScrollView ();
			}

			if (!keepOpen) Dispose();
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
				edit.ClearData ();
				edit.Delete ();
				edit = null;
			}
		}

		public void SetAnimal (AnimalType animal)
		{
			if (this.animal != animal)
			{
				this.animal = (LargeAnimalType)animal;
				foreach (IAnimalPopulationModel m in this.animal.models) {
					if (m is AnimalStartPopulationModel) {
						startPopModel = (AnimalStartPopulationModel)m;
						break;
					}
				}
				if (this.startPopModel.nests.nests == null)
					this.startPopModel.nests.nests = new AnimalStartPopulationModel.Nests.Nest[0];

				nestsData = new BitMap8 (ctrl.scene);
				foreach (AnimalStartPopulationModel.Nests.Nest nest in this.startPopModel.nests.nests) {
					nestsData.Set (new Coordinate(nest.x, nest.y), 1);
				}

				// If we don't have any nests, set can create nests default to true.
				canCreateNests = this.startPopModel.nests.nests.Length == 0;
				SetupEditData (canCreateNests);
			}
		}

		private void SetupEditData (bool showZero)
		{
			Dispose ();

			areaGrid = new GridTextureSettings (showZero, 0, 2, areaMat, showZero, areaMat);

			edit = EditData.CreateEditData ("nests", nestsData, this.ctrl.scene.progression.managedArea, 
			delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
				if (canCreateNests) return NestStates.Hover;
				else return (currentVal > 0) ? NestStates.Hover : -1;
			}, areaGrid);
			
			edit.SetFinalBrushFunction (HandleClick);
			edit.SetModeBrush (0);
		}
		
		private int HandleClick (int x, int y, int currentVal, float strength, bool shift, bool ctrl) 
		{
			int newVal = currentVal;
			switch (currentVal)
			{
			case NestStates.None :
				if (canCreateNests)
				{
					newVal = NestStates.Editable; 
					CreateNewNestAt (x, y);	
				}
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

		public void EditNest (AnimalStartPopulationModel.Nests.Nest nest)
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

			// Update the (visual) edit data
			edit.SetData (nestsData);
		}

		private void CreateNewNestAt (int x, int y)
		{
			AnimalStartPopulationModel.Nests.Nest newNest = new AnimalStartPopulationModel.Nests.Nest (this.startPopModel.nests);
			newNest.x = x;
			newNest.y = y;

			List<AnimalStartPopulationModel.Nests.Nest> nests = new List<AnimalStartPopulationModel.Nests.Nest>(this.startPopModel.nests.nests);
			nests.Add (newNest);
			this.startPopModel.nests.nests = nests.ToArray();

			EditNest (newNest);
		}

		public void FocusOnNest (AnimalStartPopulationModel.Nests.Nest nest)
		{
			HeightMap heights = ctrl.scene.progression.GetData <HeightMap> (Progression.HEIGHTMAP_ID);
			float x = nest.x * TerrainMgr.TERRAIN_SCALE;
			float y = nest.y * TerrainMgr.TERRAIN_SCALE;
			float h = heights.GetInterpolatedHeight (x,y) + 1f;
			Vector3 pos = new Vector3 (x,h,y);
			CameraControl.FocusOnPosition (pos);
		}

		public void DeleteNest (AnimalStartPopulationModel.Nests.Nest nest)
		{
			if (this.animal != null)
			{
				// Check if the nest is a part of this animal
				foreach (AnimalStartPopulationModel.Nests.Nest n in this.startPopModel.nests.nests)
				{
					if (n == nest)
					{
						if (editNest == nest) {
							editNest = null;
						}

						nestsData = new BitMap8 (ctrl.scene);
						foreach (AnimalStartPopulationModel.Nests.Nest an in this.startPopModel.nests.nests) {
							if (an != n)
								nestsData.Set (new Coordinate(an.x, an.y), 1);
						}
						
						// If we don't have any nests, set can create nests default to true.
						canCreateNests = this.startPopModel.nests.nests.Length == 0;
						SetupEditData (canCreateNests);
						break;
					}
				}
			}
		}

		private AnimalStartPopulationModel.Nests.Nest GetNestAt (int x, int y)
		{
			foreach (AnimalStartPopulationModel.Nests.Nest n in this.startPopModel.nests.nests) {
				if (n.x == x && n.y == y) {
					return n;
				}
			}
			return null;
		}
	}
}