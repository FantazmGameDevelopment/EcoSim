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

		public ActionObjectsGroup objectsGroup;

		private MethodInfo actionDeselectedMI;
		private MethodInfo handleActionObjectMI; // TODO: handleActionObjectMI
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
			if (actionDeselectedMI != null) 
			{
				actionDeselectedMI.Invoke (ecoBase, new object[] { ui, cancel });
			} 
			else 
			{
				uiList[0].estimatedTotalCostForYear = backupEstimate;
			}
		}

		public void HandleActionGroup (ActionObjectsGroup group, bool state)
		{
			if (handleActionObjectMI != null) 
			{
				//return (bool)handleActionObjectMI.Invoke (ecoBase, new object[] { x, y, ui });
			} 
			else 
			{
				if (state) uiList[0].estimatedTotalCostForYear += uiList[0].cost;
				else uiList[0].estimatedTotalCostForYear -= uiList[0].cost;
			}
		}

		public override void DoSuccession ()
		{
			// TODO: Handle all the influences of the action groups

			// Deduct budget
			scene.progression.budget -= uiList[0].estimatedTotalCostForYear;
			uiList[0].estimatedTotalCostForYear = 0;

			base.DoSuccession ();
		}

		protected override void UnlinkEcoBase ()
		{
			base.UnlinkEcoBase ();

			actionDeselectedMI = null;
			handleActionObjectMI = null;
		}
		
		protected override void LinkEcoBase ()
		{
			base.LinkEcoBase ();
			if (ecoBase != null) 
			{
				actionDeselectedMI = ecoBase.GetType ().GetMethod ("ActionDeselected",
				                                                   BindingFlags.NonPublic | BindingFlags.Instance, null,
				                                                   new Type[] { typeof(UserInteraction), typeof(bool) }, null);

				/*handleActionObjectMI = ecoBase.GetType ().GetMethod ("HandleActionObject",
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
			return 10;
		}				

		public void StartSelecting (UserInteraction ui)
		{
			EditActionObjects.instance.StartEditActionObjects (scene, new string[]{}, HandleActionGroup);
			//TerrainMgr.self.ForceRedraw ();
		}

		/*/**
		 * Called when player starts selecting tile
		 * method will create EditData instance
		 * ui is the user button pressed for doing this action
		 *
		public void StartSelecting (UserInteraction ui)
		{
			if (selectedArea == null) {
				if (uiList.Count > 1) {
					selectedArea = new SparseBitMap8 (scene);
				} else {
					selectedArea = new SparseBitMap1 (scene);
				}
				scene.progression.AddData (areaName, selectedArea);
			}
			edit = EditData.CreateEditData ("action", selectedArea, scene.progression.managedArea, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
				if (shift)
					return 0;
				return CanSelectTile (x, y, ui) ? (ui.index + 1) : invalidAreaIndex;
			}, areaGrid);
			edit.SetFinalBrushFunction (delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
				if (shift)
					return 0;
				return CanSelectTile (x, y, ui) ? (ui.index + 1) : -1;
			});
			edit.SetModeAreaSelect ();
			edit.AddTileChangedHandler (delegate (int x, int y, int oldV, int newV) {
				if ((oldV > 0) && (oldV <= uiList.Count)) {
					uiList [oldV - 1].estimatedTotalCostForYear -= uiList [oldV - 1].cost;
				}
				if ((newV > 0) && (newV <= uiList.Count)) {
					uiList [newV - 1].estimatedTotalCostForYear += uiList [newV - 1].cost;
				}
			});
		}
		 */ 

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
			//writer.WriteAttributeString ("areaname", areaName);
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}	
	}		
}