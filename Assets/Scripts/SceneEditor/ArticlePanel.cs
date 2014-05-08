using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim;

namespace Ecosim.SceneEditor
{
	public class ArticlePanel : Panel
	{
		
		
		Scene scene;
		Articles articles;
		EditorCtrl ctrl;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;
		private Vector2 scrollPos;
		private Texture2D articleTex = null;
		
		enum ETabs
		{
			Images,
			Articles,
			Encyclopedia
		};
		
		private ETabs tab = ETabs.Images;
		
		/**
		 * Called when scene is set or changed in Editor
		 */
		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
			
			images = new Dictionary<string, Texture2D> ();
			if (scene != null) {
				LoadImages ();
				articles = scene.articles;
				articleList = new List<Articles.Article> ();
				foreach (Articles.Article a in articles.articles.Values) {
					articleList.Add (a);
				}
				encList = new List<Articles.EncyclopediaEntry> ();
				foreach (Articles.EncyclopediaEntry e in articles.encyclopediaEntries.Values) {
					encList.Add (e);
				}
			}
		}
				
		private List <Articles.Article> articleList;
		private List <Articles.EncyclopediaEntry> encList;
		private int articleIndex = 0; // note index, not id!
		private int encyclopediaIndex = 0;  // note index, not id!
				
		private Dictionary <string, Texture2D> images;
		
