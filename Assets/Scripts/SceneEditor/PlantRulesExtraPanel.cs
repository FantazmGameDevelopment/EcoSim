using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.PlantRules;
using Ecosim.SceneData.Rules;
using UnityEngine;

namespace Ecosim.SceneEditor
{
	public class PlantRulesExtraPanel : ExtraPanel
	{
		private Dictionary<PlantRule, bool> plantRuleFoldStates;
		private Dictionary<PlantGerminationRule, bool> germRuleFoldStates;
		private Dictionary<SuccessionType, string[]> vegetations;

		private EditorCtrl ctrl;
		private PlantType plant;
		private Vector2 scrollPos;
		private Vector2 germScrollPos;
		private string[] parameters;
		private string[] successions;

		private bool showGerminationRules;

		private static PlantRule[] copyBuffer;
		private static PlantGerminationRule[] germCopyBuffer;

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
				if (r != null) plantRuleFoldStates.Add (r, true);
			}

			germRuleFoldStates = new Dictionary<PlantGerminationRule, bool>();
			foreach (PlantGerminationRule r in this.plant.germinationRules) {
				germRuleFoldStates.Add (r, true);
			}

			vegetations = new Dictionary<SuccessionType, string[]>();

			List<string> pList = new List<string> ();
			foreach (string p in ctrl.scene.progression.GetAllDataNames(false)) {
				Data dataFindNames = ctrl.scene.progression.GetData (p);
				if ((dataFindNames.GetMax () < 256))
					pList.Add (p);
			}
			parameters = pList.ToArray ();

			showGerminationRules = true;

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

		public void DeleteGerminationRule (PlantGerminationRule rule)
		{
			List<PlantGerminationRule> rules = new List<PlantGerminationRule>(plant.germinationRules);
			rules.Remove (rule);
			plant.germinationRules = rules.ToArray();
			germRuleFoldStates.Remove (rule);
			rule = null;
		}

		public bool Render (int mx, int my)
		{
			bool keepOpen = true;
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (ctrl.foldedOpenSmall)) 
			{
				keepOpen = false;
			}

