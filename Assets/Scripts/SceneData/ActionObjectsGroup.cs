using System.Collections.Generic;
using System.IO;
using System.Xml;

using UnityEngine;
using Ecosim.SceneData.Rules;

namespace Ecosim.SceneData
{
	public class ActionObject
	{
		public const string XML_ELEMENT = "actionobject";

		public int buildingId;
		public Buildings.Building building;

		public ActionObject ()
		{
		}

		public ActionObject (Buildings.Building building)
		{
			this.building = building;
			this.building.isActive = false;
			this.building.combinable = false;
			this.buildingId = this.building.id;
		}

		public static ActionObject Load (XmlTextReader reader, Scene scene)
		{
			ActionObject result = new ActionObject ();
			result.buildingId = int.Parse(reader.GetAttribute ("buildingid"));

			// Get the building
			List<Buildings.Building> buildings = scene.buildings.GetAllBuildings ();
			foreach (Buildings.Building building in buildings) 
			{
				if (building.id == result.buildingId) {
					result.building = building;
					break;
				}
			}
			IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			return result;
		}

		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("buildingid", buildingId.ToString());
			writer.WriteEndElement ();
		}
	}

	public class ActionObjectsGroup
	{
		public const string XML_ELEMENT = "actionobjectgroup";
		
		public string name;
		public string description;
		public int cost;
		public string dataName;

		public int index;

		public ActionObject[] actionObjects;
		
		public ActionObjectsGroup ()
		{
		}
		
		public ActionObjectsGroup (Scene scene, string name)
		{
			this.name = name;
			this.description = "";
			this.cost = 1000;
			this.dataName = "_" + StringUtil.MakeValidID (name);
			
			// Data name
			string newDataName = this.dataName;
			int tries = 1;
			while (scene.progression.HasData (newDataName)) {
				newDataName = this.dataName + tries;
				tries++;
			}
			this.dataName = newDataName;
			scene.progression.AddData (dataName, new BitMap8 (scene));
			
			index = scene.actionObjectGroups.Length;

			actionObjects = new ActionObject[] { };
			
			// Add to scene
			List<ActionObjectsGroup> tmpList = new List<ActionObjectsGroup>(scene.actionObjectGroups);
			tmpList.Add (this);
			scene.actionObjectGroups = tmpList.ToArray();
		}
		
		public static ActionObjectsGroup Load (XmlTextReader reader, Scene scene)
		{
			ActionObjectsGroup result = new ActionObjectsGroup ();
			result.name = reader.GetAttribute ("name");
			result.dataName = reader.GetAttribute ("dataname");
			result.description = reader.GetAttribute ("description");

			if (string.IsNullOrEmpty(result.dataName)) 
				result.dataName = string.Format("_actiongroup{0}", StringUtil.MakeValidID(result.name));

			List<ActionObject> actionObjs = new List<ActionObject>();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == ActionObject.XML_ELEMENT)) 
					{
						ActionObject ao = ActionObject.Load (reader, scene);
						if (ao != null) {
							actionObjs.Add (ao);
						}
					} /*else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == PlantGerminationRule.XML_ELEMENT)) {
						PlantGerminationRule germRule = PlantGerminationRule.Load (reader, scene);
						if (germRule != null) {
							germinationRules.Add (germRule);
						}
					}*/ else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower() == XML_ELEMENT)) 
					{
						break;
					}
				}
			}
			result.actionObjects = actionObjs.ToArray();
			return result;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("name", name);
			writer.WriteAttributeString ("dataname", dataName);
			writer.WriteAttributeString ("description", description);
			foreach (ActionObject aObj in actionObjects) {
				aObj.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}
		
		public void UpdateReferences (Scene scene)
		{

		}

		public static ActionObjectsGroup[] Load (string path, Scene scene)
		{
			List<ActionObjectsGroup> list = new List<ActionObjectsGroup>();
			string url = path + "actionobjectgroups.xml";
			if (File.Exists(url)) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (url));
				try {
					while (reader.Read ()) 
					{
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == ActionObjectsGroup.XML_ELEMENT)) {
							ActionObjectsGroup g = ActionObjectsGroup.Load (reader, scene);
							if (g != null) {
								list.Add (g);
							}
						}
					}
				} finally {
					reader.Close ();
				}
			}
			return list.ToArray();
		}

		public static void Save (string path, ActionObjectsGroup[] groups, Scene scene)
		{
			Directory.CreateDirectory (path);
			XmlTextWriter writer = new XmlTextWriter (path + "actionobjectgroups.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement("actionobjectgroups");
			foreach (ActionObjectsGroup g in groups) {
				g.Save (writer, scene);
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close();
		}
	}
}
