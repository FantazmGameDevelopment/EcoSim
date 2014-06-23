using System.Collections.Generic;
using System.IO;
using System.Xml;

using UnityEngine;
using Ecosim.SceneData.Rules;

namespace Ecosim.SceneData
{
	public class InfluenceValue
	{
		public Coordinate coord;
		public int value;
		
		public InfluenceValue (int x, int y, int value)
		{
			this.coord = new Coordinate (x, y);
			this.value = value;
		}
		
		public InfluenceValue (Coordinate coord, int value)
		{
			this.coord = coord;
			this.value = value;
		}
		
		public static InfluenceValue Load (string str)
		{
			string[] split = str.Split (',');
			int x = int.Parse (split[0]);
			int y = int.Parse (split[1]);
			int v = int.Parse (split[2]);
			return new InfluenceValue (x, y, v);
		}
		
		public override string ToString ()
		{
			return string.Format ("{0},{1},{2}", coord.x, coord.y, value);
		}
	}

	public class ActionObject
	{
		public const string XML_ELEMENT = "actionobject";

		public ActionObjectsGroup group;
		public int index = 0;

		public int buildingId;
		public Buildings.Building building;

		private bool _enabled = false;
		public bool enabled {
			get { return _enabled; }
			set {
				if (_enabled != value) {
					_enabled = value;
					building.isActive = true;
				}
			}
		}

		public Data data;

		public ActionObject (ActionObjectsGroup group)
		{
			this.group = group;
		}

		public ActionObject (Scene scene, ActionObjectsGroup group, Buildings.Building building)
		{
			this.group = group;
			this.building = building;
			this.building.startsActive = false;
			this.building.isActive = false;
			this.buildingId = this.building.id;
			this.data = new BitMap8 (scene);
		}

		public static ActionObject Load (XmlTextReader reader, Scene scene, ActionObjectsGroup group)
		{
			ActionObject result = new ActionObject (group);

			int bId = -1;
			if (int.TryParse (reader.GetAttribute ("buildingid"), out bId)) result.buildingId = bId;

			int index = -1;
			if (int.TryParse (reader.GetAttribute ("index"), out index)) result.index = index;

			// Get the building
			result.RetrieveBuilding (scene);

			// Influences
			result.data = new BitMap8 (scene);
			string influence = reader.GetAttribute ("influence");
			if (!string.IsNullOrEmpty (influence))
			{
				string[] split = influence.Split ('|');
				foreach (string s in split) {
					InfluenceValue i = InfluenceValue.Load (s);
					result.data.Set (i.coord, i.value);
				}
			}

			result.enabled = (reader.GetAttribute ("enabled") == "true") ? true : false;
			IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			return result;
		}

		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("buildingid", buildingId.ToString());
			writer.WriteAttributeString ("enabled", enabled.ToString().ToLower());
			writer.WriteAttributeString ("index", index.ToString());

			// Influences
			string influence = "";
			foreach (ValueCoordinate vc in this.data.EnumerateNotZero()) {
				influence += string.Format ("{0},{1},{2}|", vc.x, vc.y, vc.v);
			}
			// Remove the trailing |
			if (influence.Length > 0) influence = influence.Substring (0, influence.Length - 1);
			writer.WriteAttributeString ("influence", influence);

			writer.WriteEndElement ();
		}

