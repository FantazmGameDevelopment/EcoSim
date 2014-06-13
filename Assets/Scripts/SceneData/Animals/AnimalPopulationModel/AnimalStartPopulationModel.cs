using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData.AnimalPopulationModel
{
	public class AnimalStartPopulationModel : IAnimalPopulationModel
	{
		[System.Serializable]
		public class Nests : Data
		{
			public class Nest
			{
				public const string XML_ELEMENT = "nest";
				
				public int x;
				public int y;
				
				public int males;
				public int females;
				public int food;
				
				public int totalCapacity;
				public int malesCapacity;
				public int femalesCapacity;
				
				public Nest ()
				{
				}
				
				public static Nest Load (XmlTextReader reader, Scene scene)
				{
					Nest nest = new Nest ();
					nest.x = int.Parse(reader.GetAttribute ("x"));
					nest.y = int.Parse(reader.GetAttribute ("y"));
					nest.males = int.Parse(reader.GetAttribute ("m"));
					nest.females = int.Parse(reader.GetAttribute ("f"));
					nest.totalCapacity = int.Parse (reader.GetAttribute ("cap"));
					nest.malesCapacity = int.Parse (reader.GetAttribute ("mcap"));
					nest.females = int.Parse (reader.GetAttribute ("fcap"));
					//IOUtil.ReadUntilEndElement(reader, XML_ELEMENT);
					return nest;
				}
				
				public void Save (XmlTextWriter writer, Scene scene)
				{
					writer.WriteStartElement (XML_ELEMENT);
					writer.WriteAttributeString ("x", x.ToString());
					writer.WriteAttributeString ("y", y.ToString());
					writer.WriteAttributeString ("m", males.ToString());
					writer.WriteAttributeString ("f", females.ToString());
					writer.WriteAttributeString ("cap", totalCapacity.ToString());
					writer.WriteAttributeString ("mcap", malesCapacity.ToString());
					writer.WriteAttributeString ("fcap", femalesCapacity.ToString());
					writer.WriteEndElement ();
				}
				
				public void UpdateReferences (Scene scene)
				{
					
				}
			}

			public Nest[] nests = new Nest[0];

			public void UpdateReferences (Scene scene)
			{
				foreach (Nest n in nests) {
					n.UpdateReferences (scene);
				}
			}

			public override void Load (XmlTextReader reader, Scene scene)
			{
				base.Load (reader, scene);

				List<Nest> nests = new List<Nest>();
				
				if (!reader.IsEmptyElement) 
				{
					while (reader.Read()) 
					{
						string readerName = reader.Name.ToLower ();
						XmlNodeType nType = reader.NodeType;
						if (nType == XmlNodeType.Element)
						{
							
							if (readerName == Nest.XML_ELEMENT) {
								Nest nest = Nest.Load (reader, scene);
								if (nest != null) {
									nests.Add (nest);
								}
							} 
							
						}
						else if ((nType == XmlNodeType.EndElement) && (readerName == XML_ELEMENT)) {
							break;
						}
					}
				}
				
				this.nests = nests.ToArray();
			}

			public override void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				base.Save (writer, scene);
				foreach (Nest n in nests) {
					n.Save (writer, scene);
				}
				writer.WriteEndElement ();
			}
		}

		public const string XML_ELEMENT = "startpopmodel";

		public Nests nests = new Nests ();

		public override void Load (XmlTextReader reader, Scene scene)
		{

		}
		
		public override void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			nests.Save (writer, scene);
			writer.WriteEndElement ();
		}

		public override void UpdateReferences (Scene scene)
		{
			nests.UpdateReferences (scene);
		}

		public override string GetXMLElement ()
		{
			return XML_ELEMENT;
		}
	}
}
