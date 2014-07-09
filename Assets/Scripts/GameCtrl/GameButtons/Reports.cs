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
		private string selectedInvName = null;
		private string name;
		private Dictionary<Progression.InventarisationResult, InventarisationResultWindow> windows;

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
			int yearWidth = 65;
			int yearToggleBtnWidth = 32;
			int graphEditorWidth = 150;
			int invGraphEditorBtnWidth = 100;
			int graphBtnWidth = 120;
			int entryHeight = 32;

			foreach (Inventarisation inv in inventarisations) 
			{
				// Calculate the width of the entry name, with or without the graph editor count
				int calcWidth = (int)entry.CalcSize (new GUIContent (inv.name + ((graphEditorOpened)?" (99/99)":""))).x;
				calcWidth = ((calcWidth / (entryHeight + 1)) + 1) * (entryHeight + 1) - 1;
				if (calcWidth > colWidth) {
					colWidth = calcWidth;
				}
			}

			bool isOver = false;
			int x = (int)(button.position.x + (entryHeight + 1));
			int y = (int)(button.position.y);
			int width = 6;

			// Graph editor
			ExportMgr.self.exportEnabled = true;
			if (ExportMgr.self.exportEnabled) 
			{
				Rect graphRect = new Rect (x, y, graphEditorWidth, entryHeight);

				// Label
				graphRect.width = colWidth;
				isOver |= SimpleGUI.Label (graphRect, "Graph Editor", header);  
				graphRect.x += graphRect.width + 1;

				graphRect.width = yearWidth;
				isOver |= SimpleGUI.CheckMouseOver (graphRect); 
				if (SimpleGUI.Button (graphRect, "Toggle", entry, entrySelected)) {
					graphEditorOpened = !graphEditorOpened;
				}
				
				// Show graph editor controls
				if (graphEditorOpened) 
				{
					y += entryHeight + 1;
					Rect r = new Rect (x, y, graphBtnWidth, entryHeight);
					
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
					if (SimpleGUI.Button (r, "Clear selection", entry, entrySelected)) { 
						foreach (Inventarisation i in inventarisations) {
							foreach (Progression.InventarisationResult ir in i.results) {
								ir.selected = false;
							}
						}
					}
					// Generate graph
					r.x += r.width + 1;
					isOver |= SimpleGUI.CheckMouseOver (r);
					if (SimpleGUI.Button (r, "Generate", entry, entrySelected)) {
						new Ecosim.GameCtrl.ExportGraphWindow ();
					}
				}

				y += entryHeight + 1;
			}

			// Inventarisations
			isOver |= SimpleGUI.Label (new Rect (x, y, colWidth, entryHeight), name, header);
			// Years
			isOver |= SimpleGUI.Label (new Rect (x + colWidth + 1, y, yearWidth, entryHeight), "Years", header);

			if (isOver) {
				selectedIR = null;
				selectedInvName = null;
			}

			// Show sorted inventarisations
			Progression.InventarisationResult newSelectedIR = null;
			if (inventarisations.Count == 0) 
			{
				//y = y + ((graphEditorOpened) ? (entryHeight + 1) : 0);
				SimpleGUI.Label (new Rect (x, y + (entryHeight + 1), 197, entryHeight), "No reports available", entry);
			} 
			else 
			{
				int r = 0;
				foreach (Inventarisation inv in inventarisations) 
				{
					// Get the graph suffix
					string graphSuffix = "";
					if (graphEditorOpened) 
					{
						graphSuffix = " ";
						int total = inv.results.Count;
						int selected = 0;
						foreach (Progression.InventarisationResult ir in inv.results) {
							if (ir.selected)
								selected++;
						}
						graphSuffix = string.Format (" ({0}/{1})", selected, total);
					}

					// TODO: Make sure the Groups don't go over the screen height

					// Entry
					bool hl = (inv.name == selectedInvName);
					Rect invR = new Rect (x, y + (entryHeight + 1) * (r + 1), colWidth, entryHeight);
					bool isOverGroup = SimpleGUI.Label (invR, inv.name + graphSuffix, hl ? entrySelected : entry);
					invR.x += invR.width + 1;

					// Graph controls
					if (graphEditorOpened)
					{
						/*Rect invBtnR = invR;
						invBtnR.width = invGraphEditorBtnWidth;

						// De/select all
						isOverGroup |= SimpleGUI.CheckMouseOver (invBtnR);
						if (SimpleGUI.Button (invBtnR, "De/select all", entry, entrySelected)) { 
							// Check if we should deselect or select all
							bool foundSelected = false;
							foreach (Progression.InventarisationResult ir in inv.results) {
								if (ir.selected) {
									foundSelected = true;
									break;
								}
							}
							// Deselect all
							foreach (Progression.InventarisationResult ir in inv.results) {
								// If we found a selected, deselect, if we didn't find a selected, select it
								ir.selected = !foundSelected;
							}
						}

						invR.x += invBtnR.width + 1;*/

						// Toggle when clicking on the entry
						if (isOverGroup && Event.current.type == EventType.MouseDown) {
							// Use the event
							Event.current.Use ();

							// Check if we should deselect or select all
							bool foundSelected = false;
							foreach (Progression.InventarisationResult ir in inv.results) {
								if (ir.selected) {
									foundSelected = true;
									break;
								}
							}
							// Deselect all
							foreach (Progression.InventarisationResult ir in inv.results) {
								// If we found a selected, deselect, if we didn't find a selected, select it
								ir.selected = !foundSelected;
							}
						}
					}

					isOver |= isOverGroup;
					r++;

					if (isOverGroup) selectedInvName = inv.name;
					if (!isOverGroup && !hl) continue;

					// Show years seperately
					int i = 0;
					foreach (Progression.InventarisationResult ir in inv.results)
					{
						hl = (ir == selectedIR);
						Rect yearR = new Rect (invR.x, y + ((entryHeight + 1) * (i + 1)), yearWidth, entryHeight);
						bool isOverYear = SimpleGUI.Label (yearR, ir.year.ToString (), hl ? entrySelected : entry);
						isOver |= isOverYear;

						if (isOverYear && (Event.current.type == EventType.mouseDown))
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
							Event.current.Use ();
						}

						if (isOverYear) {
							newSelectedIR = ir;
							selectedInvName = inv.name;
						}

						// Graph toggle button
						if (graphEditorOpened) 
						{
							Rect toggleR = yearR;
							toggleR.width = yearToggleBtnWidth;
							toggleR.x += yearWidth + 1;
							isOver |= SimpleGUI.CheckMouseOver (toggleR);

							// Toggle button
							if (ir.selected) {
								if (SimpleGUI.Button (toggleR, "<b> X</b>", entry, entrySelected)) { 
									ir.selected = false;
								} 
							} else {
								if (SimpleGUI.Button (toggleR, null, null, entry, entrySelected)) {
									ir.selected = true;
								}
							}
						}

						i++;
						if ((y + (entryHeight + 1) * (i + 4)) > Screen.height) {
							// prevent entries going past bottom of screen...
							i = 0;
							x += yearWidth + 1 + ((graphEditorOpened) ? yearToggleBtnWidth : 0);
						}
					}

					//if (!isOver && selectedInvName == inv.Key)
					//	selectedInvName = null;
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

		private void RetrieveInventarisations ()
		{
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
			selectedInvName = null;
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