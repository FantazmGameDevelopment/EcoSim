using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneData.Action;
using Ecosim.SceneData;
using Ecosim.SceneEditor;

namespace Ecosim.GameCtrl.GameButtons
{
	public class VariablesAndFormulasWindow : GameWindow
	{
		public readonly VariablesAndFormulas parent;
		public readonly Scene scene;

		private int textHeight = 32;
		private bool variablesOpened = true;
		private bool formulasOpened = true;

		private List<string> categories;
		// Key = category
		private Dictionary<string, List<Progression.VariableData>> variables;
		// Key = category
		private Dictionary<string, List<Progression.FormulaData>> formulas;
		private Dictionary<string, bool> openedStates;

		private Vector2 scrollPos;

		public VariablesAndFormulasWindow (VariablesAndFormulas parent) : base (-1, -1, 650, parent.iconTex)
		{
			this.parent = parent;
			this.scene = GameControl.self.scene;
			//textHeight = (int) formatted.CalcHeight (new GUIContent (ui.description), winWidth) + 4;

			// Setup fold states
			openedStates = new Dictionary<string, bool> ();

			// Get all variables
			variables = new Dictionary<string, List<Progression.VariableData>>();
			foreach (KeyValuePair<string, Progression.VariableData> pair in scene.progression.variablesData)
			{
				// Check if category exists
				if (!variables.ContainsKey (pair.Value.category)) {
					openedStates.Add (pair.Value.category, true);
					variables.Add (pair.Value.category, new List<Progression.VariableData> ());
				}

				// Add the variable to the list
				variables [pair.Value.category].Add (pair.Value);
			}

			// Get all formulas
			formulas = new Dictionary<string, List<Progression.FormulaData>> ();
			foreach (Progression.FormulaData fd in scene.progression.formulasData)
			{
				// Check if category exists
				if (!formulas.ContainsKey (fd.category)) {
					openedStates.Add (fd.category, true);
					formulas.Add (fd.category, new List<Progression.FormulaData> ());
				}

				// Add the formula to the list
				Progression.FormulaData copy = fd.Copy ();
				FormatFormulaBody (copy);
				formulas [fd.category].Add (copy);
			}

			// Categories
			categories = new List<string> ();
			categories.AddRange (variables.Keys);
			categories.AddRange (formulas.Keys);
			categories.Sort ();
		}
		
		public override void Render ()
		{
			Rect r = new Rect (xOffset + 65, yOffset, this.width - 65, 32);
			SimpleGUI.Label (r, "Variables and formulas", title);

			GUILayout.BeginArea (new Rect (xOffset, yOffset + 33, width, Mathf.Min (600f, Screen.height - (yOffset + 33))));
			{
				scrollPos = GUILayout.BeginScrollView (scrollPos);
				{
					// Variables
					if (variables.Count > 0)
					{
						if (GUILayout.Button ("VARIABLES", entry, GUILayout.MinHeight (32), GUILayout.MaxWidth (1500f))) {
							variablesOpened = !variablesOpened;
						}

						if (variablesOpened)
						{
							GUILayout.Space (1);
							foreach (string category in categories)
							{
								List<Progression.VariableData> vars;
								if (variables.TryGetValue (category, out vars))
								{
									// Toggle category
									bool opened = openedStates [category];
									if (category.Length > 0) {
										if (GUILayout.Button (category, entry, GUILayout.MaxWidth (1500f))) {
											opened = !opened;
											openedStates [category] = opened;
										}
									}
									GUILayout.Space (1);
									if (!opened) continue;

									// Show editable variables
									foreach (Progression.VariableData vd in vars)
									{
										RenderVariable (vd);
										GUILayout.Space (1);
									}
								}
							}
						}
					}

					// Formulas
					if (formulas.Count > 0)
					{
						if (GUILayout.Button ("FORMULAS", entry, GUILayout.MinHeight (32), GUILayout.MaxWidth (1500f))) {
							formulasOpened = !formulasOpened;
						}

						if (formulasOpened)
						{
							GUILayout.Space (1);
							foreach (string category in categories)
							{
								List<Progression.FormulaData> forms;
								if (formulas.TryGetValue (category, out forms))
								{
									// Toggle category
									bool opened = openedStates [category];
									if (category.Length > 0) {
										if (GUILayout.Button (category, entry, GUILayout.MinHeight (32), GUILayout.MaxWidth (1500f))) {
											opened = !opened;
											openedStates [category] = opened;
										}
									}
									GUILayout.Space (1);
									if (!opened) continue;

									// Show formula
									foreach (Progression.FormulaData fd in forms)
									{
										RenderFormula (fd);
										GUILayout.Space (1);
									}
								}
							}
						}
					}
				}
				GUILayout.EndScrollView ();
			}
			GUILayout.EndArea ();

			base.Render ();
		}

