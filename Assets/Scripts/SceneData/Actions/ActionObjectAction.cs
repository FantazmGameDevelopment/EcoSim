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
		public const string XML_ELEMENT = "object";

		public List<ActionObjectsGroup> actionObjectGroups = new List<ActionObjectsGroup>();
		public bool processInfluenceRules = true;

		private string description;
		private long backupEstimate;

		private List<ActionObject> selectedObjects = new List<ActionObject>();

		private MethodInfo processInfluencesMI;
		
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

		public void HandleActionObjectClicked (ActionObjectsGroup group, ActionObject actionObject, bool objectState)
		{	
			// Remember the 'selected' objects
			if (objectState) {
				selectedObjects.Add (actionObject);
			} else {
				selectedObjects.Remove (actionObject);
			}

			// Add or deduct the costs by the object state
			uiList[0].estimatedTotalCostForYear += (objectState) ? uiList[0].cost : -uiList[0].cost;
		}

		public override void DoSuccession ()
		{
			// Loop through all associated object groups and check whether they have changed
			foreach (ActionObjectsGroup ag in actionObjectGroups)
			{
				switch (ag.groupType)
				{
				// Check whether there's an action object of this group changed
				case ActionObjectsGroup.GroupType.Combined :
				{
					bool processGroup = false;
					foreach (ActionObject ao in ag.actionObjects) 
					{
						if (selectedObjects.Contains (ao))
						{
							processGroup = true;
							selectedObjects.Remove (ao);
							if (selectedObjects.Count == 0) break;
						}
					}

					// Process the combined group
					if (processGroup) {
						ProcessCombinedGroup (ag);
					}
				}
					break;

				// Check all objects separate and 
				case ActionObjectsGroup.GroupType.Collection :
				{
					List<ActionObject> objsToProcess = new List<ActionObject>();
					foreach (ActionObject ao in ag.actionObjects) 
					{
						if (selectedObjects.Contains (ao))
						{
							objsToProcess.Add (ao);
							selectedObjects.Remove (ao);
							if (selectedObjects.Count == 0) break;
						}
					}

					// Process the collection group
					if (objsToProcess.Count > 0) {
						ProcessCollectionGroup (ag, objsToProcess);
					}
				}
					break;
				}

				if (selectedObjects.Count == 0) break;
			}

			selectedObjects.Clear ();

			// Deduct budget
			scene.progression.budget -= uiList[0].estimatedTotalCostForYear;
			uiList[0].estimatedTotalCostForYear = 0;

			base.DoSuccession ();
		}

		private void ProcessCombinedGroup (ActionObjectsGroup group)
		{
			if (group.enabled) 
			{
				// Loop through all tiles who have the groups data on them
				foreach (ValueCoordinate vc in group.combinedData.EnumerateNotZero())
				{
					// Handle the influence rules
					foreach (ActionObjectInfluenceRule ir in group.influenceRules)
					{
						if (processInfluencesMI != null) {
							try {
								processInfluencesMI.Invoke (ecoBase, new object[] { vc, group.name });
							} catch (Exception e) {
								Log.LogException (e);
							}
						}

						if (processInfluenceRules) {
							ProcessInfluenceRules (ir, vc, group.combinedData);
						}
					}
				}
			}
		}

		private void ProcessCollectionGroup (ActionObjectsGroup group, List<ActionObject> objectsToProcess)
		{
			// Process collection group
			foreach (ActionObject obj in objectsToProcess)
			{
				if (obj.enabled)
				{
					// Get the data from the object and apply all the rules on the coordinate
					Data objData = scene.progression.GetData (group.dataName.ToLower() + "_obj" + obj.index);
					foreach (ValueCoordinate vc in objData.EnumerateNotZero())
					{
						foreach (ActionObjectInfluenceRule ir in group.influenceRules)
						{
							if (processInfluencesMI != null) {
								try {
									processInfluencesMI.Invoke (ecoBase, new object[] { vc, group.name });
								} catch (Exception e) {
									Log.LogException (e);
								}
							}

							if (processInfluenceRules) {
								ProcessInfluenceRules (ir, vc, objData);
							}
						}
					}
				}
			}
		}

		private void ProcessInfluenceRules (ActionObjectInfluenceRule ir, ValueCoordinate vc, Data data)
		{
			// Check if rule applies
			bool ruleApplies = false;
			switch (ir.valueType)
			{
				// Check if the groups data is a certain value
			case ActionObjectInfluenceRule.ValueType.Value :
				
				int ruleVal = Mathf.RoundToInt ( ir.lowRange * (float)data.GetMax () );
				ruleApplies = (ruleVal == vc.v);
				break;
				
			case ActionObjectInfluenceRule.ValueType.Range :
				
				float max = (float)data.GetMax ();
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

		protected override void UnlinkEcoBase ()
		{
			base.UnlinkEcoBase ();
		}
		
		protected override void LinkEcoBase ()
		{
			base.LinkEcoBase ();
			if (ecoBase != null) 
			{
				processInfluencesMI = ecoBase.GetType ().GetMethod ("ProcessInfluences",
				                                                   BindingFlags.NonPublic | BindingFlags.Instance, null,
				                                                    new Type[] { typeof(ValueCoordinate), typeof(string) }, null);

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
			EditActionObjects.instance.StartEditActionObjects (scene, actionObjectGroups.ToArray(), HandleActionObjectClicked);
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
			action.processInfluenceRules = (reader.GetAttribute("doinflrules") == "true") ? true : false;
			
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteraction.XML_ELEMENT)) {
						action.uiList.Add (UserInteraction.Load (action, reader));
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ActionObjectsGroup.XML_ELEMENT)) {
						string groupName = reader.GetAttribute ("name");
						foreach (ActionObjectsGroup group in scene.actionObjectGroups) {
							if (group.name == groupName) {
								action.actionObjectGroups.Add (group);
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
			writer.WriteAttributeString ("doinflrules", processInfluenceRules.ToString().ToLower());
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			foreach (ActionObjectsGroup group in actionObjectGroups) {
				writer.WriteStartElement (ActionObjectsGroup.XML_ELEMENT);
				writer.WriteAttributeString ("name", group.name);
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}	
	}		
}