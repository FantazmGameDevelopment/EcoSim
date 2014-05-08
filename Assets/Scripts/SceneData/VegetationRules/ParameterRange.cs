using System.Collections;
using System.Xml;
using System;

using Ecosim;

namespace Ecosim.SceneData.Rules
{
	/**
	 * Range of valid parameter values for a parameter in a rule
	 */
	public class ParameterRange : ICloneable
	{
		public const string XML_ELEMENT = "range";
		
		public string paramName; // name of parameter
		public int lowRange; // parameter value won't leave range
		public int highRange; // parameter value won't leave range
		public Data data; // quick access to parameter data

		public float lowRangePerc = -1f; // only used for the editor (slider)
		public float highRangePerc = -1f; // only used for the editor (slider)
		
		public static ParameterRange Load (XmlTextReader reader, Scene scene)
		{
			ParameterRange result = new ParameterRange();
			string paramName = reader.GetAttribute("parameter");
			if (paramName == null) {
				// to be able to read old style *.xml
				paramName = reader.GetAttribute("type");
			}
			result.paramName = paramName;

			string lowStr = reader.GetAttribute ("low");
			string highStr = reader.GetAttribute ("high");

			// Check for percentages
			if (lowStr.IndexOf(".") > -1 || highStr.IndexOf(".") > -1) {
				result.lowRangePerc = float.Parse(lowStr);
				result.highRangePerc = float.Parse(highStr);

				lowStr = "0";
				highStr = "0";
			}

			result.lowRange = int.Parse(lowStr);
			result.highRange = int.Parse(highStr);

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

		public void UpdateReferences (Scene scene)
		{
			data = scene.progression.GetData(paramName);
		}

		public object Clone()
		{
			ParameterRange clone = new ParameterRange();
			clone.paramName = paramName;
			clone.lowRange = lowRange;
			clone.highRange = highRange;
			clone.data = data;
			return clone;
		}
	}
}