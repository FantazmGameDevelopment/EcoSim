using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData
{
	/**
	 * Loads/saves the animals.xml file
	 * The only reason this class exists is to prevent clutter in Scene class :-)
	 */
	public static class LoadSaveAnimals
	{
		public static AnimalType[] Load (string path, Scene scene)
		{
			List<AnimalType> list = new List<AnimalType>();
			string url = path + "animals.xml";
			if (File.Exists(url)) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (url));
				try {
					while (reader.Read ()) 
					{
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == AnimalType.XML_ELEMENT)) {
							AnimalType at = AnimalType.Load (reader, scene);
							if (at != null) {
								list.Add (at);
							}
						}
					}
				} finally {
					reader.Close ();
				}
			}
			return list.ToArray();
		}
		
		public static void Save (string path, AnimalType[] animals, Scene scene)
		{
			Directory.CreateDirectory (path);
			XmlTextWriter writer = new XmlTextWriter (path + "animals.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement("animals");
			foreach (AnimalType at in animals) {
				at.Save (writer, scene);
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close();
		}
	}
	
}