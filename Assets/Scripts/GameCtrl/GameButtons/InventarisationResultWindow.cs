using UnityEngine;
using System.Globalization;
using System.Collections;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;

namespace Ecosim.GameCtrl.GameButtons
{
	public class InventarisationResultWindow : GameWindow
	{
		private int winWidth = 296;
		
		private readonly InventarisationAction action;
		private readonly Progression.InventarisationResult result;
		private EditData edit;
		private int[] counts;
						
		private readonly Scene scene;
		private readonly Reports parent;
		
		private static int CalcWidth (GUIStyle style, Progression.InventarisationResult ir) {
			int calcWidth = (int) style.CalcSize (new GUIContent (ir.name + " " + ir.year)).x;
			calcWidth = ((calcWidth / 33) + 1) * 33 + 64;
			if (calcWidth < 263) calcWidth = 263;
			return calcWidth;
		}
		
		public InventarisationResultWindow (Reports parent, Progression.InventarisationResult ir, InventarisationAction action) :
			base (-1, -1, CalcWidth (title, ir), action.uiList[0].activeIcon)
		{
			this.winWidth = CalcWidth (title, ir);
			this.parent = parent;
			this.action = action;
			this.result = ir;
			this.scene = GameControl.self.scene;
			edit = action.GetInventarisationMap (ir.AreaMap);
			counts = new int[InventarisationAction.MAX_VALUE_INDEX + 1];
			foreach (ValueCoordinate vc in ir.AreaMap.EnumerateNotZero ()) {
				int val = vc.v - 1;
				if (val <= InventarisationAction.MAX_VALUE_INDEX) {
					counts [val] ++;
				}
			}
		}
		
		public override void Render ()
		{
			SimpleGUI.Label (new Rect (xOffset + 65, yOffset, winWidth - 65, 32), result.name + " " + result.year, title);
			int index = 0;
			for (int i = 0; i < InventarisationAction.MAX_VALUE_INDEX; i++) {
				InventarisationAction.InventarisationValue iv = action.valueTypes[i];
				if (iv != null) {
					index ++;
					SimpleGUI.Label (new Rect (xOffset, yOffset + 33 * index, 32, 32), scene.assets.GetHighlightedIcon (iv.iconId), black);
					SimpleGUI.Label (new Rect (xOffset + 33, yOffset + 33 * index, winWidth - 132, 32), iv.name, entry);
					SimpleGUI.Label (new Rect (xOffset + winWidth - 98, yOffset + 33 * index, 98, 32), counts [i].ToString (), entry);
				}
			}
			base.Render ();
		}
			
		protected override void OnClose ()
		{
			parent.WindowIsClosed (result);
			edit.Delete ();
			base.OnClose ();
		}
		
	}
}
