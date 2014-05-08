using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.Action;
using UnityEngine;

namespace Ecosim.SceneEditor
{
	public class RulesExtraPanel : ExtraPanel
	{
		EditorCtrl ctrl;
		VegetationType vegetation;
		Vector2 scrollPos;
		string[] selectableActionStrings;
		UserInteraction[] selectableActions;
		string[] parameters;
		string[] vegetations;
		private static GradualParameterChange[] copyBuffer;
		private static VegetationRule[] copyBuffer2;

		public static void ClearCopyBuffer () {
			copyBuffer = null;
			copyBuffer2 = null;
		}
		
		public RulesExtraPanel (EditorCtrl ctrl, VegetationType veg)
		{
			this.ctrl = ctrl;
			vegetation = veg;
			List<string> actionStrList = new List<string> ();
			List<UserInteraction> actionList = new List<UserInteraction> ();
			actionStrList.Add ("<No action>");
			actionList.Add (null);
			foreach (UserInteraction ui in ctrl.scene.actions.EnumerateUI()) {
				if (ui.action is AreaAction) {
					actionStrList.Add (ui.name);
					actionList.Add (ui);
				}
			}
			selectableActionStrings = actionStrList.ToArray ();
			selectableActions = actionList.ToArray ();
			List<string> pList = new List<string> ();
			foreach (string p in ctrl.scene.progression.GetAllDataNames()) {
				Data dataFindNames = ctrl.scene.progression.GetData (p);
				if ((dataFindNames.GetMax () < 256) && (!p.StartsWith ("_")))
					pList.Add (p);
			}
			parameters = pList.ToArray ();
			vegetations = new string[vegetation.successionType.vegetations.Length];
			for (int i = 0; i < vegetations.Length; i++) {
				vegetations [i] = vegetation.successionType.vegetations [i].name;
			}
		}
		
		int FindActionIndex (UserInteraction action)
		{
			for (int i = 0; i < selectableActions.Length; i++) {
				if (selectableActions [i] == action)
					return i;
			}
			return 0;
		}
		
