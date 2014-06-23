using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleParameters : ParameterPaintPanel
	{
		private string[] parameters;
		private string[] bitSizeStings = new string[] { "1 bit (0-1)", "2 bit (0-3)", "4 bit (0-15)", "8 bit (0-255)", "Sparse 1-bit", "Sparse 8-bit", "TextMarkers", "Calculated" };
		private int bitSizeIndex = 3;

		private string newParamName = "Parameter";
		private int activeParameter = 0;
		private Dictionary<CalculatedData.Calculation.ParameterCalculation, int> calculationActiveParameters;
		private Dictionary<CalculatedData.Calculation, bool> calculatedDataOpenStates;

		// When onlyEditThisData is set, the user isn't given a choice to select
		// which data to edit, but can only edit this data.
		private Data onlyEditThisData = null;
		private bool calcParamsOpen = false;
		private Vector2 calcParamsScrollPos = Vector2.zero;
		
		public HandleParameters (EditorCtrl ctrl, MapsPanel parent, Scene scene, Data data) : base (ctrl, parent, scene)
		{
			this.scene = scene;
			onlyEditThisData = data;
			
			if (scene != null) {
				List<string> pList = new List<string> ();
				foreach (string p in scene.progression.GetAllDataNames(false)) {
					Data dataFindNames = scene.progression.GetData (p);
					if ((dataFindNames.GetMax() < 256))
						pList.Add (p);
				}

				parameters = pList.ToArray ();
				activeParameter = 0;
			}

			calculationActiveParameters = new Dictionary<CalculatedData.Calculation.ParameterCalculation, int>();
			calculatedDataOpenStates = new Dictionary<CalculatedData.Calculation, bool>();

			Setup ("parameter");
		}

		protected override void Setup (string editDataParamName)
		{
			data = (onlyEditThisData != null) ? onlyEditThisData :
				(scene.progression.GetData (parameters [activeParameter]));
			maxParamValue = data.GetMax ();

			base.Setup (editDataParamName);
		}
				
		public override bool Render (int mx, int my)
		{
			Data currentData = (onlyEditThisData != null)?onlyEditThisData:
				(scene.progression.GetData (parameters [activeParameter]));

			if (onlyEditThisData == null) 
			{
				GUILayout.BeginHorizontal ();
				{
					if (GUILayout.Button (parameters [activeParameter], GUILayout.Width (140))) 
					{
						ctrl.StartSelection (parameters, activeParameter,
						newIndex => {
							if (newIndex != activeParameter && edit != null) {
								activeParameter = newIndex;
								data = scene.progression.GetData (parameters [activeParameter]);
								edit.SetData (data);
								maxParamValue = data.GetMax ();
								paramStrength = maxParamValue;
								paramStrengthStr = paramStrength.ToString ();
							}
						});
					}
					string typeName = currentData.GetType().ToString();
					typeName = typeName.Substring(typeName.LastIndexOf('.') + 1);
					GUILayout.Label (string.Format("Range: 0-{0} ({1})", maxParamValue, typeName));
					GUILayout.FlexibleSpace ();
					if ((parameters.Length > 1) && GUILayout.Button ("Delete")) 
					{
						ctrl.StartDialog ("Delete '" + parameters [activeParameter] + "'?",
					actionResult => {
							// Check for calculated data
							Data data = scene.progression.GetData (parameters [activeParameter]);
							if (data is CalculatedData) 
							{
								CalculatedData calcData = (CalculatedData)data;
								List<CalculatedData.Calculation> calculations = new List<CalculatedData.Calculation>(scene.calculations);
								calculations.Remove (calcData.calculation);
								scene.calculations = calculations.ToArray ();
								calcData.calculation = null;
							}
							scene.progression.DeleteData (parameters [activeParameter]);
							List<string> parametersList = new List<string> (parameters);
							parametersList.RemoveAt (activeParameter);
							parameters = parametersList.ToArray ();
							activeParameter = 0;
							this.data = scene.progression.GetData (parameters [activeParameter]);
							edit.SetData (data);
						}, null);
					}
				}
				GUILayout.EndHorizontal ();
			}

			GUILayout.Space (5);
			this.RenderBrushMode ();
			GUILayout.Space (5);
			this.RenderSaveRestoreApply ();
			GUILayout.Space (5);
			this.RenderFromImage (parameters[activeParameter]);

			if (onlyEditThisData == null) 
			{
				// Create parameter
				GUILayout.Space (5);
				GUILayout.BeginHorizontal (ctrl.skin.box);
				{
					GUILayout.Label ("Create parameter", GUILayout.Width (100));
					if (GUILayout.Button (bitSizeStings [bitSizeIndex], tabNormal, GUILayout.Width (120))) 
					{
						ctrl.StartSelection (bitSizeStings, bitSizeIndex,
						newIndex => { 
							bitSizeIndex = newIndex;
						});
					}
					newParamName = GUILayout.TextField (newParamName, GUILayout.Width (80));
					if (GUILayout.Button ("Create")) 
					{
						newParamName = newParamName.Trim ();
						if (!StringUtil.IsValidID (newParamName)) {
							ctrl.StartOkDialog ("Not a valid parameter name ([a-zA-Z][a-zA-Z0-9_]*)", null);
						} else if (scene.progression.HasData (newParamName)) {
							ctrl.StartOkDialog ("Name already exists", null);
						} else {
							data = null;
							switch (bitSizeIndex) {
							case 0 :
								data = new BitMap1 (scene);
								break;
							case 1 :
								data = new BitMap2 (scene);
								break;
							case 2 :
								data = new BitMap4 (scene);
								break;
							case 3 :
								data = new BitMap8 (scene);
								break;
							case 4 :
								data = new SparseBitMap1 (scene);
								break;
							case 5 :
								data = new SparseBitMap8 (scene);
								break;
							case 6 :
								data = new TextBitMap (scene);
								break;
							case 7 :
								data = new CalculatedData (scene, newParamName);
								// Create a new calculation entry and add it to the list
								List<CalculatedData.Calculation> calculations = new List<CalculatedData.Calculation>(scene.calculations);
								CalculatedData.Calculation calc = new CalculatedData.Calculation (newParamName);
								calc.data = data as CalculatedData;
								((CalculatedData)data).calculation = calc;
								calculations.Add (calc);
								scene.calculations = calculations.ToArray();
								break;
							}
							scene.progression.AddData (newParamName, data);
							List<string> paramNameList = new List<string> (parameters);
							paramNameList.Add (newParamName);
							parameters = paramNameList.ToArray ();
							activeParameter = parameters.Length - 1;
							edit.SetData (data);
							maxParamValue = data.GetMax ();
							paramStrength = maxParamValue;
							paramStrengthStr = paramStrength.ToString ();
						}
					}
				}
				GUILayout.EndHorizontal ();
			}

			if (onlyEditThisData == null) 
			{
				// Calculated (combined) parameters
				GUILayout.Space (5);
				GUILayout.BeginVertical (ctrl.skin.box);
				{
					GUILayout.BeginHorizontal ();
					if (GUILayout.Button (calcParamsOpen ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
					{
						calcParamsOpen = !calcParamsOpen;
					}
					GUILayout.Label (" Calculated parameters");
					GUILayout.EndHorizontal ();
					GUILayout.Space (2);

					if (calcParamsOpen)
					{
						if (scene.calculations.Length > 0)
						{
							calcParamsScrollPos = GUILayout.BeginScrollView (calcParamsScrollPos);
							{
								foreach (CalculatedData.Calculation c in scene.calculations)
								{
									GUILayout.BeginVertical (ctrl.skin.box);
									{
										// Check if we have an entry
										bool calcOpened = false;
										if (!calculatedDataOpenStates.TryGetValue (c, out calcOpened)) {
											calculatedDataOpenStates.Add (c, calcOpened);
										}

										GUILayout.BeginHorizontal ();
										{
											if (GUILayout.Button (calcOpened ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
											{
												calcOpened = !calcOpened;
												calculatedDataOpenStates[c] = calcOpened;
											}
											GUILayout.Label (" " + c.paramName);
										}
										GUILayout.EndHorizontal ();
										GUILayout.Space (2);

										if (calcOpened)
										{
											GUILayout.Space (3);
											EcoGUI.IntField (" Offset:", ref c.offset, GUILayout.Width (40), GUILayout.Width (50));

											foreach (CalculatedData.Calculation.ParameterCalculation p in c.calculations)
											{
												GUILayout.BeginHorizontal ();
												{
													int paramIndex = 0;
													if (!calculationActiveParameters.TryGetValue (p, out paramIndex)) {

														foreach (string parameter in parameters) {
															if (parameter == p.paramName) break;
															paramIndex++;
														}
														calculationActiveParameters.Add (p, paramIndex);
													}

													GUILayout.Space (10);
													GUILayout.Label ("+ (", GUILayout.Width (12));
													EcoGUI.FloatField ("", ref p.multiplier, 2, null, GUILayout.Width (50));
													GUILayout.Label ("*", GUILayout.Width (5));

													if (GUILayout.Button (parameters[paramIndex], GUILayout.MaxWidth (550))) 
													{
														CalculatedData.Calculation tmpCC = c;
														ctrl.StartSelection (parameters, paramIndex,
														newIndex => {
															if (newIndex != paramIndex) {
																// Check if we didn't choose an calculated parameter
																/*Data newData = scene.progression.GetData (parameters[newIndex]);
																if (newData is CalculatedData) {
																	ctrl.StartOkDialog ("You're not allowed to refer to other Calculated Data parameters within a Calculated Data parameter.", null);
																} else {
																	calculationActiveParameters[p] = newIndex;
																	p.paramName = parameters[newIndex];
																}*/ 
																if (parameters[newIndex] == tmpCC.paramName) {
																	ctrl.StartOkDialog ("You're not allowed to refer to the same Calculated Data parameter this calculation belongs to.", null);
																} else {
																	calculationActiveParameters[p] = newIndex;
																	p.paramName = parameters[newIndex];
																}
															}
														});
														break;
													}

													GUILayout.Label (")", GUILayout.Width (5));

													if (GUILayout.Button ("-", GUILayout.Width (20)))
													{
														List<CalculatedData.Calculation.ParameterCalculation> list = new List<CalculatedData.Calculation.ParameterCalculation>(c.calculations);
														list.Remove (p);
														c.calculations = list.ToArray ();
														break;
													}
												}
												GUILayout.EndHorizontal ();
											}

											if (GUILayout.Button ("+", GUILayout.Width (20))) 
											{
												List<CalculatedData.Calculation.ParameterCalculation> list = new List<CalculatedData.Calculation.ParameterCalculation>(c.calculations);

												// Get a random parameter
												string parameterName = "";
												foreach (string param in parameters) {
													if (param != c.paramName) {
														parameterName = param;
														break;
													}
												}
												list.Add (new CalculatedData.Calculation.ParameterCalculation( parameterName ));
												list [list.Count - 1].data = scene.progression.GetData (parameterName);
												c.calculations = list.ToArray ();
											}
										}
									}	
									GUILayout.EndVertical ();
								}
							}
							GUILayout.EndScrollView ();
						}
						else {
							GUILayout.Label (" No Calculated Data parameters found.");
						}
					}
				}
				GUILayout.EndVertical ();
			}
			return false;
		}

		delegate int ProcessParamFnXY (int x,int y,int val,int changeVal);

		void ApplyParamFnXY (ProcessParamFnXY fn, Data param)
		{
			BitMap8 data = new BitMap8 (scene);
			edit.CopyData (data);
			byte[] changeVals = data.data;
			
			int minX = scene.width;
			int maxX = 0;
			int minY = scene.height;
			int maxY = 0;
			
			int p = 0;
			int minVal = param.GetMin ();
			int maxVal = param.GetMax ();
			
			for (int y = 0; y < scene.height; y++) {
				for (int x = 0; x < scene.height; x++) {
					int changeVal = changeVals [p];
					int val = param.Get (x, y);
					int newVal = fn (x, y, val, changeVal);
					if (newVal != val) {
						param.Set (x, y, Mathf.Clamp (newVal, minVal, maxVal));
						minX = Mathf.Min (x, minX);
						minY = Mathf.Min (y, minY);
						maxX = Mathf.Max (x, maxX);
						maxY = Mathf.Max (y, maxY);
					}
					p++;
				}
			}
			if (minX < maxX) {
				edit.SetData (data);
			}
		}
	}
}