		void FormatFormulaBody (Progression.FormulaData fd)
		{
			string s = fd.body;
			List<string> names = new List<string> ();

			char splitchar = '"';
			int firstIndex = s.IndexOf (splitchar);
			while (true) 
			{
				try {
					int secondIndex = s.IndexOf (splitchar, firstIndex + 1);
					if (secondIndex >= 0) {
						names.Add (s.Substring (firstIndex, (secondIndex - firstIndex)+1));
						firstIndex = s.IndexOf (splitchar, secondIndex + 1);
					} else {
						break;
					}
				} catch {
					break;
				}
			}

			// Replace found names with variable data's name
			foreach (string name in names) {
				string trimmedName = name.Trim (splitchar);
				Progression.VariableData vd;
				if (scene.progression.variablesData.TryGetValue (trimmedName, out vd)) {
					// Replace
					fd.body = fd.body.Replace (name, vd.name);
				}
			}
		}

		void RenderFormula (Progression.FormulaData fd)
		{
			if (!fd.enabled) return;

			GUILayout.Label (fd.name, header, GUILayout.MinHeight (32), GUILayout.MaxWidth (1500));
			GUILayout.BeginVertical ();
			{
				GUILayout.Label (fd.body, textField, GUILayout.MaxWidth (1500));        
				GUILayout.Label ("", textField, GUILayout.MaxWidth (1500), GUILayout.Height (7));
			}
			GUILayout.EndVertical ();
		}

		void RenderVariable (Progression.VariableData vd)
		{
			if (!vd.enabled) return;

			// Check for list
			object val = scene.progression.variables [vd.variable];
			if (val is IList)
			{
				// List
				IList list = (IList)val;
				//GUILayout.Label (vd.name, header, GUILayout.MinHeight (32), GUILayout.MaxWidth (1500));
				//GUILayout.Space (1);

				bool listIsUpdated = false;
				int count = list.Count;
				for (int i = 0; i < count; i++) 
				{
					// Render and update
					/*GUILayout.BeginHorizontal ();
					{
						GUI.enabled = false;
						GUILayout.Label ("", textField, GUILayout.MinHeight (32), GUILayout.Width (100));
						GUI.enabled = true;
						GUILayout.Space (1);

						bool isUpdated;
						object prevVal = list [i];
						object newVal = RenderField (i.ToString (), prevVal, out isUpdated);
						if (isUpdated) {
							list [i] = newVal;
							listIsUpdated = true;
						}
					}
					GUILayout.EndHorizontal ();
					GUILayout.Space (1);*/

					// Render and update
					bool isUpdated;
					object prevVal = list [i];
					object newVal = RenderField (vd.name + " " + (i+1), prevVal, out isUpdated);
					if (isUpdated) {
						list [i] = newVal;
						listIsUpdated = true;
					}
					if (i < count - 1)
						GUILayout.Space (1);
				}

				if (listIsUpdated) {
					scene.progression.variables [vd.variable] = list;
				}
			}
			else
			{
				// Render and update
				bool isUpdated;
				object newVal = RenderField (vd.name, val, out isUpdated);
				if (isUpdated) scene.progression.variables [vd.variable] = newVal;
			};
		}

