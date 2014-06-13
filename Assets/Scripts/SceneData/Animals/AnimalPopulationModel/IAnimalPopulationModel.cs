using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData.AnimalPopulationModel
{
	public abstract class IAnimalPopulationModel
	{
		[System.Serializable]
		public class Data 
		{
			public bool use;
			public bool show;
			//public string XML_ELEMENT = "data";
			public virtual void Save (XmlTextWriter writer, Scene scene) 
			{ 
				writer.WriteAttributeString ("show", use.ToString().ToLower());
				writer.WriteAttributeString ("use", use.ToString().ToLower());
			}
			public virtual void Load (XmlTextReader reader, Scene scene) 
			{
				this.show = bool.Parse (reader.GetAttribute ("show"));
				this.use = bool.Parse (reader.GetAttribute ("use"));
			}
			public virtual void UpdateReferences (Scene scene) { }
		}

		public abstract void Load (XmlTextReader reader, Scene scene);
		public abstract void Save (XmlTextWriter writer, Scene scene);
		public abstract void UpdateReferences (Scene scene);
		public abstract string GetXMLElement ();
	}
}

/*
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData.AnimalPopulationModel
{
	public class AnimalStartPopulationModel : IAnimalPopulationModel
	{
		public const string XML_ELEMENT = "startpopmodel";

		public void Load (XmlTextReader reader, Scene scene)
		{
			if (!reader.IsEmptyElement) 
			{
				while (reader.Read()) 
				{
					string readerName = reader.Name.ToLower ();
					XmlNodeType nType = reader.NodeType;
					if (nType == XmlNodeType.Element)
					{

						if (readerName == Nest.XML_ELEMENT && useNests) {
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
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteEndElement ();
		}

		public void UpdateReferences (Scene scene)
		{

		}

		public string GetXMLElement ()
		{
			return XML_ELEMENT;
		}
	}
}
 */ 
