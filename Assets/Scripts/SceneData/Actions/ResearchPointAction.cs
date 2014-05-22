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
	public class ResearchPointAction : BasicAction
	{
		/*public class ParameterString
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

			public void Save (XmlTextWriter writer)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("param", paramName);
				writer.WriteAttributeString ("start", start);
				writer.WriteAttributeString ("end", end);
				writer.WriteEndElement ();
			}
		}*/

		// Port notes: EcoSim 1.0 view JDisplayTag, JResearchPointsMgr and JResearchPoint

		public const string XML_ELEMENT = "researchpoint";
		private string description = "Research point";
		//public ParameterString[] parameters;

		private Dictionary<ResearchPoint.Measurement, Coordinate> newMeasurements;

		// Edit
		private EditData edit = null;
		private GridTextureSettings areaGrid = null;
		private Material areaMat = null;

		// EcoBase linkage
		private MethodInfo actionDeselectedMI;
		private MethodInfo createResearchPointStringMI;

		public ResearchPointAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public ResearchPointAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			UserInteraction ui = new UserInteraction (this);
			uiList.Add (ui);

			CreateDefaultScript ();
		}

		~ResearchPointAction ()
		{
			if (areaMat != null) {
				UnityEngine.Object.Destroy (areaMat);
			}
		}

		public override void UpdateReferences ()
		{
			if (areaMat != null) {
				UnityEngine.Object.DestroyImmediate (areaMat);
			}

			// Just for showing where we can place the points
			areaMat = new Material (EcoTerrainElements.GetMaterial ("MapResearchPointsGrid"));
			areaGrid = new GridTextureSettings (true, 0, 2, areaMat, true, areaMat);
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
			//consts.Add ("string AREA", "\"" + areaName + "\"");
			return CompileScript (consts);
		}
		
		public override void ActionSelected (UserInteraction ui)
		{
			base.ActionSelected (ui);
			new Ecosim.GameCtrl.GameButtons.ResearchPointActionWindow (ui);
		}
		
		public void ActionDeselected (UserInteraction ui, bool cancel)
		{
			if (edit != null) {
				edit.Delete ();
				edit = null;
			}

			if (newMeasurements == null)
				newMeasurements = new Dictionary<ResearchPoint.Measurement, Coordinate>();

			foreach (ResearchPoint rp in scene.progression.researchPoints) 
			{
				foreach (ResearchPoint.Measurement m in rp.measurements)
				{
					// Mark all temporary measurement as false and set their message to null
					if (m.isTemporary)
					{
						m.isTemporary = false;
						newMeasurements.Add (m, new Coordinate (rp.x, rp.y));
					}
				}

				RenderResearchPointsMgr.GetMarkerOf (rp).SetVisuals (false);
			}

			if (actionDeselectedMI != null) {
				actionDeselectedMI.Invoke (ecoBase, new object[] { ui, cancel });
			}
		}

		public override void DoSuccession ()
		{
			foreach (KeyValuePair<ResearchPoint.Measurement, Coordinate> pair in newMeasurements)
			{
				if (createResearchPointStringMI != null) {
					object msg = createResearchPointStringMI.Invoke (ecoBase, new object[] { pair.Value });
					pair.Key.message = msg.ToString();
				} else {
					pair.Key.message = "WIP";
				}
			}
			newMeasurements.Clear ();
		}

		public override void FinalizeSuccession() 
		{
			foreach (UserInteraction ui in uiList) 
			{
				// Substract the cost for this item for this year...
				scene.progression.budget -= ui.estimatedTotalCostForYear;
				// set cost to 0 (don't do it for ongoing measures)
				ui.estimatedTotalCostForYear = 0;
			}
		}

		public void StartSelecting (UserInteraction ui)
		{
			edit = EditData.CreateEditData ("rps", null, scene.progression.managedArea, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) 
			{
				return CanSelectTile (x, y, ui) ? 1 : -1;
			}, areaGrid);

			edit.SetFinalBrushFunction (delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) 
			{
				HandleClick (x, y);
				return 0; // We show the new points via the Markers and not the grid
			});

			edit.SetModeBrush (0);
		}

		public bool CanSelectTile (int x, int y, UserInteraction ui)
		{
			return true;
		}

		private void HandleClick (int x, int y)
		{
			ResearchPoint rp = RenderResearchPointsMgr.GetResearchPointAt (x, y);
			ResearchPoint.Measurement m = rp.GetLastMeasurement ();
			ResearchPointMarker marker = RenderResearchPointsMgr.GetMarkerOf (rp);

			// Create a new measurement if there's none or if the last measurement is not temporary
			if (m == null || !m.isTemporary) 
			{
				m = rp.AddNewMeasurement (scene, description);
				m.isTemporary = true;
				marker.SetVisuals (true);

				// Update total costs
				uiList [0].estimatedTotalCostForYear += uiList [0].cost;
			} 
			// We undo the temporary measurent
			else if (m.isTemporary) 
			{
				// Update total costs
				uiList [0].estimatedTotalCostForYear -= uiList [0].cost;

				rp.DeleteMeasurement (m);

				// If the point has no measurements left, we should destroy it,
				// else we should undo it's 'new' status
				if (!rp.HasMeasurements ()) {
					RenderResearchPointsMgr.DeleteResearchPoint (rp);
				} else {
					marker.SetVisuals (false);
				}
			}
		}

		protected override void UnlinkEcoBase ()
		{
			base.UnlinkEcoBase ();
			actionDeselectedMI = null;
			createResearchPointStringMI = null;
		}
		
		protected override void LinkEcoBase ()
		{
			base.LinkEcoBase ();
			if (ecoBase != null) {
				actionDeselectedMI = ecoBase.GetType ().GetMethod ("ActionDeselected",
				                                                   	BindingFlags.NonPublic | BindingFlags.Instance, null,
				                                                   	new Type[] { typeof(UserInteraction), typeof(bool) }, null);

				createResearchPointStringMI = ecoBase.GetType ().GetMethod ("CreateResearchPointString",
				                                                   	BindingFlags.NonPublic | BindingFlags.Instance, null,
	                                                            	new Type[] { typeof(Coordinate) }, null);        
			}
		}

		public override int GetMinUICount ()
		{
			return 1;
		}
		
		public override int GetMaxUICount ()
		{
			return 1;
		}

		public static ResearchPointAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			ResearchPointAction action = new ResearchPointAction (scene, id);

			//List<ParameterString> paramList = new List<ParameterString>();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteraction.XML_ELEMENT)) {
						action.uiList.Add (UserInteraction.Load (action, reader));
						/*} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ParameterString.XML_ELEMENT)) {
						paramList.Add (ParameterString.Load (scene, reader));*/
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			//action.parameters = paramList.ToArray ();
			return action;
		}
		
		public override void Save (XmlTextWriter writer)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("id", id.ToString ());
			/*foreach (ParameterString p in parameters) {
				p.Save (writer);
			}*/
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}
	}
}