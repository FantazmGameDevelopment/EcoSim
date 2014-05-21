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
	public class ActionObjectAction : BasicAction
	{
		// TODO: EcoBase Linkage

		public const string XML_ELEMENT = "object";

		public List<ActionObjectsGroup> objectsGroups = new List<ActionObjectsGroup>();
		public List<ActionObjectsGroup> enabledGroups = new List<ActionObjectsGroup>();

		private string description;
		private long backupEstimate;
		
		public ActionObjectAction (Scene scene, int id) : base (scene, id)
		{
		}
		
		public ActionObjectAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			description = "Object " + id;
			UserInteraction ui = new UserInteraction (this);
			uiList.Add (ui);
		}
		
		~ActionObjectAction ()
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
			//Dictionary <string, string> consts = new Dictionary<string, string> ();
			//consts.Add ("string AREA", "\"" + areaName + "\"");
			//return CompileScript (consts);
			return true;
		}

		public override void ActionSelected (UserInteraction ui)
		{
			backupEstimate = uiList[0].estimatedTotalCostForYear;

			base.ActionSelected (ui);
			new Ecosim.GameCtrl.GameButtons.ObjectActionWindow (ui);
		}

		public void ActionDeselected (UserInteraction ui, bool cancel)
		{
			uiList[0].estimatedTotalCostForYear = backupEstimate;
		}

		public void HandleActionGroup (ActionObjectsGroup group, bool currentState)
		{
			if (currentState) 
			{
				enabledGroups.Add (group);
				uiList[0].estimatedTotalCostForYear += uiList[0].cost;
			}
			else 
			{
				enabledGroups.Remove (group);
				uiList[0].estimatedTotalCostForYear -= uiList[0].cost;
			}
		}

		public override void DoSuccession ()
		{
			// Loop through all enabled groups
			foreach (ActionObjectsGroup group in enabledGroups)
			{
				if (group.enabled)
				{
					// Loop through all tiles who have the groups data on them
					foreach (ValueCoordinate vc in group.data.EnumerateNotZero())
					{
						// Handle the influence rules
						foreach (ActionObjectInfluenceRule ir in group.influenceRules)
						{
							// Check if rule applies
							bool ruleApplies = false;
							switch (ir.valueType)
							{
							// Check if the groups data is a certain value
							case ActionObjectInfluenceRule.ValueType.Value :

								int ruleVal = Mathf.RoundToInt ( ir.lowRange * (float)group.data.GetMax () );
								ruleApplies = (ruleVal == vc.v);
								break;

							case ActionObjectInfluenceRule.ValueType.Range :

								float max = (float)group.data.GetMax ();
								int minVal = (int)(ir.lowRange * max);
								int maxVal = (int)(ir.highRange * max);
								ruleApplies = ((vc.v >= minVal) && (vc.v <= maxVal));

								break;
							}

							// Apply influences
							if (ruleApplies)
							{
								// Check math type
								int currVal = ir.data.Get (vc);
								int newVal = 0;

								switch (ir.mathType)
								{

								case ActionObjectInfluenceRule.MathTypes.Equals :
									newVal = (int)ir.mathValue;
									break;

								case ActionObjectInfluenceRule.MathTypes.Plus :
									newVal = currVal + (int)ir.mathValue;
									break;

								case ActionObjectInfluenceRule.MathTypes.Minus :
									newVal = currVal - (int)ir.mathValue;
									break;

								case ActionObjectInfluenceRule.MathTypes.Multiply :
									newVal = Mathf.RoundToInt((float)currVal * ir.mathValue);
									break;
								
								}
								
								ir.data.Set (vc, Mathf.Clamp (newVal, 0, ir.data.GetMax()));
							}
						}
					}
				}
			}

			// Clear the groups
			enabledGroups.Clear ();

			// Deduct budget
			scene.progression.budget -= uiList[0].estimatedTotalCostForYear;
			uiList[0].estimatedTotalCostForYear = 0;

			base.DoSuccession ();
		}

		protected override void UnlinkEcoBase ()
		{
			base.UnlinkEcoBase ();
		}
		
		protected override void LinkEcoBase ()
		{
			base.LinkEcoBase ();
			if (ecoBase != null) 
			{
				/*actionDeselectedMI = ecoBase.GetType ().GetMethod ("ActionDeselected",
				                                                   BindingFlags.NonPublic | BindingFlags.Instance, null,
				                                                   new Type[] { typeof(UserInteraction), typeof(bool) }, null);

				handleActionObjectMI = ecoBase.GetType ().GetMethod ("HandleActionObject",
				                                                     BindingFlags.NonPublic | BindingFlags.Instance, null,
				                                                     new Type[] { typeof(ActionObject), typeof(bool) }, null);*/
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
			return 1;
		}				

		public void StartSelecting (UserInteraction ui)
		{
			EditActionObjects.instance.StartEditActionObjects (scene, objectsGroups.ToArray(), HandleActionGroup);
			//TerrainMgr.self.ForceRedraw ();
		}

		public void FinishSelecting (UserInteraction ui, bool isCanceled)
		{
			if (!isCanceled) {
				EditActionObjects.instance.ProcessSelectedObjects ();
			}
			EditActionObjects.instance.StopEditBuildings (scene);
			TerrainMgr.self.ForceRedraw ();
		}

		public override Dictionary<string, string> SaveProgress ()
		{
			return base.SaveProgress ();
		}
		
		public override void LoadProgress (bool initScene, Dictionary <string, string> properties)
		{
			base.LoadProgress (initScene, properties);
		}
		
		public static ActionObjectAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			ActionObjectAction action = new ActionObjectAction (scene, id);
			action.SetDescription(reader.GetAttribute ("description"));
			
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteraction.XML_ELEMENT)) {
						action.uiList.Add (UserInteraction.Load (action, reader));
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ActionObjectsGroup.XML_ELEMENT)) {
						string groupName = reader.GetAttribute ("name");
						foreach (ActionObjectsGroup group in scene.actionObjectGroups) {
							if (group.name == groupName) {
								action.objectsGroups.Add (group);
							}
						}
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
			foreach (ActionObjectsGroup group in objectsGroups) {
				writer.WriteStartElement (ActionObjectsGroup.XML_ELEMENT);
				writer.WriteAttributeString ("name", group.name);
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}	
	}		
}