using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;

namespace Ecosim.SceneEditor
{
	public class VariablePanel : Panel
	{
		private Scene scene;
		private EditorCtrl ctrl;
		private Vector2 scrollPos;
//		private GUIStyle tabNormal;
//		private GUIStyle tabSelected;
		private string[] types = new string[] { "bool", "int", "long", "float", "string", "coord",
			"bool[]", "int[]", "long[]", "float[]", "string[]", "coord[]" };
		private List<string> reserved = new List<string> (new string[] { "if", "for", "foreach", "type", "string", "int",
			"long", "bool", "float", "double", "while", "break", "case", "else", "void",
			"private", "protected", "public", "year", "budget", "allowResearch", "allowMeasures", "startYear", "lastMeasure", "lastMeasureGroup", "lastMeasureCount",
			"lastResearch", "lastResearchGroup", "lastResearchCount" });
		int currentTypeIndex = 4;
		string newVarName = "";
		string newVarError = "";
		
		public List<string> keys;

		private bool showPresentValues;
		private bool formulasOpened = true;
		
		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			keys = new List<string> ();
//			tabNormal = ctrl.listItem;
//			tabSelected = ctrl.listItemSelected;
			if (scene == null)
				return;
			textFieldDict = new Dictionary<string, string> ();
			SetupTextFieldStrings ();	
		}
		
		void SetupTextFieldStrings ()
		{
			textFieldDict.Clear ();
			foreach (KeyValuePair<string, object> kv in scene.progression.variables) {
				if (Progression.predefinedVariables.Contains (kv.Key)) continue;
				object val = kv.Value;
				if (val is IList) {
					int i = 0;
					foreach (object o in ((IList) val)) {
						AddTextField (kv.Key, i++, o);
					}
				} else {
					AddTextField (kv.Key, -1, val);
				}
			}
			newVarError = "";
			
			keys.Clear ();
			keys.AddRange (scene.progression.variables.Keys);
			foreach (string k in Progression.predefinedVariables) {
				keys.Remove (k);
			}
			keys.Sort ();
		}
		
		void AddTextField (string name, int index, object val)
		{
			string str = null;
			if (index >= 0) {
				name = name + " " + index;
			}
			if (val is Coordinate) {
				str = ((Coordinate)val).x.ToString () + "," + ((Coordinate)val).y.ToString ();
			} else if (val is bool) {
				str = ((bool)val) ? "true" : "false";
			} else {
				str = val.ToString ();
			}
			if (textFieldDict.ContainsKey (name)) {
				textFieldDict [name] = str;
			} else {
				textFieldDict.Add (name, str);
			}
		}
		
		Dictionary<string, string> textFieldDict;
		
		void EditField (object val, string name, int index)
		{
			string dictName = (index >= 0) ? (name + " " + index) : name;
			string dictVal = textFieldDict [dictName];
			string newVal = GUILayout.TextField (dictVal, GUILayout.Width (120));
			if (newVal != dictVal) {
				newVarError = "";
				// try to update original value
				if (val is string) {
					val = newVal;
				} else if (val is int) {
					int outNr;
					if (int.TryParse (newVal, out outNr)) {
						val = outNr;
					}
				} else if (val is long) {
					long outNr;
					if (long.TryParse (newVal, out outNr)) {
						val = outNr;
					}
				} else if (val is float) {
					float outNr;
					if (float.TryParse (newVal, out outNr)) {
						val = outNr;
					}
				} else if (val is bool) {
					val = ((newVal.ToLower () == "true") || (newVal.ToLower () == "yes"));
				}
				if (index < 0) {
					scene.progression.variables [name] = val;
				} else {
					IList list = (IList)(scene.progression.variables [name]);
					list [index] = val;
				}
				textFieldDict [dictName] = newVal;
			}
		}
		
		string DisplayType (object obj)
		{
			if (obj is bool)
				return "bool";
			else if (obj is int)
				return "int";
			else if (obj is long)
				return "long";
			else if (obj is float)
				return "float";
			else if (obj is string)
				return "string";
			else if (obj is Coordinate)
				return "coord";
			else if (obj is List<bool>)
				return "bool[]";
			else if (obj is List<int>)
				return "int[]";
			else if (obj is List<long>)
				return "long[]";
			else if (obj is List<float>)
				return "float[]";
			else if (obj is List<string>)
				return "string[]";
			else if (obj is List<Coordinate>)
				return "coord[]";
			return "unknown!";
		}
		
