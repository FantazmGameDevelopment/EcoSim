using UnityEngine;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Ecosim.EcoScript;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneEditor
{
	public class ActionPanel : Panel
	{
		private Scene scene;
		private EditorCtrl ctrl;
		private Vector2 scrollPos;
		private GUIStyle tabNormal;
//		private GUIStyle tabSelected;
		private string[] actionTypeNames;
		private string[] actionObjectGroups;
		private BasicAction sidePanelAction = null;
		private bool sidePanelShowErrors = false;
		private string sidePanelScript = null;
		private bool showSidePanel = false;
		private bool hasCompiled = false;
				
		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			tabNormal = ctrl.listItem;
//			tabSelected = ctrl.listItemSelected;
			foldedOpen = new HashSet<BasicAction> ();
			hasCompiled = false;
			if (scene == null)
				return;

			actionTypeNames = new string[ActionMgr.actionTypes.Length];
			for (int i = 0; i < actionTypeNames.Length; i++) {
				string str = ActionMgr.actionTypes [i].ToString ();
				actionTypeNames [i] = str.Substring (str.LastIndexOf ('.') + 1);
			}

			actionObjectGroups = new string[] { };
		}
		
		private HashSet<BasicAction> foldedOpen;
		private int addActionTypeIndex = 0;
		private int addActionObjectGroupIndex = 0;
		
		void HandleDialogAction (DialogAction action)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("dialog text", GUILayout.Width (80));
			action.dialogText = GUILayout.TextArea (action.dialogText, GUILayout.Height (40));
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("short text", GUILayout.Width (80));
			action.shortDescText = GUILayout.TextField (action.shortDescText, GUILayout.Width (120));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}

		void HandleAreaAction (AreaAction action)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("area name", GUILayout.Width (80));
			action.areaName = GUILayout.TextField (action.areaName, GUILayout.Width (140));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("empty indicator", GUILayout.Width (80));
			if (GUILayout.Button (scene.assets.icons [action.gridIconId], tabNormal)) {
				ctrl.StartIconSelection (action.gridIconId, newIndex => {
					action.gridIconId = newIndex;
				});
			}
			GUILayout.Space (8);
			GUILayout.Label ("invalid indicator", GUILayout.Width (80));
			if (GUILayout.Button (scene.assets.icons [action.invalidTileIconId], tabNormal)) {
				ctrl.StartIconSelection (action.invalidTileIconId, newIndex => {
					action.invalidTileIconId = newIndex;
				});
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}

		void HandleMarkerAction (MarkerAction action)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("area name", GUILayout.Width (80));
			action.areaName = GUILayout.TextField (action.areaName, GUILayout.Width (140));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}
		
		void HandleInventarisationAction (InventarisationAction action)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("area name", GUILayout.Width (80));
			action.areaName = GUILayout.TextField (action.areaName, GUILayout.Width (140));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("empty indicator", GUILayout.Width (80));
			if (GUILayout.Button (scene.assets.icons [action.gridIconId], tabNormal)) {
				ctrl.StartIconSelection (action.gridIconId, newIndex => {
					action.gridIconId = newIndex;
				});
			}
			GUILayout.Space (8);
			GUILayout.Label ("invalid indicator", GUILayout.Width (80));
			if (GUILayout.Button (scene.assets.icons [action.invalidTileIconId], tabNormal)) {
				ctrl.StartIconSelection (action.invalidTileIconId, newIndex => {
					action.invalidTileIconId = newIndex;
				});
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			
			for (int i = 0; i <= InventarisationAction.MAX_VALUE_INDEX; i++) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("#" + i, GUILayout.Width (30));
				InventarisationAction.InventarisationValue iv = action.valueTypes [i];
				if (iv == null) {
					if (GUILayout.Button ("+", GUILayout.Width (20))) {
						iv = new InventarisationAction.InventarisationValue ();
						iv.name = "Result " + i;
						action.valueTypes [i] = iv;
					}
				} else {
					if (GUILayout.Button ("-", GUILayout.Width (20))) {
						action.valueTypes [i] = null;
					} else {
						if (GUILayout.Button (scene.assets.icons [iv.iconId], tabNormal)) {
							InventarisationAction.InventarisationValue tmpIv = iv;
							ctrl.StartIconSelection (tmpIv.iconId, newIndex => {
								tmpIv.iconId = newIndex;
							});
						}
						iv.name = GUILayout.TextField (iv.name, GUILayout.Width (100));
					}
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
		}

		void HandlePurchaseLandAction (PurchaseLandAction action)
		{
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label ("Area name", GUILayout.Width (80));
				action.areaName = GUILayout.TextField (action.areaName, GUILayout.Width (140));
				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			{
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Invalid indicator", GUILayout.Width (80));
					if (GUILayout.Button (scene.assets.icons [action.invalidTileIconId], tabNormal)) {
						ctrl.StartIconSelection (action.invalidTileIconId, newIndex => {
							action.invalidTileIconId = newIndex;
						});
					}
					//GUILayout.FlexibleSpace ();
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (10);
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Undo indicator", GUILayout.Width (80));
					if (GUILayout.Button (scene.assets.icons [action.undoTileIconId], tabNormal)) {
						ctrl.StartIconSelection (action.undoTileIconId, newIndex => {
							action.undoTileIconId = newIndex;
						});
					}
					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndHorizontal ();
		}

		void HandleResearchPointAction (ResearchPointAction action)
		{
			// TODO: Create an editor where the module builder can create their own string without using the script
		}
		
		void HandleSuccessionAction (SuccessionAction action)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("succession", GUILayout.Width (80));
			action.skipNormalSuccession = GUILayout.Toggle (action.skipNormalSuccession, "skip basic succession");
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}

		void HandlePlantsAction (PlantsAction action)
		{
			GUILayout.BeginHorizontal ();
			{
				action.skipNormalPlantsLogic = GUILayout.Toggle (action.skipNormalPlantsLogic, "Skip normal plants logic");
			}
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			{
				GUI.enabled = !action.skipNormalPlantsLogic;
				{
					action.skipNormalSpawnLogic = GUILayout.Toggle (action.skipNormalSpawnLogic, "Skip normal spawn logic");
				}
				GUI.enabled = true;
			}
			GUILayout.EndHorizontal ();
		}

		void HandleAnimalsAction (AnimalsAction action)
		{
			//GUILayout.BeginHorizontal ();
			//{
				action.skipNormalAnimalsLogic = GUILayout.Toggle (action.skipNormalAnimalsLogic, "Skip normal animals logic");
			//}
			//GUILayout.EndHorizontal ();
		}

		void HandleLargeAnimalsAction (LargeAnimalsAction action)
		{
			//GUILayout.BeginHorizontal ();
			//{
				action.skipNormalAnimalsLogic = GUILayout.Toggle (action.skipNormalAnimalsLogic, "Skip normal animals logic");
				//action.skipNormalGrowthLogic = GUILayout.Toggle (action.skipNormalGrowthLogic, "Skip normal growth logic");
				//action.skipNormalDecreaseLogic = GUILayout.Toggle (action.skipNormalDecreaseLogic, "Skip normal decrease logic");
				//action.skipNormalLandUseLogic = GUILayout.Toggle (action.skipNormalLandUseLogic, "Skip normal land use logic");
			//}
			//GUILayout.EndHorizontal ();
		}

		void HandleActionObjectAction (ActionObjectAction action)
		{
			GUILayout.Space (3);
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Space (2);
				action.processInfluenceRules = GUILayout.Toggle (action.processInfluenceRules, "Process influence rules");
				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal ();
			GUILayout.Space (3);

			if (actionObjectGroups.Length != scene.actionObjectGroups.Length)
			{
				actionObjectGroups = new string[scene.actionObjectGroups.Length];
				for (int i = 0; i < scene.actionObjectGroups.Length; i++) {
					actionObjectGroups[i] = string.Format("\"{0}\" ({1})", scene.actionObjectGroups[i].name, scene.actionObjectGroups[i].groupType.ToString());
				}
				addActionObjectGroupIndex = 0;
			}

			// Choose possible action groups
			int index = 0;
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Space (3);
					GUILayout.Label ("Index", GUILayout.Width (35));
					GUILayout.Label ("Group type", GUILayout.Width (70));
					GUILayout.Label ("Group name");
				}
				GUILayout.EndHorizontal ();

				foreach (ActionObjectsGroup group in action.actionObjectGroups)
				{
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Space (3);
						GUILayout.Label (index++.ToString(), GUILayout.Width (35));
						GUILayout.Label (group.groupType.ToString(), GUILayout.Width (70));
						GUILayout.Label (string.Format("\"{0}\"", group.name));
						if (GUILayout.Button ("-", GUILayout.Width (20)))
						{
							action.actionObjectGroups.Remove (group);
							break;
						}
					}
					GUILayout.EndHorizontal ();
				}

				GUILayout.BeginHorizontal ();
				{
					if (scene.actionObjectGroups.Length > 0)
					{
						if (GUILayout.Button (actionObjectGroups[addActionObjectGroupIndex]))
						{
							ctrl.StartSelection (actionObjectGroups, addActionObjectGroupIndex, newIndex => {
								addActionObjectGroupIndex = newIndex;
							});
						}

						if (GUILayout.Button ("Add", GUILayout.Width (35))) 
						{
							// Check if the index is still valid and check if we don't already have it added
							if (addActionObjectGroupIndex < scene.actionObjectGroups.Length) {
								if (!action.actionObjectGroups.Contains (scene.actionObjectGroups[addActionObjectGroupIndex])) {
									action.actionObjectGroups.Add (scene.actionObjectGroups[addActionObjectGroupIndex]);
								}
							}
						}
					} else GUILayout.Label ("No Action Object groups found.");
				}
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndVertical ();
		}

		void HandleCheatsAction (CheatsAction action)
		{
			// Cheats list
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.Label ("Cheats");
				GUILayout.Space (2);

				int idx = 1;
				foreach (CheatsAction.Cheat c in action.cheats)
				{
					GUILayout.BeginVertical (ctrl.skin.box);
					{
						GUILayout.BeginHorizontal ();
						{
							c.enabled = GUILayout.Toggle (c.enabled, "", GUILayout.Width (20));
							GUI.enabled = c.enabled;

							GUILayout.Label ("ID:" + (idx++), GUILayout.Width (30));

							GUILayout.Label ("Name", GUILayout.Width (50));
							c.name = GUILayout.TextField (c.name, GUILayout.Width (200));

							if (GUILayout.Button ("-", GUILayout.Width (20)))
							{
								action.cheats.Remove (c);
								break;
							}
						}
						GUILayout.EndHorizontal ();
						GUILayout.Space (3);
						GUILayout.BeginHorizontal ();
						{
							GUILayout.Space (54);
							GUILayout.Label ("Cheat", GUILayout.Width (50));
							c.body = GUILayout.TextField (c.body, GUILayout.Width (220));
						}
						GUILayout.EndHorizontal ();
						GUILayout.Space (3);
						GUILayout.BeginHorizontal ();
						{
							GUILayout.Space (54);
							GUILayout.Label ("Feedback", GUILayout.Width (50));
							c.feedback = GUILayout.TextField (c.feedback, GUILayout.Width (220));
						}
						GUILayout.EndHorizontal ();

						GUI.enabled = true;
					}
					GUILayout.EndVertical ();
					GUILayout.Space (2);
				}

				if (GUILayout.Button ("New cheat", GUILayout.Width (80)))
				{
					action.cheats.Add ( new CheatsAction.Cheat ("New cheat", "cheatbody") );
				}
			}
			GUILayout.EndVertical ();
		}
		
		private string debugStr = "";
		private CompilerErrorCollection errors = null;
		
		public bool Render (int mx, int my)
		{
			ActionMgr actions = scene.actions;
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Compile")) {
				if (Compiler.CompileScripts (scene, out errors, true)) {
				}
			}
			GUILayout.FlexibleSpace ();
			if (errors != null) {
				if (GUILayout.Button ((errors.HasErrors) ? (ctrl.error) : (ctrl.warning), ctrl.skin.label)) {
					sidePanelAction = null;
					sidePanelShowErrors = true;
					showSidePanel = true;
				}
			}
			GUILayout.EndHorizontal ();
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			int index = 0;
			foreach (BasicAction action in actions.EnumerateActions<BasicAction>()) {
				GUILayout.BeginVertical (ctrl.skin.box);
				GUILayout.BeginHorizontal ();
				bool isFoldedOpen = foldedOpen.Contains (action);
				if (GUILayout.Button (isFoldedOpen ? ctrl.foldedOpenSmall : ctrl.foldedCloseSmall)) {
					if (isFoldedOpen)
						foldedOpen.Remove (action);
					else
						foldedOpen.Add (action);
				}
				GUILayout.Label ("#" + action.id, GUILayout.Width (40));
				action.isActive = GUILayout.Toggle (action.isActive, "", GUILayout.Width (20));
				if (action.DescriptionIsWritable ()) {
					action.SetDescription (GUILayout.TextField (action.GetDescription (), GUILayout.Width (180)));
				} else {
					GUILayout.Label (action.GetDescription ());
				}
				GUILayout.FlexibleSpace ();
				if ((action.errors != null) && (action.errors.Count > 0)) {
					if (GUILayout.Button ((action.errors.HasErrors) ? (ctrl.error) : (ctrl.warning), ctrl.skin.label)) {
						sidePanelAction = action;
						sidePanelShowErrors = true;
						sidePanelScript = action.Script;
						showSidePanel = true;
					}
				}
				if (action.HasScript ()) {
					if (GUILayout.Button (ctrl.script, ctrl.skin.label)) {
						sidePanelAction = action;
						sidePanelShowErrors = false;
						sidePanelScript = action.Script;
						showSidePanel = true;
					}
				}
				if (index > 0) {
					if (GUILayout.Button ("^", GUILayout.Width (20))) {
						actions.MoveActionUp (action);
						actions.UpdateReferences ();
						break;
					}
				}
				if (GUILayout.Button ("x", GUILayout.Width (20))) {
					actions.RemoveAction (action);
					actions.UpdateReferences ();
					break;
				}
				GUILayout.EndHorizontal ();
				if (isFoldedOpen) {
					GUILayout.Space (8);
					int uiListCount = action.uiList.Count;
					foreach (UserInteraction ui in action.uiList) 
					{
						GUILayout.BeginHorizontal (); // 1
						if (GUILayout.Button (scene.assets.GetIcon (ui.iconId), tabNormal)) {
							UserInteraction tmpAction = ui;
							ctrl.StartIconSelection (ui.iconId, newIndex => {
								tmpAction.iconId = newIndex;
							});
						}
						GUILayout.BeginVertical ();
						GUILayout.BeginHorizontal ();
						string newName = GUILayout.TextField (ui.name, GUILayout.Width (80));
						if (newName != ui.name) {
							ui.name = newName;
							actions.UpdateReferences ();
						}
						ui.description = GUILayout.TextField (ui.description, GUILayout.Width (200));
						GUILayout.FlexibleSpace ();
						if ((uiListCount > action.GetMinUICount ()) && GUILayout.Button ("-", GUILayout.Width (20))) {
							action.uiList.Remove (ui);
							actions.UpdateReferences ();

							// Exception time
							if (action is InventarisationAction)
							{
								// Remove bias of the ui
								InventarisationAction invAction = (InventarisationAction)action;
								InventarisationAction.BiasRange range = invAction.GetBias (ui.index);
								if (range != null)
									invAction.biasses.Remove (range);
							}
							break;
						}
						GUILayout.EndHorizontal ();
						GUILayout.BeginHorizontal ();
						string costStr = ui.cost.ToString ();
						GUILayout.Label ("Price", GUILayout.Width (40));
						string newCostStr = GUILayout.TextField (costStr, GUILayout.Width (40));
						if (costStr != newCostStr) {
							long parseVal;
							if (long.TryParse (newCostStr, out parseVal)) {
								ui.cost = parseVal;
							}
						}
						ui.help = GUILayout.TextField ((ui.help == null) ? "" : ui.help, GUILayout.Width (200));
						GUILayout.FlexibleSpace ();
						GUILayout.EndHorizontal ();
						GUILayout.EndVertical ();
						GUILayout.EndHorizontal (); // ~1

						// Exception time
						if (action is InventarisationAction) 
						{
							InventarisationAction.BiasRange range = ((InventarisationAction)action).GetBias (ui.index);
							EcoGUI.skipHorizontal = true;
							GUILayout.BeginHorizontal ();
							{
								EcoGUI.RangeSliders ("\t\t   Bias", 
								                     ref range.min,
								                     ref range.max,
								                     0f, 1f, 
								                     GUILayout.Width (80), 
								                     GUILayout.Width (40));
								GUILayout.Space (3);
								EcoGUI.EnumButton<InventarisationAction.BiasRange.RoundTypes>
									("", range.roundType, delegate (InventarisationAction.BiasRange.RoundTypes val) {
										range.roundType = val;
									}, null, null);
							}
							GUILayout.EndHorizontal ();
							EcoGUI.skipHorizontal = false;
						}

						GUILayout.Space (5);
					} // foreach uiAction

					if (uiListCount < action.GetMaxUICount ()) {
						GUILayout.BeginHorizontal ();
						GUILayout.Label ("UI Interaction", GUILayout.Width (80));
						if (GUILayout.Button ("Add Icon")) {
							action.uiList.Add (new UserInteraction (action));
							actions.UpdateReferences ();
						}
						GUILayout.FlexibleSpace ();
						GUILayout.EndHorizontal ();
					}
					
					if (action is DialogAction) {
						HandleDialogAction ((DialogAction)action);
					} else if (action is AreaAction) {
						HandleAreaAction ((AreaAction)action);
					} else if (action is InventarisationAction) {
						HandleInventarisationAction ((InventarisationAction)action);
					} else if (action is ResearchPointAction) {
						HandleResearchPointAction ((ResearchPointAction)action);
					} else if (action is MarkerAction) {
						HandleMarkerAction ((MarkerAction)action);
					} else if (action is SuccessionAction) {
						HandleSuccessionAction ((SuccessionAction)action);
					} else if (action is PlantsAction) {
						HandlePlantsAction ((PlantsAction)action);
					} else if (action is LargeAnimalsAction) {
						HandleLargeAnimalsAction ((LargeAnimalsAction)action);
					} else if (action is AnimalsAction) {
						HandleAnimalsAction ((AnimalsAction)action);
					} else if (action is ActionObjectAction) {
						HandleActionObjectAction ((ActionObjectAction)action);
					} else if (action is CheatsAction) {
						HandleCheatsAction ((CheatsAction)action);
					} else if (action is PurchaseLandAction) {
						HandlePurchaseLandAction ((PurchaseLandAction)action);
					}
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Script", GUILayout.Width (80));
					if (!action.HasScript ()) {
						if (GUILayout.Button ("Create script")) {
							action.CreateDefaultScript ();
						}
					} else {
						if (GUILayout.Button ("Delete script")) {
							BasicAction tmpAction = action;
							ctrl.StartDialog ("Delete script for action '" + action.GetDescription () + "'?", result => {
								tmpAction.DeleteScript ();
							}, null);
							action.DeleteScript ();
						}
						if (GUILayout.Button ("Call 'Debug'")) {
							action.DebugFn (debugStr);
						}
						debugStr = GUILayout.TextField (debugStr, GUILayout.Width (60));
					}
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
				}
				GUILayout.EndVertical ();
				index ++;
			}
			GUILayout.Space (16);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Add Action", GUILayout.Width (100));
			if (GUILayout.Button (actionTypeNames [addActionTypeIndex].ToString (), tabNormal, GUILayout.Width (100))) {
				ctrl.StartSelection (actionTypeNames, addActionTypeIndex, newIndex => {
					addActionTypeIndex = newIndex;
				});
			}
			if (GUILayout.Button ("Add action")) {
				actions.CreateAction (ActionMgr.actionTypes [addActionTypeIndex]);
				actions.UpdateReferences ();

			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.EndScrollView ();
			return true;
		}
		
		public void RenderExtra (int mx, int my)
		{
		}
		
		private Vector2 sideScroll;
		private string debugFnStr = "";
		
		public void RenderSide (int mx, int my)
		{
			BasicAction action = sidePanelAction;
			CompilerErrorCollection errors = (action != null) ? action.errors : this.errors;
			GUILayout.BeginVertical ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (ctrl.foldedOpenSmall)) {
				sidePanelAction = null;
				showSidePanel = false;
			}
			if (action != null) {
				GUILayout.Label ("#" + action.id, GUILayout.Width (40));
				action.isActive = GUILayout.Toggle (action.isActive, "", GUILayout.Width (20));
				GUILayout.Label (action.GetDescription ());
			} else {
				GUILayout.Label ("Errors outside actions");
			}
			GUILayout.FlexibleSpace ();
			if ((errors != null) && (errors.Count > 0)) {
				if (GUILayout.Button ((errors.HasErrors) ? (ctrl.error) : (ctrl.warning), ctrl.skin.label)) {
					sidePanelShowErrors = true;
				}
			}
			if ((action != null) && action.HasScript ()) {
				if (GUILayout.Button (ctrl.script, ctrl.skin.label)) {
					sidePanelShowErrors = false;
				}
			}
			GUILayout.EndHorizontal ();
			if (sidePanelShowErrors) {
				sideScroll = GUILayout.BeginScrollView (sideScroll, GUILayout.Width (390));
				int nrErrors = (errors != null) ? (errors.Count) : 0;
				for (int i = 0; i < nrErrors; i++) {
					CompilerError err = errors [i];
					GUILayout.BeginHorizontal (ctrl.skin.box);
					GUILayout.Label (((action == null) ? (err.FileName + " ") : "") + err.Line + ": " + err.ErrorText);
					GUILayout.EndHorizontal ();
				}
				GUILayout.EndScrollView ();
			} else if ((action != null) && action.HasScript ()) {
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Update")) {
					action.Script = sidePanelScript;
				}
				if (action.HasDebugFn) {
					if (GUILayout.Button ("Call DebugFn")) {
						action.DebugFn (debugFnStr);
					}
					debugFnStr = GUILayout.TextField (debugFnStr, GUILayout.Width (60));
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				sideScroll = GUILayout.BeginScrollView (sideScroll);
				sidePanelScript = GUILayout.TextArea (sidePanelScript);
				GUILayout.EndScrollView ();
			}
			GUILayout.EndVertical ();
		}

		public bool NeedSidePanel ()
		{
			return (showSidePanel);
		}

		public bool IsAvailable ()
		{
			return (scene != null);
		}
		
		public void Activate ()
		{
			if ((scene != null) && !hasCompiled) {
				foreach (BasicAction action in scene.actions.EnumerateActions ()) {
					action.LoadScript ();
				}
				Compiler.CompileScripts (scene, out errors, true);
				hasCompiled = true;
			}
		}
		
		public void Deactivate ()
		{
		}

		public void Update ()
		{
		}
		
	}
}
