using System.Collections.Generic;
using System.IO;
using System.Xml;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneData
{
	public class Articles
	{
		public class Article {
			public Article (int id) {
				this.id = id;
			}
			public readonly int id;
			public string description = ""; // for easier reference
			public string text = "";
		}
		
		public class EncyclopediaEntry {
			public EncyclopediaEntry (int id) {
				this.id = id;
				this.keyword = "Entry " + id;
				this.text = "[enc]\n[title]Entry " + id + "\n[par]Description";
			}
			public readonly int id;
			public string keyword;
			public string url = null;
			public string text;
		}
		
		public Dictionary<int, Article> articles;
		public Dictionary<int, EncyclopediaEntry> encyclopediaEntries;
		public Dictionary<string, EncyclopediaEntry> encyclopediaEntriesByKeyword;
		
		
		private int nextArticleId = 0;
		private int nextEncEntryId = 0;
		
		public Article CreateNewArticle() {
			Article article = new Article (nextArticleId++);
			articles.Add (article.id, article);
			return article;
		}

		public EncyclopediaEntry CreateNewEncyclopediaEntry() {
			EncyclopediaEntry enc = new EncyclopediaEntry (nextEncEntryId++);
			encyclopediaEntries.Add (enc.id, enc);
			return enc;
		}
		
//		private readonly Scene scene;

		public Articles (Scene scene)
		{
//			this.scene = scene;
			articles = new Dictionary<int, Article>();
			encyclopediaEntries = new Dictionary<int, EncyclopediaEntry> ();
			encyclopediaEntriesByKeyword = new Dictionary<string, EncyclopediaEntry> ();
		}
		
		
		private void Load (XmlTextReader reader) {
			bool skipReadHack = false;
			while (skipReadHack || reader.Read()) {
				skipReadHack = false;
				XmlNodeType nType = reader.NodeType;
				if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "article")) {
					int id = int.Parse(reader.GetAttribute ("id"));
					if (id >= nextArticleId) {
						nextArticleId = id + 1;
					}
					string descr = reader.GetAttribute ("description");
					string text = reader.ReadElementContentAsString();
					Article article = new Article(id);
					article.text = text;
					article.description = descr;
					articles.Add (id, article);
					// IOUtil.ReadUntilEndElement (reader, "article");
					skipReadHack = true;
				}
				else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "encyclopedia")) {
					int id = int.Parse(reader.GetAttribute ("id"));
					if (id >= nextEncEntryId) {
						nextEncEntryId = id + 1;
					}
					string keyword = reader.GetAttribute ("keyword");
					string url = reader.GetAttribute ("url");
					string text = reader.ReadElementContentAsString();
					EncyclopediaEntry encEntry = new EncyclopediaEntry(id);
					encEntry.keyword = keyword;
					encEntry.text = text;
					encEntry.url = url;
					encyclopediaEntries.Add (id, encEntry);
					// IOUtil.ReadUntilEndElement (reader, "encyclopedia");
					skipReadHack = true;
				}
				else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == "articles")) {
					break;
				}
			}
			
		}
		
		public static Articles Load (string path, Scene scene)
		{
			Articles articles = new Articles (scene);
			if (File.Exists (path + "articles.xml")) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path + "articles.xml"));
				try {
					while (reader.Read()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "articles")) {
							articles.Load (reader);
						}
					}
				} finally {
					reader.Close ();
				}
			}
			return articles;
		}
		
		public Article GetArticleWithId (int id) {
			Article result;
			if (articles.TryGetValue (id, out result)) {
				return result;
			}
			Log.LogError ("Can't find article with id '" + id + "'");
			return null;
		}
		
		/**
		 * returns article with given name (description) or null if not found
		 */
		public Article GetArticleByName (string name) {
			foreach (Article a in articles.Values) {
				if (a.description == name) {
					return a;
				}
			}
			return null;
		}
		
		public void DeleteArticle (int id) {
			articles.Remove (id);
		}
		
		public EncyclopediaEntry GetEncyclopediaEntryWithId (int id) {
			return encyclopediaEntries[id];
		}
		
		public void DeleteEncyclopediaEntry (int id) {
			encyclopediaEntries.Remove (id);
		}
		
		public void Save (string path)
		{
			string articlesPath = path + "ArticleData";
			if (!Directory.Exists (articlesPath)) {
				Directory.CreateDirectory (articlesPath);
			}
			XmlTextWriter writer = new XmlTextWriter (path + "articles.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement ("articles");
			foreach (Article article in articles.Values) {
				writer.WriteStartElement ("article");
				writer.WriteAttributeString ("id", article.id.ToString ());
				writer.WriteAttributeString ("description", article.description);
				writer.WriteCData (article.text);
				writer.WriteEndElement ();
			}
			foreach (EncyclopediaEntry entry in encyclopediaEntries.Values) {
				writer.WriteStartElement ("encyclopedia");
				writer.WriteAttributeString ("id", entry.id.ToString ());
				writer.WriteAttributeString ("keyword", entry.keyword);
				if (entry.url != null) {
					writer.WriteAttributeString ("url", entry.url);
				}
				writer.WriteCData (entry.text);
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();		
		}
		
		public void UpdateReferences () {
			encyclopediaEntriesByKeyword.Clear ();
			foreach (EncyclopediaEntry entry in encyclopediaEntries.Values) {
				if (!encyclopediaEntriesByKeyword.ContainsKey (entry.keyword)) {
					encyclopediaEntriesByKeyword.Add (entry.keyword, entry);
				}
			}
		}
	}
}
