using System.Collections.Generic;
using System.Xml;
using System;

using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneData.PlantRules
{
	public class PlantRule : ICloneable
	{
		public const string XML_ELEMENT = "rule";

		public string description = "";

		public ParameterRange[] parameterConditions;
		public VegetationCondition[] vegetationConditions;

		public float chance = 1.0f;
		public int delta = 1;
		public bool canSpawn = true;

		public PlantRule ()
		{
			description = "New rule";

			parameterConditions = new ParameterRange[0];
			vegetationConditions = new VegetationCondition[] { new VegetationCondition() };
		}

		public static PlantRule Load (XmlTextReader reader, Scene scene)
		{
			PlantRule rule = new PlantRule ();
			rule.description = reader.GetAttribute ("description");
			rule.chance = float.Parse(reader.GetAttribute ("chance"));
			rule.delta = int.Parse(reader.GetAttribute ("delta"));
			rule.canSpawn = bool.Parse(reader.GetAttribute ("canspawn"));

			List<ParameterRange> paramConditions = new List<ParameterRange>();
			List<VegetationCondition> vegConditions = new List<VegetationCondition>();

			if (!reader.IsEmptyElement) {
				while (reader.Read ()) {
					XmlNodeType nType = reader.NodeType;
					if (nType == XmlNodeType.Element) {
						switch (reader.Name.ToLower()) 
						{
						case ParameterRange.XML_ELEMENT :
							ParameterRange pr = ParameterRange.Load (reader, scene);
							if (pr != null) {
								paramConditions.Add (pr);
							}
							break;
						case VegetationCondition.XML_ELEMENT : 
							VegetationCondition vc = VegetationCondition.Load (reader, scene);
							if (vc != null) {
								vegConditions.Add (vc);
							}
							break;
						}
					} else if (nType == XmlNodeType.EndElement && reader.Name.ToLower() == XML_ELEMENT) {
						break;
					}
				}
			}

			rule.parameterConditions = paramConditions.ToArray();
			rule.vegetationConditions = vegConditions.ToArray();
			return rule;
		}

		public object Clone () 
		{
			PlantRule clone = new PlantRule();
			clone.description = description;
			clone.parameterConditions = new ParameterRange[parameterConditions.Length];
			for (int i = 0; i < clone.parameterConditions.Length; i++) {
				clone.parameterConditions[i] = (ParameterRange)parameterConditions[i].Clone();
			}
			clone.vegetationConditions = new VegetationCondition[vegetationConditions.Length];
			for (int i = 0; i < clone.vegetationConditions.Length; i++) {
				clone.vegetationConditions[i] = (VegetationCondition)vegetationConditions[i].Clone();
			}
			clone.chance = chance;
			clone.delta = delta;
			clone.canSpawn = canSpawn;
			return clone;
		}

		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("description", description);
			writer.WriteAttributeString ("chance", chance.ToString());
			writer.WriteAttributeString ("delta", delta.ToString());
			writer.WriteAttributeString ("canspawn", canSpawn.ToString().ToLower());
			foreach (ParameterRange pr in parameterConditions) {
				pr.Save (writer, scene);
			}
			foreach (VegetationCondition vc in vegetationConditions) {
				vc.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}

		public void UpdateReferences (Scene scene, PlantType veg)
		{
			foreach (ParameterRange pr in parameterConditions) {
				pr.UpdateReferences (scene);
			}
		}
	}
}