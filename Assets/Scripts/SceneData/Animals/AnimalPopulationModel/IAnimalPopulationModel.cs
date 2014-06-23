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
		public class AnimalPopulationModelDataBase 
		{
			public bool use = false;
			public bool show = false;
			public bool opened = true;

			public virtual void Save (XmlTextWriter writer, Scene scene) 
			{ 
				writer.WriteAttributeString ("show", use.ToString().ToLower());
				writer.WriteAttributeString ("use", use.ToString().ToLower());
				writer.WriteAttributeString ("opened", use.ToString().ToLower());
			}
			public virtual void Load (XmlTextReader reader, Scene scene) 
			{
				ParseBool (reader, ref this.show, "show");
				ParseBool (reader, ref this.use, "use");
				ParseBool (reader, ref this.opened, "opened");
			}

			protected void ParseBool (XmlTextReader reader, ref bool field, string name)
			{
				string attribute = reader.GetAttribute (name);
				if (!string.IsNullOrEmpty (attribute))
					field = bool.Parse (attribute);
			}

			public virtual void UpdateReferences (Scene scene) { }
			public virtual void PrepareSuccession () { }
			public virtual void DoSuccession () { }
			public virtual void FinalizeSuccession () { }
		}

		public abstract void Load (XmlTextReader reader, Scene scene);
		public abstract void Save (XmlTextWriter writer, Scene scene);
		public abstract void UpdateReferences (Scene scene);
		public abstract string GetXMLElement ();

		public abstract void PrepareSuccession ();
		public abstract void DoSuccession ();
		public abstract void FinalizeSuccession ();
	}
}

/*
[System.Serializable]
public class SpecifiedNumber : AnimalPopulationModelDataBase
{
	public string XML_ELEMENT = "specified";

	public void Load (XmlTextReader reader, Scene scene)
	{
		base.Load (reader, scene);
		IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
	}
	
	public void Save (XmlTextWriter writer, Scene scene)
	{
		writer.WriteStartElement (XML_ELEMENT);
		base.Save (writer, scene);
		writer.WriteEndElement ();
	}
}
public SpecifiedNumber specifiedNumber = new SpecifiedNumber ();
*/

/*
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData.AnimalPopulationModel
{
	public class AnimalPopulationGrowthModel : IAnimalPopulationModel
	{
		public const string XML_ELEMENT = "growthmodel";

		[System.Serializable]
		public class FixedNumber : AnimalPopulationModelDataBase
		{
			public string XML_ELEMENT = "fixed";

			public enum Type {
				PerFemale,
				PerPair
			}
			public Type type;
			public int minLitterSize;
			public int maxLitterSize;

			public void Load (XmlTextReader reader, Scene scene)
			{
				base.Load (reader, scene);
				this.type = (Type)System.Enum.Parse(typeof(Type), reader.GetAttribute ("type"));
				this.minLitterSize = int.Parse (reader.GetAttribute ("minlittersize"));
				this.maxLitterSize = int.Parse (reader.GetAttribute ("maxlittersize"));
				IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			}

			public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				base.Save (writer, scene);
				writer.WriteAttributeString ("type", this.type.ToString());
				writer.WriteAttributeString ("minlittersize", this.minLitterSize.ToString());
				writer.WriteAttributeString ("maxlittersize", this.maxLitterSize.ToString());
				writer.WriteEndElement ();
			}
		}

		public FixedNumber fixedNumber = new FixedNumber ();

		public override void Load (XmlTextReader reader, Scene scene)
		{
			if (!reader.IsEmptyElement) 
			{
				while (reader.Read()) 
				{
					string readerName = reader.Name.ToLower ();
					XmlNodeType nType = reader.NodeType;
					if (nType == XmlNodeType.Element)
					{
						
						// Add more AnimalPopulationModelDataBases
						if (readerName == fixedNumber.XML_ELEMENT) {
							fixedNumber.Load (reader, scene);
						} 
						
					}
					else if ((nType == XmlNodeType.EndElement) && (readerName == XML_ELEMENT)) {
						break;
					}
				}
			}
		}
		
		public override void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			fixedNumber.Save (writer, scene);
			writer.WriteEndElement ();
		}

		public override void UpdateReferences (Scene scene)
		{
			fixedNumber.UpdateReferences (scene);
		}

		public override string GetXMLElement ()
		{
			return XML_ELEMENT;
		}
	}
}
 */ 