		int FindParamIndex (string paramName)
		{
			for (int i = 0; i < parameters.Length; i++) {
				if (parameters [i] == paramName)
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
			GUILayout.Label ("Rules", GUILayout.Width (100));
			GUILayout.Label (vegetation.successionType.name + "\n" + vegetation.name);
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Copy")) {
				copyBuffer = new GradualParameterChange [vegetation.gradualChanges.Length];
				for (int i = 0; i < copyBuffer.Length; i++) {
					copyBuffer[i] = (GradualParameterChange) vegetation.gradualChanges[i].Clone ();
				}
				
				copyBuffer2 = new VegetationRule[vegetation.rules.Length];
				for (int i = 0; i < copyBuffer2.Length; i++) {
					copyBuffer2[i] = (VegetationRule) vegetation.rules[i].Clone ();
				}
			}
			if ((copyBuffer != null) && GUILayout.Button ("Paste")) {
				vegetation.gradualChanges = new GradualParameterChange[copyBuffer.Length];
				for (int i = 0; i < copyBuffer.Length; i++) {
					vegetation.gradualChanges[i] = (GradualParameterChange) copyBuffer[i].Clone ();
				}
				vegetation.rules = new VegetationRule[copyBuffer2.Length];
				for (int i = 0; i < copyBuffer2.Length; i++) {
					vegetation.rules[i] = (VegetationRule) copyBuffer2[i].Clone();
					if (vegetation.rules[i].vegetation.successionType != vegetation.successionType) {
						vegetation.rules[i].vegetation = vegetation;
						vegetation.rules[i].vegetationId = vegetation.index;
					}
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);

			// GRADUAL PARAMETER CHANGE
			GUILayout.BeginVertical (ctrl.skin.box);
			GUILayout.Label ("Gradual Parameter Changes");
			foreach (GradualParameterChange gpc in vegetation.gradualChanges) {
				GUILayout.BeginVertical (ctrl.skin.box);
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Action type", GUILayout.Width (100));
				UserInteraction action = gpc.action;
				string actionName = (action != null) ? (action.name) : "<No action>";
				if (GUILayout.Button (actionName, GUILayout.Width (100))) {
					GradualParameterChange tmpGPC = gpc;
					ctrl.StartSelection (selectableActionStrings, FindActionIndex (action),
					newIndex => {
						tmpGPC.actionName = (newIndex > 0) ? (selectableActionStrings [newIndex]) : null;
						tmpGPC.action = selectableActions [newIndex]; });	
				}
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("-", GUILayout.Width (20))) {
					List<GradualParameterChange> gcList = new List<GradualParameterChange> (vegetation.gradualChanges);
					gcList.Remove (gpc);
					vegetation.gradualChanges = gcList.ToArray ();
					break;
				}
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Chance (0..1)", GUILayout.Width (100));
				gpc.chance = GUILayout.HorizontalSlider (gpc.chance, 0.0f, 1.0f, GUILayout.Width (100));
				string chanceStr = gpc.chance.ToString ("0.00");
				string newChanceStr = GUILayout.TextField (chanceStr, GUILayout.Width (40));
				if (newChanceStr != chanceStr) {
					float outVal;
					if (float.TryParse (newChanceStr, out outVal)) {
						gpc.chance = outVal;
					}
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button (gpc.paramName, GUILayout.Width (100))) {
					GradualParameterChange tmpGPC = gpc;
					ctrl.StartSelection (parameters, FindParamIndex (gpc.paramName),
							newIndex => {
						tmpGPC.paramName = parameters [newIndex];
						tmpGPC.data = ctrl.scene.progression.GetData (tmpGPC.paramName); });	
				}
				string minStr = GUILayout.TextField (gpc.lowRange.ToString (), GUILayout.Width (40));
				string maxStr = GUILayout.TextField (gpc.highRange.ToString (), GUILayout.Width (40));
				GUILayout.Label ("(" + gpc.data.GetMin () + " - " + gpc.data.GetMax () + ")  delta");
				string deltaStr = GUILayout.TextField (gpc.deltaChange.ToString (), GUILayout.Width (40));
				int outNr;
				if (int.TryParse (minStr, out outNr))
					gpc.lowRange = outNr;
				if (int.TryParse (maxStr, out outNr))
					gpc.highRange = outNr;
				if (int.TryParse (deltaStr, out outNr))
					gpc.deltaChange = outNr;
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();				
				GUILayout.EndVertical ();
			}
			GUILayout.BeginHorizontal ();
			if ((parameters.Length > 0) && GUILayout.Button ("+", GUILayout.Width (20))) {
				List<GradualParameterChange> gcList = new List<GradualParameterChange> (vegetation.gradualChanges);
				GradualParameterChange newGPC = new GradualParameterChange ();
				newGPC.paramName = parameters [0];
				newGPC.data = ctrl.scene.progression.GetData (newGPC.paramName);
				newGPC.action = null;
				newGPC.actionName = null;
				gcList.Add (newGPC);
				vegetation.gradualChanges = gcList.ToArray ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
			// END GRADUAL PARAMETER CHANGE
			
			// TRANSITION RULES
			GUILayout.BeginVertical (ctrl.skin.box);
			GUILayout.Label ("Transition Rules");
			foreach (VegetationRule r in vegetation.rules) {
				GUILayout.BeginVertical (ctrl.skin.box);
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Action type", GUILayout.Width (100));
				UserInteraction action = r.action;
				string actionName = (action != null) ? (action.name) : "<No action>";
				if (GUILayout.Button (actionName, GUILayout.Width (100))) {
					VegetationRule tmpR = r;
					ctrl.StartSelection (selectableActionStrings, FindActionIndex (action),
					newIndex => {
						tmpR.actionName = (newIndex > 0) ? (selectableActionStrings [newIndex]) : null;
						tmpR.action = selectableActions [newIndex]; });	
				}
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("-", GUILayout.Width (20))) {
					List<VegetationRule> rList = new List<VegetationRule> (vegetation.rules);
					rList.Remove (r);
					vegetation.rules = rList.ToArray ();
					break;
				}
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Chance (0..1)", GUILayout.Width (100));
				r.chance = GUILayout.HorizontalSlider (r.chance, 0.0f, 1.0f, GUILayout.Width (100));
				string chanceStr = r.chance.ToString ("0.00");
				string newChanceStr = GUILayout.TextField (chanceStr, GUILayout.Width (40));
				if (newChanceStr != chanceStr) {
					float outVal;
					if (float.TryParse (newChanceStr, out outVal)) {
						r.chance = outVal;
					}
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				foreach (ParameterRange pr in r.ranges) {
					GUILayout.BeginHorizontal ();
					if (GUILayout.Button (pr.paramName, GUILayout.Width (100))) {
						ParameterRange tmpPR = pr;
						ctrl.StartSelection (parameters, FindParamIndex (tmpPR.paramName),
							newIndex => {
							tmpPR.paramName = parameters [newIndex];
							tmpPR.data = ctrl.scene.progression.GetData (tmpPR.paramName); });	
					}
					string minStr = GUILayout.TextField (pr.lowRange.ToString (), GUILayout.Width (40));
					string maxStr = GUILayout.TextField (pr.highRange.ToString (), GUILayout.Width (40));
					GUILayout.Label ("(" + pr.data.GetMin () + " - " + pr.data.GetMax () + ")");
					int outNr;
					if (int.TryParse (minStr, out outNr))
						pr.lowRange = outNr;
					if (int.TryParse (maxStr, out outNr))
						pr.highRange = outNr;
					GUILayout.FlexibleSpace ();
					if (GUILayout.Button ("-", GUILayout.Width (20))) {
						List<ParameterRange> prList = new List<ParameterRange> (r.ranges);
						prList.Remove (pr);
						r.ranges = prList.ToArray ();
						break;
					}
					GUILayout.EndHorizontal ();
				}
				GUILayout.BeginHorizontal ();
				if ((parameters.Length > 0) && GUILayout.Button ("+", GUILayout.Width (20))) {
					ParameterRange newPR = new ParameterRange ();
					newPR.paramName = parameters [0];
					newPR.data = ctrl.scene.progression.GetData (parameters [0]);
					newPR.lowRange = 0;
					newPR.highRange = 255;
					List<ParameterRange> prList = new List<ParameterRange> (r.ranges);
					prList.Add (newPR);
					r.ranges = prList.ToArray ();
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Target vegetation", GUILayout.Width (100));
				if (GUILayout.Button (r.vegetation.name, GUILayout.Width (200))) {
					ctrl.StartSelection (vegetations, r.vegetationId,
						newIndex => {
						r.vegetationId = newIndex;
						r.vegetation = vegetation.successionType.vegetations [newIndex]; });	
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.EndVertical ();
			}
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("+", GUILayout.Width (20))) {
				VegetationRule newR = new VegetationRule ();
				newR.vegetationId = vegetation.index;
				newR.vegetation = vegetation;
				newR.ranges = new ParameterRange[0];
				List<VegetationRule> list = new List<VegetationRule> (vegetation.rules);
				list.Add (newR);
				vegetation.rules = list.ToArray ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
			// END TRANSITION RULES
			
			
			GUILayout.EndScrollView ();
			return keepOpen;
		}

		public bool RenderSide (int mx, int my)
		{
			return false;
		}
	
	}
}
