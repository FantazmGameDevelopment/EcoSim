using UnityEngine;
using System.Collections.Generic;
using System;
using Ecosim;
using Ecosim.SceneData;

namespace Ecosim.GameCtrl.GameButtons
{
	public class Encyclopedia : GameButtonHandler
	{
		private Texture2D iconTex;
		private Texture2D closeTex;
		private Texture2D closeHTex;
		private GUIStyle header;
		private GUIStyle entry;
		private GUIStyle entrySelected;
		private int selectedIndex = -1;
		private Dictionary<char, List<Articles.EncyclopediaEntry>> entries;
		private List<char> sortedKeys;
		
		private Dictionary<string, RenderEntry> encWindows;
		
		private class RenderEntry : GameWindow
		{
			private readonly Encyclopedia parent;
//			private readonly Articles.EncyclopediaEntry entry;
			public readonly string keyword;
			public readonly string url;
			private readonly Texture2D tex;
						
			public RenderEntry (Encyclopedia parent, Articles.EncyclopediaEntry entry) : base (-1, -1, 512, parent.iconTex)
			{
				this.parent = parent;
//				this.entry = entry;
				this.keyword = entry.keyword;
				this.url = entry.url;
				tex = new Texture2D (2, 2, TextureFormat.ARGB32, false, true);
				RenderFontToTexture.self.RenderNewsArticle (entry.text, GameControl.self.scene, tex, true);
				height = tex.height + 33;
				if (parent.encWindows.ContainsKey (keyword)) {
					parent.encWindows[keyword].Close ();
				}
				parent.encWindows.Add (keyword, this);
			}
			
			/**
			 * renders encyclopedia window
			 */
			public override void Render ()
			{
				if (url != null) {
					SimpleGUI.Label (new Rect (xOffset + 65, yOffset, tex.width - 65 - 165, 32), keyword, title);
					if (SimpleGUI.Button (new Rect (xOffset + tex.width - 164, yOffset, 164, 32), "More...", entry, entrySelected)) {
						Application.OpenURL(url);
					}
				}
				else {
					SimpleGUI.Label (new Rect (xOffset + 65, yOffset, tex.width - 65, 32), keyword, title);
				}
				SimpleGUI.Label (new Rect (xOffset, yOffset + 33, tex.width, tex.height), tex);
				base.Render ();
			}
			
			protected override void OnClose ()
			{
				parent.encWindows.Remove (keyword);
				base.OnClose ();
			}
		}
		
		public Encyclopedia ()
		{
			iconTex = Resources.Load ("Icons/encyclopedia_w") as Texture2D;
			header = GameControl.self.skin.FindStyle ("ArialB16-50");
			entry = GameControl.self.skin.FindStyle ("Arial16-75");
			entrySelected = GameControl.self.skin.FindStyle ("Arial16-W");
		}
		
		public override bool SelectRender (GameButton button)
		{
			bool isOver = base.SelectRender (button);
			int x = (int)(button.position.x + 33);
			int y = (int)(button.position.y);
			int width = Mathf.Max (4, entries.Count);
			isOver |= SimpleGUI.Label (new Rect (x, y, width * 33 - 1, 32), "Encyclopedia", header);
			int i = 0;
			foreach (char c in sortedKeys) {
				bool isOverLetter = SimpleGUI.Label (new Rect (x + 33 * i, y + 33, 32, 32), c.ToString (), (i == selectedIndex)?entrySelected:entry);
				isOver |= isOverLetter;
				if (isOverLetter) {
					selectedIndex = i;
				}
				if (selectedIndex == i) {
					List<Articles.EncyclopediaEntry> list = entries [c];
					
					int colWidth = 164;
					foreach (Articles.EncyclopediaEntry ee in list) {
						int calcWidth = (int) entry.CalcSize (new GUIContent (ee.keyword)).x;
						calcWidth = ((calcWidth / 33) + 1) * 33 - 1;
						if (calcWidth > colWidth) {
							colWidth = calcWidth;
						}
					}
					
					int j = 0;
					int xExtra = 0;
					foreach (Articles.EncyclopediaEntry ee in list) {
						bool isOverEE = SimpleGUI.Label (new Rect (x + xExtra + 33 * i, y + 33 * (j + 2), colWidth, 32), ee.keyword, entry, entrySelected);
						isOver |= isOverEE;
						if (isOverEE && (Event.current.type == EventType.MouseDown)) {
							new RenderEntry (this, ee);
							Event.current.Use ();
						}				
						j++;
						if ((y + 33 * (j + 4)) > Screen.height) {
							// prevent entries going past bottom of screen...
							j = 0;
							xExtra += colWidth + 1;
						}
					}
				}
				i++;
			}
			while (i < width) {
				isOver |= SimpleGUI.Label (new Rect (x + 33 * i, y + 33, 32, 32), "", header);
				i++;
			}
			return isOver;
		}
		
		
		public override void UpdateScene (Scene scene, GameButton button)
		{
			entries = new Dictionary<char, List<Articles.EncyclopediaEntry>> ();
			foreach (Articles.EncyclopediaEntry entry in scene.articles.encyclopediaEntries.Values) {
				if ((entry.keyword != null) && (entry.keyword.Length > 0)) {
					char c = char.ToUpper(entry.keyword [0]);
					if (entries.ContainsKey (c)) {
						entries [c].Add (entry);
					} else {
						List<Articles.EncyclopediaEntry> list = new List<Articles.EncyclopediaEntry> ();
						list.Add (entry);
						entries.Add (c, list);
					}
				}
			}
			sortedKeys = new List<char>(entries.Keys);
			sortedKeys.Sort ();
			foreach (char c in sortedKeys) {
				List<Articles.EncyclopediaEntry> list = entries[c];
				list.Sort((e1,e2) => e1.keyword.ToUpper ().CompareTo(e2.keyword.ToUpper ()));
			}
			selectedIndex = -1;
			encWindows = new Dictionary<string, RenderEntry> ();
			base.UpdateScene (scene, button);
		}

		public override void UpdateState (GameButton button)
		{
			button.isVisible = true;
			button.alwaysRender = false;
			base.UpdateState (button);
		}			
	}
}