﻿using System.Collections.Generic;
using System.IO;
using System.Xml;

using Ecosim.SceneData.Rules;
using Ecosim.SceneData.PlantRules;

namespace Ecosim.SceneData
{
	public class PlantType
	{
		public const string XML_ELEMENT = "plant";

		public string name;
		public int index;

		public PlantRule[] rules;

		public int maxPerTile;
		public int spawnRadius;
		public int spawnCount;
		public int spawnMultiplier;

		public string dataName;

		public PlantType ()
		{

		}

		public PlantType (Scene scene)
		{
			index = scene.plantTypes.Length;
			name = "New plant " + index;

			rules = new PlantRule[0];

			maxPerTile = 3;
			spawnRadius = 5;
			spawnCount = 10;
			spawnMultiplier = 1;

			// Add to scene
			List<PlantType> tmpPlantList = new List<PlantType>(scene.plantTypes);
			tmpPlantList.Add (this);
			scene.plantTypes = tmpPlantList.ToArray();

			// Data name
			dataName = string.Format("_plant{0}", index);
			if (!scene.progression.HasData (dataName))
				scene.progression.AddData (dataName, new BitMap8 (scene));
		}

		public static PlantType Load (XmlTextReader reader, Scene scene)
		{
			PlantType plant = new PlantType ();
			plant.name = reader.GetAttribute ("name");
			plant.maxPerTile = int.Parse(reader.GetAttribute ("maxpertile"));
			plant.spawnRadius = int.Parse(reader.GetAttribute ("spawnradius"));
			plant.spawnCount = int.Parse(reader.GetAttribute ("spawncount"));
			plant.spawnMultiplier = int.Parse(reader.GetAttribute ("spawnmultiplier"));
			plant.dataName = reader.GetAttribute ("dataname");

			if (string.IsNullOrEmpty(plant.dataName)) 
				plant.dataName = string.Format("_plant{0}", StringUtil.MakeValidID(plant.name));

			// Check if the data exists

			List<PlantRule> rules = new List<PlantRule>();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == PlantRule.XML_ELEMENT)) {
						PlantRule rule = PlantRule.Load (reader, scene);
						if (rule != null) {
							rules.Add (rule);
						}
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower() == XML_ELEMENT)) {
						break;
					}
				}
			}
			plant.rules = rules.ToArray();
			return plant;
		}

		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("name", name);
			writer.WriteAttributeString ("maxpertile", maxPerTile.ToString());
			writer.WriteAttributeString ("spawnradius", spawnRadius.ToString());
			writer.WriteAttributeString ("spawncount", spawnCount.ToString());
			writer.WriteAttributeString ("spawnmultiplier", spawnMultiplier.ToString());
			writer.WriteAttributeString ("dataname", dataName);
			foreach (PlantRule r in rules) {
				r.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}

		public void UpdateReferences (Scene scene)
		{
			foreach (PlantRule r in rules) {
				r.UpdateReferences (scene, this);
			}
		}
	}
}
