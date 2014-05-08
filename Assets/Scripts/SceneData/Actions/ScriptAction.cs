using System.Collections.Generic;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class ScriptAction : BasicAction
	{
		public const string XML_ELEMENT = "script";
		private string description = "Unnamed script";
		
		public ScriptAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public ScriptAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			// as scripts is basically the purpose of this action, it starts with
			// the default template already created.
			CreateDefaultScript ();
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
		
		public static ScriptAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			ScriptAction action = new ScriptAction (scene, id);
			action.description = reader.GetAttribute ("description");

			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteraction.XML_ELEMENT)) {
						action.uiList.Add (UserInteraction.Load (action, reader));
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			return action;
		}
		
		public override void Save (XmlTextWriter writer)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("id", id.ToString ());
			writer.WriteAttributeString ("description", description);
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}
	}
}