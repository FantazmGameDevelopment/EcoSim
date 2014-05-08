using System.Collections.Generic;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	/**
	 * Icon, name description to show in player GUI
	 */
	public class UserInteractionGroup
	{
		public const string CATEGORY_RESEARCH = "research";
		public const string CATEGORY_MEASURES = "measures";
		public const string XML_ELEMENT = "uicat";
		public readonly string category;
		private readonly ActionMgr actions;
		private readonly Scene scene;
		
		public class GroupData
		{
			public int iconId;
			public Texture2D icon;
			public Texture2D activeIcon;
			public UserInteraction[] uiList; // all the ui elements for group
			public string[] uiNamesList; // only used for finding references to ui elements while loading
		}
		
		public GroupData[] groups;
		
		public UserInteractionGroup (Scene scene, ActionMgr actions, string name)
		{
			category = name;
			this.scene = scene;
			this.actions = actions;
			groups = new GroupData[0];
		}
		
		public GroupData ReadGroup (XmlTextReader reader)
		{
			GroupData grp = new GroupData ();
			grp.iconId = int.Parse (reader.GetAttribute ("icon"));
			List<string> names = new List<string> ();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "ui")) {
						names.Add (reader.GetAttribute ("name"));
						IOUtil.ReadUntilEndElement (reader, "ui");
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == "group")) {
						break;
					}
				}
			}
			grp.uiNamesList = names.ToArray ();
			return grp;
		}
				
		public static UserInteractionGroup Load (Scene scene, ActionMgr actions, XmlTextReader reader)
		{
			UserInteractionGroup ui = new UserInteractionGroup (scene, actions, reader.GetAttribute ("category"));
			List<GroupData> groupsList = new List<GroupData> ();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "group")) {
						groupsList.Add (ui.ReadGroup (reader));
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			ui.groups = groupsList.ToArray ();
			return ui;
		}
		
		public void Save (XmlTextWriter writer)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("category", category);
			foreach (GroupData grp in groups) {
				writer.WriteStartElement ("group");
				writer.WriteAttributeString ("icon", grp.iconId.ToString ());
				foreach (UserInteraction ui in grp.uiList) {
					writer.WriteStartElement ("ui");
					writer.WriteAttributeString ("name", ui.name);
					writer.WriteEndElement ();
				}
				writer.WriteEndElement ();
			}
			
			writer.WriteEndElement ();
		}
		
		public void UpdateReferences ()
		{
			foreach (GroupData grp in groups) {
				grp.icon = scene.assets.GetIcon (grp.iconId);
				grp.activeIcon = scene.assets.GetHighlightedIcon (grp.iconId);
				if (grp.uiList == null) {
					List<UserInteraction> uiList = new List<UserInteraction> ();
					if (grp.uiNamesList != null) {
						foreach (string uiName in grp.uiNamesList) {
							UserInteraction ui = actions.GetUIByName (uiName);
							if (ui != null) {
								uiList.Add (ui);
							}
						}
					}
					grp.uiList = uiList.ToArray ();
					grp.uiNamesList = null; // not needed anymore
				}
			}
		}
	}
}