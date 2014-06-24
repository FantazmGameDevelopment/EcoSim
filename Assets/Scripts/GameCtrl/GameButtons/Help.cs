using UnityEngine;
using System.Collections;
using System;
using Ecosim;
using Ecosim.SceneData;

namespace Ecosim.GameCtrl.GameButtons
{
	public class Help : GameButtonHandler
	{
		private Texture2D iconTex;
		private static Texture2D helpDescrTex;
		private RenderEntry helpWindow;
		private Scene scene;
		
		
		
		public Help () {
			iconTex =  Resources.Load ("Icons/help_w") as Texture2D;
		}
			
		private class RenderEntry : GameWindow
		{
			private readonly Help parent;
			private readonly Texture2D tex;
						
			public RenderEntry (Help parent) : base (-1, -1, 512, parent.iconTex)
			{
				this.parent = parent;
				tex = Help.helpDescrTex;
				height = tex.height + 33;
			}
			
			/**
			 * renders help window
			 */
			public override void Render ()
			{
				SimpleGUI.Label (new Rect (xOffset + 65, yOffset, tex.width - 65, 32), "Help", title);
				SimpleGUI.Label (new Rect (xOffset, yOffset + 33, tex.width, tex.height), tex);
				base.Render ();
			}
			
			protected override void OnClose ()
			{
				parent.ClosedWindow ();
			}
		}
		
		private void ClosedWindow () {
			GameControl.self.showHelpTips = false;
			helpWindow = null;
		}

		
		public override void OnClick ()
		{
			if (helpWindow != null) {
				helpWindow.Close ();
			}
			helpWindow = new RenderEntry (this);
			GameControl.self.showHelpTips = true;
			base.OnClick ();
		}
		
		public override void UpdateScene (Scene scene, GameButton button)
		{
			if (scene != this.scene) {
				if (scene == null) {
					helpDescrTex = null;
				}
				else {
					Articles.Article article = scene.articles.GetArticleByName ("Help");
					if (article != null) {
						if (helpDescrTex == null) {
							helpDescrTex = new Texture2D (2, 2, TextureFormat.ARGB32, false, true);
						}
						RenderFontToTexture.self.RenderNewsArticle (article.text, GameControl.self.scene, helpDescrTex, true);
					}
					else {
						helpDescrTex = null;
					}
				}
				this.scene = scene;
			}
			button.isVisible = (helpDescrTex != null);
			base.UpdateScene (scene, button);
		}
		

		public override void UpdateState (GameButton button) {
			button.isVisible = (helpDescrTex != null);
			button.alwaysRender = false;
			helpWindow = null;
		}			
	}
}