using UnityEngine;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Ecosim.EcoScript;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneEditor
{
	public static class EcoGUI
	{
		public static void IntField (string name, ref int val, GUILayoutOption nameLayout = null, GUILayoutOption valLayout = null)
		{
			GUILayout.BeginHorizontal ();
			{
				if (!string.IsNullOrEmpty (name))
					GUILayout.Label (name, nameLayout);

				string str = GUILayout.TextField (val.ToString(), valLayout);
				if (str != val.ToString())
				{
					int newVal;
					if (int.TryParse (str, out newVal)) {
						val = newVal;
					}
				}
			}
			GUILayout.EndHorizontal ();
		}

		public static void FloatField (string name, ref float val, int decimals = 2, GUILayoutOption nameLayout = null, GUILayoutOption valLayout = null)
		{
			GUILayout.BeginHorizontal ();
			{
				if (!string.IsNullOrEmpty (name))
					GUILayout.Label (name, nameLayout);

				string formatStr = "0";
				if (decimals > 0) formatStr += ".";
				for (int i = 0; i < decimals; i++) 
					formatStr += "0";

				string valStr = val.ToString(formatStr);
				string newValStr = GUILayout.TextField (valStr, valLayout);
				if (newValStr != valStr)
				{
					float newVal;
					if (float.TryParse (newValStr, out newVal)) {
						val = newVal;
					}
				}
			}
			GUILayout.EndHorizontal ();
		}
	}
}
