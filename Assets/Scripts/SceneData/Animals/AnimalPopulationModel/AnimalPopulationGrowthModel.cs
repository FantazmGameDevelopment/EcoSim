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

			public enum Types {
				PerFemale,
				//PerPair
			}
			public Types type;
			public int minLitterSize;
			public int maxLitterSize;

			public FixedNumber (IAnimalPopulationModel model) : base (model)
			{
				
			}

			public void Load (XmlTextReader reader, Scene scene)
			{
				base.Load (reader, scene);
				this.type = (Types)System.Enum.Parse(typeof(Types), reader.GetAttribute ("type"));
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

			/// <summary>
			/// Processes the fixed number and returns the growth.
			/// </summary>
			/*public int Process (System.Random random, int currPop, int males, int females)
			{
				switch (type)
				{
				case Type.PerFemale :
					// P[1] = M[0] + V[0] +(M[0]/M[0] * V[0] * W) 
					float minW = (float)minLitterSize;
					float maxW = (float)maxLitterSize;
					int w = minLitterSize + Mathf.RoundToInt((maxW - minW) * (float)random.NextDouble());
					return males + females + ((males / females) * females * w);
				}
				return 0;
			}*/
		}
		public FixedNumber fixedNumber;

		public AnimalPopulationGrowthModel (AnimalType animal) : base (animal)
		{
			this.fixedNumber = new FixedNumber (this);
		}

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

		public override void PrepareSuccession ()
		{
			fixedNumber.PrepareSuccession ();
		}
		
		public override void DoSuccession ()
		{
			fixedNumber.DoSuccession ();
		}
		
		public override void FinalizeSuccession ()
		{
			fixedNumber.FinalizeSuccession ();
		}
	}
}