using System.Collections;
using System.Xml;
using System;

using Ecosim;

namespace Ecosim.SceneData.VegetationRules
{
	/**
	 * When a tile changes vegetation type (through succession), the parameters of the tile can be changed using this class.
	 */
	public class ParameterChange : ICloneable
	{
		public const string XML_ELEMENT = "parameter";
		
		public string paramName; // name of parameter
		public int lowRange; // parameter value won't leave range
		public int highRange; // parameter value won't leave range
		public Data data; // quick access to parameter data
		
		public object Clone () {
			ParameterChange clone = new ParameterChange ();
			clone.paramName = paramName;
			clone.lowRange = lowRange;
			clone.highRange = highRange;
			clone.data = data;
			return clone;
		}
		
		public static ParameterChange Load (XmlTextReader reader, Scene scene)
		{
			
			ParameterChange result = new ParameterChange();
			string paramName = reader.GetAttribute("parameter");
			if (paramName == null) {
				// to be able to read old style vegetation.xml
				paramName = reader.GetAttribute("type");
			}
			result.paramName = paramName;
			if (reader.GetAttribute("min") != null) {
				result.lowRange = int.Parse(reader.GetAttribute("min"));// * 100 / 255;
				result.highRange = int.Parse(reader.GetAttribute("max"));// * 100 / 255;
			}
			else {
				result.lowRange = int.Parse(reader.GetAttribute("low"));
				result.highRange = int.Parse(reader.GetAttribute("high"));
			}

			IOUtil.ReadUntilEndElement(reader, XML_ELEMENT);
			return result;
		}
		
		public void Save(XmlTextWriter writer, Scene scene) {
			writer.WriteStartElement(XML_ELEMENT);
			writer.WriteAttributeString("parameter", paramName);
			writer.WriteAttributeString("low", lowRange.ToString());
			writer.WriteAttributeString("high", highRange.ToString());
			writer.WriteEndElement();
		}
		
		public void UpdateReferences (Scene scene, VegetationType veg)
		{
			if (paramName == null) {
				paramName = "Unknown";
			}
			data = scene.progression.GetData(paramName);
		}
	}
}