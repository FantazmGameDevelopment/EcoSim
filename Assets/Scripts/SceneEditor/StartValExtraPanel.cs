using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using UnityEngine;

namespace Ecosim.SceneEditor
{
	public class StartValExtraPanel : ExtraPanel
	{
		EditorCtrl ctrl;
		VegetationType vegetation;
		Vector2 scrollPos;
		string[] parameters;
		
		private static ParameterChange[] copyBuffer = null;

		public static void ClearCopyBuffer () {
			copyBuffer = null;
		}
		
		public StartValExtraPanel (EditorCtrl ctrl, VegetationType veg)
		{
			this.ctrl = ctrl;
			vegetation = veg;
			List<string> pList = new List<string> ();
			foreach (string p in ctrl.scene.progression.GetAllDataNames()) {
				Data dataFindNames = ctrl.scene.progression.GetData (p);
				if ((dataFindNames.GetMax () < 256) && (!p.StartsWith ("_")))
					pList.Add (p);
			}
			parameters = pList.ToArray ();
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
			GUILayout.Label ("Start Values", GUILayout.Width (100));
			GUILayout.Label (vegetation.successionType.name + "\n" + vegetation.name);
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Copy")) {
				copyBuffer = new ParameterChange[vegetation.changes.Length];
				for (int i = 0; i < copyBuffer.Length; i++) {
					copyBuffer[i] = (ParameterChange) (vegetation.changes[i].Clone());
				}
			}
			if ((copyBuffer != null) && GUILayout.Button ("Paste")) {
				vegetation.changes = new ParameterChange[copyBuffer.Length];
				for (int i = 0; i < copyBuffer.Length; i++) {
					vegetation.changes[i] = (ParameterChange) (copyBuffer[i].Clone());
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			GUILayout.BeginVertical (ctrl.skin.box);
			GUILayout.Label ("Parameter ranges after transition");
			foreach (ParameterChange pc in vegetation.changes) {
				GUILayout.BeginVertical (ctrl.skin.box);
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button (pc.paramName, GUILayout.Width (100))) {
					ParameterChange tmpChange = pc;
					ctrl.StartSelection (parameters, FindParamIndex (tmpChange.paramName),
							newIndex => {
						tmpChange.paramName = parameters [newIndex];
						tmpChange.data = ctrl.scene.progression.GetData (tmpChange.paramName); });	
				}
				string minStr = GUILayout.TextField (pc.lowRange.ToString (), GUILayout.Width (40));
				string maxStr = GUILayout.TextField (pc.highRange.ToString (), GUILayout.Width (40));
				GUILayout.Label ("(" + pc.data.GetMin () + " - " + pc.data.GetMax () + ")");
				int outNr;
				if (int.TryParse (minStr, out outNr))
					pc.lowRange = outNr;
				if (int.TryParse (maxStr, out outNr))
					pc.highRange = outNr;
				
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("-", GUILayout.Width (20))) {
					List<ParameterChange> pcList = new List<ParameterChange> (vegetation.changes);
					pcList.Remove (pc);
					vegetation.changes = pcList.ToArray ();
					break;
				}
				
				GUILayout.EndHorizontal ();
				GUILayout.EndVertical ();
			}
			GUILayout.BeginHorizontal ();
			if ((parameters.Length > 0) && GUILayout.Button ("+", GUILayout.Width (20))) {
				ParameterChange newPC = new ParameterChange ();
				newPC.paramName = parameters [0];
				newPC.data = ctrl.scene.progression.GetData (parameters [0]);
				newPC.lowRange = 0;
				newPC.highRange = 255;
				List<ParameterChange> pcList = new List<ParameterChange> (vegetation.changes);
				pcList.Add (newPC);
				vegetation.changes = pcList.ToArray ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.EndVertical ();
			GUILayout.EndScrollView ();
			return keepOpen;
		}

		public bool RenderSide (int mx, int my)
		{
			return false;
		}
	
	}
}
