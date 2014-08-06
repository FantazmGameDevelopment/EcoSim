using UnityEngine;
using System.Collections.Generic;
using System;
using System.Globalization;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;

namespace Ecosim.GameCtrl.GameButtons
{
	public class VariablesAndFormulas : GameButtonHandler
	{
		public Texture2D iconTex;
		private Texture2D toggleVisual;
		private Texture2D toggleVisualH;
		private GUIStyle header;
		private GUIStyle entry;
		private GUIStyle entrySelected;
		private GUIStyle textBgStyle;

		private VariablesAndFormulasWindow window;

		public VariablesAndFormulas ()
		{
			//iconTex = Resources.Load ("Icons/variables_w") as Texture2D;
			header = GameControl.self.skin.FindStyle ("ArialB16-50");
			entry = GameControl.self.skin.FindStyle ("Arial16-75");
			entrySelected = GameControl.self.skin.FindStyle ("Arial16-W");
			toggleVisual = (Texture2D)Resources.Load ("Icons/cross_w", typeof (Texture2D));
			toggleVisualH = (Texture2D)Resources.Load ("Icons/cross_zw", typeof (Texture2D));
			textBgStyle = GameControl.self.skin.GetStyle ("50");
		}

		public override bool SelectRender (GameButton button)
		{
			bool isOver = false;
			return isOver;
		}

		public override void UpdateState (GameButton button)
		{
			iconTex = button.icon;
			button.isVisible = GameControl.self.scene.progression.showVariablesInGame; 
		}

		public override void OnClick ()
		{
			if (window != null) {
				window.Close ();
			}
			
			window = new VariablesAndFormulasWindow (this);
			base.OnClick ();
		}
	}
}
