using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using System;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.Render;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class MarkerAction : BasicAction
	{
		public const string XML_ELEMENT = "marker";
		public string areaName;
		private RenderGameMarkers editMarkers;
		private MethodInfo actionDeselectedMI;
		private string description;
				
		public MarkerAction (Scene scene, int id) : base (scene, id)
		{
		}

		public MarkerAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			description = "Marker " + id;
			areaName = "marker" + id.ToString ();
			UserInteraction ui = new UserInteraction (this);
			uiList.Add (ui);
		}
		
		~MarkerAction ()
		{
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
		
		
		/**
		 * Overriden CompileScript to add constants
		 */
		public override bool CompileScript ()
		{
			Dictionary <string, string> consts = new Dictionary<string, string> ();
			consts.Add ("string AREA", "\"" + areaName + "\"");
			return CompileScript (consts);
		}
		
		public override void ActionSelected (UserInteraction ui)
		{
			base.ActionSelected (ui);
			new Ecosim.GameCtrl.GameButtons.MarkerActionWindow (ui);
		}
		
		public void ActionDeselected (UserInteraction ui, bool cancel)
		{
			if (!cancel) {
				// Count the total new markers
				int newMarkersCount = 0;
				SparseBitMap8 markers = scene.progression.GetData<SparseBitMap8>(areaName);
				foreach (ValueCoordinate vc in markers.EnumerateNotZero()) {
					newMarkersCount++;
				}

				// Remember the last taken researche values
				scene.progression.variables [Progression.PredefinedVariables.lastMeasure.ToString()] = this.description;
				scene.progression.variables [Progression.PredefinedVariables.lastMeasureGroup.ToString()] = "Marker";
				scene.progression.variables [Progression.PredefinedVariables.lastMeasureCount.ToString()] = newMarkersCount;

				// Save and update affected area
				scene.progression.AddActionTaken (this.id);
				Data area = AffectedArea;
				if (area != null) {
					foreach (ValueCoordinate vc in markers.EnumerateNotZero()) {
						area.Set (vc, 1);
					}
				}
			}

			if (actionDeselectedMI != null) {
				actionDeselectedMI.Invoke (ecoBase, new object[] { ui, cancel });
			}

			if (!cancel) {
				scene.actions.MeasureTaken ();
			}
		}

		protected override void UnlinkEcoBase ()
		{
			base.UnlinkEcoBase ();
			actionDeselectedMI = null;
		}
		
		protected override void LinkEcoBase ()
		{
			base.LinkEcoBase ();
			if (ecoBase != null) {
				actionDeselectedMI = ecoBase.GetType ().GetMethod ("ActionDeselected",
						BindingFlags.NonPublic | BindingFlags.Instance, null,
						new Type[] { typeof(UserInteraction), typeof(bool) }, null);
			}
		}
		
		public override void UpdateReferences ()
		{
		}
		
		public override int GetMinUICount ()
		{
			return 1;
		}
		
		public override int GetMaxUICount ()
		{
			return 10;
		}				
		
		public override Dictionary<string, string> SaveProgress ()
		{
			return base.SaveProgress ();
		}
		
		public override void LoadProgress (bool initScene, Dictionary <string, string> properties)
		{
			base.LoadProgress (initScene, properties);
		}
	
		public static MarkerAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			MarkerAction action = new MarkerAction (scene, id);
			action.description = reader.GetAttribute ("description");
			action.areaName = reader.GetAttribute ("areaname");

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
			writer.WriteAttributeString ("areaname", areaName);
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}	
	}		
}