		void AddToList (IList list)
		{
			if (list is List<bool>) {
				list.Add (false);
			} else if (list is List<int>) {
				list.Add (0);
			} else if (list is List<long>) {
				list.Add (0L);
			} else if (list is List<float>) {
				list.Add (0.0f);
			} else if (list is List<string>) {
				list.Add ("");
			} else if (list is List<Coordinate>) {
				list.Add (new Coordinate (0, 0));
			}
		}
		
		public bool Render (int mx, int my)
		{
			// Show present(ation) values, name etc.
			GUILayout.BeginHorizontal ();//ctrl.skin.box);
			{
				if (EcoGUI.Toggle ("Show variables and formulas in-game", ref scene.progression.showVariablesInGame))
				{
					if (GUILayout.Button (((showPresentValues)?"Hide":"Show") + " data", GUILayout.Width (100))) {
						showPresentValues = !showPresentValues;
					}
				}
				else showPresentValues = false;
			}
			GUILayout.EndVertical ();
			GUILayout.Space (5f);

			// Variables
			if (showPresentValues) {
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("<b> variable</b>", GUILayout.MinWidth (100), GUILayout.MaxWidth (150));
					GUILayout.Label ("<b> name</b>", GUILayout.Width (100));
					GUILayout.Label ("<b> category</b>", GUILayout.Width (100));
					GUILayout.Label ("<b> show</b>", GUILayout.Width (35));
				}
				GUILayout.EndHorizontal ();
			}

			ManagedDictionary<string, object> variables = scene.progression.variables;
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			GUILayout.BeginVertical ();

			foreach (string key in keys) 
			{
				GUILayout.BeginHorizontal ();

				GUILayout.Label (key, GUILayout.MinWidth (100), GUILayout.MaxWidth (150));
				object val = variables [key];
				if (!showPresentValues) {
					GUILayout.Label ("<b>" + DisplayType (val) + "</b>", GUILayout.Width (40));
					if (val is IList) {	
						IList list = (IList)val;
						if (GUILayout.Button ("+")) {
							AddToList (list);
							SetupTextFieldStrings ();
							break;
						}
						if (GUILayout.Button ("-", GUILayout.Width (20))) {
							variables.Remove (key);
							SetupTextFieldStrings ();
							break;
						}
						GUILayout.FlexibleSpace ();
						GUILayout.EndHorizontal ();
						for (int i = 0; i < list.Count; i++) {
							GUILayout.BeginHorizontal ();
							GUILayout.Label ("", GUILayout.Width (150));
							GUILayout.Label (i.ToString (), GUILayout.Width (40));
							EditField (list [i], key, i);
							GUILayout.FlexibleSpace ();
							if (GUILayout.Button ("-", GUILayout.Width (20))) {
								list.RemoveAt (i);
								SetupTextFieldStrings ();
								return true; // ugly, but can't break out of 2 loops
							}
							GUILayout.EndHorizontal ();
						}
					} else {
						EditField (val, key, -1);
						GUILayout.FlexibleSpace ();
						if (GUILayout.Button ("-", GUILayout.Width (20))) {
							variables.Remove (key);
							SetupTextFieldStrings ();
							break;
						}
						GUILayout.EndHorizontal ();
					}
				} else {

					Progression.VariableData vd = null;
					if (!scene.progression.variablesData.ContainsKey (key)) {
						vd = new Progression.VariableData (key, key, "");
						scene.progression.variablesData.Add (key, vd);
					} else vd = scene.progression.variablesData [key];

					GUI.enabled = vd.enabled;

					// Name and category
					vd.name = GUILayout.TextField (vd.name, GUILayout.Width (100));
					vd.category = GUILayout.TextField (vd.category, GUILayout.Width (100));

					GUI.enabled = true;

					// Enabled
					EcoGUI.Toggle ("", ref vd.enabled);

					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
				}
			}

			GUILayout.Space (8);

