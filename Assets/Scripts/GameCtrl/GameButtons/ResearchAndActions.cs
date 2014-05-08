using UnityEngine;
using System.Collections.Generic;
using System;
using System.Globalization;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;

namespace Ecosim.GameCtrl.GameButtons
{
	public class ResearchAndActions : GameButtonHandler
	{
		private Texture2D closeTex;
		private Texture2D closeHTex;
		private GUIStyle header;
		private GUIStyle entryNoText;
		private GUIStyle entryNoTextSelected;
		private GUIStyle entry;
		private GUIStyle entrySelected;
		private GUIStyle entryRJ;
		private GUIStyle entryRJSelected;
		private int selectedIndex = -1;
		private UserInteraction selectedUI = null;
		private UserInteractionGroup grp;
		private bool isResearch;
		private string name;

		public ResearchAndActions ()
		{
			header = GameControl.self.skin.FindStyle ("ArialB16-50");
			entryNoText = GameControl.self.skin.FindStyle ("75");
			entryNoTextSelected = GameControl.self.skin.FindStyle ("BGWhite");
			entry = GameControl.self.skin.FindStyle ("Arial16-75");
			entrySelected = GameControl.self.skin.FindStyle ("Arial16-W");
			entryRJ = GameControl.self.skin.FindStyle ("Arial16-75-Right");
			entryRJSelected = GameControl.self.skin.FindStyle ("Arial16-W-Right");
		}
		
		public override bool SelectRender (GameButton button)
		{
			bool isOver = false;
			int x = (int)(button.position.x + 33);
			int y = (int)(button.position.y);
			int width = Mathf.Max (4, grp.groups.Length);
			isOver |= SimpleGUI.Label (new Rect (x, y, width * 33 - 1, 32), name, header);
			int i = 0;
			UserInteraction newSelectedUI = null;
			foreach (UserInteractionGroup.GroupData gd in grp.groups) {
				bool isOverGroup = SimpleGUI.Label (new Rect (x + 33 * i, y + 33, 32, 32),
					(i == selectedIndex) ? (gd.icon) : (gd.activeIcon),
					(i == selectedIndex) ? entryNoTextSelected : entryNoText);
				isOver |= isOverGroup;
				if (isOverGroup) {
					selectedIndex = i;
				}
				if (selectedIndex == i) {
					int colWidth = 164;
					foreach (UserInteraction ui in gd.uiList) {
						if (ui.action.isActive) {
							int itemWidth = (int) entry.CalcSize(new GUIContent (ui.name)).x;
							itemWidth = ((itemWidth / 33) + 1) * 33 - 1;
							if (itemWidth > colWidth) {
								colWidth = itemWidth;
							}
						}
					}
					int j = 0;
					int xExtra = 0;
					foreach (UserInteraction ui in gd.uiList) {
						if (ui.action.isActive) {
							bool hl = (ui == selectedUI);
							bool isOverUI = SimpleGUI.Label (new Rect (x + xExtra + 33 * i, y + 33 * (j + 2), 32, 32),
							hl ? (ui.icon) : (ui.activeIcon), hl ? entryNoTextSelected : entryNoText);
							isOverUI |= SimpleGUI.Label (new Rect (x + xExtra + 33 * i + 33, y + 33 * (j + 2), colWidth, 32), ui.name,
							hl ? entrySelected : entry);
							isOverUI |= SimpleGUI.Label (new Rect (x + xExtra + 33 * i + colWidth + 34, y + 33 * (j + 2), 98, 32),
							ui.cost.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB")),
							hl ? entryRJSelected : entryRJ);
							if (isOverUI) {
								newSelectedUI = ui;
							}
							isOver |= isOverUI;
							if (isOverUI && (Event.current.type == EventType.MouseDown)) {
								ui.action.ActionSelected (ui);
								Event.current.Use ();
								CameraControl.MouseOverGUI = true;
							}
							j++;
							if ((y + 33 * (j + 4)) > Screen.height) {
								// prevent entries going past bottom of screen...
								j = 0;
								xExtra += colWidth + 34 + 99;
							}
						}
					}
				}
				i++;
			}
			if (selectedUI != newSelectedUI) {
				if (newSelectedUI != null) {
					GameControl.ExtraHelp (newSelectedUI.description);
				}
			}
			selectedUI = newSelectedUI;
			while (i < width) {
				isOver |= SimpleGUI.Label (new Rect (x + 33 * i, y + 33, 32, 32), "", header);
				i++;
			}
			return isOver;
		}
		
		public override void UpdateScene (Scene scene, GameButton button)
		{
			if (button.name == "Research") {
				scene.actions.uiGroups.TryGetValue (UserInteractionGroup.CATEGORY_RESEARCH, out grp);
				isResearch = true;
			} else {
				scene.actions.uiGroups.TryGetValue (UserInteractionGroup.CATEGORY_MEASURES, out grp);
				isResearch = false;
			}
			name = button.name;
			selectedIndex = -1;
		}

		public override void UpdateState (GameButton button)
		{
			button.isVisible = ((grp != null) && (grp.groups.Length > 0)) && CameraControl.IsNear;
			if (isResearch && !(GameControl.self.scene.progression.allowResearch))
				button.isVisible = false;
			if (!isResearch && !(GameControl.self.scene.progression.allowMeasures))
				button.isVisible = false;
			button.alwaysRender = false;
		}			
	}
}