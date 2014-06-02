using System.Collections.Generic;
using System.Xml;
using System;

using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;
using Ecosim.SceneData.Rules;

namespace Ecosim.SceneData.VegetationRules
{
	/**
	 * parameters of a vegetation tile can change over time, depending on chance and measures active on the tile
	 */
	public class VegetationRule : ICloneable
	{
		public const string XML_ELEMENT = "rule";
		public string actionName = null; // name of action
		public UserInteraction action = null;
		public float chance = 1.0f; // chance the change will be made (0.0..1.0)
		public ParameterRange[] ranges; // valid parameter values for rule to be able to fire
		public int vegetationId; // destination vegetation id (if rule succeeds)
		public VegetationType vegetation; // destination vegetation (if rule succeeds)
		
		public VegetationRule ()
		{
			ranges = new ParameterRange[0];
		}
		
		public static VegetationRule Load (XmlTextReader reader, Scene scene)
		{
			VegetationRule result = null;
			string actionName = reader.GetAttribute ("action");
			result = new VegetationRule ();
			result.actionName = actionName;
			result.chance = float.Parse (reader.GetAttribute ("chance"));
			result.vegetationId = int.Parse (reader.GetAttribute ("target"));
			List<ParameterRange> ranges = new List<ParameterRange> ();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ParameterRange.XML_ELEMENT)) {
						ParameterRange pr = ParameterRange.Load (reader, scene);
						if (pr != null) {
							ranges.Add (pr);
						}
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			result.ranges = ranges.ToArray ();
			return result;
		}
				
		public object Clone () {
			VegetationRule clone = new VegetationRule ();
			clone.action = action;
			clone.actionName = actionName;
			clone.chance = chance;
			clone.ranges = (ParameterRange[]) ranges.Clone ();
			clone.vegetationId = vegetationId;
			clone.vegetation = vegetation;
			return clone;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			if (action != null) {
				writer.WriteAttributeString ("action", action.name);
			}
			else if (actionName != null) {
				writer.WriteAttributeString ("action", actionName);
			}
			writer.WriteAttributeString ("chance", chance.ToString ());
			writer.WriteAttributeString ("target", vegetationId.ToString ());
			foreach (ParameterRange pr in ranges) {
				pr.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}

		public void UpdateReferences (Scene scene, VegetationType veg)
		{
			if (action != null) {
				actionName = action.name;
			}
			action = null;
			if (actionName != null) {
				UserInteraction ui = scene.actions.GetUIByName (actionName);
				if ((ui != null) && (ui.action is AreaAction)) {
					action = ui;
				} else {
					UnityEngine.Debug.Log ("Action '" + actionName + "' is not referencing an AreaAction");
				}
			}
			vegetation = veg.successionType.vegetations [vegetationId];
			foreach (ParameterRange pr in ranges) {
				pr.UpdateReferences (scene);
			}
			
		}
	}
}