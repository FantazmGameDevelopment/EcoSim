using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData
{
	
	/**
	 * Loads/saves the vegetation.xml file
	 * The only reason this class exists is to prevent clutter in Scene class :-)
	 */
	public static class LoadSaveVegetation
	{
		public static SuccessionType[] Load (string path, Scene scene)
		{
			List<SuccessionType> list = new List<SuccessionType> ();
			XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path + "vegetation.xml"));
			try {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == SuccessionType.XML_ELEMENT)) {
						SuccessionType st = SuccessionType.Load (reader, scene);
						if (st != null) {
							list.Add (st);
						}
					}
				}
			} finally {
				reader.Close ();
			}
			return list.ToArray ();
		}
		
		public static void Save (string path, SuccessionType[] successions, Scene scene)
		{
			Directory.CreateDirectory (path);
			XmlTextWriter writer = new XmlTextWriter (path + "vegetation.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement ("successions");
			foreach (SuccessionType s in successions) {
				s.Save(writer, scene);
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close();
		}
	}
}
