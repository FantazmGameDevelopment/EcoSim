using UnityEngine;
using System.Collections.Generic;
using System;
using System.Globalization;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;

namespace Ecosim.GameCtrl.GameButtons
{
	public class Reports : GameButtonHandler
	{
		private Texture2D closeTex;
		private Texture2D closeHTex;
		private Texture2D toggleVisual;
		private Texture2D toggleVisualH;
		private GUIStyle header;
		private GUIStyle entry;
		private GUIStyle entrySelected;
		private Progression.InventarisationResult selectedIR = null;
		private Inventarisation selectedINV = null;
		private string selectedInvName = null;
		private int totalYearsSelected = 0;
		private string name;
		private Dictionary<Progression.InventarisationResult, InventarisationResultWindow> windows;
		private Vector2 entriesScrollPos;

		// Inventarisations
		private class Inventarisation
		{
			public string name;
			public List<Progression.InventarisationResult> results;

			public Inventarisation (string name)
			{
				this.name = name;
				this.results = new List<Progression.InventarisationResult> ();
			}
		}
		private List<Inventarisation> inventarisations = null;

		// Graph editor
		private bool graphEditorOpened = false;
		
		public Reports ()
		{
			header = GameControl.self.skin.FindStyle ("ArialB16-50");
			entry = GameControl.self.skin.FindStyle ("Arial16-75");
			entrySelected = GameControl.self.skin.FindStyle ("Arial16-W");
			toggleVisual = (Texture2D)Resources.Load ("Icons/cross_w", typeof (Texture2D));
			toggleVisualH = (Texture2D)Resources.Load ("Icons/cross_zw", typeof (Texture2D));
		}
		