			// New variable
			GUILayout.BeginHorizontal ();
			newVarName = GUILayout.TextField (newVarName, GUILayout.Width (100));
			newVarName = newVarName.Replace (" ", "");
			if (GUILayout.Button (types [currentTypeIndex], GUILayout.Width (40))) {
				ctrl.StartSelection (types, currentTypeIndex, newIndex => {
					currentTypeIndex = newIndex;
				});
			}
			if (GUILayout.Button ("Create")) {
				newVarName = newVarName.Trim ();
				if (!StringUtil.IsValidID (newVarName)) {
					newVarError = "'" + newVarName + "' is not a valid variable identifier";
				} else if (scene.progression.variables.ContainsKey (newVarName)) {
					newVarError = "'" + newVarName + "' is already used";
				} else if (reserved.Contains (newVarName)) {
					newVarError = "'" + newVarName + "' is a reserved keyword";
				} else {
					switch (currentTypeIndex) {
					case 0 :
						variables.Add (newVarName, false);
						break;
					case 1 :
						variables.Add (newVarName, 0);
						break;
					case 2 :
						variables.Add (newVarName, 0L);
						break;
					case 3 :
						variables.Add (newVarName, 0.0f);
						break;
					case 4 :
						variables.Add (newVarName, "");
						break;
					case 5 :
						variables.Add (newVarName, new Coordinate (0, 0));
						break;
					case 6 :
						variables.Add (newVarName, new List<bool> ());
						break;
					case 7 :
						variables.Add (newVarName, new List<int> ());
						break;
					case 8 :
						variables.Add (newVarName, new List<long> ());
						break;
					case 9 :
						variables.Add (newVarName, new List<float> ());
						break;
					case 10 :
						variables.Add (newVarName, new List<string> ());
						break;
					case 11 :
						variables.Add (newVarName, new List<Coordinate> ());
						break;
					}
					SetupTextFieldStrings ();
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.Space (8);
			if (!string.IsNullOrEmpty (newVarError)) {
				GUILayout.Label (newVarError);
				//GUILayout.FlexibleSpace ();
				GUILayout.Space (10);
			}
			GUILayout.EndVertical ();

			// Formulas
			if (showPresentValues) 
			{
				GUILayout.BeginVertical (ctrl.skin.box);
				{
					EcoGUI.Foldout ("Formulas", ref formulasOpened);
					GUILayout.Space (2);

					if (formulasOpened)
					{
						if (scene.progression.formulasData.Count > 0)
						{
							GUILayout.BeginHorizontal ();
							{
								GUILayout.Space (20);
								GUILayout.Label ("<b> name</b>", GUILayout.Width (125));
								GUILayout.Label ("<b>category</b>", GUILayout.Width (125));
								GUILayout.FlexibleSpace ();
								GUILayout.Label ("<b>show</b>", GUILayout.Width (30));
								GUILayout.Space (40);
							}
							GUILayout.EndHorizontal ();
							GUILayout.Space (2);

							for (int i = 0; i < scene.progression.formulasData.Count; i++)
							{
								Progression.FormulaData fd = scene.progression.formulasData [i];
								GUILayout.BeginVertical (ctrl.skin.box);
								{
									GUILayout.BeginHorizontal ();
									{
										GUI.enabled = fd.enabled;

										EcoGUI.skipHorizontal = true;
										EcoGUI.Foldout ("", ref fd.opened, GUILayout.Width (20));
										EcoGUI.skipHorizontal = false;

										fd.name = GUILayout.TextField (fd.name, GUILayout.Width (125));
										fd.category = GUILayout.TextField (fd.category, GUILayout.Width (125));

										GUI.enabled = true;
										GUILayout.FlexibleSpace ();
										EcoGUI.Toggle ("", ref fd.enabled, 20);

										// Up
										GUI.enabled = (i > 0);
										if (GUILayout.Button ("\u02C4", GUILayout.Width (20))) {
											scene.progression.formulasData.Remove (fd);
											scene.progression.formulasData.Insert (i - 1, fd);
										}
										GUI.enabled = true;

										if (GUILayout.Button ("-", GUILayout.Width (20))) {
											Progression.FormulaData tmp = fd;
											ctrl.StartDialog (string.Format ("Are you sure you want to delete formula '{0}'", tmp.name), delegate {
												scene.progression.formulasData.Remove (tmp);
											}, null);	 
										}
									}
									GUILayout.EndHorizontal ();
									GUILayout.Space (3);

									if (fd.enabled && fd.opened) 
									{
										fd.body = GUILayout.TextArea (fd.body);
										GUILayout.Space (2);
									}
								}
								GUILayout.EndVertical ();
							}
						}
						GUILayout.Space (5);
						if (GUILayout.Button ("New formula", GUILayout.Width (100)))
						{
							Progression.FormulaData fd = new Progression.FormulaData ("Formula name", "Formula Category", "Formula Body");
							fd.opened = true;
							scene.progression.formulasData.Add (fd);
						}
					}
				}
				GUILayout.EndVertical ();
			}

			GUILayout.EndScrollView ();
			return true;
		}
		
		public void RenderExtra (int mx, int my)
		{
		}

		public void RenderSide (int mx, int my)
		{
		}

		public bool NeedSidePanel ()
		{
			return false;
		}

		public bool IsAvailable ()
		{
			return (scene != null);
		}
		
		public void Activate ()
		{
			textFieldDict.Clear ();
			SetupTextFieldStrings ();
		}
		
		public void Deactivate ()
		{
		}

		public void Update ()
		{
		}
		
	}
}
