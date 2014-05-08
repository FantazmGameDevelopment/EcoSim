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
		public class VegetationCondition : ICloneable
		{
			public const string XML_ELEMENT = "vegetation";

			public int successionIndex;
			public int vegetationIndex;

			public VegetationCondition (int successionIndex, int vegetationIndex)
			{
				this.successionIndex = successionIndex;
				this.vegetationIndex = vegetationIndex;
			}

			public static VegetationCondition Load (XmlTextReader reader, Scene scene)
			{
				int sucIndex = int.Parse(reader.GetAttribute("successionindex"));
				int vegIndex = int.Parse(reader.GetAttribute("vegetationindex"));
				VegetationCondition result = new VegetationCondition (sucIndex, vegIndex);
				IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
				return result;
			}

			public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("successionindex", successionIndex.ToString());
				writer.WriteAttributeString ("vegetationindex", vegetationIndex.ToString());
				writer.WriteEndElement ();
			}

			public object Clone()
			{
				VegetationCondition clone = new VegetationCondition (successionIndex, vegetationIndex);
				return clone;
			}

			public bool IsCompatible (int sucIndex, int vegIndex)
			{
				bool correctVegetation = false;
				{
					bool correctSuccession = false;
					if (this.successionIndex < 0) 
						correctSuccession = true;
					else if (this.successionIndex == sucIndex) 
						correctSuccession = true;
					
					if (correctSuccession) {
						if (this.vegetationIndex < 0)
							correctVegetation = true;
						else if (this.vegetationIndex == vegIndex)
							correctVegetation = true;
					}
				}
				return correctVegetation;
			}
		}

		public const string XML_ELEMENT = "rule";

		public string description = "";

		public ParameterRange[] parameterConditions;
		public VegetationCondition[] vegetationConditions;

		public float chance = 1.0f;
		public int delta = 1;
		public bool canSpawn = true;

		public PlantRule ()
		{
			parameterConditions = new ParameterRange[]{};
			vegetationConditions = new VegetationCondition[]{};
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

			/*
			 * 
			List<ParameterChange> newChanges = new List<ParameterChange> ();
			List<VegetationRule> newRules = new List<VegetationRule> ();
			List<string> acceptableActionsList = new List<string> ();
			List<GradualParameterChange> gradualChanges = new List<GradualParameterChange> ();
			List<TileType> tileList = new List<TileType> ();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ParameterChange.XML_ELEMENT)) {
						ParameterChange pc = ParameterChange.Load (reader, scene);
						if (pc != null) {
							newChanges.Add (pc);
						}
					}
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "action")) {
						
						string measureName = reader.GetAttribute ("name");
						if (measureName != null) {
							acceptableActionsList.Add (measureName.ToLower ());
						}
						IOUtil.ReadUntilEndElement (reader, "action");
						
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == GradualParameterChange.XML_ELEMENT)) {
						GradualParameterChange gpc = GradualParameterChange.Load (reader, scene);
						if (gpc != null) {
							gradualChanges.Add (gpc);
						}
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == VegetationRule.XML_ELEMENT)) {
						VegetationRule r = VegetationRule.Load (reader, scene);
						if (r != null) {
							newRules.Add (r);
						}
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == TileType.XML_ELEMENT)) {
						tileList.Add (TileType.Load (reader, scene));
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			veg.changes = newChanges.ToArray ();
			veg.gradualChanges = gradualChanges.ToArray ();
			veg.rules = newRules.ToArray ();
			veg.tiles = tileList.ToArray ();

			veg.acceptableActionStrings = acceptableActionsList.ToArray ();
			return veg;
			 */ 
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