		public void RetrieveBuilding (Scene scene)
		{
			// Get the building
			List<Buildings.Building> buildings = scene.buildings.GetAllBuildings ();
			foreach (Buildings.Building b in buildings) 
			{
				if (b.id == buildingId) {
					this.building = b;
					break;
				}
			}
		}
	}

	public class ActionObjectInfluenceRule
	{
		public const string XML_ELEMENT = "influencerule";

		public enum MathTypes
		{
			Equals,
			Multiply,
			Plus,
			Minus
		}

		public enum ValueType
		{
			Range,
			Value
		}

		public string paramName;
		public MathTypes mathType;
		public float mathValue;

		public float lowRange;
		public float highRange;

		public ValueType valueType;
		public Data data;

		public ActionObjectInfluenceRule ()
		{
		}

		public static ActionObjectInfluenceRule Load (XmlTextReader reader, Scene scene)
		{
			ActionObjectInfluenceRule result = new ActionObjectInfluenceRule ();
			result.paramName = reader.GetAttribute ("parameter");
			result.lowRange = float.Parse (reader.GetAttribute ("min"));
			result.highRange = float.Parse (reader.GetAttribute ("max"));
			result.mathType = (MathTypes)System.Enum.Parse (typeof(MathTypes), reader.GetAttribute ("mathtype"));
			result.mathValue = float.Parse (reader.GetAttribute ("mathvalue"));
			result.valueType = (ValueType)System.Enum.Parse (typeof(ValueType), reader.GetAttribute ("valuetype"));



			IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			return result;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("parameter", paramName);
			writer.WriteAttributeString ("mathtype", mathType.ToString());
			writer.WriteAttributeString ("mathvalue", mathValue.ToString());
			writer.WriteAttributeString ("valuetype", valueType.ToString());
			writer.WriteAttributeString ("min", lowRange.ToString());
			writer.WriteAttributeString ("max", highRange.ToString());
			writer.WriteEndElement ();
		}

		public void UpdateReferences (Scene scene)
		{
			data = scene.progression.GetData (paramName);
		}
	}

	public class ActionObjectsGroup
	{
		public const string XML_ELEMENT = "actionobjectgroup";

		/* Combined: This group is treated as a whole and acts as 'one' combined object.
		 * Collection: Every object in this collection is treated seperately.
		 */ 
		public enum GroupType
		{
			Collection,
			Combined
		}
		public GroupType groupType;

		public Scene scene;
		public string name;
		public string description;

		public int index;

		public ActionObject[] actionObjects;
		public ActionObjectInfluenceRule[] influenceRules;

		private bool _enabled = false;
		public bool enabled
		{
			get { return _enabled; }
			set {
				_enabled = value;

				switch (groupType)
				{
				case GroupType.Combined :
					// We enabled all action objects
					if (_enabled) {
						foreach (ActionObject obj in actionObjects) {
							obj.enabled = true;
						}
					}
					break;

				case GroupType.Collection :
					// We let the action objects themselves see whether they're enabled yes or no
					break;
				}
			}
		}

		/// <summary>
		/// The combined data data reference. Only used when groupType equals Combined.
		/// </summary>
		public Data combinedData;

		public ActionObjectsGroup ()
		{
		}
		
		public ActionObjectsGroup (Scene scene, string name)
		{
			this.scene = scene;
			this.name = name;
			this.description = "";
			this.index = scene.actionObjectGroups.Length;

			actionObjects = new ActionObject[] { };
			influenceRules = new ActionObjectInfluenceRule[] { };
			this.combinedData = new BitMap8 (scene);

			// Add to scene
			List<ActionObjectsGroup> tmpList = new List<ActionObjectsGroup>(scene.actionObjectGroups);
			tmpList.Add (this);
			scene.actionObjectGroups = tmpList.ToArray();
		}
		
		public static ActionObjectsGroup Load (XmlTextReader reader, Scene scene)
		{
			ActionObjectsGroup result = new ActionObjectsGroup ();
			result.name = reader.GetAttribute ("name");
			result.description = reader.GetAttribute ("description");

			if (!string.IsNullOrEmpty(reader.GetAttribute ("grouptype")))
				result.groupType = (GroupType)System.Enum.Parse(typeof(GroupType), reader.GetAttribute ("grouptype"));

			// Influences
			result.combinedData = new BitMap8 (scene);
			string influence = reader.GetAttribute ("influence");
			if (!string.IsNullOrEmpty (influence))
			{
				string[] split = influence.Split ('|');
				foreach (string s in split) {
					InfluenceValue i = InfluenceValue.Load (s);
					result.combinedData.Set (i.coord, i.value);
				}
			}

			List<ActionObject> actionObjs = new List<ActionObject>();
			List<ActionObjectInfluenceRule> influenceRules = new List<ActionObjectInfluenceRule>();

			if (!reader.IsEmptyElement) 
			{
				while (reader.Read()) 
				{
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == ActionObject.XML_ELEMENT)) 
					{
						ActionObject ao = ActionObject.Load (reader, scene, result);
						if (ao != null) {
							// Check index
							if (ao.index <= 0) {
								if (actionObjs.Count > 0)
									ao.index = actionObjs[actionObjs.Count - 1].index + 1;
								else ao.index = 1;
							}
							// Check building id
							if (ao.buildingId <= 0) {
								if (actionObjs.Count > 0)
									ao.buildingId = actionObjs[actionObjs.Count - 1].buildingId + 1;
								else ao.buildingId = 1;
								ao.RetrieveBuilding (scene);
							}
							actionObjs.Add (ao);
						}
					} 
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == ActionObjectInfluenceRule.XML_ELEMENT)) {
						ActionObjectInfluenceRule rule = ActionObjectInfluenceRule.Load (reader, scene);
						if (rule != null) {
							influenceRules.Add (rule);
						}
					} 
					else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower() == XML_ELEMENT)) 
					{
						break;
					}
				}
			}
			result.actionObjects = actionObjs.ToArray();
			result.influenceRules = influenceRules.ToArray();
			return result;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("name", name);
			writer.WriteAttributeString ("description", description);
			writer.WriteAttributeString ("grouptype", groupType.ToString());

			// Influences
			string influence = "";
			foreach (ValueCoordinate vc in this.combinedData.EnumerateNotZero()) {
				influence += string.Format ("{0},{1},{2}|", vc.x, vc.y, vc.v);
			}
			// Remove the trailing |
			if (influence.Length > 0) influence = influence.Substring (0, influence.Length - 1);
			writer.WriteAttributeString ("influence", influence);

			foreach (ActionObject aObj in actionObjects) {
				aObj.Save (writer, scene);
			}
			foreach (ActionObjectInfluenceRule inflRule in influenceRules) {
				inflRule.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}
		
		public void UpdateReferences (Scene scene)
		{
			foreach (ActionObjectInfluenceRule rule in influenceRules)
			{
				rule.UpdateReferences (scene);
			}
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