			GUILayout.Label ("Succession Rules", GUILayout.Width (100));
			GUILayout.Label (plant.name);
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal (); // Buttons
			{
				if (GUILayout.Button ("Fold all"))
				{
					List<PlantRule> rules = new List<PlantRule>();
					foreach (KeyValuePair<PlantRule, bool> pair in plantRuleFoldStates) {
						rules.Add (pair.Key);
					}
					foreach (PlantRule r in rules) {
						plantRuleFoldStates[r] = true;
					}
				}
				
				if (GUILayout.Button ("Unfold all"))
				{
					List<PlantRule> rules = new List<PlantRule>();
					foreach (KeyValuePair<PlantRule, bool> pair in plantRuleFoldStates) {
						rules.Add (pair.Key);
					}
					foreach (PlantRule r in rules) {
						plantRuleFoldStates[r] = false;
					}
				}

				GUILayout.Space (10f);

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
			GUILayout.EndHorizontal (); // ~Buttons

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
								RenderChance ("Chance", ref pr.chance);

								GUILayout.BeginHorizontal (); // Change
								{
									GUILayout.Label ("Population change", GUILayout.Width (120));
									pr.delta = (int)GUILayout.HorizontalSlider (pr.delta, -plant.maxPerTile, plant.maxPerTile, GUILayout.Width (120));
									string deltaStr = pr.delta.ToString ("0");
									string newDeltaStr = GUILayout.TextField (deltaStr, GUILayout.Width (40));
									if (newDeltaStr != deltaStr) {
										int outVal;
										if (int.TryParse (newDeltaStr, out outVal)) {
											pr.delta = outVal;
										}
									}
								}
								GUILayout.EndHorizontal (); // ~Change

								RenderChance ("Spawn chance", ref pr.spawnChance);

								RenderVegetationConditions (ref pr.vegetationConditions);
								RenderParameterConditions (ref pr.parameterConditions);
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
			if (!showGerminationRules) return false;

			bool keepOpen = true;
			GUILayout.BeginHorizontal ();
			/*if (GUILayout.Button (ctrl.foldedOpenSmall)) 
			{
				keepOpen = false;
			}*/
			
			GUILayout.Label ("Germination Rules", GUILayout.Width (100));
			GUILayout.Label (plant.name);
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal (); // Buttons
			{
				if (GUILayout.Button ("Fold all"))
				{
					List<PlantGerminationRule> rules = new List<PlantGerminationRule>();
					foreach (KeyValuePair<PlantGerminationRule, bool> pair in germRuleFoldStates) {
						rules.Add (pair.Key);
					}
					foreach (PlantGerminationRule r in rules) {
						germRuleFoldStates[r] = true;
					}
				}
				
				if (GUILayout.Button ("Unfold all"))
				{
					List<PlantGerminationRule> rules = new List<PlantGerminationRule>();
					foreach (KeyValuePair<PlantGerminationRule, bool> pair in germRuleFoldStates) {
						rules.Add (pair.Key);
					}
					foreach (PlantGerminationRule r in rules) {
						germRuleFoldStates[r] = false;
					}
				}
				
				GUILayout.Space (10f);
				
				if (GUILayout.Button ("Copy")) 
				{
					germCopyBuffer = new PlantGerminationRule[plant.germinationRules.Length];
					for (int i = 0; i < germCopyBuffer.Length; i++) {
						germCopyBuffer[i] = (PlantGerminationRule) plant.germinationRules[i].Clone();
					}
				}
				if ((germCopyBuffer != null) && GUILayout.Button ("Paste (add)")) 
				{
					List<PlantGerminationRule> rulesList = new List<PlantGerminationRule>(plant.germinationRules);
					for (int i = 0; i < germCopyBuffer.Length; i++) {
						rulesList.Add ((PlantGerminationRule)germCopyBuffer[i].Clone());
					}
					plant.germinationRules = rulesList.ToArray();
				}
				if ((germCopyBuffer != null) && GUILayout.Button ("Paste (replace)")) 
				{
					plant.germinationRules = new PlantGerminationRule[germCopyBuffer.Length];
					for (int i = 0; i < plant.germinationRules.Length; i++) {
						plant.germinationRules[i] = (PlantGerminationRule)germCopyBuffer[i].Clone();
					}
				}
				if ((germCopyBuffer != null) && GUILayout.Button ("Clear"))
				{
					germCopyBuffer = null;
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal (); // ~Buttons

			germScrollPos = GUILayout.BeginScrollView (germScrollPos, false, false);
			{
				foreach (PlantGerminationRule gr in plant.germinationRules)
				{
					GUILayout.BeginVertical (ctrl.skin.box); // PlantGerminationRule
					{
						GUILayout.BeginHorizontal ();
						{
							if (!germRuleFoldStates.ContainsKey (gr)) germRuleFoldStates.Add (gr, true);
							if (GUILayout.Button (germRuleFoldStates[gr] ? (ctrl.foldedCloseSmall) : (ctrl.foldedOpenSmall), ctrl.icon12x12)) 
							{
								germRuleFoldStates[gr] = !germRuleFoldStates[gr];
							}

							GUILayout.Label ("Description", GUILayout.Width (80));
							gr.description = GUILayout.TextField (gr.description);
							
							if (GUILayout.Button("-", GUILayout.Width(20)))
							{
								PlantGerminationRule tmpGR = gr;
								ctrl.StartDialog (string.Format("Delete germination rule ({0})?", tmpGR.description), newVal => { 
									DeleteGerminationRule (tmpGR);
								}, null);
							}
						}
						GUILayout.EndHorizontal ();

						if (!germRuleFoldStates[gr])
						{
							RenderChance ("Chance", ref gr.chance);
							RenderVegetationConditions (ref gr.vegetationConditions);
							RenderParameterConditions (ref gr.parameterConditions);
						}
					}
					GUILayout.EndVertical (); // ~PlantGerminationRule
				} // ~PlantGerminationRule foreach

				GUILayout.BeginHorizontal ();
				if (GUILayout.Button ("+", GUILayout.Width (20))) {
					PlantGerminationRule newR = new PlantGerminationRule ();
					List<PlantGerminationRule> list = new List<PlantGerminationRule> (plant.germinationRules);
					list.Add (newR);
					newR.description = "Germination Rule " + list.Count;
					plant.germinationRules = list.ToArray ();
					germRuleFoldStates.Add (newR, false);
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndScrollView ();

			return keepOpen;
		}

		private void RenderChance (string name, ref float chance)
		{
			GUILayout.Space (1f);
			GUILayout.BeginHorizontal (); // Chance
			{
				GUILayout.Label (name + " (0..1)", GUILayout.Width (120));
				chance = GUILayout.HorizontalSlider (chance, 0.0f, 1.0f, GUILayout.Width (120));
				string chanceStr = chance.ToString ("0.00");
				string newChanceStr = GUILayout.TextField (chanceStr, GUILayout.Width (40));
				if (newChanceStr != chanceStr) {
					float outVal;
					if (float.TryParse (newChanceStr, out outVal)) {
						chance = outVal;
					}
				}
			}
			GUILayout.EndHorizontal (); // ~Chance
		}

		private void RenderVegetationConditions(ref VegetationCondition[] vegetationConditions)
		{
			GUILayout.BeginVertical (ctrl.skin.box); // Vegetation conditions
			{
				GUILayout.BeginHorizontal (); // Header
				{
					GUILayout.Label ("Vegetation conditions");
					GUILayout.FlexibleSpace();
					
					if (GUILayout.Button ("+", GUILayout.Width (20)))
					{
						List<VegetationCondition> vcList = new List<VegetationCondition> (vegetationConditions);
						VegetationCondition newVC = new VegetationCondition (-1, -1);
						vcList.Add (newVC);
						vegetationConditions = vcList.ToArray();
					}
				}
				GUILayout.EndHorizontal (); //~Header

				int index = 0;
				foreach (VegetationCondition vc in vegetationConditions)
				{
					GUILayout.BeginVertical (ctrl.skin.box);
					GUILayout.BeginHorizontal ();
					{
						// Succession, -1 means any succession so we show the index + 1
						if (GUILayout.Button (successions[vc.successionIndex + 1], GUILayout.Width(150)))
						{
							VegetationCondition tmpVC = vc;
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
								VegetationCondition tmpVC = vc;
								ctrl.StartSelection (vegs, tmpVC.vegetationIndex + 1, 
								                     newIndex => {
									tmpVC.vegetationIndex = newIndex - 1;
								});
							}
						}
						
						GUILayout.FlexibleSpace();
						if (index > 0) {
							if (GUILayout.Button ("-", GUILayout.Width (20))) 
							{
								List<VegetationCondition> vcList = new List<VegetationCondition>(vegetationConditions);
								vcList.Remove(vc);
								vegetationConditions = vcList.ToArray();
								break;
							}
						}
					}
					GUILayout.EndHorizontal ();
					GUILayout.EndVertical ();

					index++;
				}
			}
			GUILayout.EndVertical (); //~Vegetation conditions
		}

		private void RenderParameterConditions(ref ParameterRange[] parameterConditions)
		{
			GUILayout.BeginVertical (ctrl.skin.box); // Parameter conditions
			{
				GUILayout.BeginHorizontal (); // Header
				{
					GUILayout.Label ("Parameter conditions");
					GUILayout.FlexibleSpace();
					
					if (GUILayout.Button ("+", GUILayout.Width (20)))
					{
						List<ParameterRange> prList = new List<ParameterRange> (parameterConditions);
						ParameterRange newPR = new ParameterRange ();
						newPR.paramName = parameters [0];
						newPR.data = ctrl.scene.progression.GetData (newPR.paramName);
						newPR.lowRange = 0;
						newPR.highRange = newPR.data.GetMax();
						prList.Add (newPR);
						parameterConditions = prList.ToArray();
					}
				}
				GUILayout.EndHorizontal (); //~Header
				
				foreach (ParameterRange pc in parameterConditions)
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
							List<ParameterRange> prList = new List<ParameterRange> (parameterConditions);
							prList.Remove (pc);
							parameterConditions = prList.ToArray ();
							break;
						}
					}
					GUILayout.EndHorizontal ();
					GUILayout.EndVertical ();
				}
			}
			GUILayout.EndVertical (); //~Parameter conditions 
		}

		public bool NeedSidePanel()
		{
			return (showGerminationRules);
		}

		public void Dispose ()
		{

		}
	}
}
