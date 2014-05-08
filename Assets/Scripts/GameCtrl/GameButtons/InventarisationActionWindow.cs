using UnityEngine;
using System.Globalization;
using System.Collections;
using Ecosim.SceneData.Action;

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
		
		private static Texture2D tickbox;
		private static Texture2D tickboxEmpty;
		private static Texture2D tickboxH;
		private static Texture2D tickboxEmptyH;
		private bool isAccepted = false;
		
		public InventarisationActionWindow (UserInteraction ui) : base (-1, -1, winWidth, ui.activeIcon)
		{
			this.ui = ui;
			GameControl.ExtraHelp (ui.help);
			action = (InventarisationAction) ui.action;
			GameControl.self.hideToolBar = true;
			GameControl.self.hideSuccessionButton = true;
			
			costStr = ui.cost.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB"));
			action.StartSelecting (ui);
			textHeight = (int) formatted.CalcHeight (new GUIContent (ui.description), winWidth) + 4;
		}
		
		public override void Render ()
		{
			if (totalCost != ui.estimatedTotalCostForYear) {
				totalCost = ui.estimatedTotalCostForYear;
				int nrTiles = (ui.cost == 0)?0:((int) (totalCost / ui.cost));
				totalCostStr = totalCost.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB"));
				totalTilesStr = nrTiles.ToString ("#,##0", CultureInfo.GetCultureInfo ("en-GB"));
			}
			SimpleGUI.Label (new Rect (xOffset + 65, yOffset, winWidth - 65, 32), ui.name, title);
			SimpleGUI.Label (new Rect (xOffset, yOffset + 33, winWidth, textHeight), ui.description, formatted);
			SimpleGUI.Label (new Rect (xOffset, yOffset + textHeight + 34, 263, 32), "Selected tiles", entry);
			SimpleGUI.Label (new Rect (xOffset + 264, yOffset + textHeight + 34, 88, 32), totalTilesStr, entry);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + textHeight + 34, 32, 32), "", entry);
			SimpleGUI.Label (new Rect (xOffset, yOffset + textHeight + 67, 263, 32), "Cost per tile", entry);
			SimpleGUI.Label (new Rect (xOffset + 264, yOffset + textHeight + 67, 88, 32), costStr, entry);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + textHeight + 67, 32, 32), "x", entry);
			SimpleGUI.Label (new Rect (xOffset, yOffset + textHeight + 100, 263, 32), "Total cost", entry);
			SimpleGUI.Label (new Rect (xOffset + 264, yOffset + textHeight + 100, 88, 32), totalCostStr, entry);
			SimpleGUI.Label (new Rect (xOffset + 353, yOffset + textHeight + 100, 32, 32), "=", entry);
			SimpleGUI.Label (new Rect (xOffset, yOffset + textHeight + 133, 261, 32), "", header);
			if (SimpleGUI.Button (new Rect (xOffset + 262, yOffset + textHeight + 133, winWidth - 262, 32), "Accept", entry, entrySelected)) {
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
