using System.Collections.Generic;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class ResearchPointAction : BasicAction
	{
		public class ParameterString
		{
			public const string XML_ELEMENT = "paramstr";

			public string start;
			public string paramName;
			public string end;

			public ParameterString (Scene scene)
			{
			}

			public static ParameterString Load (Scene scene, XmlTextReader reader)
			{
				ParameterString param = new ParameterString (scene);
				param.paramName = reader.GetAttribute ("param");
				param.start = reader.GetAttribute ("start");
				param.end = reader.GetAttribute ("end");
				return param;
			}

			public override void Save (XmlTextWriter writer)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("param", paramName);
				writer.WriteAttributeString ("start", start);
				writer.WriteAttributeString ("end", end);
				writer.WriteEndElement ();
			}
		}

		// Port notes: EcoSim 1.0 view JDisplayTag, JResearchPointsMgr and JResearchPoint

		public const string XML_ELEMENT = "researchpoint";
		private string description = "Research point";

		public ParameterString[] parameters;

		public ResearchPointAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public ResearchPointAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			UserInteraction ui = new UserInteraction (this);
			uiList.Add (ui);
		}
		
		public override string GetDescription ()
		{
			return description;
		}
		
		public override void SetDescription (string description)
		{
			this.description = description;
		}
		
		public override bool DescriptionIsWritable ()
		{
			return true;
		}
		
		public static ResearchPointAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			ResearchPointAction action = new ResearchPointAction (scene, id);

			List<ParameterString> paramList = new List<ParameterString>();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteraction.XML_ELEMENT)) {
						action.uiList.Add (UserInteraction.Load (action, reader));
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ParameterString.XML_ELEMENT)) {
						paramList.Add (ParameterString.Load (scene, reader));
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			action.parameters = paramList.ToArray ();
			return action;
		}
		
		public override void Save (XmlTextWriter writer)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("id", id.ToString ());
			foreach (ParameterString p in parameters) {
				p.Save (writer);
			}
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}

		public override int GetMinUICount ()
		{
			return 1;
		}
		
		public override int GetMaxUICount ()
		{
			return 1;
		}
	}
}