		public override bool SelectRender (GameButton button)
		{
			RetrieveInventarisations ();

			int colWidth = 164;
			int yearWidth = 80;
			int yearToggleBtnWidth = 32;
			int graphEditorWidth = 150;
			int invGraphEditorBtnWidth = 100;
			int graphBtnWidth = 80;
			int entryHeight = 32;

			foreach (Inventarisation inv in inventarisations) 
			{
				// Calculate the width of the entry name, with or without the graph editor count
				int calcWidth = (int)entry.CalcSize (new GUIContent (inv.name + ((graphEditorOpened)?" (99/99)":""))).x;
				calcWidth = (((calcWidth / (entryHeight + 1)) + 1) * (entryHeight + 1) - 1) + 25;
				if (calcWidth > colWidth) {
					colWidth = calcWidth;
				}
			}

			bool isOver = false;
			int x = (int)(button.position.x + (entryHeight + 1));
			int y = (int)(button.position.y);
			int width = 6;

			// Graph editor
			if (ExportMgr.self.graphExportEnabled) 
			{
				Rect graphRect = new Rect (x, y, graphEditorWidth, entryHeight);

				// Label
				graphRect.width = colWidth + yearWidth - 175;
				isOver |= SimpleGUI.Label (graphRect, "Graph Editor", header);  
				graphRect.x += graphRect.width + 1;

				graphRect.width = 175 - 33;
				isOver |= SimpleGUI.CheckMouseOver (graphRect); 
				if (SimpleGUI.Button (graphRect, ((graphEditorOpened)?"Hide Functions":"Show Functions"), entry, entrySelected)) {
					graphEditorOpened = !graphEditorOpened;
				}
				graphRect.x += graphRect.width + 1;
				graphRect.width = 32;
				isOver |= SimpleGUI.CheckMouseOver (graphRect); 
				if (SimpleGUI.Button (graphRect, " ?", entry, entrySelected)) {
					new ExportGraphInfoWindow ();
				}
				
				// Show graph editor controls
				if (graphEditorOpened) 
				{
					// Calculate graph btn width
					int newGraphBtnWidth = (int)((float)(colWidth + yearWidth) / 3f);
					if (newGraphBtnWidth > graphBtnWidth)
						graphBtnWidth = newGraphBtnWidth;

					y += entryHeight + 1;
					Rect r = new Rect (x, y, graphBtnWidth, entryHeight);

					int totalCosts = 0;
					bool enoughBudget = true;

					// Costs
					switch (ExportMgr.self.graphCostType)
					{
					case ExportMgr.GraphCostTypes.OnePrice :
					{
						// Price
						Rect pr = new Rect (r);

						// Cost header
						pr.width = ((float)(colWidth + yearWidth) / 3f);
						isOver |= SimpleGUI.Label (pr, "", header);
						pr.x += pr.width + 1;
						pr.width--;
						isOver |= SimpleGUI.Label (pr, "Cost", entry);

						// Set costs
						totalCosts = ExportMgr.self.graphCosts;
						enoughBudget = (GameControl.self.scene.progression.budget - GameControl.self.scene.progression.expenses >= totalCosts); 

						// Actual costs
						if (!enoughBudget) GUI.color = Color.red;
						pr.x += pr.width + 1;
						pr.width ++;
						isOver |= SimpleGUI.Label (pr, ExportMgr.self.graphCosts.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB")), entry);
						GUI.color = Color.white;

						// Up the entry height
						y += entryHeight + 1;
						r.y += entryHeight + 1;
					}
					break;
					}

					// Select all
					isOver |= SimpleGUI.CheckMouseOver (r);
					if (SimpleGUI.Button (r, "Select all", entry, entrySelected)) { 
						foreach (Inventarisation i in inventarisations) {
							foreach (Progression.InventarisationResult ir in i.results) {
								ir.selected = true;
							}
						}
					}
					// Clear selection
					r.x += r.width + 1;
					isOver |= SimpleGUI.CheckMouseOver (r);
					if (SimpleGUI.Button (r, "Clear all", entry, entrySelected)) { 
						foreach (Inventarisation i in inventarisations) {
							foreach (Progression.InventarisationResult ir in i.results) {
								ir.selected = false;
							}
						}
					}

					// Generate graph
					GUI.enabled = enoughBudget && totalYearsSelected > 0;
					r.x += r.width + 1;
					isOver |= SimpleGUI.CheckMouseOver (r);
					bool generateClicked = false;
					if (GUI.enabled) generateClicked = SimpleGUI.Button (r, "Generate", entry, entrySelected);
					if (!GUI.enabled) generateClicked = SimpleGUI.Button (r, "Generate", entry);
					if (generateClicked)
					{
						// Subtract costs
						GameControl.self.scene.progression.budget -= totalCosts;
						GameControl.BudgetChanged ();

						new Ecosim.GameCtrl.ExportGraphWindow ();
					}
					GUI.enabled = true;
				}

				y += entryHeight + 1;
			}

			// Inventarisations
			isOver |= SimpleGUI.Label (new Rect (x, y, colWidth, entryHeight), name, header);
			// Years
			isOver |= SimpleGUI.Label (new Rect (x + colWidth + 1, y, yearWidth, entryHeight), "Years", header);

			if (isOver) {
				selectedIR = null;
				//selectedInvName = null;
			}

			// Reset total selected
			totalYearsSelected = 0;

			// Show sorted inventarisations
			Progression.InventarisationResult newSelectedIR = null;
			if (inventarisations.Count == 0) 
			{
				//y = y + ((graphEditorOpened) ? (entryHeight + 1) : 0);
				SimpleGUI.Label (new Rect (x, y + (entryHeight + 1), 197, entryHeight), "No reports available", entry);
			} 
			else 
			{
				GUILayout.BeginArea (new Rect (x, y + (entryHeight), colWidth, Screen.height - (y + entryHeight)));
				entriesScrollPos = GUILayout.BeginScrollView (entriesScrollPos);
				{
					GUILayout.Space (1);
					foreach (Inventarisation inv in inventarisations) 
					{
						// Get the graph suffix (X/X)
						string graphSuffix = "";
						if (graphEditorOpened) 
						{
							graphSuffix = " ";
							int total = inv.results.Count;
							int selected = 0;
							foreach (Progression.InventarisationResult ir in inv.results) {
								if (ir.selected) {
									selected++;
									totalYearsSelected++;
								}
							}
							graphSuffix = string.Format (" ({0}/{1})", selected, total);
						}

						// Check if this entry is selected/highlighted
						bool isSelected = (inv.name == selectedInvName);
						bool isOverGroup = false;
						string label = inv.name + graphSuffix;

						// We show a label if it's selected
						if (isSelected) 
						{
							GUILayout.Label (label, entrySelected, GUILayout.MaxWidth (Mathf.Infinity), GUILayout.Height (entryHeight));
							// Check for mouse over
							isOverGroup = CheckGUILayoutMouseOver (colWidth);
						}
						// We make it a button if it's not selected
						else 
						{
							if (GUILayout.Button (label, entry, GUILayout.MaxWidth (Mathf.Infinity), GUILayout.Height (entryHeight))) {
								// Select the inventarisation
								selectedInvName = inv.name;
								selectedINV = inv;
							}
							// Check for mouse over
							isOverGroup = CheckGUILayoutMouseOver (colWidth);
						}
						GUILayout.Space (1);

						// Remember if we have an overall mouse over
						isOver |= isOverGroup;

						// Show functions if selected and editor is opened
						if (isSelected && graphEditorOpened)
						{
							GUILayout.BeginHorizontal ();
							{
								// Select all
								if (GUILayout.Button ("Select all", entry, GUILayout.MaxWidth (colWidth * 0.5f), GUILayout.Height (entryHeight))) {
									foreach (Progression.InventarisationResult ir in inv.results) {
										ir.selected = true;
									}
								}
								// Check if we have a mouse over
								isOver |= CheckGUILayoutMouseOver ();

								GUILayout.Space (1f);

								// Select all
								if (GUILayout.Button ("Clear all", entry, GUILayout.MaxWidth (colWidth * 0.5f), GUILayout.Height (entryHeight))) {
									foreach (Progression.InventarisationResult ir in inv.results) {
										ir.selected = false;
									}
								}
								// Check if we have a mouse over
								isOver |= CheckGUILayoutMouseOver (colWidth * 0.5f);
							}
							GUILayout.EndHorizontal ();
							GUILayout.Space (1);
						}
					}
				}
				GUILayout.EndScrollView ();
				GUILayout.EndArea ();

				// Show years seperately
				if (selectedINV != null)
				{
					// Calculate the amount of columns
					float yearsAreaHeight = Screen.height - (y + entryHeight);
					int maxEntriesPerColumn = Mathf.FloorToInt (yearsAreaHeight / (entryHeight + 1));
					int columns = Mathf.FloorToInt ((float)selectedINV.results.Count / maxEntriesPerColumn) + 1;

					// Setup the layout area
					float yearEntryWidth = yearWidth + ((graphEditorOpened)?yearToggleBtnWidth+1:0);
					float yearsAreaWidth = columns * (yearEntryWidth + 1);
					Rect yearsAreaRect = new Rect (x + colWidth + 1, y + (entryHeight), yearsAreaWidth, yearsAreaHeight);
					GUILayout.BeginArea (yearsAreaRect);
					GUILayout.Space (1);

					GUILayout.BeginHorizontal(); // For use of columns

					for (int i = 0; i < columns; i++) 
					{
						GUILayout.BeginVertical (); // Column
						for (int n = 0; n < maxEntriesPerColumn; n++) 
						{
							int resultIndex = n + (maxEntriesPerColumn * i);
							if (resultIndex < selectedINV.results.Count)
							{
								Progression.InventarisationResult ir = selectedINV.results [resultIndex];

								if (graphEditorOpened) { GUILayout.BeginHorizontal (); }
								
								bool isSelected = (ir == selectedIR);
								GUIStyle yearStyle = (isSelected) ? entrySelected : entry;
								if (GUILayout.Button (ir.year.ToString (), yearStyle, GUILayout.MaxWidth (yearWidth), GUILayout.Height (entryHeight)))
								{
									// Handle click
									HandleInventarisationResultClicked (ir);
								}
								isOver |= CheckGUILayoutMouseOver (yearWidth);
								
								// Graph toggle button
								if (graphEditorOpened) 
								{
									GUILayout.Space (1);
									
									// Toggle button
									string toggleBtnLabel = (ir.selected) ? "<b> X</b>" : null;
									if (GUILayout.Button (toggleBtnLabel, entry, GUILayout.MaxWidth (yearToggleBtnWidth), GUILayout.Height (entryHeight))) { 
										ir.selected = !ir.selected;
									} 
									isOver |= CheckGUILayoutMouseOver (yearToggleBtnWidth);
								}
								
								if (graphEditorOpened) { GUILayout.EndHorizontal (); }
								GUILayout.Space (1);
							}
							else break;
						}
						GUILayout.EndVertical (); // ~ Column
						GUILayout.Space (1);
					}

					GUILayout.EndHorizontal (); // ~ For use of columns
					GUILayout.EndArea ();
				}
			}
			selectedIR = newSelectedIR;
			return isOver;

			/////// OLD ///////
			/*Progression.InventarisationResult newSelectedIR = null;
			List<Progression.InventarisationResult> inventarisations = GameControl.self.scene.progression.inventarisationResults;
			if (inventarisations.Count == 0) {
				SimpleGUI.Label (new Rect (x, y + 33, 197, 32), "No reports available", entry);
			} else {
				int colWidth = 164;
				foreach (Progression.InventarisationResult ir in inventarisations) {
					int calcWidth = (int)entry.CalcSize (new GUIContent (ir.name)).x;
					calcWidth = ((calcWidth / 33) + 1) * 33 - 1;
					if (calcWidth > colWidth) {
						colWidth = calcWidth;
					}
				}

				int i = 0;
				foreach (Progression.InventarisationResult ir in inventarisations) {
					bool hl = (ir == selectedIR);
					bool isOverGroup = SimpleGUI.Label (new Rect (x, y + 33 * (i + 1), colWidth, 32), ir.name, hl ? entrySelected : entry);
					isOverGroup |= SimpleGUI.Label (new Rect (x + colWidth + 1, y + 33 * (i + 1), 65, 32), ir.year.ToString (), hl ? entrySelected : entry);
					if (isOverGroup && (Event.current.type == EventType.MouseDown)) {
						InventarisationAction ia = (InventarisationAction)GameControl.self.scene.actions.GetAction (ir.actionId);
						InventarisationResultWindow irw = new InventarisationResultWindow (this, ir, ia);
						if (windows.ContainsKey (ir)) {
							windows [ir].Close ();
						}
						// On request: close all open windows...
						List<InventarisationResultWindow> tmpCopy = new List<InventarisationResultWindow> (windows.Values);
						foreach (InventarisationResultWindow w in tmpCopy) {
							w.Close ();
						}
						windows.Add (ir, irw);
						Event.current.Use ();
					}
					if (isOverGroup) {
						newSelectedIR = ir;
					}
					isOver |= isOverGroup;
					i++;
					if ((y + 33 * (i + 4)) > Screen.height) {
						// prevent entries going past bottom of screen...
						i = 0;
						x += colWidth + 67;
					}
				}
			}
			selectedIR = newSelectedIR;
			return isOver;*/
		}

		private void HandleInventarisationResultClicked (Progression.InventarisationResult ir)
		{
			InventarisationAction ia = (InventarisationAction)GameControl.self.scene.actions.GetAction (ir.actionId);
			InventarisationResultWindow irw = new InventarisationResultWindow (this, ir, ia);
			if (windows.ContainsKey (ir)) {
				windows [ir].Close ();
			}
			// On request: close all open windows...
			List<InventarisationResultWindow> tmpCopy = new List<InventarisationResultWindow> (windows.Values);
			foreach (InventarisationResultWindow w in tmpCopy) {
				w.Close ();
			}
			windows.Add (ir, irw);
		}

		private bool CheckGUILayoutMouseOver ()
		{
			return CheckGUILayoutMouseOver (0f);
		}

		private bool CheckGUILayoutMouseOver (float overrideWidth)
		{
			// Check if we have a mouse over, we set the width manually
			// because if we would have a scollbar this would not be taken
			// into account in the mouse over check. This principle
			// also applies to other mouse over checks.
			Rect uiRect = GUILayoutUtility.GetLastRect ();
			if (overrideWidth > 0f) uiRect.width = overrideWidth;
			bool isOver = uiRect.Contains (Event.current.mousePosition);
			CameraControl.MouseOverGUI |= isOver;
			return isOver;
		}

		private void RetrieveInventarisations ()
		{
			if (inventarisations != null) return;

			// Get and sort all inventarisations
			inventarisations = new List<Inventarisation> ();
			
			// Sort them by name
			foreach (Progression.InventarisationResult ir in GameControl.self.scene.progression.inventarisations)
			{
				// Add to the list
				Inventarisation inv = null;
				foreach (Inventarisation i in inventarisations) {
					if (i.name == ir.name) {
						inv = i;
						break;
					}
				}
				if (inv == null) {
					inv = new Inventarisation (ir.name);
					inventarisations.Add (inv);
				}
				inv.results.Add (ir);
			}
		}
		
		public void WindowIsClosed (Progression.InventarisationResult ir)
		{
			windows.Remove (ir);
			inventarisations = null;
			//selectedInvName = null;
			selectedIR = null;
		}
		
		public override void UpdateScene (Scene scene, GameButton button)
		{
			if (windows != null) {
				List<InventarisationResultWindow> tmpCopy = new List<InventarisationResultWindow> (windows.Values);
				foreach (InventarisationResultWindow w in tmpCopy) {
					w.Close ();
				}
			}
			name = button.name;
			selectedIR = null;
			selectedInvName = null;
			selectedINV = null;
			inventarisations = null;
			windows = new Dictionary<Progression.InventarisationResult, InventarisationResultWindow> ();
		}

		public override void UpdateState (GameButton button)
		{
			button.isVisible = CameraControl.IsNear;
			button.alwaysRender = false;
		}			
	}
}