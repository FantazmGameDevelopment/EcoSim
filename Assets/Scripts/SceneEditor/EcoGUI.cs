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

		public static void SplitLabel (string label, GUIStyle style, params GUILayoutOption[] options)
		{
			GUILayout.Label (label, style, options);
			/*string[] split = label.Split (new string[] { "\n","\r" }, System.StringSplitOptions.None);
			foreach (string s in split) {
				GUILayout.Label ((s.Length > 0) ? s : " ", style, options);
			}*/
		}

		public static int IntField (string name, int val)
		{
			IntField (name, ref val, 0f);
			return val;
		}

		public static void IntField (string name, ref int val)
		{
			IntField (name, ref val, null, null);
		}

		public static int IntField (string name, int val, float nameWidth)
		{
			IntField (name, ref val, nameWidth, 0f);
			return val;
		}

		public static int IntField (string name, int val, float nameWidth, float valWidth)
		{
			IntField (name, ref val, nameWidth, valWidth);
			return val;
		}

		public static void IntField (string name, ref int val, float nameWidth, float valWidth = 0f)
		{
			IntField (name, ref val, nameWidth > 0 ? GUILayout.Width (nameWidth) : null, valWidth > 0 ? GUILayout.Width (valWidth) : null);
		}

		public static int IntField (string name, int val, GUILayoutOption nameLayout, GUILayoutOption valLayout = null)
		{
			IntField (name, ref val, nameLayout, valLayout);
			return val;
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

				bool prevSkipHorizontal = skipHorizontal;
				skipHorizontal = true;

				EcoGUI.FloatField (null, ref minRange, 2, null, GUILayout.Width (40));
				//GUILayout.Label (minRange.ToString("0.00"), GUILayout.Width (25));
				if (valLayout != null) 	minRange = GUILayout.HorizontalSlider (minRange, min, max, valLayout);
				else 					minRange = GUILayout.HorizontalSlider (minRange, min, max);

				if (valLayout != null) 	maxRange = GUILayout.HorizontalSlider (maxRange, min, max, valLayout);
				else 					maxRange = GUILayout.HorizontalSlider (maxRange, min, max);
				//GUILayout.Label (maxRange.ToString("0.00"), GUILayout.Width (25));
				EcoGUI.FloatField (null, ref maxRange, 2, null, GUILayout.Width (40));

				skipHorizontal = prevSkipHorizontal;

				if (maxRange < minRange) maxRange = minRange;
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
		}

		public static bool Foldout (string name, ref bool opened, GUILayoutOption nameLayout = null)
		{
			return Foldout (new GUIContent (name, ""), ref opened, nameLayout);
		}

		public static bool Foldout (GUIContent content, ref bool opened, GUILayoutOption nameLayout = null)
		{
			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				GUILayout.Space (2);
				if (GUILayout.Button (opened ? EditorCtrl.self.foldedOpenSmall : EditorCtrl.self.foldedCloseSmall, EditorCtrl.self.icon12x12))
				{
					opened = !opened;
				}
				RenderName (content, nameLayout);
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

		public static bool Toggle (GUIContent content, ref bool value, float nameWidth = 0f)
		{
			return Toggle (content, ref value, ((nameWidth > 0f) ? GUILayout.Width (nameWidth) : null));
		}

		public static bool Toggle (string name, ref bool value, float nameWidth = 0f)
		{
			return Toggle (new GUIContent (name, ""), ref value, ((nameWidth > 0f) ? GUILayout.Width (nameWidth) : null));
		}

		public static bool Toggle (string name, ref bool value, GUILayoutOption nameLayout)
		{
			return Toggle (new GUIContent (name, ""), ref value, nameLayout);
		}

		public static bool Toggle (GUIContent content, ref bool value, GUILayoutOption nameLayout)
		{
			if (!skipHorizontal) GUILayout.BeginHorizontal ();
			{
				//RenderName (name, nameLayout);
				GUILayout.Space (2);
				if (nameLayout != null)  value = GUILayout.Toggle (value, content, nameLayout);
				else 					 value = GUILayout.Toggle (value, content);
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
			return value;
		}

		public static void EnumButton<T> (string name, T value, System.Action<T> onChanged, float nameWidth, float valueWidth) where T : struct, System.IConvertible
		{
			EnumButton <T> (name, value, onChanged, GUILayout.Width (nameWidth), GUILayout.Width (valueWidth));
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
					for (int i = 0; i < items.Count; i++) {
						// Format the PascalCase with spaces
						string itemName = System.Text.RegularExpressions.Regex.Replace (items[i], "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]);
						itemName = itemName.Trim ();
						items[i] = itemName;
					}

					EditorCtrl.self.StartSelection (items.ToArray(), selected, delegate(int index) {
						onChanged ((T)System.Enum.Parse (typeof(T), items[index].Replace (" ", "")));
					});
				}
			}
			if (!skipHorizontal) GUILayout.EndHorizontal ();
		}

		private static void RenderName (string name, GUILayoutOption nameLayout = null)
		{
			RenderName (new GUIContent (name, ""), nameLayout);
		}

		private static void RenderName (GUIContent content, GUILayoutOption nameLayout = null)
		{
			if (!string.IsNullOrEmpty (content.text)) 
			{
				GUILayout.Space (2);
				if (nameLayout != null) GUILayout.Label (content, nameLayout);
				else 					GUILayout.Label (content);
			}
		}
	}
}
