using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using Ecosim;
using UnityEngine;

namespace Ecosim.SceneData
{
	public class ResearchPoint
	{
		public class Measurement
		{
			public const string XML_ELEMENT = "measurement";

			public readonly int year;
			public string message;

			public string name;
			public bool isTemporary = false;

			public Measurement(int year)
			{
				this.year = year;
			}

			public static Measurement Load (XmlTextReader reader, Scene scene)
			{
				int year = int.Parse(reader.GetAttribute ("year"));
				Measurement newM = new Measurement(year);
				newM.message = reader.GetAttribute ("message");
				newM.name = reader.GetAttribute ("name");
				return newM;
			}
			
			public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("year", year.ToString());
				writer.WriteAttributeString ("message", message);
				writer.WriteAttributeString ("name", name);
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

		public Measurement AddNewMeasurement (Scene scene, string description)
		{
			Measurement newM = new Measurement (scene.progression.year);
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
