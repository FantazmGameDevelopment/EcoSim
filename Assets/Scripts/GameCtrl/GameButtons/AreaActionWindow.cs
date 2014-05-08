using UnityEngine;
using System.Globalization;
using System.Collections;
using Ecosim.SceneData.Action;

namespace Ecosim.GameCtrl.GameButtons
{
	public class AreaActionWindow : GameWindow
	{
		const int winWidth = 384;
		
		private readonly UserInteraction ui;
		private readonly AreaAction action;
		
		private readonly string dialogText;
		private readonly string shortText;
		private readonly string costStr;
		private string totalCostStr;
		private string totalTilesStr;
		private long totalCost = -1L;
		
		private static Texture2D tickbox;
		private static Texture2D tickboxEmpty;
		private static Texture2D tickboxH;
		private static Texture2D tickboxEmptyH;
		private bool isAccepted = false;
		
		public AreaActionWindow (UserInteraction ui) : base (-1, -1, winWidth, ui.activeIcon)
		{
			this.ui = ui;
			GameControl.ExtraHelp (ui.help);
			action = (AreaAction) ui.action;
			GameControl.self.hideToolBar = true;
			GameControl.self.hideSuccessionButton = true;
			
			costStr = ui.cost.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB"));
			action.StartSelecting (ui);
		}
		
		public override void Render ()
		{
			if (totalCost != ui.estimatedTotalCostForYear) {
				totalCost = ui.estimatedTotalCostForYear;
				int nrTiles = (int) (totalCost / ui.cost);
				totalCostStr = totalCost.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB"));
				totalTilesStr = nrTiles.ToString ("#,##0", CultureInfo.GetCultureInfo ("en-GB"));
			}
			SimpleGUI.Label (new Rect (xOffset + 65, yOffset, winWidth - 65, 32), ui.name, title);
			SimpleGUI.Label (new Rect (xOffset, yOffset + 33, winWidth, 65), ui.description, formatted);
			SimpleGUI.Label (new Rect (xOffset, yOffset + 99, 263, 32), "Selected tiles", entry);
			SimpleGUI.Label (new Rect (xOffset + 264, yOffset + 99, 88, 32), totalTilesStr, entry);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + 99, 32, 32), "", entry);
			SimpleGUI.Label (new Rect (xOffset, yOffset + 132, 263, 32), "Cost per tile", entry);
			SimpleGUI.Label (new Rect (xOffset + 264, yOffset + 132, 88, 32), costStr, entry);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + 132, 32, 32), "x", entry);
			SimpleGUI.Label (new Rect (xOffset, yOffset + 165, 263, 32), "Total cost", entry);
			SimpleGUI.Label (new Rect (xOffset + 264, yOffset + 165, 88, 32), totalCostStr, entry);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + 165, 32, 32), "=", entry);
			SimpleGUI.Label (new Rect (xOffset, yOffset + 198, 261, 32), "", header);
			if (SimpleGUI.Button (new Rect (xOffset + 262, yOffset + 198, winWidth - 262, 32), "Accept", entry, entrySelected)) {
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
			action.FinishSelecting (ui, !isAccepted);
			GameControl.ExpensesChanged ();
			base.OnClose ();
		}
		
	}
}
