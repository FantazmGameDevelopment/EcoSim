using UnityEngine;
using System.Collections.Generic;
using System;
using System.Globalization;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;

namespace Ecosim.GameCtrl.GameButtons
{
	public class Export : GameButtonHandler
	{
		private class ExportDataWindow : GameWindow
		{
			private readonly Scene scene;
			private readonly Export parent;
			private string[] selectionTypes;
			private int currentSelectionType;
			private Dictionary<string, bool> toggleStates = new Dictionary<string, bool>();

			private bool isExporting;
			private int totalExportCosts;

			private Vector2 scrollPos;
			private EditData edit;
			private Data customSelectionData;
			private bool customDataActive;
			private Data selectionData;
			private GridTextureSettings areaGrid;
			private Material areaMat;
			private Material selectedAreaMat;

			private List<string> years = new List<string> ();
			private List<string> parameters = new List<string>();
			private List<string> inventarisations = new List<string>();
			private List<string> animals = new List<string>();
			private List<string> plants = new List<string>();
			private List<string> measures = new List<string>();
			private List<string> researchPoints = new List<string> ();

			private Dictionary<string, List<string>> lists = new Dictionary<string, List<string>> ();

			public ExportDataWindow (Export parent) : base (-1, -1, 512, parent.iconTex)
			{
				this.parent = parent;
				this.scene = GameControl.self.scene;

				// Setup selection types
				List<string> selectionTypes = new List<string> ();
				selectionTypes.Add ("All");
				for (int i = 0; i < GameControl.self.scene.progression.targetAreas; i++) {
					selectionTypes.Add (string.Format ("Target Area {0}", (i + 1).ToString ()));
				}
				selectionTypes.Add ("Custom...");
				this.selectionTypes = selectionTypes.ToArray ();

				foreach (string s in selectionTypes) {
					GetToggleState (s);
				}

				// Setup edit data
				customSelectionData = new BitMap1 (GameControl.self.scene);
				SetupEditData ();

				// Setup years
				for (int i = scene.progression.startYear; i < scene.progression.year; i++) {
					years.Add (i.ToString ());
				}

				// Setup data names (parameters)
				foreach (string p in scene.progression.GetAllDataNames (false)) {
					if (ExportMgr.self.ShouldExportParameter (p)) {	
						if (parameters.Contains (p)) continue;
						parameters.Add (p);
					}
				}

				// Filter out parameters if we have only when surveyed
				if (ExportMgr.self.dataType == ExportMgr.DataTypes.OnlyWhenSurveyed) 
				{
					// Get all datanames from all surveys
					List<string> surveyDataNames = new List<string> ();

					// Research points
					foreach (ResearchPoint r in scene.progression.researchPoints) {
						foreach (ResearchPoint.Measurement rm in r.measurements) {
							// Check each value and check if it 
							foreach (KeyValuePair<string, string> p in rm.data.values) {
								if (surveyDataNames.Contains (p.Key) == false) {
									surveyDataNames.Add (p.Key);
								}
							}
						}
					}

					// TODO: Also for inventarisations?

					// Remove all parameters that aren't in the survery data names list
					for (int i = parameters.Count - 1; i >= 0; i--) 
					{
						if (!surveyDataNames.Contains (parameters [i])) {
							parameters.RemoveAt (i);
						}
					}
				}

				// If we only show data when surveyed, then animals and plants will NEVER
				// show up, because the data is only retrieved when surveying. So we can't choose
				// animals and plants...
				if (ExportMgr.self.dataType == ExportMgr.DataTypes.Always)
				{
					// Animals
					foreach (AnimalType a in scene.animalTypes) {
						if (ExportMgr.self.ShouldExportAnimal (a.name)) {
							animals.Add (a.name);
						}
					}

					// Plants
					foreach (PlantType p in scene.plantTypes) {
						if (ExportMgr.self.ShouldExportPlant (p.name)) {
							plants.Add (p.name);
						}
					}
				}

				// Inventarisations
				foreach (Progression.InventarisationResult ir in scene.progression.inventarisations) {
					if (inventarisations.Contains (ir.name)) continue;
					inventarisations.Add (ir.name);
				}

				// Research points
				foreach (ResearchPoint rp in scene.progression.researchPoints) {
					if (!rp.HasMeasurements () || researchPoints.Contains (rp.measurements [0].name)) continue;
					researchPoints.Add (rp.measurements [0].name);
				}

				// Measures
				foreach (Progression.ActionTaken ta in scene.progression.actionsTaken) {
					BasicAction a = scene.actions.GetAction (ta.id);
					if (a != null) {
						string key = a.GetDescription ();
						if (measures.Contains (key)) continue;
						measures.Add (key);
					}
				}

				// Setup lists dict
				lists.Add ("Years", years);
				lists.Add ("Animals", animals);
				lists.Add ("Plants", plants);
				lists.Add ("Parameters", parameters);
				lists.Add ("Surveys", inventarisations);
				lists.Add ("Research points", researchPoints);
				lists.Add ("Measures", measures);
			}
			
