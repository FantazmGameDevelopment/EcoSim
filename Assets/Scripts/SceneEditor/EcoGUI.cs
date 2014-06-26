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
		public static bool skipHorizontal = false;

		public static void IntField (string name, ref int val)
		{
			IntField (name, ref val, null, null);
		}

		public static void IntField (string name, ref int val, float nameWidth = 0f, float valWidth = 0f)
		{
			IntField (name, ref val, nameWidth > 0 ? GUILayout.Width (nameWidth) : null, valWidth > 0 ? GUILayout.Width (valWidth) : null);
		}

		public static void IntField (string name, ref int val, GUILayoutOption nameLayout = null, GUILayoutOption valLayout = null)
		{
			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				RenderName (name, nameLayout);

				string str = val.ToString();
				if (valLayout != null) 	str = GUILayout.TextField (str, valLayout);
				else 					str = GUILayout.TextField (str);

				if (str != val.ToString())
				{
					if (str.Length == 0) str = "0";
					int newVal;
					if (int.TryParse (str, out newVal)) {
						val = newVal;
					}
				}
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
		}

		public static void FloatField (string name, ref float val, int decimals = 2)
		{
			FloatField (name, ref val, decimals, null, null);
		}

		public static void FloatField (string name, ref float val, int decimals, float nameWidth, float valWidth)
		{
			FloatField (name, ref val, decimals, nameWidth > 0 ? GUILayout.Width (nameWidth) : null, valWidth > 0 ? GUILayout.Width (valWidth) : null);
		}

		public static void FloatField (string name, ref float val, int decimals, GUILayoutOption nameLayout, GUILayoutOption valLayout)
		{
			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				RenderName (name, nameLayout);

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
					if (newValStr.Length == 0) newValStr = "0";
					float newVal;
					if (float.TryParse (newValStr, out newVal)) {
						val = newVal;
					}
				}
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
		}

		public static void RangeSliders (string name, ref int minRange, ref int maxRange, int min, int max, GUILayoutOption nameLayout = null, GUILayoutOption valLayout = null)
		{
			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				RenderName (name, nameLayout);
				
				GUILayout.Label (minRange.ToString(), GUILayout.Width (25));
				if (valLayout != null) 	minRange = (int)GUILayout.HorizontalSlider (minRange, min, max, valLayout);
				else 					minRange = (int)GUILayout.HorizontalSlider (minRange, min, max);
				
				if (valLayout != null) 	maxRange = (int)GUILayout.HorizontalSlider (maxRange, min, max, valLayout);
				else 					maxRange = (int)GUILayout.HorizontalSlider (maxRange, min, max);
				GUILayout.Label (maxRange.ToString(), GUILayout.Width (25));
				
				if (maxRange < minRange) maxRange = minRange;
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
		}

		public static void RangeSliders (string name, ref float minRange, ref float maxRange, float min, float max, GUILayoutOption nameLayout = null, GUILayoutOption valLayout = null)
		{
			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				RenderName (name, nameLayout);

				GUILayout.Label (minRange.ToString("0.00"), GUILayout.Width (25));
				if (valLayout != null) 	minRange = GUILayout.HorizontalSlider (minRange, min, max, valLayout);
				else 					minRange = GUILayout.HorizontalSlider (minRange, min, max);

				if (valLayout != null) 	maxRange = GUILayout.HorizontalSlider (maxRange, min, max, valLayout);
				else 					maxRange = GUILayout.HorizontalSlider (maxRange, min, max);
				GUILayout.Label (maxRange.ToString("0.00"), GUILayout.Width (25));

				if (maxRange < minRange) maxRange = minRange;
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
		}

		public static bool Foldout (string name, ref bool opened, GUILayoutOption nameLayout = null)
		{
			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button (opened ? EditorCtrl.self.foldedOpenSmall : EditorCtrl.self.foldedCloseSmall, EditorCtrl.self.icon12x12))
				{
					opened = !opened;
				}
				RenderName (name, nameLayout);
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
			return opened;
		}

		public static bool FoldoutEditableName (ref string name, ref bool opened, GUILayoutOption nameLayout = null)
		{
			return FoldoutEditableName (ref name, ref opened, false, nameLayout);
		}

		public static bool FoldoutEditableName (ref string name, ref bool opened, bool multiline, GUILayoutOption nameLayout = null)
		{
			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button (opened ? EditorCtrl.self.foldedOpenSmall : EditorCtrl.self.foldedCloseSmall, EditorCtrl.self.icon12x12))
				{
					opened = !opened;
				}

				if (name == null) name = "";
				GUILayout.Space (2);
				if (nameLayout != null) name = (multiline) ? GUILayout.TextArea (name, nameLayout) : GUILayout.TextField (name, nameLayout);
				else 					name = (multiline) ? GUILayout.TextArea (name) : GUILayout.TextField (name);
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
			if (multiline) GUILayout.Space (3);
			return opened;
		}

		public static bool Toggle (string name, ref bool value, GUILayoutOption nameLayout = null)
		{
			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				//RenderName (name, nameLayout);
				GUILayout.Space (2);
				value = GUILayout.Toggle (value, name, nameLayout);
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
			return value;
		}

		public static void EnumButton<T> (string name, T value, System.Action<T> onChanged, GUILayoutOption nameLayout = null, GUILayoutOption valLayout = null) where T : struct, System.IConvertible
		{
			if (!typeof (T).IsEnum) 
			{
				throw new System.ArgumentException("EcoGUI.EnumButton: T must be an enum(erated) type");
			}

			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				RenderName (name, nameLayout);
				bool clicked = false;
				if (valLayout != null)  clicked = GUILayout.Button (value.ToString(), valLayout);
				else 					clicked = GUILayout.Button (value.ToString());

				if (clicked)
				{
					List<string> items = new List<string>(System.Enum.GetNames (value.GetType()));
					int selected = items.IndexOf (value.ToString());
					EditorCtrl.self.StartSelection (items.ToArray(), selected, delegate(int index) {
						onChanged ((T)System.Enum.Parse (typeof(T), items[index]));
					});
				}
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
		}

		private static void RenderName (string name, GUILayoutOption nameLayout = null)
		{
			if (!string.IsNullOrEmpty (name)) 
			{
				GUILayout.Space (2);
				if (nameLayout != null) GUILayout.Label (name, nameLayout);
				else 					GUILayout.Label (name);
			}
		}
	}
}
