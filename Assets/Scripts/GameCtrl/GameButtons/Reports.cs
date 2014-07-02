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
		private GUIStyle header;
		private GUIStyle entry;
		private GUIStyle entrySelected;
		private Progression.InventarisationResult selectedIR = null;
		private string selectedInvName = null;
		private string name;
		private Dictionary<Progression.InventarisationResult, InventarisationResultWindow> windows;

		private Dictionary<string, List<Progression.InventarisationResult>> inventarisations = null;
		
		public Reports ()
		{
			header = GameControl.self.skin.FindStyle ("ArialB16-50");
			entry = GameControl.self.skin.FindStyle ("Arial16-75");
			entrySelected = GameControl.self.skin.FindStyle ("Arial16-W");
		}
		
		public override bool SelectRender (GameButton button)
		{
			RetrieveInventarisations ();

			int colWidth = 164;
			int yearWidth = 65;
			int entryHeight = 32;

			foreach (KeyValuePair <string, List<Progression.InventarisationResult>> inv in inventarisations) 
			{
				int calcWidth = (int)entry.CalcSize (new GUIContent (inv.Key)).x;
				calcWidth = ((calcWidth / (entryHeight + 1)) + 1) * (entryHeight + 1) - 1;
				if (calcWidth > colWidth) {
					colWidth = calcWidth;
				}
			}

			bool isOver = false;
			int x = (int)(button.position.x + (entryHeight + 1));
			int y = (int)(button.position.y);
			int width = 6;
			isOver |= SimpleGUI.Label (new Rect (x, y, colWidth, entryHeight), name, header);
			isOver |= SimpleGUI.Label (new Rect (x + colWidth + 1, y, yearWidth, entryHeight), "Years", header);



			// Show sorted inventarisations
			Progression.InventarisationResult newSelectedIR = null;
			if (inventarisations.Count == 0) 
			{
				SimpleGUI.Label (new Rect (x, y + 33, 197, 32), "No reports available", entry);
			} 
			else 
			{
				int r = 0;
				foreach (KeyValuePair <string, List<Progression.InventarisationResult>> inv in inventarisations) 
				{
					// TODO: Make sure the Groups don't go over the screen height
					bool hl = (inv.Key == selectedInvName);
					bool isOverGroup = SimpleGUI.Label (new Rect (x, y + (entryHeight + 1) * (r + 1), colWidth, entryHeight), inv.Key, hl ? entrySelected : entry);
					isOver |= isOverGroup;
					r++;

					if (isOverGroup) selectedInvName = inv.Key;
					if (!isOverGroup && !hl) continue;

					// Show years seperately
					int i = 0;
					foreach (Progression.InventarisationResult ir in inv.Value)
					{
						hl = (ir == selectedIR);
						bool isOverYear = SimpleGUI.Label (new Rect (x + colWidth + 1, y + (entryHeight + 1) * (i + 1), yearWidth, entryHeight), ir.year.ToString (), hl ? entrySelected : entry);

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
							selectedInvName = inv.Key;
						}

						isOver |= isOverYear;
						i++;

						if ((y + 33 * (i + 4)) > Screen.height) {
							// prevent entries going past bottom of screen...
							i = 0;
							x += colWidth + 67;
						}
					}

					//if (!isOver && selectedInvName == inv.Key)
					//	selectedInvName = null;
				}
			}
			selectedIR = newSelectedIR;
			return isOver;

			///////////////////
			/////// OLD ///////
			///////////////////
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
			inventarisations = new Dictionary<string, List<Progression.InventarisationResult>> ();
			
			// Sort them by name
			foreach (Progression.InventarisationResult ir in GameControl.self.scene.progression.inventarisations)
			{
				// Add to the list
				if (!inventarisations.ContainsKey (ir.name)) {
					inventarisations.Add (ir.name, new List<Progression.InventarisationResult> ());
				}
				inventarisations [ir.name].Add (ir);
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
			windows = new Dictionary<Progression.InventarisationResult, InventarisationResultWindow> ();
		}

		public override void UpdateState (GameButton button)
		{
			button.isVisible = CameraControl.IsNear;
			button.alwaysRender = false;
		}			
	}
}