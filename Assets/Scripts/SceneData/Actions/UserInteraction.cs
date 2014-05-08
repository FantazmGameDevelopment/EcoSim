using System.Collections.Generic;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;

namespace Ecosim.SceneData.Action
{
	/**
	 * Icon, name description to show in player GUI
	 */
	public class UserInteraction
	{
		public const string XML_ELEMENT = "icon";
		
		public UserInteraction(BasicAction action) {
			this.action = action;
		}
		
		public string name = "action";
		public string description = "";
		public string help = "";
		public long cost; // cost per unit
		public long estimatedTotalCostForYear = 0;
		public int index; // index within action
		public int iconId; // reference to index in icon texture
		public UnityEngine.Texture2D icon;
		public UnityEngine.Texture2D activeIcon;
		
		
		public readonly BasicAction action; // action this icon belongs to

		public static UserInteraction Load (BasicAction action, XmlTextReader reader) {
			UserInteraction ui = new UserInteraction (action);
			
			ui.name = reader.GetAttribute ("name");
			ui.description = reader.GetAttribute ("description");
			ui.help = reader.GetAttribute ("help");
			ui.cost = long.Parse (reader.GetAttribute ("cost"));
			
			ui.iconId = int.Parse (reader.GetAttribute ("icon"));
			IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			return ui;
		}
		
		public void Save (XmlTextWriter writer) {
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("name", name);
			writer.WriteAttributeString ("description", description);
			writer.WriteAttributeString ("help", help);
			
			writer.WriteAttributeString ("cost", cost.ToString ());
			writer.WriteAttributeString ("icon", iconId.ToString ());
			writer.WriteEndElement ();
		}
	}
}