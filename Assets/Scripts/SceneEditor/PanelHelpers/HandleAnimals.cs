using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleAnimals : PanelHelper
	{
	
//		readonly MapsPanel parent;
		readonly EditorCtrl ctrl;
//		readonly Scene scene;
		string[] animalNames;
		int index = 0;
		
		public HandleAnimals (EditorCtrl ctrl, MapsPanel parent, Scene scene)
		{
			this.ctrl = ctrl;
//			this.parent = parent;
//			this.scene = scene;
			
			List<string> animalNamesList = new List<string> ();
			foreach (EcoTerrainElements.AnimalPrototype ap in EcoTerrainElements.self.animals) {
				animalNamesList.Add (ap.name);
			}
			animalNames = animalNamesList.ToArray ();
			Setup ();
		}

		void Setup ()
		{
			AnimalMgr.self.editMode = true;
			AnimalMgr.self.ForceRefresh ();
		}
		
		public bool Render (int mx, int my)
		{
			GUILayout.Space (8);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", GUILayout.Width (100));
			if (GUILayout.Button (animalNames [index], GUILayout.Width (140))) {
				ctrl.StartSelection (animalNames, index,
					newIndex => {
					index = newIndex;
				});
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.Label ("Click on terrain to place animal, right-click to remove animals");
			return false;
		}

		public void Disable ()
		{
			AnimalMgr.self.editMode = false;
			AnimalMgr.self.ForceRefresh ();
		}

		public void Update ()
		{
			bool ctrlKey = Input.GetKey (KeyCode.LeftControl) | Input.GetKey (KeyCode.RightControl);
			Vector3 mousePos = Input.mousePosition;
			if ((CameraControl.MouseOverGUI) ||
			Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt)) {
				return;
			}
			if (Input.GetMouseButtonDown (0) && !ctrlKey) {
				Vector3 pos;
				if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out pos)) {
					int x = (int)(pos.x / TerrainMgr.TERRAIN_SCALE);
					int y = (int)(pos.z / TerrainMgr.TERRAIN_SCALE);
					AnimalMgr.self.AddAnimalAt (index, x, y);
				}
			}
			if ((Input.GetMouseButtonDown (1)) || (Input.GetMouseButton (0) && ctrlKey)) {
				Vector3 pos;
				if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out pos)) {
					int x = (int)(pos.x / TerrainMgr.TERRAIN_SCALE);
					int y = (int)(pos.z / TerrainMgr.TERRAIN_SCALE);
					AnimalMgr.self.RemoveAnimalAt (x, y);
				}
			}
		}
	}
}
