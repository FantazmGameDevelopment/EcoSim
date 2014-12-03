using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

using Ecosim;

namespace Ecosim.SceneData
{
	/**
	 * SaveGame stores some basic data for save games
	 * It contains player info, the scene name, and the current year
	 * It also contains some descriptive information to show in load/save
	 * screen.
	 */
	public class SaveGame
	{
		public const string XML_ELEMENT = "savegame";
		public string sceneName; // needed for loading scene
		public string sceneDescription; // needed for preview as scene won't be loaded
		public int year;
		public long budget;
		public bool gameEnded = false;
		public System.DateTime date;
		public PlayerInfo playerInfo;
		
		SaveGame ()
		{
		}
		
		public SaveGame (Scene scene)
		{
			sceneName = scene.sceneName;
			sceneDescription = scene.description;
			if (scene.progression.year == 0) {
				year = scene.progression.startYear;
			}
			else {
				year = scene.progression.year;
			}
			budget = scene.progression.budget;
			gameEnded = scene.progression.gameEnded;
			playerInfo = scene.playerInfo;
		}

		void Load (XmlTextReader reader)
		{
			budget = long.Parse (reader.GetAttribute ("budget"));
			year = int.Parse (reader.GetAttribute ("year"));
			sceneName = reader.GetAttribute ("scene");
			gameEnded = (reader.GetAttribute ("gameended") == "true");
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == PlayerInfo.XML_ELEMENT)) {
						playerInfo = PlayerInfo.Load (reader);
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
		}
		
		public static SaveGame Load (int slotNr)
		{
			SaveGame saveGame = new SaveGame ();
			string path = GameSettings.GetPathForSlotNr (slotNr) + "savegame.xml";
			if (File.Exists (path)) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path));
				try {
					while (reader.Read()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == XML_ELEMENT)) {
							saveGame.Load (reader);
						}
					}
					saveGame.date = File.GetLastWriteTimeUtc (path);
					return saveGame;
				} finally {
					reader.Close ();
				}
			}
			return null;
		}
		
		public void Save (Scene scene)
		{
			if (!Directory.Exists (GameSettings.SaveGamesPath)) {
				Directory.CreateDirectory (GameSettings.SaveGamesPath);
			}
			if (!Directory.Exists (GameSettings.GetPathForSlotNr (scene.slotNr))) {
				Directory.CreateDirectory (GameSettings.GetPathForSlotNr (scene.slotNr));
			}
			string path = GameSettings.GetPathForSlotNr (scene.slotNr) + "savegame.xml";
			XmlTextWriter writer = new XmlTextWriter (path, System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("budget", budget.ToString ());
			writer.WriteAttributeString ("year", year.ToString ());
			writer.WriteAttributeString ("scene", scene.sceneName);
			writer.WriteAttributeString ("gameended" , gameEnded?"true":"false");
			playerInfo.Save (writer, scene);
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();
		}
	}
	
}