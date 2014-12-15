using UnityEngine;
using System.Globalization;
using System.Collections;
using Ecosim.SceneData.Action;
using Ecosim.SceneData;

namespace Ecosim.GameCtrl.GameButtons
{
	public class InventarisationActionWindow : GameWindow
	{
		const int winWidth = 384;
		private int textHeight;
		
		private readonly UserInteraction ui;
		private readonly InventarisationAction action;
		
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
		
		public InventarisationActionWindow (UserInteraction ui) : base (-1, -1, winWidth, ui.activeIcon)
		{
			this.ui = ui;
			// We save the total cost and combine the totals so we can make multiple inventarisation of the same kind
			preTotalCost = ui.estimatedTotalCostForYear;
			ui.estimatedTotalCostForYear = 0L;

			GameControl.ExtraHelp (ui.help);
			action = (InventarisationAction) ui.action;
			GameControl.self.hideToolBar = true;
			GameControl.self.hideSuccessionButton = true;
			
			costStr = ui.cost.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB"));
			action.StartSelecting (ui);
			textHeight = 32;//(int) formatted.CalcHeight (new GUIContent (ui.description), winWidth) + 4;
			inventarisationName = ui.name;
			durationInYears = 1;
		}
		
		public override void Render ()
		{
			long newTotalCost = ui.estimatedTotalCostForYear * durationInYears;
			if (totalCost != newTotalCost) {
				totalCost = newTotalCost;
				int nrTiles = (ui.cost == 0)?0:((int) (totalCost / ui.cost / durationInYears));
				totalCostStr = totalCost.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB"));
				totalTilesStr = nrTiles.ToString ("#,##0", CultureInfo.GetCultureInfo ("en-GB"));
			}

			//SimpleGUI.Label (new Rect (xOffset, yOffset + 33, winWidth, textHeight), ui.description, formatted);
			SimpleGUI.Label (new Rect(xOffset + 65,  yOffset, winWidth - 65, 32), "New survey", title);
			SimpleGUI.Label (new Rect(xOffset, 	 	 yOffset + 33, 70, 32), "Name", entry); 
			inventarisationName = SimpleGUI.TextField (new Rect (xOffset + 71, yOffset + 33, winWidth - 71, textHeight), inventarisationName, 50, header);

			SimpleGUI.Label (new Rect (xOffset, 	  yOffset + textHeight + 34, 263, 32), "Selected tiles", entry);
			SimpleGUI.Label (new Rect (xOffset + 264, yOffset + textHeight + 34, 88, 32), totalTilesStr, entry);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + textHeight + 34, 32, 32), "", entry);
			SimpleGUI.Label (new Rect (xOffset, 	  yOffset + textHeight + 67, 263, 32), "Cost per tile", entry);
			SimpleGUI.Label (new Rect (xOffset + 264, yOffset + textHeight + 67, 88, 32), costStr, entry);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + textHeight + 67, 32, 32), "x", entry);

			// Amount of years
			string label = "Duration (years)";// (GameControl.self.scene.progression.yearsPerTurn == 1)?"Duration (years)":"Duration (turns)";
			SimpleGUI.Label (new Rect (xOffset, yOffset + textHeight + 100, 263, 32), label, entry); 
			string years = SimpleGUI.TextField (new Rect (xOffset + 264, yOffset + textHeight + 100, 88, 32), durationInYears.ToString(), header);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + textHeight + 100, 32, 32), "x", entry);

			// Strip non numeric characters
			int outI = 1;
			int.TryParse (years, out outI);
			durationInYears = Mathf.Clamp (outI, 1, 1000);

			SimpleGUI.Label (new Rect (xOffset, 	  yOffset + textHeight + 133, 263, 32), "Total cost", entry);
			SimpleGUI.Label (new Rect (xOffset + 264, yOffset + textHeight + 133, 88, 32), totalCostStr, entry);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + textHeight + 133, 32, 32), "=", entry);

			SimpleGUI.Label (new Rect (xOffset, yOffset + textHeight + 166, 261, 32), "", header);
			if (SimpleGUI.Button (new Rect (xOffset + 263, yOffset + textHeight + 166, winWidth - 262, 32), "Accept", entry, entrySelected)) {
				isAccepted = true;
				Close ();
			}
			base.Render ();
		}
			
		protected override void OnClose ()
		{
			GameControl.ClearExtraHelp (ui.help);
			GameControl.self.hideToolBar = false;
			GameControl.self.hideSuccessionButton = false;

			// Finish selection (resets costs for all UIs)
			action.FinishSelecting (ui, !isAccepted);

			// Manually (re)set the total cost for this year,
			// the value is stored in the constructor
			ui.estimatedTotalCostForYear = preTotalCost;

			if (isAccepted)
			{
				// Create a new active inventarisation
				Scene scene = GameControl.self.scene;
				int startYear = scene.progression.year;
				int lastYear = startYear + (durationInYears * scene.progression.yearsPerTurn);
				string name = inventarisationName;
				string areaName = action.invAreaName;
				int actionId = action.id;
				Progression.Inventarisation inv = new Progression.Inventarisation (scene, startYear, lastYear, name, areaName, actionId, ui.index, (int)totalCost);
				scene.progression.activeInventarisations.Add (inv);

				// Add the newly made costs to the 'total'
				ui.estimatedTotalCostForYear += totalCost;
			}

			GameControl.ExpensesChanged ();

			base.OnClose ();
		}
		
	}
}
