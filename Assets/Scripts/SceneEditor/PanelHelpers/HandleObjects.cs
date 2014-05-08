using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleObjects : PanelHelper
	{
		private readonly MapsPanel parent;
		private Scene scene;
		private EditorCtrl ctrl;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;
		private PanelHelper helper;
		
		private enum ObectType
		{
			Buildings,
			Roads,
			Animals
		}
		
		private ObectType objectMode = ObectType.Roads;

		public HandleObjects (EditorCtrl ctrl, MapsPanel parent, Scene scene)
		{
			this.parent = parent;
			this.ctrl = ctrl;
			
			this.scene = parent.scene;
			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
			
			Setup ();
		}
		
		void Setup ()
		{
			switch (objectMode) {
			case ObectType.Buildings :
				SetupBuildings ();
				break;
			case ObectType.Roads :
				SetupRoads ();
				break;
			case ObectType.Animals :
				SetupAnimals ();
				break;
			}
		}
		
		void ResetEdit ()
		{
			if (helper != null) {
				helper.Disable ();
				helper = null;
			}
		}
		
		void SetupBuildings ()
		{
			ResetEdit ();
			helper = new HandleBuildings(ctrl, parent, scene);
		}

		void SetupRoads ()
		{
			ResetEdit ();
			helper = new HandleRoads (ctrl, parent, scene);
		}

		void SetupAnimals ()
		{
			ResetEdit ();
			helper = new HandleAnimals (ctrl, parent, scene);
		}
				
		public bool Render (int mx, int my)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("object type", GUILayout.Width (100));
			if (GUILayout.Button ("Buildings", (objectMode == ObectType.Buildings) ? tabSelected : tabNormal, GUILayout.Width (90))) {
				objectMode = ObectType.Buildings;
				SetupBuildings ();
			}
			if (GUILayout.Button ("Roads", (objectMode == ObectType.Roads) ? tabSelected : tabNormal, GUILayout.Width (90))) {
				objectMode = ObectType.Roads;
				SetupRoads ();
			}
			if (GUILayout.Button ("Animals", (objectMode == ObectType.Animals) ? tabSelected : tabNormal, GUILayout.Width (90))) {
				objectMode = ObectType.Animals;
				SetupAnimals ();
			}
			
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			
			bool result = false;
			if (helper != null) {
				result = helper.Render (mx, my);
			}
			return result;
		}
		
		public void Disable ()
		{
			ResetEdit ();
		}

		public void Update() {
			if (helper != null) {
				helper.Update();
			}
		}
	}
}
