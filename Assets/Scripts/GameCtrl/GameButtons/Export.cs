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
			private readonly Export parent;
			private string[] selectionTypes;
			private int currentSelectionType;
			private Dictionary<string, bool> toggleStates = new Dictionary<string, bool>();

			private Vector2 scrollPos;
			private EditData edit;
			private Data customSelectionData;
			private bool customDataActive;
			private Data selectionData;
			private GridTextureSettings areaGrid;
			private Material areaMat;
			private Material selectedAreaMat;

			public ExportDataWindow (Export parent) : base (-1, -1, 512, parent.iconTex)
			{
				this.parent = parent;
				ExportMgr.self.GetNewExportData ();

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
				SetupEditData (GameControl.self.scene.progression.managedArea, false);
			}
			
			/**
			 * renders help window
			 */
			public override void Render ()
			{
				SimpleGUI.Label (new Rect (xOffset + 65, yOffset, this.width, 32), "Export data", title);

				// Check if we have export data
				if (ExportMgr.self.currentExportData == null) 
				{
					SimpleGUI.Label (new Rect (xOffset, yOffset + 33, this.width + 65, 32), "Loading please wait...", parent.textBgStyle);
					base.Render ();
					return;
				}

				float width = this.width + 65;
				GUILayout.BeginArea (new Rect (xOffset, yOffset + 33, width, 640f));
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
									// Get the data
									if (currentSelectionType == 0) 
									{
										SetupEditData (GameControl.self.scene.progression.managedArea, false);
									} 
									else if (currentSelectionType == selectionTypes.Length - 1) 
									{
										SetupEditData (customSelectionData, true);
									} 
									else 
									{
										SetupEditData (GameControl.self.scene.progression.GetTargetArea (currentSelectionType), false);
									}
								}
							}
							GUILayout.EndHorizontal ();
							idx++;
						}
					}

					GUILayout.Space (1);
					if (RenderToggleButton ("Years"))
					{
						foreach (ExportData.YearData y in ExportMgr.self.currentExportData.years)
						{
							GUILayout.Space (1);
							RenderEntryToggleButton (y.year.ToString ());
						}
					}

					GUILayout.Space (1);
					if (RenderToggleButton ("Data"))
					{
						foreach (string s in ExportMgr.self.currentExportData.EnumerateColumns())
						{
							if (s != "year" && s != "x" && s != "y" && s != "costs")
							{
								GUILayout.Space (1);
								RenderEntryToggleButton (s);
							}
						}
					}
					GUILayout.Space (1);

					// Save etc
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Label ("", header, GUILayout.MaxWidth (1500f));
						if (GUILayout.Button ("Save...", entry, GUILayout.Width (100f))) 
						{
							// Remove columns so that they aren't exported
							List<string> savedColumns = new List<string> (ExportMgr.self.currentExportData.columns);
							for (int i = ExportMgr.self.currentExportData.columns.Count - 1; i >= 0; i--) {
								string c = ExportMgr.self.currentExportData.columns [i];
								if (c != "year" && c != "x" && c != "y" && c != "costs") {
									if (!GetToggleState (c)) {
										ExportMgr.self.currentExportData.columns.Remove (c);
									}
								}
							}

							// Saved years and coords
							Dictionary<ExportData.YearData, List<ExportData.YearData.CoordinateData>> savedYears = new Dictionary<ExportData.YearData, List<ExportData.YearData.CoordinateData>> ();
							foreach (ExportData.YearData y in ExportMgr.self.currentExportData.years) {
								List<ExportData.YearData.CoordinateData> coords = new List<ExportData.YearData.CoordinateData> ();
								savedYears.Add (y, coords);
								foreach (ExportData.YearData.CoordinateData c in y.coords) {
									coords.Add (c);
								}
							}

							// Remove years
							for (int i = ExportMgr.self.currentExportData.years.Count - 1; i >= 0; i--) {
								ExportData.YearData y = ExportMgr.self.currentExportData.years [i];
								string ys = y.year.ToString ();
								if (!GetToggleState (ys)) {
									ExportMgr.self.currentExportData.years.Remove (y);
								}
							}

							// Remove coordinates (if curr selection type = 0, then it's ALL)
							if (currentSelectionType > 0) {
								// Check all coords of all years
								foreach (ExportData.YearData y in ExportMgr.self.currentExportData.years) {
									for (int i = y.coords.Count - 1; i >= 0; i--) {
										// Get coord
										ExportData.YearData.CoordinateData coord = y.coords [i];

										/** Check the selection type **/

										// All (managed)
										if (currentSelectionType == 0) 
										{
											if (GameControl.self.scene.progression.managedArea.Get (coord.coord) <= 0) {
												// Remove coord
												y.coords.Remove (coord);
											}
										}
										// Custom...
										else if (currentSelectionType == selectionTypes.Length - 1) 
										{
											if (customSelectionData == null || customSelectionData.Get (coord.coord) <= 0) {
												// Remove coord
												y.coords.Remove (coord);
											}
										} 
										// Target areas
										else 
										{
											int targetArea = currentSelectionType;
											Data area = GameControl.self.scene.progression.GetTargetArea (targetArea);
											if (area.Get (coord.coord) <= 0) {
												// Remove coord
												y.coords.Remove (coord);
											}
										}
									}
								}
							}

							// Export data
							ExportMgr.self.ExportCurrentData (delegate 
							{
								// Reset
								ExportMgr.self.currentExportData.columns = savedColumns;
								ExportMgr.self.currentExportData.years = new List<ExportData.YearData> ();
								foreach (KeyValuePair<ExportData.YearData, List<ExportData.YearData.CoordinateData>> pair in savedYears) {
									ExportMgr.self.currentExportData.years.Add (pair.Key);
									pair.Key.coords = new List<ExportData.YearData.CoordinateData> ();
									foreach (ExportData.YearData.CoordinateData c in pair.Value) {
										pair.Key.coords.Add (c);
									}
								}
							});
						}
					}
					GUILayout.EndHorizontal ();
				}
				GUILayout.EndScrollView ();
				GUILayout.EndArea ();

				base.Render ();
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

				if (GUILayout.Button ("Toggle " + name, entry, GUILayout.MaxWidth (1500f))) {
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

			protected void SetupEditData (Data data, bool canEdit)
			{
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

		public override void OnClick ()
		{
			if (window != null) {
				window.Close ();
			}

			window = new ExportDataWindow (this);
			base.OnClick ();
		}
		
		public override void UpdateState (GameButton button)
		{
			button.isVisible = CameraControl.IsNear;
			button.alwaysRender = false;
		}	

		public void ClosedWindow () 
		{
			window = null;
		}
	}
}