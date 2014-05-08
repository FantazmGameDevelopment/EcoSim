using UnityEngine;
using System.Globalization;
using System.Collections;
using Ecosim.SceneData.Action;

namespace Ecosim.GameCtrl.GameButtons
{
	public class ActionDialogWindow : GameWindow
	{
		const int winWidth = 494;
		private int textHeight = 400;
		
		private readonly UserInteraction ui;
		private readonly DialogAction action;
		
		private readonly string dialogText;
		private readonly string shortText;
		private readonly string costStr;
		
		private static Texture2D tickbox;
		private static Texture2D tickboxEmpty;
		private static Texture2D tickboxH;
		private static Texture2D tickboxEmptyH;
		
		private bool isSelected = false;
		
		public ActionDialogWindow (UserInteraction ui, bool isSelected) : base (-1, -1, winWidth, ui.activeIcon)
		{
			this.ui = ui;
			GameControl.ExtraHelp (ui.help);
			this.isSelected = isSelected;
			action = (DialogAction) ui.action;
			GameControl.self.hideToolBar = true;
			GameControl.self.hideSuccessionButton = true;
			
			if (tickbox == null) {
				tickbox = Resources.Load ("Icons/tickbox_w") as Texture2D;
				tickboxEmpty = Resources.Load ("Icons/tickboxempty_w") as Texture2D;
				tickboxH = Resources.Load ("Icons/tickbox_zw") as Texture2D;
				tickboxEmptyH = Resources.Load ("Icons/tickboxempty_zw") as Texture2D;
			}
			dialogText = GameControl.self.scene.expression.ParseAndSubstitute (action.dialogText, true);
			shortText = GameControl.self.scene.expression.ParseAndSubstitute (action.shortDescText, true);
			costStr = ui.cost.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB"));
			textHeight = (int) formatted.CalcHeight (new GUIContent (action.dialogText), winWidth) + 4;
		}
		
		public override void Render ()
		{
			SimpleGUI.Label (new Rect (xOffset + 65, yOffset, winWidth - 65, 32), ui.name, title);
			SimpleGUI.Label (new Rect (xOffset, yOffset + 33, winWidth, textHeight), dialogText, formatted);
			SimpleGUI.Label (new Rect (xOffset, yOffset + textHeight + 34, 301, 32), shortText, entry);
			SimpleGUI.Label (new Rect (xOffset + 302, yOffset + textHeight + 34, winWidth - 302 - 33, 32), costStr, entry);
			if (SimpleGUI.Button (new Rect (xOffset + winWidth - 32, yOffset + textHeight + 34, 32, 32),
				isSelected?tickbox:tickboxEmpty, isSelected?tickboxH:tickboxEmptyH, black, white)) {
				isSelected = !isSelected;
			}
			SimpleGUI.Label (new Rect (xOffset, yOffset + textHeight + 67, 301, 32), "", header);
			if (SimpleGUI.Button (new Rect (xOffset + 302, yOffset + textHeight + 67, winWidth - 302, 32), "Accept", entry, entrySelected)) {
				if (isSelected) {
					action.DialogChangedToChecked ();
				}
				else {
					action.DialogChangedToUnchecked ();
				}
				Close ();
			}
			
			base.Render ();
		}
			
		protected override void OnClose ()
		{
			GameControl.ClearExtraHelp (ui.help);
			GameControl.self.hideToolBar = false;
			GameControl.self.hideSuccessionButton = false;
			GameControl.ExpensesChanged ();
			base.OnClose ();
		}
		
	}
}
