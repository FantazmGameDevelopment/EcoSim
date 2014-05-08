using System.Collections.Generic;
using System;
using System.Xml;
using System.Reflection;
using Ecosim;
using Ecosim.SceneData;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class WaterAction : BasicAction
	{
		public const string XML_ELEMENT = "water";
		private string description = "Calculate water heights";
		private MethodInfo calculateWaterMI;
		
		public WaterAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public WaterAction (Scene scene) : base(scene, scene.actions.lastId)
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

		protected override void LinkEcoBase ()
		{
			base.LinkEcoBase ();
			if (ecoBase != null) {
				calculateWaterMI = ecoBase.GetType ().GetMethod ("CalculateWater",
				BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] {}, null);
				
				scene.progression.waterHandler = this;
			}
		}
		
		public void CalculateWater () {
			if (calculateWaterMI != null) {
				try {
					calculateWaterMI.Invoke (ecoBase, null);
					return;
				}
				catch (System.Exception e) {
					Log.LogException (e);
				}
			}
			scene.progression.calculatedWaterHeightMap.CopyFrom (scene.progression.waterHeightMap);
		}
		
		public static WaterAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			WaterAction action = new WaterAction (scene, id);
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