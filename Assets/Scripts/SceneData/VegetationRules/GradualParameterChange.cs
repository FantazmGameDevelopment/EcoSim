using System.Collections;
using System.Xml;
using System;

using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneData.VegetationRules
{
	/**
	 * parameters of a vegetation tile can change over time, depending on chance and measures active on the tile
	 */
	public class GradualParameterChange : ICloneable
	{
		public const string XML_ELEMENT = "gradualchange";
		public string paramName; // name of parameter
		public string actionName; // name of action
		public UserInteraction action;
		public float chance = 1.0f; // chance the change will be made (0.0..1.0)
		public int lowRange = 0; // parameter value won't leave range
		public int highRange = 255; // parameter value won't leave range
		public int deltaChange = 0; // delta change per year of parameter
		public Data data; // quick access to parameter data
		
		public object Clone () {
			GradualParameterChange clone = new GradualParameterChange ();
			clone.paramName = paramName;
			clone.actionName = actionName;
			clone.action = action;
			clone.chance = chance;
			clone.lowRange = lowRange;
			clone.highRange = highRange;
			clone.deltaChange = deltaChange;
			clone.data = data;
			return clone;
		}
		
		public static GradualParameterChange Load (XmlTextReader reader, Scene scene)
		{
			
			GradualParameterChange result = null;
			string actionName = reader.GetAttribute ("action");
			string paramName = reader.GetAttribute ("parameter");
			result = new GradualParameterChange ();
			result.actionName = actionName;
			result.paramName = paramName;
			result.lowRange = int.Parse (reader.GetAttribute ("low"));
			result.highRange = int.Parse (reader.GetAttribute ("high"));
			result.deltaChange = int.Parse (reader.GetAttribute ("delta"));
			result.chance = float.Parse (reader.GetAttribute ("chance"));
			IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			return result;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("parameter", paramName);
			if (action != null) {
				writer.WriteAttributeString ("action", action.name);
			}
			else if (actionName != null) {
				writer.WriteAttributeString ("action", actionName);
			}
			writer.WriteAttributeString ("low", lowRange.ToString ());
			writer.WriteAttributeString ("high", highRange.ToString ());
			writer.WriteAttributeString ("delta", deltaChange.ToString ());
			writer.WriteAttributeString ("chance", chance.ToString ());
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
					UnityEngine.Debug.Log ("Action '" + actionName + "' is not referening an AreaAction");
				}
			}
			
			data = scene.progression.GetData(paramName);
		}
	}
}