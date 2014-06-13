using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim.SceneData.Action;
using UnityEngine;

namespace Ecosim.SceneEditor
{
	public class AllowedActionsPanel : ExtraPanel
	{
		EditorCtrl ctrl;
		VegetationType vegetation;
		Vector2 scrollPos;
		string[] selectableActionStrings;
		UserInteraction[] selectableActions;
		bool[] selectedActions;
		static UserInteraction[] copyBuffer = null;
		
		public static void ClearCopyBuffer () {
			copyBuffer = null;
		}
		
		public AllowedActionsPanel (EditorCtrl ctrl, VegetationType veg)
		{
			this.ctrl = ctrl;
			vegetation = veg;
			List<string> actionStrList = new List<string> ();
			List<UserInteraction> actionList = new List<UserInteraction> ();
			List<bool> selectedActionsList = new List<bool> ();
			foreach (UserInteraction ui in ctrl.scene.actions.EnumerateUI()) {
				if ((ui.action is AreaAction) || (ui.action is InventarisationAction) || (ui.action is MarkerAction)) {
					actionStrList.Add (ui.name);
					actionList.Add (ui);
					selectedActionsList.Add (veg.IsAcceptableAction(ui));
				}
			}
			selectableActionStrings = actionStrList.ToArray ();
			selectableActions = actionList.ToArray ();
			selectedActions = selectedActionsList.ToArray ();
		}
		
		int FindActionIndex (UserInteraction action)
		{
			for (int i = 0; i < selectableActions.Length; i++) {
				if (selectableActions [i] == action)
					return i;
			}
			return 0;
		}
		
		public bool Render (int mx, int my)
		{
			bool keepOpen = true;
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (ctrl.foldedOpen, ctrl.icon)) {
				keepOpen = false;
			}
			GUILayout.Label ("Acceptable actions", GUILayout.Width (100));
			GUILayout.Label (vegetation.successionType.name + "\n" + vegetation.name);
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Copy")) {
				copyBuffer = (UserInteraction[]) vegetation.acceptableActions.Clone ();
			}
			if ((copyBuffer != null) && GUILayout.Button ("Paste")) {
				vegetation.acceptableActions = (UserInteraction[]) copyBuffer.Clone ();
				for (int i = 0; i < selectedActions.Length; i++) {
					selectedActions [i] = false;
				}
				foreach (UserInteraction ui in vegetation.acceptableActions) {
					selectedActions [FindActionIndex (ui)] = true;
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Select all")) {
				for (int i = 0; i < selectedActions.Length; i++) {
					selectedActions [i] = true;
				}
				vegetation.acceptableActions = selectableActions;
			}
			if (GUILayout.Button ("Clear all")) {
				for (int i = 0; i < selectedActions.Length; i++) {
					selectedActions [i] = false;
				}
				vegetation.acceptableActions = new UserInteraction[0];
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			
			GUILayout.BeginHorizontal ();
			for (int i = 0; i < selectableActions.Length; i++) {
				if ((i > 0) && (i % 3 == 0)) {
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
					GUILayout.BeginHorizontal ();
				}
				bool isActive = selectedActions[i];
				if (GUILayout.Button (selectableActionStrings[i], isActive?ctrl.listItemSelected:ctrl.listItem, GUILayout.Width (120))) {
					isActive = !isActive;
					selectedActions [i] = isActive;
					List<UserInteraction> list = new List<UserInteraction> (vegetation.acceptableActions);
					if (isActive) {
						list.Add (selectableActions[i]);
					}
					else {
						list.Remove (selectableActions[i]);
					}
					vegetation.acceptableActions = list.ToArray ();
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			GUILayout.EndScrollView ();
			return keepOpen;
		}

		public bool RenderSide (int mx, int my)
		{
			return false;
		}

		public void Dispose ()
		{

		}
	}
}
