using UnityEngine;
using System.Collections;
using System;
using Ecosim;
using Ecosim.SceneData;

namespace Ecosim.GameCtrl.GameButtons
{
	public class ShowOldArticles : GameButtonHandler
	{
		private Texture2D iconTex;
		private Texture2D backTex;
		private Texture2D forwardTex;
		private Texture2D backHTex;
		private Texture2D forwardHTex;
		private static Texture2D articleTex	;
		private RenderEntry articleWindow;
		private int index;
		private GUIStyle texBgStyle;
		
		
		
		public ShowOldArticles () {
			iconTex =  Resources.Load ("Icons/articles_w") as Texture2D;
			backTex =  Resources.Load ("Icons/back_w") as Texture2D;
			forwardTex =  Resources.Load ("Icons/forward_w") as Texture2D;
			backHTex =  Resources.Load ("Icons/back_zw") as Texture2D;
			forwardHTex =  Resources.Load ("Icons/forward_zw") as Texture2D;
			texBgStyle = GameControl.self.skin.GetStyle ("50");
		}
			
		private class RenderEntry : GameWindow
		{
			private readonly ShowOldArticles parent;
			private readonly Texture2D tex;
						
			public RenderEntry (ShowOldArticles parent) : base (-1, -1, 512, parent.iconTex)
			{
				this.parent = parent;
				tex = ShowOldArticles.articleTex;
				height = tex.height + 33;
			}
			
			/**
			 * renders help window
			 */
			public override void Render ()
			{
				if (SimpleGUI.Button (new Rect (xOffset + tex.width - 65, yOffset, 32, 32), parent.backTex, parent.backHTex, black, white)) {

					if (parent.index > 0) {
						parent.index -= 1;
						parent.RenderArticle ();
					}
				}
				if (SimpleGUI.Button (new Rect (xOffset + tex.width - 32, yOffset, 32, 32), parent.forwardTex, parent.forwardHTex, black, white)) {
					if (parent.index < GameControl.self.scene.progression.messages.Count - 1) {
						parent.index += 1;
						parent.RenderArticle ();
					}
				}
				SimpleGUI.Label (new Rect (xOffset + 65, yOffset, tex.width - 131, 32), "Previous Articles and Letters", title);
				SimpleGUI.Label (new Rect (xOffset, yOffset + 33, tex.width, tex.height), tex, parent.texBgStyle);
				base.Render ();
			}
			
			protected override void OnClose ()
			{
				parent.ClosedWindow ();
			}
		}
		
		private void ClosedWindow () {
			articleWindow = null;
		}

		
		public override void OnClick ()
		{
			if (articleWindow != null) {
				articleWindow.Close ();
			}
			if (GameControl.self.scene.progression.messages.Count == 0) {
				string text = "[letter]\n[par]There currenlty are no old articles available.";
				RenderFontToTexture.self.RenderNewsArticle (text, GameControl.self.scene, articleTex, true);
			}
			else {
				index = GameControl.self.scene.progression.messages.Count - 1;
				RenderArticle ();
			}
			articleWindow = new RenderEntry (this);
			base.OnClick ();
		}
		
		public void RenderArticle () {
			string text = GameControl.self.scene.progression.messages[index].text;
			RenderFontToTexture.self.RenderNewsArticle (text, GameControl.self.scene, articleTex, true);		
		}
		
		public override void UpdateScene (Scene scene, GameButton button)
		{
			if (articleTex == null) {
				articleTex = new Texture2D (2, 2, TextureFormat.ARGB32, false, true);
			}
			articleWindow = null;
			base.UpdateScene (scene, button);
		}
		

		public override void UpdateState (GameButton button) {
			button.isVisible = true;
			button.alwaysRender = false;
		}			
	}
}