		private void DeleteImages () {
			foreach (Texture2D tex in images.Values) {
				if (tex) {
					Object.DestroyImmediate (tex);
				}
			}
			images.Clear ();
		}
		
		
		private void LoadImages () {
			DeleteImages ();
			string path = GameSettings.GetPathForScene (scene.sceneName) + "ArticleData" + Path.DirectorySeparatorChar;
			string[] pngFiles = Directory.GetFiles (path, "*.png");
			string[] jpgFiles = Directory.GetFiles (path, "*.jpg");
			LoadFiles (path, pngFiles);
			LoadFiles (path, jpgFiles);
		}
		
		
		private void LoadFiles (string path, string[] names) {
			foreach (string name in names) {
				string fileName = Path.GetFileName (name);
				byte[] data = File.ReadAllBytes(name);
				Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false, true);
				tex.LoadImage (data);
				images.Add (fileName, tex);
			}
		}
		
		private void RenderImages (int mx, int my)
		{
			foreach (KeyValuePair<string, Texture2D> kv in images) {
				GUILayout.BeginHorizontal ();
				Texture2D tex = kv.Value;
				int height = Mathf.Clamp ((100 * tex.height) / tex.width, 20, 140);
				GUILayout.Label (tex, GUILayout.Height (height), GUILayout.Width (100));
				GUILayout.Label (kv.Key);
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
		}
		
		private void RenderArticle (int mx, int my) {
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Article", GUILayout.Width (100));
			if (GUILayout.Button ("Create new")) {
				articleList.Add (articles.CreateNewArticle ());
				articleIndex = articleList.Count - 1;
				GenerateArticleTex (articleList[articleIndex].text);
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			if (articleList.Count > 0) {
				Articles.Article article = articleList[articleIndex];
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button("Article #" + article.id, tabNormal, GUILayout.Width (100))) {
					List<string> articleNames = new List<string> ();
					foreach (Articles.Article a in articleList) {
						articleNames.Add ("#" + a.id + " " + a.description);
					}
					ctrl.StartSelection (articleNames.ToArray (), articleIndex, newIndex => {
						articleIndex = newIndex;
						GenerateArticleTex (articleList[articleIndex].text);
					});
				}
				article.description = GUILayout.TextField (article.description, GUILayout.Width (160));
				if (GUILayout.Button ("Delete")) {
					ctrl.StartDialog ("Do you really want to delete Article #" + article.id + " (" + article.description + ")?",
						result => {
						articleList.Remove (article);
						articles.DeleteArticle (article.id);
						articleIndex = 0;
						if (articleList.Count > 0) {
							GenerateArticleTex (articleList[0].text);
						}
						else {
							ctrl.StopShowImage ();
						}
					}, null);
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				string newText = GUILayout.TextArea (article.text, GUILayout.MinHeight (500));
				if (newText != article.text) {
					article.text = newText;
					GenerateArticleTex (newText);
				}
			}
		}

		private void RenderEncyclopediaE (int mx, int my) {
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Enc. entry", GUILayout.Width (100));
			if (GUILayout.Button ("Create new")) {
				encList.Add (articles.CreateNewEncyclopediaEntry ());
				encyclopediaIndex = encList.Count - 1;
				GenerateArticleTex (encList[encyclopediaIndex].text);
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			if (encList.Count > 0) {
				Articles.EncyclopediaEntry enc = encList[encyclopediaIndex];
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button("Entry #" + enc.id, tabNormal, GUILayout.Width (100))) {
					List<string> encNames = new List<string> ();
					foreach (Articles.EncyclopediaEntry e in encList) {
						encNames.Add ("#" + e.id + " " + e.keyword);
					}
					ctrl.StartSelection (encNames.ToArray (), encyclopediaIndex, newIndex => {
						encyclopediaIndex = newIndex;
						GenerateArticleTex (encList[encyclopediaIndex].text);
					});
				}
				enc.keyword = GUILayout.TextField (enc.keyword, GUILayout.Width (100));
				if (GUILayout.Button ("Delete")) {
					ctrl.StartDialog ("Do you really want to delete Encyclopedia entry #" + enc.id + " (" + enc.keyword + ")?",
						result => {
						encList.Remove (enc);
						articles.DeleteEncyclopediaEntry (enc.id);
						encyclopediaIndex = 0;
						if (encList.Count > 0) {
							GenerateArticleTex (encList[0].text);
						}
						else {
							ctrl.StopShowImage ();
						}
					}, null);
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("URL", GUILayout.Width (100));
				string newUrl = GUILayout.TextField ((enc.url == null)?"":(enc.url), GUILayout.Width (280)).Trim ();
				if (newUrl == "") {
					enc.url = null;
				}
				else {
					enc.url = newUrl;
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				string newText = GUILayout.TextArea (enc.text, GUILayout.MinHeight (500));
				if (newText != enc.text) {
					enc.text = newText;
					GenerateArticleTex (newText);
				}
			}
		}
		
		public void GenerateArticleTex(string text) {
			if (articleTex == null) {
				articleTex = new Texture2D (2, 2, TextureFormat.ARGB32, false, true);
			}
			RenderFontToTexture.self.RenderNewsArticle (text, scene, articleTex, true);
			ctrl.StartShowImage(articleTex);
		}
		
		/**
		 * Called every frame when this panel is active
		 * It is guaranteed that setup has been called with
		 * a valid scene before the first call of Render.
		 */
		public bool Render (int mx, int my)
		{
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			GUILayout.BeginVertical ();
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Images", (tab == ETabs.Images) ? tabSelected : tabNormal, GUILayout.Width (80))) {
				tab = ETabs.Images;
				LoadImages ();
			}
			if (GUILayout.Button ("Articles", (tab == ETabs.Articles) ? tabSelected : tabNormal, GUILayout.Width (80))) {
				tab = ETabs.Articles;
				if (articleList.Count > 0) {
					GenerateArticleTex (articleList[articleIndex].text);
				}
				else {
					ctrl.StopShowImage ();
				}
			}
			if (GUILayout.Button ("Encyclopedia", (tab == ETabs.Encyclopedia) ? tabSelected : tabNormal, GUILayout.Width (80))) {
				tab = ETabs.Encyclopedia;
				if (encList.Count > 0) {
					GenerateArticleTex (encList[encyclopediaIndex].text);
				}
				else {
					ctrl.StopShowImage ();
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.Space (4);
			switch (tab) {
			case ETabs.Images :
				RenderImages (mx, my);
				break;
			case ETabs.Articles :
				RenderArticle (mx, my);
				break;
			case ETabs.Encyclopedia :
				RenderEncyclopediaE (mx, my);
				break;
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndVertical ();
			GUILayout.EndScrollView ();
			return false;
		}
		
		/* Called for extra edit sub-panel, will be called after Render */
		public void RenderExtra (int mx, int my)
		{
		}

		/* Called for extra side edit sub-panel, will be called after RenderExtra */
		public void RenderSide (int mx, int my)
		{
		}
		
		/* Returns true if a side panel is needed. Won't be called before RenderExtra has been called */
		public bool NeedSidePanel ()
		{
			return false;
		}
		
		public bool IsAvailable ()
		{
			return (scene != null);
		}

		public void Activate ()
		{
		}
		
		public void Deactivate ()
		{
			DeleteImages ();
			ctrl.StopShowImage ();
		}

		public void Update ()
		{
		}
	}
}