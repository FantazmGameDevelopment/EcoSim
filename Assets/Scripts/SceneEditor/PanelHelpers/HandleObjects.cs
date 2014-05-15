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
		
		private enum ObjectType
		{
			Buildings,
			Roads,
			Animals,
			ActionGroups,
		}
		
		private ObjectType objectMode = ObjectType.Roads;

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
			switch (objectMode) 
			{
			case ObjectType.ActionGroups :
				SetupActionGroups ();
				break;
			case ObjectType.Buildings :
				SetupBuildings ();
				break;
			case ObjectType.Roads :
				SetupRoads ();
				break;
			case ObjectType.Animals :
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

		void SetupActionGroups ()
		{
			ResetEdit ();
			helper = new HandleActionObjectGroups (ctrl, parent, scene);
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
			{
				GUILayout.Label ("Type:", GUILayout.Width (30));

				if (GUILayout.Button ("Buildings", (objectMode == ObjectType.Buildings) ? tabSelected : tabNormal)) {//, GUILayout.Width (90))) {
					objectMode = ObjectType.Buildings;
					SetupBuildings ();
				}
				if (GUILayout.Button ("Roads", (objectMode == ObjectType.Roads) ? tabSelected : tabNormal)) {//, GUILayout.Width (90))) {
					objectMode = ObjectType.Roads;
					SetupRoads ();
				}
				if (GUILayout.Button ("Animals", (objectMode == ObjectType.Animals) ? tabSelected : tabNormal)) {//, GUILayout.Width (90))) {
					objectMode = ObjectType.Animals;
					SetupAnimals ();
				}
				if (GUILayout.Button ("Action Groups", (objectMode == ObjectType.ActionGroups) ? tabSelected : tabNormal)) {//, GUILayout.Width (90))) {
					objectMode = ObjectType.ActionGroups;
					SetupActionGroups ();
				}
				//GUILayout.FlexibleSpace ();
			}
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
