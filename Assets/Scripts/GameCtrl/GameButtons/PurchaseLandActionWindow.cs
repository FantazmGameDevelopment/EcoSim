using UnityEngine;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneData.Action;
using Ecosim.SceneData;

namespace Ecosim.GameCtrl.GameButtons
{
	public class PurchaseLandActionWindow : GameWindow
	{
		const int winWidth = 351;
		private int textHeight;
		
		private readonly UserInteraction ui;
		private readonly PurchaseLandAction action;
		
		private readonly string dialogText;
		private readonly string shortText;
		private readonly string costStr;
		private string totalCostStr;
		private string totalTilesStr;
		private long totalCost = -1L;
		
		// Save the total cost of the ui before start selecting
		private long preTotalCost = -1L;
		
		private static Texture2D tickbox;
		private static Texture2D tickboxEmpty;
		private static Texture2D tickboxH;
		private static Texture2D tickboxEmptyH;
		private bool isAccepted = false;
		
		private string inventarisationName;
		private int durationInYears;
		private Scene scene;

		private long preEstimatedTotalCostForYear;
		private List<int> selectedTilesPerPriceClass;
		private List<Texture2D> priceClassIcons;
		
		public PurchaseLandActionWindow (UserInteraction ui) : base (-1, -1, winWidth, ui.activeIcon)
		{
			this.ui = ui;
			this.scene = GameControl.self.scene;

			GameControl.ExtraHelp (this.ui.help);
			GameControl.self.hideToolBar = true;
			GameControl.self.hideSuccessionButton = true;

			this.action = (PurchaseLandAction) ui.action;
			this.action.StartSelecting (this.ui);
			this.action.OnTileChanged += HandleActionOnTileChanged;

			this.preEstimatedTotalCostForYear = this.ui.estimatedTotalCostForYear;
			this.ui.estimatedTotalCostForYear = 0L;
			this.selectedTilesPerPriceClass = new List<int> ();
			this.priceClassIcons = new List<Texture2D> ();
			foreach (Progression.PriceClass pc in this.scene.progression.priceClasses) {
				this.selectedTilesPerPriceClass.Add (0);
				this.priceClassIcons.Add (this.scene.assets.GetHighlightedIcon (pc.normalIconId));
			}

			// Update selected tiles per price class counts
			int priceClasses = this.scene.progression.priceClasses.Count;
			foreach (ValueCoordinate vc in this.scene.progression.GetData (this.action.areaName).EnumerateNotZero ()) {
				if (vc.v > priceClasses) {
					this.selectedTilesPerPriceClass [vc.v - priceClasses - 1]++;
				}
			}
		}
		
		public override void Render ()
		{
			float h = 32;
			SimpleGUI.Label (new Rect(xOffset + 65,  yOffset, winWidth - 65,h), ui.description, title);

			float x = xOffset;
			float y = yOffset + textHeight + 34;
			float w = 0;

			string costFormat = ("#,##0\\.-");
			CultureInfo ci = CultureInfo.GetCultureInfo ("en-GB");

			int idx = 0;
			int totalCost = 0;
			foreach (Progression.PriceClass pc in this.scene.progression.priceClasses) 
			{
				x = xOffset;

				w = 32;
				SimpleGUI.Label (new Rect (x,y,w,h), this.priceClassIcons[idx], black);
				//w = 175;
				//SimpleGUI.Label (new Rect (x,y,w,h), pc.name, entry);
				x += w + 1; w = 90;
				SimpleGUI.Label (new Rect (x,y,w,h), pc.cost.ToString (costFormat, ci), entry);
				x += w + 1;	w = 32;
				SimpleGUI.Label (new Rect (x,y,w,h), "x", entry);
				x += w + 1; w = 70;
				SimpleGUI.Label (new Rect (x,y,w,h), this.selectedTilesPerPriceClass [idx].ToString (), entry);
				x += w + 1;	w = 32;
				SimpleGUI.Label (new Rect (x,y,w,h), "=", entry);
				x += w + 1; w = 90;

				int pcTotalCost = pc.cost * this.selectedTilesPerPriceClass [idx];
				SimpleGUI.Label (new Rect (x,y,w,h), pcTotalCost.ToString (costFormat, ci), entry);
				y += h + 1;

				totalCost += pcTotalCost;
				idx++;
			}
			this.ui.estimatedTotalCostForYear = totalCost;

			x = xOffset;
			w = 227;
			SimpleGUI.Label (new Rect (x,y,w,h), "Total cost", entry);
			x += w + 1; w = 32;
			SimpleGUI.Label (new Rect (x,y,w,h), "=", entry);
			x += w + 1; w = 90;
			SimpleGUI.Label (new Rect (x,y,w,h), this.ui.estimatedTotalCostForYear.ToString (costFormat, ci), entry);
			y += h + 1;

			w = winWidth - w;
			w = 175;

			x = xOffset;
			w = 227;
			SimpleGUI.Label (new Rect (x,y,w,h), "", header);
			x += w + 1; w = 123;
			if (SimpleGUI.Button (new Rect (x,y,w,h), "Accept", entry, entrySelected)) {
				isAccepted = true;
				Close ();
			}
			base.Render ();
		}
		
		protected override void OnClose ()
		{
			GameControl.ClearExtraHelp (this.ui.help);
			GameControl.self.hideToolBar = false;
			GameControl.self.hideSuccessionButton = false;
			
			// Finish selection (resets costs for all UIs)
			action.FinishSelecting (this.ui, !this.isAccepted);

			// Undo
			if (!this.isAccepted)
				this.ui.estimatedTotalCostForYear = this.preEstimatedTotalCostForYear;

			// Expenses has changed
			GameControl.ExpensesChanged ();

			base.OnClose ();
		}

		private void HandleActionOnTileChanged (int x, int y, int oldV, int newV) 
		{
			if (oldV == newV) return;

			// Calculate new total cost
			int priceClasses = this.scene.progression.priceClasses.Count;

			// Check for selected yes or no
			if (oldV < newV) {
				// Newly selected
				int idx = (newV - priceClasses - 1);
				this.selectedTilesPerPriceClass [idx]++;
			} else {
				// Deselected
				int idx = (newV - 1);
				this.selectedTilesPerPriceClass [idx]--;
			}
		}
	}
}
