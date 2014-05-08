using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.PlantRules;
using Ecosim.SceneData.Rules;
using UnityEngine;

namespace Ecosim.SceneEditor
{
	public class PlantRulesExtraPanel : ExtraPanel
	{
		Dictionary<PlantRule, bool> plantRuleFoldStates;
		Dictionary<SuccessionType, string[]> vegetations;

		EditorCtrl ctrl;
		PlantType plant;
		Vector2 scrollPos;
		string[] parameters;
		string[] successions;

		private static PlantRule[] copyBuffer;

		public static void ClearCopyBuffer() 
		{
			copyBuffer = null;
		}

		public PlantRulesExtraPanel (EditorCtrl ctrl, PlantType plant) 
		{
			this.ctrl = ctrl;
			this.plant = plant;

			plantRuleFoldStates = new Dictionary<PlantRule, bool>();
			foreach (PlantRule r in this.plant.rules) {
				plantRuleFoldStates.Add (r, false);
			}

			vegetations = new Dictionary<SuccessionType, string[]>();

			List<string> pList = new List<string> ();
			foreach (string p in ctrl.scene.progression.GetAllDataNames()) {
				Data dataFindNames = ctrl.scene.progression.GetData (p);
				if ((dataFindNames.GetMax () < 256) && (!p.StartsWith ("_")))
					pList.Add (p);
			}
			parameters = pList.ToArray ();

			RetrieveSuccessions ();
		}

		private void RetrieveSuccessions()
		{
			if (successions == null || successions.Length != ctrl.scene.successionTypes.Length)
			{
				List<string> sList = new List<string>();
				sList.Add ("<Any succession>");
				foreach (SuccessionType st in ctrl.scene.successionTypes) {
					sList.Add (st.name);
					if (!vegetations.ContainsKey(st)) vegetations.Add (st, null);
				}
				successions = sList.ToArray();
			}

			foreach (SuccessionType st in ctrl.scene.successionTypes) {
				if (vegetations [st] == null || st.vegetations.Length != vegetations [st].Length - 1) {
					List<string> vList = new List<string>();
					vList.Add ("<Any vegetation>");
					foreach (VegetationType vt in st.vegetations) {
						vList.Add (vt.name);
					}
					vegetations [st] = vList.ToArray();
				}
			}
		}

		private int FindParamIndex (string paramName)
		{
			for (int i = 0; i < parameters.Length; i++) {
				if (parameters [i] == paramName)
					return i;
			}
			return 0;
		}

		private SuccessionType FindSuccessionType (int index)
		{
			for (int i = 0; i < ctrl.scene.successionTypes.Length; i++)
			{
				if (ctrl.scene.successionTypes[i].index == index)
					return ctrl.scene.successionTypes[i];
			}
			return null;
		}

		public void DeleteRule (PlantRule rule)
		{
			List<PlantRule> rules = new List<PlantRule>(plant.rules);
			rules.Remove (rule);
			plant.rules = rules.ToArray();
			plantRuleFoldStates.Remove (rule);
			rule = null;
		}

		public bool Render (int mx, int my)
		{
			bool keepOpen = true;
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (ctrl.foldedOpen, ctrl.icon)) 
			{
				keepOpen = false;
			}

