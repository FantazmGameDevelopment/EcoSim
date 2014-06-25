using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;

namespace Ecosim.SceneData
{
	public class ReportsMgr
	{
		public const string XML_ELEMENT = "reports";
		private readonly Scene scene;

		public List<Questionnaire> questionnaires;

		public ReportsMgr (Scene scene)
		{
			this.scene = scene;
			this.questionnaires = new List<Questionnaire>();
		}

		private void Load (XmlTextReader reader)
		{
			while (reader.Read()) 
			{
				XmlNodeType nType = reader.NodeType;
				if (nType == XmlNodeType.Element)
				{
					switch (reader.Name.ToLower())
					{
					case Questionnaire.XML_ELEMENT : 
						this.questionnaires.Add (Questionnaire.Load (reader, scene));
						break;
					}
				} 
				else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
					break;
				}
			}
		}

		public static ReportsMgr Load (string path, Scene scene)
		{
			ReportsMgr mgr = new ReportsMgr (scene);
			if (File.Exists (path + XML_ELEMENT + ".xml")) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path + XML_ELEMENT + ".xml"));
				try {
					while (reader.Read()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == XML_ELEMENT)) {
							mgr.Load (reader);
						}
					}
				} finally {
					reader.Close ();
				}
			}
			return mgr;
		}
		
		public void Save (string path)
		{
			XmlTextWriter writer = new XmlTextWriter (path + XML_ELEMENT + ".xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement (XML_ELEMENT);
			foreach (Questionnaire q in questionnaires) {
				q.Save (writer, scene);
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();		
		}
		
		public void UpdateReferences ()
		{
			foreach (Questionnaire q in questionnaires) {
				q.UpdateReferences (scene);
			}
		}
	}
}
