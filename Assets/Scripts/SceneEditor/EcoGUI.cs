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
				if (!string.IsNullOrEmpty (name)) {
					if (nameLayout != null) 	GUILayout.Label (name, nameLayout);
					else 						GUILayout.Label (name);
				}

				string formatStr = "0";
				if (decimals > 0) formatStr += ".";
				for (int i = 0; i < decimals; i++) 
					formatStr += "0";

				string valStr = val.ToString(formatStr);
				string newValStr = "";
				if (valLayout != null) 	newValStr = GUILayout.TextField (valStr, valLayout);
				else 					newValStr = GUILayout.TextField (valStr);

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

		public static void RangeSliders (string name, ref int minRange, ref int maxRange, int min, int max, GUILayoutOption nameLayout = null, GUILayoutOption valLayout = null)
		{
			GUILayout.BeginHorizontal ();
			{
				if (!string.IsNullOrEmpty (name)) {
					if (nameLayout != null)
						GUILayout.Label (name, nameLayout);
					else GUILayout.Label (name);
				}
				
				GUILayout.Label (minRange.ToString(), GUILayout.Width (25));
				if (valLayout != null) 	minRange = (int)GUILayout.HorizontalSlider (minRange, min, max, valLayout);
				else 					minRange = (int)GUILayout.HorizontalSlider (minRange, min, max);
				
				if (valLayout != null) 	maxRange = (int)GUILayout.HorizontalSlider (maxRange, min, max, valLayout);
				else 					maxRange = (int)GUILayout.HorizontalSlider (maxRange, min, max);
				GUILayout.Label (maxRange.ToString(), GUILayout.Width (25));
				
				if (maxRange < minRange) maxRange = minRange;
			}
			GUILayout.EndHorizontal ();
		}

		public static void RangeSliders (string name, ref float minRange, ref float maxRange, float min, float max, GUILayoutOption nameLayout = null, GUILayoutOption valLayout = null)
		{
			GUILayout.BeginHorizontal ();
			{
				if (!string.IsNullOrEmpty (name)) {
					if (nameLayout != null)
						GUILayout.Label (name, nameLayout);
					else GUILayout.Label (name);
				}

				GUILayout.Label (minRange.ToString("0.00"), GUILayout.Width (25));
				if (valLayout != null) 	minRange = GUILayout.HorizontalSlider (minRange, min, max, valLayout);
				else 					minRange = GUILayout.HorizontalSlider (minRange, min, max);

				if (valLayout != null) 	maxRange = GUILayout.HorizontalSlider (maxRange, min, max, valLayout);
				else 					maxRange = GUILayout.HorizontalSlider (maxRange, min, max);
				GUILayout.Label (maxRange.ToString("0.00"), GUILayout.Width (25));

				if (maxRange < minRange) maxRange = minRange;
			}
			GUILayout.EndHorizontal ();
		}
	}
}