			/**
			 * renders help window
			 */
			public override void Render ()
			{
				if (isExporting) return;
				
				Rect r = new Rect (xOffset + 65, yOffset, this.width, 32);
				SimpleGUI.Label (r, "Export data", title);

				// Setup cost and save UI
				float width = this.width + 65;
				int saveBtnWidth = (SaveFileDialog.SystemDialogAvailable ())?150:220;
				int costTextWidth = 150;

				GUILayout.BeginArea (new Rect (xOffset, yOffset + 33, width, Mathf.Min (600f, Screen.height - (yOffset + 33))));
				{
					int totalCosts = 0;
					bool enoughBudget = true;

					switch (ExportMgr.self.costType)
					{
					case ExportMgr.CostTypes.OnePrice :
					{
						totalCosts = ExportMgr.self.costs;
						enoughBudget = (scene.progression.budget + scene.progression.expenses) > totalCosts;

						GUILayout.BeginHorizontal ();
						{
							// Cost
							GUILayout.Label ("", header, GUILayout.Width (width - costTextWidth - saveBtnWidth));
							GUILayout.Space (1);
							GUILayout.Label ("Cost", entry, GUILayout.Width (costTextWidth));
							GUILayout.Space (1);

							// Check if we have enough budget
							if (!enoughBudget) GUI.color = Color.red;
							GUILayout.Label (ExportMgr.self.costs.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB")), entry, GUILayout.Width (saveBtnWidth));
							GUI.color = Color.white;
						}
						GUILayout.EndHorizontal ();
					}
					break;

					case ExportMgr.CostTypes.PricePerYear : 
					{
						// Count selected years
						int yearsSelected = 0;
						foreach (string y in this.years) {
							if (GetToggleState (y)) {
								yearsSelected ++;
							}
						}

						// Years selected
						GUILayout.BeginHorizontal ();
						{
							GUILayout.Label ("", header, GUILayout.Width (width - costTextWidth - saveBtnWidth - 1));
							GUILayout.Space (1);
							GUILayout.Label ("Selected years", entry, GUILayout.Width (saveBtnWidth));
							GUILayout.Space (1);
							GUILayout.Label (yearsSelected.ToString (), entry, GUILayout.Width (saveBtnWidth - 33));
							GUILayout.Space (1);
							GUILayout.Label ("", entry, GUILayout.Width (32));
						}
						GUILayout.EndHorizontal ();
						GUILayout.Space (1);

						// Cost per year
						GUILayout.BeginHorizontal ();
						{
							GUILayout.Label ("", header, GUILayout.Width (width - costTextWidth - saveBtnWidth - 1));
							GUILayout.Space (1);
							GUILayout.Label ("Cost per year", entry, GUILayout.Width (saveBtnWidth));
							GUILayout.Space (1);
							GUILayout.Label (ExportMgr.self.costs.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB")), entry, GUILayout.Width (saveBtnWidth - 33));
							GUILayout.Space (1);
							GUILayout.Label ("x", entry, GUILayout.Width (32));
						}
						GUILayout.EndHorizontal ();
						GUILayout.Space (1);

						totalCosts = ExportMgr.self.costs * yearsSelected;
						enoughBudget = (scene.progression.budget + scene.progression.expenses) > totalCosts;
						
						// Total cost
						GUILayout.BeginHorizontal ();
						{
							GUILayout.Label ("", header, GUILayout.Width (width - costTextWidth - saveBtnWidth - 1));
							GUILayout.Space (1);
							GUILayout.Label ("Total cost", entry, GUILayout.Width (saveBtnWidth));
							GUILayout.Space (1);

							// Check if we have enough budget
							if (!enoughBudget) GUI.color = Color.red;
							GUILayout.Label (totalCosts.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB")), entry, GUILayout.Width (saveBtnWidth - 33));
							GUI.color = Color.white;

							GUILayout.Space (1);
							GUILayout.Label ("=", entry, GUILayout.Width (32));
						}
						GUILayout.EndHorizontal ();
					}
					break;
					}

					// Save button
					GUILayout.Space (1);
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Label ("", header, GUILayout.Width (width - saveBtnWidth));
						GUILayout.Space (1);

						// Check if we have enough budget
						string saveName = (SaveFileDialog.SystemDialogAvailable ()) ? "Export and save..." : "Export and save to Desktop";
						GUI.enabled = enoughBudget;
						if (GUILayout.Button (saveName, entry, GUILayout.Width (saveBtnWidth))) 
						{
							totalExportCosts = totalCosts;
							DoExport ();
						}
						GUI.enabled = true;
					}
					GUILayout.EndHorizontal ();
					GUILayout.Space (1);

					scrollPos = GUILayout.BeginScrollView (scrollPos);
					{
						// Selection
						if (RenderToggleButton ("Selection"))
						{
							int idx = 0;
							foreach (string s in selectionTypes)
							{
								GUILayout.Space (1);
								GUILayout.BeginHorizontal ();
								{
									int prevCurrSelectionType = currentSelectionType;

									// Setup toggle state
									toggleStates [s] = (idx == currentSelectionType);
									if (RenderEntryToggleButton (s)) {
										currentSelectionType = idx;
									}

									// Check for selection
									if (prevCurrSelectionType != currentSelectionType) {
										SetupEditData ();
									}
								}
								GUILayout.EndHorizontal ();
								idx++;
							}
						}

						// Show all lists
						foreach (KeyValuePair <string, List<string>> pair in lists) 
						{
							if (pair.Value.Count > 0) 
							{
								GUILayout.Space (1);
								bool toggled = false;
								GUILayout.BeginHorizontal ();
								{
									// Toggle
									toggled = RenderToggleButton (pair.Key);

									// Select all
									GUILayout.Space (1);
									if (GUILayout.Button ("Select all", entry, GUILayout.Width (100f))) {
										foreach (string s in pair.Value) {
											GetToggleState (s);
											toggleStates [s] = true;
										}
									}

									// Deselect all
									GUILayout.Space (1);
									if (GUILayout.Button ("Deselect all", entry, GUILayout.Width (100f))) {
										foreach (string s in pair.Value) {
											GetToggleState (s);
											toggleStates [s] = false;
										}
									}
								}
								GUILayout.EndHorizontal ();

								if (toggled) {
									foreach (string s in pair.Value) {
										GUILayout.Space (1);
										RenderEntryToggleButton (s);
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

			private void DoExport ()
			{
				isExporting = true;
				
				// Years
				List<string> years = new List<string> ();
				foreach (string y in this.years) {
					if (GetToggleState (y)) {
						years.Add (y);
					}
				}
				
				// Datanames
				List<string> dataNames = new List<string> ();
				List<string> allDataNames = new List<string> ();
				allDataNames.AddRange (this.parameters);
				allDataNames.AddRange (this.plants);
				allDataNames.AddRange (this.animals);
				allDataNames.AddRange (this.inventarisations);
				allDataNames.AddRange (this.measures);
				allDataNames.AddRange (this.researchPoints);
				foreach (string s in allDataNames) {
					if (GetToggleState (s)) {
						dataNames.Add (s);
					}
				}
				
				// Delete edit data
				if (edit != null) {
					edit.ClearData ();
					edit.ClearSelection ();
					edit.Delete ();
				}
				
				// Do export and save
				ExportSettings settings = new ExportSettings (GetAreaSelection (), years, dataNames);
				ExportMgr.self.ExportData (settings, delegate 
				{ 
					isExporting = false; 
					SetupEditData ();

					// Update budget
					scene.progression.budget -= totalExportCosts;
					GameControl.BudgetChanged ();
				}, 
				delegate() 
				{
					isExporting = false;
					SetupEditData ();
				});
			}

			private Data GetAreaSelection ()
			{
				// Check the selection type //
				
				// All (managed)
				if (currentSelectionType == 0)
				{
					return scene.progression.managedArea;
				}
				// Custom...
				else if (currentSelectionType == selectionTypes.Length - 1) 
				{
					return customSelectionData;
				} 
				// Target areas
				else 
				{
					int targetArea = currentSelectionType;
					return scene.progression.GetTargetArea (targetArea);
				}
				return null;
			}

			public bool RenderEntryToggleButton (string name)
			{
				// Toggle button
				bool toggled = GetToggleState (name);

				GUILayout.BeginHorizontal ();
				{
					// closeIcon ?
					if (GUILayout.Button (((toggled)?"<b> X</b>":""), entry, GUILayout.Width (32))) {
						toggled = !toggled;
						toggleStates [name] = toggled;
					}
					GUILayout.Space (1);
					if (GUILayout.Button (name, header, GUILayout.MaxWidth (1500f))) {
						toggled = !toggled;
						toggleStates [name] = toggled;
					}
				}
				GUILayout.EndHorizontal ();
				return toggled;
			}

			public bool RenderToggleButton (string name)
			{
				bool toggled = GetToggleState (name, true);

				if (GUILayout.Button ("" + name, entry, GUILayout.MaxWidth (1500f))) {
					toggled = !toggled;
					toggleStates[name] = toggled;
				}
				return toggled;
			}

			private bool GetToggleState (string name)
			{
				return GetToggleState (name, true);
			}

			private bool GetToggleState (string name, bool defaultValue)
			{
				if (toggleStates.ContainsKey (name) == false)
					toggleStates.Add (name, defaultValue);
				return toggleStates[name];
			}
			
			protected override void OnClose ()
			{
				if (edit != null) {
					edit.ClearData ();
					edit.ClearSelection ();
					edit.Delete ();
				}

				parent.ClosedWindow ();
			}

			protected void SetupEditData ()
			{
				Data data;
				bool canEdit;

				// Set the data and canEdit bool
				if (currentSelectionType == 0) 
				{
					data = GameControl.self.scene.progression.managedArea;
					canEdit = false;
				} 
				else if (currentSelectionType == selectionTypes.Length - 1) 
				{
					data = customSelectionData;
					canEdit = true;
				} 
				else 
				{
					data = GameControl.self.scene.progression.GetTargetArea (currentSelectionType);
					canEdit = false;
				}

				// Check for custom data
				customDataActive = (data == customSelectionData);

				// Setup (temp) selection data
				selectionData = new BitMap1 (GameControl.self.scene);
				foreach (ValueCoordinate vc in data.EnumerateNotZero ()) {
					selectionData.Set (vc.x, vc.y, 1);
				}

				if (areaMat == null)
					areaMat = new Material (EcoTerrainElements.GetMaterial ("MapGrid100"));

				if (selectedAreaMat == null)
					selectedAreaMat = new Material (EcoTerrainElements.GetMaterial ("ActiveMapGrid100"));

				if (areaGrid == null)
					areaGrid = new GridTextureSettings (false, 0, 2, areaMat, true, selectedAreaMat);

				if (edit != null) {
					edit.ClearData ();
					edit.ClearSelection ();
					edit.Delete ();
				}

				edit = EditData.CreateEditData ("exportArea", selectionData, GameControl.self.scene.progression.managedArea, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
					if (shift)
						return 0;
					return (canEdit) ? 1 : -1;
				}, areaGrid);

				edit.SetFinalBrushFunction (delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
					if (customDataActive) {
						edit.CopyData (customSelectionData);
					}
					if (shift)
						return 0;
					return (canEdit) ? 1 : -1;
				});
				edit.SetModeAreaSelect ();
			}
		}

		private Texture2D iconTex;
		private Texture2D toggleVisual;
		private Texture2D toggleVisualH;
		private GUIStyle header;
		private GUIStyle entry;
		private GUIStyle entrySelected;
		private GUIStyle textBgStyle;

		private ExportDataWindow window;

		public Export ()
		{
			iconTex =  Resources.Load ("Icons/budget_w") as Texture2D;
			header = GameControl.self.skin.FindStyle ("ArialB16-50");
			entry = GameControl.self.skin.FindStyle ("Arial16-75");
			entrySelected = GameControl.self.skin.FindStyle ("Arial16-W");
			toggleVisual = (Texture2D)Resources.Load ("Icons/cross_w", typeof (Texture2D));
			toggleVisualH = (Texture2D)Resources.Load ("Icons/cross_zw", typeof (Texture2D));
			textBgStyle = GameControl.self.skin.GetStyle ("50");
		}
		
		public override bool SelectRender (GameButton button)
		{
			bool isOver = false;
			return isOver;
		}
		
		public override void UpdateScene (Scene scene, GameButton button)
		{

		}

		public override void UpdateState (GameButton button)
		{
			button.isVisible = ExportMgr.self.exportEnabled;
		}

		public override void OnClick ()
		{
			if (window != null) {
				window.Close ();
			}

			window = new ExportDataWindow (this);
			base.OnClick ();
		}	

		public void ClosedWindow () 
		{
			window = null;
		}
	}
}