using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData
{
	
	/**
	 * Loads/saves the plants.xml file
	 * The only reason this class exists is to prevent clutter in Scene class :-)
	 */
	public static class LoadSavePlants
	{
		public static PlantType[] Load (string path, Scene scene)
		{
			List<PlantType> list = new List<PlantType>();
			string url = path + "plants.xml";
			if (File.Exists(url)) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (url));
				try {
					while (reader.Read ()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == PlantType.XML_ELEMENT)) {
							PlantType pt = PlantType.Load (reader, scene);
							if (pt != null) {
								list.Add (pt);
							}
						}
					}
				} finally {
					reader.Close ();
				}
			}
			return list.ToArray();
		}

		public static void Save (string path, PlantType[] plants, Scene scene)
		{
			Directory.CreateDirectory (path);
			XmlTextWriter writer = new XmlTextWriter (path + "plants.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement("plants");
			foreach (PlantType t in plants) {
				t.Save (writer, scene);
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close();
		}
	}

}