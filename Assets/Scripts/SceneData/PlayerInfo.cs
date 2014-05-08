using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

using Ecosim;

namespace Ecosim.SceneData
{
	/**
	 * First, Last name and male/female
	 */
	public class PlayerInfo
	{
		public const string XML_ELEMENT = "playerinfo";

		public string firstName; // player first name
		public string familyName; // player last name
		public bool isMale;
		
		public PlayerInfo() {
		}

		public static PlayerInfo Load (XmlTextReader reader)
		{
			PlayerInfo playerInfo = new PlayerInfo();
			playerInfo.firstName = reader.GetAttribute ("firstname");
			playerInfo.familyName = reader.GetAttribute ("familyname");
			playerInfo.isMale = (reader.GetAttribute ("gender").ToLower ().StartsWith ("m"));
			IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			return playerInfo;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("firstname", firstName);
			writer.WriteAttributeString ("familyname", familyName);
			writer.WriteAttributeString ("gender", isMale ? "m" : "v");
			writer.WriteEndElement ();
		}
	}
	
}