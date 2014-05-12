using System.Collections.Generic;
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
		public PlantGerminationRule[] germinationRules;

		public int maxPerTile;
		public int spawnRadius;
		public int spawnCount;
		public int spawnMultiplier;

		public string dataName;

		public PlantType ()
		{

		}

		public PlantType (Scene scene, string name)
		{
			this.name = name;
			this.dataName = StringUtil.MakeValidID (name);

			// Data name
			string newDataName = this.dataName;
			int tries = 1;
			while (scene.progression.HasData (newDataName)) {
				newDataName = this.dataName + tries;
				tries++;
			}
			this.dataName = newDataName;
			scene.progression.AddData (dataName, new BitMap8 (scene));

			index = scene.plantTypes.Length;
			rules = new PlantRule[1];
			germinationRules = new PlantGerminationRule[0];

			maxPerTile = 3;
			spawnRadius = 5;
			spawnCount = 10;
			spawnMultiplier = 1;

			// Add to scene
			List<PlantType> tmpPlantList = new List<PlantType>(scene.plantTypes);
			tmpPlantList.Add (this);
			scene.plantTypes = tmpPlantList.ToArray();
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
			List<PlantGerminationRule> germinationRules = new List<PlantGerminationRule>();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == PlantRule.XML_ELEMENT)) {
						PlantRule rule = PlantRule.Load (reader, scene);
						if (rule != null) {
							rules.Add (rule);
						}
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == PlantGerminationRule.XML_ELEMENT)) {
						PlantGerminationRule germRule = PlantGerminationRule.Load (reader, scene);
						if (germRule != null) {
							germinationRules.Add (germRule);
						}
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower() == XML_ELEMENT)) {
						break;
					}
				}
			}
			plant.rules = rules.ToArray();
			plant.germinationRules = germinationRules.ToArray();
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
			foreach (PlantGerminationRule gr in germinationRules) {
				gr.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}

		public void UpdateReferences (Scene scene)
		{
			foreach (PlantRule r in rules) {
				r.UpdateReferences (scene, this);
			}

			foreach (PlantGerminationRule r in germinationRules) {
				r.UpdateReferences (scene, this);
			}
		}

		public static PlantType Find (Scene scene, string name)
		{
			foreach (PlantType t in scene.plantTypes) {
				if (t.name == name) return t;
			}
			return null;
		}

		public static PlantType FindByDataName (Scene scene, string dataName)
		{
			foreach (PlantType t in scene.plantTypes) {
				if (t.dataName == dataName) return t;
			}
			return null;
		}
	}
}