			GUILayout.Label ("Rules", GUILayout.Width (100));
			GUILayout.Label (plant.name);
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal (); // Copy
			{
				if (GUILayout.Button ("Copy")) 
				{
					copyBuffer = new PlantRule [plant.rules.Length];
					for (int i = 0; i < copyBuffer.Length; i++) {
						copyBuffer[i] = (PlantRule) plant.rules[i].Clone();
					} 
				}
				if ((copyBuffer != null) && GUILayout.Button ("Paste (add)")) 
				{
					List<PlantRule> rulesList = new List<PlantRule>(plant.rules);
					for (int i = 0; i < copyBuffer.Length; i++) {
						rulesList.Add((PlantRule)copyBuffer[i].Clone());
					}
					plant.rules = rulesList.ToArray();
				}
				if ((copyBuffer != null) && GUILayout.Button ("Paste (replace)")) 
				{
					plant.rules = new PlantRule[copyBuffer.Length];
					for (int i = 0; i < plant.rules.Length; i++) {
						plant.rules[i] = (PlantRule)copyBuffer[i].Clone();
					}
				}
				if ((copyBuffer != null) && GUILayout.Button ("Clear"))
				{
					ClearCopyBuffer ();
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~Copy

			// Check lists
			RetrieveSuccessions ();

			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			{
				//GUILayout.BeginVertical (ctrl.skin.box); // Rules
				{
					//GUILayout.Label ("Rules", GUILayout.Width (100));

					// Rules
					foreach (PlantRule pr in plant.rules)
					{
						GUILayout.BeginVertical (ctrl.skin.box); // Rule
						{
							GUILayout.BeginHorizontal (); // Description
							{
								if (!plantRuleFoldStates.ContainsKey (pr)) plantRuleFoldStates.Add (pr, true);
								if (GUILayout.Button (plantRuleFoldStates[pr] ? (ctrl.foldedCloseSmall) : (ctrl.foldedOpenSmall), ctrl.icon12x12)) 
								{
									plantRuleFoldStates[pr] = !plantRuleFoldStates[pr];
								}

								GUILayout.Label ("Description", GUILayout.Width (80));
								pr.description = GUILayout.TextField (pr.description);

								if (GUILayout.Button("-", GUILayout.Width(20)))
								{
									PlantRule tmpPR = pr;
									ctrl.StartDialog (string.Format("Delete rule ({0})?", tmpPR.description), newVal => { 
										DeleteRule (tmpPR);
									}, null);
								}
							}
							GUILayout.EndHorizontal (); //~Description

							if (!plantRuleFoldStates[pr])
							{
								GUILayout.BeginHorizontal (); // Chance
								{
									GUILayout.Label ("Chance (0..1)", GUILayout.Width (70));
									pr.chance = GUILayout.HorizontalSlider (pr.chance, 0.0f, 1.0f, GUILayout.Width (70));
									string chanceStr = pr.chance.ToString ("0.00");
									string newChanceStr = GUILayout.TextField (chanceStr, GUILayout.Width (40));
									if (newChanceStr != chanceStr) {
										float outVal;
										if (float.TryParse (newChanceStr, out outVal)) {
											pr.chance = outVal;
										}
									}
								}
								//GUILayout.EndHorizontal (); // ~Chance
								//GUILayout.BeginHorizontal (); // Delta
								{
									GUILayout.Label ("Change", GUILayout.Width (40));
									pr.delta = (int)GUILayout.HorizontalSlider (pr.delta, -plant.maxPerTile, plant.maxPerTile, GUILayout.Width (70));
									string deltaStr = pr.delta.ToString ("0");
									string newDeltaStr = GUILayout.TextField (deltaStr, GUILayout.Width (40));
									if (newDeltaStr != deltaStr) {
										int outVal;
										if (int.TryParse (newDeltaStr, out outVal)) {
											pr.delta = outVal;
										}
									}
								}
								GUILayout.EndHorizontal (); // ~Delta
								
								GUILayout.BeginHorizontal (); // Can spawn
								{
									pr.canSpawn = GUILayout.Toggle (pr.canSpawn, "Can spawn seedlings", GUILayout.Width (150));
								}
								GUILayout.EndHorizontal (); // ~Can spawn

								GUILayout.BeginVertical (ctrl.skin.box); // Vegetation conditions
								{
									GUILayout.BeginHorizontal (); // Header
									{
										GUILayout.Label ("Vegetation conditions");
										GUILayout.FlexibleSpace();

										if (GUILayout.Button ("+", GUILayout.Width (20)))
										{
											List<PlantRule.VegetationCondition> vcList = new List<PlantRule.VegetationCondition> (pr.vegetationConditions);
											PlantRule.VegetationCondition newVC = new PlantRule.VegetationCondition (-1, -1);
											vcList.Add (newVC);
											pr.vegetationConditions = vcList.ToArray();
										}
									}
									GUILayout.EndHorizontal (); //~Header

									foreach (PlantRule.VegetationCondition vc in pr.vegetationConditions)
									{
										GUILayout.BeginVertical (ctrl.skin.box);
										GUILayout.BeginHorizontal ();
										{
											// Succession, -1 means any succession so we show the index + 1
											if (GUILayout.Button (successions[vc.successionIndex + 1], GUILayout.Width(150)))
											{
												PlantRule.VegetationCondition tmpVC = vc;
												ctrl.StartSelection (successions, tmpVC.successionIndex + 1, 
													newIndex => {
														tmpVC.successionIndex = newIndex - 1;
														tmpVC.vegetationIndex = -1;
												});
											}

											// Vegetation
											if (vc.successionIndex >= 0) 
											{
												string[] vegs = vegetations [FindSuccessionType(vc.successionIndex)];
												if (GUILayout.Button (vegs[vc.vegetationIndex + 1], GUILayout.Width (150)))
												{
													PlantRule.VegetationCondition tmpVC = vc;
													ctrl.StartSelection (vegs, tmpVC.vegetationIndex + 1, 
													    newIndex => {
															tmpVC.vegetationIndex = newIndex - 1;
													});
												}
											}

											GUILayout.FlexibleSpace();
											if (GUILayout.Button ("-", GUILayout.Width (20))) 
											{
												List<PlantRule.VegetationCondition> vcList = new List<PlantRule.VegetationCondition>(pr.vegetationConditions);
												vcList.Remove(vc);
												pr.vegetationConditions = vcList.ToArray();
												break;
											}
										}
										GUILayout.EndHorizontal ();
										GUILayout.EndVertical ();
									}
								}
								GUILayout.EndVertical (); //~Vegetation conditions

								GUILayout.BeginVertical (ctrl.skin.box); // Parameter conditions
								{
									GUILayout.BeginHorizontal (); // Header
									{
										GUILayout.Label ("Parameter conditions");
										GUILayout.FlexibleSpace();

										if (GUILayout.Button ("+", GUILayout.Width (20)))
										{
											List<ParameterRange> prList = new List<ParameterRange> (pr.parameterConditions);
											ParameterRange newPR = new ParameterRange ();
											newPR.paramName = parameters [0];
											newPR.data = ctrl.scene.progression.GetData (newPR.paramName);
											newPR.lowRange = 0;
											newPR.highRange = newPR.data.GetMax();
											prList.Add (newPR);
											pr.parameterConditions = prList.ToArray();
										}
									}
									GUILayout.EndHorizontal (); //~Header

									foreach (ParameterRange pc in pr.parameterConditions)
									{
										GUILayout.BeginVertical (ctrl.skin.box);
										GUILayout.BeginHorizontal ();
										{
											if (GUILayout.Button (pc.paramName, GUILayout.Width (120)))
											{
												ctrl.StartSelection (parameters, FindParamIndex (pc.paramName), 
												                     newIndex => {
													pc.paramName = parameters[newIndex];
													pc.data = ctrl.scene.progression.GetData (pc.paramName);
												});
											}
											
											int min = pc.data.GetMin();
											int max = pc.data.GetMax();

											// Check for percentages
											if (pc.lowRangePerc > -1f) {
												pc.lowRange = (int)((float)max * pc.lowRangePerc);
												pc.lowRangePerc = -1f;
											}
											if (pc.highRangePerc > -1f) {
												pc.highRange = (int)((float)max * pc.highRangePerc);
												pc.highRangePerc = -1f;
											}

											float minPerc = (float)pc.lowRange / (float)max;
											float maxPerc = (float)pc.highRange / (float)max;
											
											//GUILayout.Label (string.Format("({0}-{1})", min, max), GUILayout.Width (40));
											
											// Min perc
											GUILayout.Space(2);
											GUILayout.Label (minPerc.ToString("0.00"), GUILayout.Width (25));
											pc.lowRange = (int)GUILayout.HorizontalSlider (pc.lowRange, min, max, GUILayout.Width (62));
											
											// Max perc
											pc.highRange = (int)GUILayout.HorizontalSlider (pc.highRange, min, max, GUILayout.Width (62));
											GUILayout.Label (maxPerc.ToString("0.00"), GUILayout.Width (25));
											
											// Make the ranges correct
											if (pc.lowRange > pc.highRange) pc.highRange = pc.lowRange;
											if (pc.highRange > max) pc.highRange = max;

											GUILayout.FlexibleSpace();
											if (GUILayout.Button ("-", GUILayout.Width (20))) 
											{
												List<ParameterRange> prList = new List<ParameterRange> (pr.parameterConditions);
												prList.Remove (pc);
												pr.parameterConditions = prList.ToArray ();
												break;
											}
										}
										GUILayout.EndHorizontal ();
										GUILayout.EndVertical ();
									}
								}
								GUILayout.EndVertical (); //~Parameter conditions 
							}
						}
						GUILayout.EndVertical (); //~Rule
					}

					GUILayout.BeginHorizontal ();
					if (GUILayout.Button ("+", GUILayout.Width (20))) {
						PlantRule newR = new PlantRule ();
						List<PlantRule> list = new List<PlantRule> (plant.rules);
						list.Add (newR);
						newR.description = "Rule " + list.Count;
						plant.rules = list.ToArray ();
						plantRuleFoldStates.Add (newR, false);
					}
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
				}
				//GUILayout.EndVertical (); //~Rules
				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndScrollView ();
			return keepOpen;
		}

		public bool RenderSide (int mx, int my)
		{
			return false;
		}
	}
}