		object RenderField (string name, object val, out bool isUpdated)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label (name, header, GUILayout.MinHeight (32), GUILayout.MaxWidth (1500));
			GUILayout.Space (1);

			isUpdated = false;
			if (val is string)
				val = RenderStringField (val.ToString (), out isUpdated);
			else if (val is int)
				val = RenderIntField ((int)val, out isUpdated);
			else if (val is long)
				val = RenderLongField ((long)val, out isUpdated);
			else if (val is float)
				val = RenderFloatField ((float)val, out isUpdated);
			else if (val is bool)
				val = RenderBoolField ((bool)val, out isUpdated);
			else if (val is Coordinate) {
				val = RenderCoordinateField ((Coordinate)val, out isUpdated);
			}

			GUILayout.EndHorizontal ();
			return val;
		}

		string RenderStringField (string val, out bool isUpdated)
		{
			isUpdated = false;
			string newVal = GUILayout.TextField (val, textField, GUILayout.MinHeight (32), GUILayout.Width (100));
			isUpdated = (newVal != val);
			return newVal;
		}

		bool RenderBoolField (bool val, out bool isUpdated)
		{
			isUpdated = false;
			if (GUILayout.Button (val.ToString (), entry, GUILayout.MinHeight (32), GUILayout.Width (100))) {
				val = !val;
				isUpdated = true;
			}
			return val;
		}

		int RenderIntField (int val, out bool isUpdated)
		{
			isUpdated = false;
			string str = val.ToString ();
			string newStr = GUILayout.TextField (str, textField, GUILayout.MinHeight (32), GUILayout.Width (100));
			if (newStr != str) {
				if (newStr.Length == 0) newStr = "0";
				int newVal;
				if (int.TryParse (newStr, out newVal)) {
					val = newVal;
					isUpdated = true;
				}
			}
			return val;
		}

		long RenderLongField (long val, out bool isUpdated)
		{
			isUpdated = false;
			string str = val.ToString ();
			string newStr = GUILayout.TextField (str, textField, GUILayout.MinHeight (32), GUILayout.Width (100));
			if (newStr != str) {
				if (newStr.Length == 0) newStr = "0";
				long newVal;
				if (long.TryParse (newStr, out newVal)) {
					val = newVal;
					isUpdated = true;
				}
			}
			return val;
		}

		float RenderFloatField (float val, out bool isUpdated)
		{
			isUpdated = false;
			string str = val.ToString ();
			string newStr = GUILayout.TextField (str, textField, GUILayout.MinHeight (32), GUILayout.Width (100));
			if (newStr != str) {
				if (newStr.Length == 0) newStr = "0";
				float newVal;
				if (float.TryParse (newStr, out newVal)) {
					val = newVal;
					isUpdated = true;
				}
			}
			return val;
		}

		Coordinate RenderCoordinateField (Coordinate val, out bool isUpdated)
		{
			isUpdated = false;
			short x = val.x;
			short y = val.y;
			for (int i = 0; i < 2; i++)
			{
				bool isX = (i==0);
				string str = (isX?x:y).ToString ();
				GUILayout.Label ((isX)?" X":" Y", title, GUILayout.MinHeight (32), GUILayout.Width (32));
				GUILayout.Space (1);

				string newStr = GUILayout.TextField (str, textField, GUILayout.MinHeight (32), GUILayout.Width (50));
				if (newStr != str) {
					if (newStr.Length == 0) newStr = "0";
					short newVal;
					if (short.TryParse (newStr, out newVal)) {
						if (isX) x = newVal;
						else y = newVal;
						isUpdated = true;
					}
				}
				GUILayout.Space (1);
			}
			if (x != val.x || y != val.y) {
				val = new Coordinate (x, y);
				isUpdated = true;
			}
			return val;
		}
		
		protected override void OnClose ()
		{
			base.OnClose ();
		}
	}
}

