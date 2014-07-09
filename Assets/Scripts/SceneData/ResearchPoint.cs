using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Ecosim.SceneData.Action;

using Ecosim;
using UnityEngine;

namespace Ecosim.SceneData
{
	/// <summary>
	/// Container class used in the custom scripts
	/// </summary>
	public class ResearchPointData
	{
		public const string XML_ELEMENT = "rpdata";
		
		public string formattedString;
		public Dictionary <string, string> values;
		
		public ResearchPointData ()
		{
			this.formattedString = "";
			this.values = new Dictionary<string, string> ();
		}
		
		public void AddValue (string dataName, object value)
		{
			if (values.ContainsKey (dataName)) 
				values [dataName] = value.ToString ();
			else 
				values.Add (dataName, value.ToString ());
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("str", formattedString);
			foreach (KeyValuePair<string, string> value in values) {
				writer.WriteStartElement ("value");
				writer.WriteAttributeString ("k", value.Key);
				writer.WriteAttributeString ("v", value.Value);
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}
		
		public static ResearchPointData Load (XmlTextReader reader, Scene scene)
		{
			ResearchPointData data = new ResearchPointData ();
			data.formattedString = reader.GetAttribute ("str");

			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == "value")) {
						string key = reader.GetAttribute ("k");
						string value = reader.GetAttribute ("v");
						data.values.Add (key, value);
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower() == XML_ELEMENT)) {
						break;
					}
				}
			}

			return data;
		}
	}

	public class ResearchPoint
	{
		public class Measurement
		{
			public const string XML_ELEMENT = "measurement";

			public readonly int year;
			public readonly int actionId;
			public ResearchPointData data; // TODO:

			public string name;
			public bool isTemporary = false;

			public Measurement (int actionId, int year)
			{
				this.actionId = actionId;
				this.year = year;
			}

			public static Measurement Load (XmlTextReader reader, Scene scene)
			{
				int year = int.Parse(reader.GetAttribute ("year"));
				int actionId = int.Parse (reader.GetAttribute ("actionid"));
				Measurement newM = new Measurement (actionId, year);
				newM.name = reader.GetAttribute ("name");

				if (!reader.IsEmptyElement) {
					while (reader.Read()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == ResearchPointData.XML_ELEMENT)) {
							newM.data = ResearchPointData.Load (reader, scene);
						} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower() == XML_ELEMENT)) {
							break;
						}
					}
				}
				return newM;
			}
			
			public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("year", year.ToString());
				writer.WriteAttributeString ("name", name);
				writer.WriteAttributeString ("actionid", actionId.ToString());
				if (data != null) {
					data.Save (writer, scene);
				}
				writer.WriteEndElement ();
			}
		}

		public const string XML_ELEMENT = "researchpoint";

		public int x;
		public int y;

		public List<Measurement> measurements;

		public ResearchPoint ()
		{
		}

		public ResearchPoint (int x, int y)
		{
			this.x = x;
			this.y = y;

			measurements = new List<Measurement>();
		}

		public Measurement GetLastMeasurement ()
		{
			if (measurements.Count <= 0) return null;
			else return measurements[measurements.Count - 1];
		}

		public Measurement AddNewMeasurement (Scene scene, int actionId, string description)
		{
			Measurement newM = new Measurement (actionId, scene.progression.year);
			newM.name = description;
			measurements.Add (newM);
			return newM;
		}

		public void DeleteMeasurement (Measurement m)
		{
			measurements.Remove (m);
		}

		public bool HasMeasurements ()
		{
			return measurements.Count > 0;
		}

		public static ResearchPoint Load (XmlTextReader reader, Scene scene)
		{
			ResearchPoint newRP = new ResearchPoint();
			newRP.x = int.Parse(reader.GetAttribute ("x"));
			newRP.y = int.Parse(reader.GetAttribute ("y"));

			newRP.measurements = new List<Measurement>();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == Measurement.XML_ELEMENT)) {
						Measurement m = Measurement.Load (reader, scene);
						if (m != null) {
							newRP.measurements.Add (m);
						}
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower() == XML_ELEMENT)) {
						break;
					}
				}
			}

			return newRP;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("x", x.ToString());
			writer.WriteAttributeString ("y", y.ToString());

			foreach (Measurement m in measurements) {
				m.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}
	}
}
