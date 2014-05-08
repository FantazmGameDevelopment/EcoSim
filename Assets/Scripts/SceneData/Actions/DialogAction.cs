using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System;
using UnityEngine;
using Ecosim;
using Ecosim.SceneData;

namespace Ecosim.SceneData.Action
{
	public class DialogAction : BasicAction
	{
		public const string XML_ELEMENT = "dialog";
		public string dialogText = "Dialog text";
		public string shortDescText = "Do thing?";
		private string description;
		private MethodInfo dialogCheckedMI;
		private MethodInfo dialogUncheckedMI;
		public bool isSelected = false;
		
		public DialogAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public DialogAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			description = "Dialog " + id;
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
		
		public override int GetMinUICount ()
		{
			return 1;
		}
		
		public override int GetMaxUICount ()
		{
			return 1;
		}

		public override void ActionSelected (UserInteraction ui)
		{
			base.ActionSelected (ui);
			new Ecosim.GameCtrl.GameButtons.ActionDialogWindow (ui, isSelected);
		}
		
		protected override void UnlinkEcoBase ()
		{
			base.UnlinkEcoBase ();
			dialogCheckedMI = null;
			dialogUncheckedMI = null;
		}
		
		protected override void LinkEcoBase ()
		{
			base.LinkEcoBase ();
			if (ecoBase != null) {
				dialogCheckedMI = ecoBase.GetType ().GetMethod ("DialogChecked",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {}, null);
				dialogUncheckedMI = ecoBase.GetType ().GetMethod ("DialogUnchecked",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {}, null);
			}
		}
		
		/**
		 * Called when user clicked Accept in dialog window to close
		 * and the checkbox has changed to Checked
		 */
		public virtual void DialogChangedToChecked ()
		{
			if (!isSelected) {
				isSelected = true;
				uiList [0].estimatedTotalCostForYear = uiList [0].cost;
				if (dialogCheckedMI != null) {
					dialogCheckedMI.Invoke (ecoBase, null);
				}
			}
		}

		/**
		 * Called when user clicked Accept in dialog window to close
		 * and the checkbox has changed to Unchecked
		 */
		public virtual void DialogChangedToUnchecked ()
		{
			if (isSelected) {
				isSelected = false;
				if (dialogUncheckedMI != null) {
					uiList [0].estimatedTotalCostForYear = 0;
					dialogUncheckedMI.Invoke (ecoBase, null);
				}
			}
		}
		
		public override Dictionary<string, string> SaveProgress ()
		{
			Dictionary<string, string> result = base.SaveProgress ();
			if (isSelected) {
				// we only bother to add isselected property when it is set to true
				if (result == null) {
					result = new Dictionary<string, string> ();
				}
				result.Add ("isselected", "true");
			}
			return result;
		}
		
		public override void LoadProgress (bool initScene, Dictionary <string, string> properties)
		{
			string val;
			if ((properties != null) && (properties.TryGetValue ("isselected", out val))) {
				isSelected = (val == "true");
				if (isSelected) {
					uiList [0].estimatedTotalCostForYear = uiList [0].cost;
				}
			}
			base.LoadProgress (initScene, properties);
		}
	
		public static DialogAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			DialogAction action = new DialogAction (scene, id);
			action.description = reader.GetAttribute ("description");
			action.dialogText = reader.GetAttribute ("dialogtext");
			action.shortDescText = reader.GetAttribute ("shorttext");

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
			writer.WriteAttributeString ("dialogtext", dialogText);
			writer.WriteAttributeString ("shorttext", shortDescText);
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}
	
	}
}