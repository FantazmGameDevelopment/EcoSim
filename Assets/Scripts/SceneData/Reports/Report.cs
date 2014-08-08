using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData
{
	public class ReportParagraph
	{
		public const string XML_ELEMENT = "paragraph";

		public bool useTitle;
		public string title;
		public bool useDescription;
		public string description;
		public bool useMaxChars;
		public int maxChars;

		public bool opened;
		public bool titleOpened;
		public bool descriptionOpened;

		public ReportParagraph ()
		{
			this.title = "New paragraph title";
			this.useTitle = true; // This is default for now
			this.description = "New description";
			this.maxChars = 200;
		}

		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("usetitle", useTitle.ToString().ToLower());
			writer.WriteAttributeString ("title", title);
			writer.WriteAttributeString ("usedescr", useDescription.ToString().ToLower());
			writer.WriteAttributeString ("descr", description);
			writer.WriteAttributeString ("usemaxchars", useMaxChars.ToString().ToLower());
			writer.WriteAttributeString ("maxchars", maxChars.ToString());
			writer.WriteEndElement ();
		}

		public void Load (XmlTextReader reader, Scene scene)
		{
			this.useTitle = bool.Parse (reader.GetAttribute ("usetitle"));
			this.title = reader.GetAttribute ("title");
			this.useDescription = bool.Parse (reader.GetAttribute ("usedescr"));
			this.description = reader.GetAttribute ("descr");
			this.useMaxChars = bool.Parse (reader.GetAttribute ("usemaxchars"));
			this.maxChars = int.Parse (reader.GetAttribute ("maxchars"));

			if (!reader.IsEmptyElement)
			{
				while (reader.Read ())
				{
					XmlNodeType nt = reader.NodeType;
					if (nt == XmlNodeType.Element) 
					{
						/*switch (reader.Name.ToLower ())
						{
						case X : 
							break;
						}*/
					}
					else if (nt == XmlNodeType.EndElement && reader.Name.ToLower () == XML_ELEMENT) {
						break;
					}
				}
			}
		}

		public void UpdateReferences (Scene scene)
		{
		}
	}

	public class Report : ReportBase
	{
		public const string XML_ELEMENT = "report";

		private Scene scene;

		public bool useName;
		public bool useNumber;

		public List<ReportParagraph> paragraphs;

		public bool paragraphsOpened;

		public Report () : base()
		{
			this.paragraphs = new List<ReportParagraph>();
			this.name = "New report";
			this.paragraphsOpened = true;
			this.showHeader = true;
		}

		public static Report Load (XmlTextReader reader, Scene scene)
		{
			Report r = new Report ();
			r.LoadBase (reader, scene);
			r.useName = bool.Parse (reader.GetAttribute ("usename"));
			r.useNumber = bool.Parse (reader.GetAttribute ("usenumber"));
			
			if (!reader.IsEmptyElement)
			{
				while (reader.Read ())
				{
					XmlNodeType nt = reader.NodeType;
					if (nt == XmlNodeType.Element) 
					{
						switch (reader.Name.ToLower ())
						{
						case ReportParagraph.XML_ELEMENT :
							ReportParagraph p = new ReportParagraph ();
							p.Load (reader, scene);
							r.paragraphs.Add (p);
							break;
						}
					}
					else if (nt == XmlNodeType.EndElement && reader.Name.ToLower () == XML_ELEMENT) {
						break;
					}
				}
			}
			
			return r;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			this.SaveBase (writer, scene);
			writer.WriteAttributeString ("usename", useName.ToString().ToLower());
			writer.WriteAttributeString ("usenumber", useNumber.ToString().ToLower());
			foreach (ReportParagraph p in this.paragraphs) {
				p.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}
		
		public void UpdateReferences (Scene scene)
		{
			foreach (ReportParagraph p in this.paragraphs) {
				p.UpdateReferences (scene);
			}
		}
	}
}
