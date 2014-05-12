using System.Collections.Generic;
using System.Xml;
using System;

using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneData.PlantRules
{
	public class VegetationCondition : ICloneable
	{
		public const string XML_ELEMENT = "vegetation";
		
		public int successionIndex;
		public int vegetationIndex;

		public VegetationCondition ()
		{
			this.successionIndex = -1;
			this.vegetationIndex = -1;
		}
		
		public VegetationCondition (int successionIndex, int vegetationIndex)
		{
			this.successionIndex = successionIndex;
			this.vegetationIndex = vegetationIndex;
		}
		
		public static VegetationCondition Load (XmlTextReader reader, Scene scene)
		{
			int sucIndex = int.Parse(reader.GetAttribute("successionindex"));
			int vegIndex = int.Parse(reader.GetAttribute("vegetationindex"));
			VegetationCondition result = new VegetationCondition (sucIndex, vegIndex);
			IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			return result;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("successionindex", successionIndex.ToString());
			writer.WriteAttributeString ("vegetationindex", vegetationIndex.ToString());
			writer.WriteEndElement ();
		}
		
		public object Clone()
		{
			VegetationCondition clone = new VegetationCondition (successionIndex, vegetationIndex);
			return clone;
		}
		
		public bool IsCompatible (int sucIndex, int vegIndex)
		{
			bool correctVegetation = false;
			{
				bool correctSuccession = false;
				if (this.successionIndex < 0) 
					correctSuccession = true;
				else if (this.successionIndex == sucIndex) 
					correctSuccession = true;
				
				if (correctSuccession) {
					if (this.vegetationIndex < 0)
						correctVegetation = true;
					else if (this.vegetationIndex == vegIndex)
						correctVegetation = true;
				}
			}
			return correctVegetation;
		}
	}
}
