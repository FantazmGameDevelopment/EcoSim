using System.Collections.Generic;
using System.IO;
using System.Xml;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneData
{
	public class ActionMgr
	{
		private readonly Scene scene;
		public string assemblyId;

		public static System.Type[] actionTypes = new System.Type[] {
			typeof(AreaAction),
			typeof(InventarisationAction),
			typeof(MarkerAction),
			typeof(DialogAction),
			typeof(ScriptAction),
			typeof(WaterAction),
			typeof(ConversionAction),
			typeof(SuccessionAction),
			typeof(PlantsAction)
		};
		
		public ActionMgr (Scene scene)
		{
			this.scene = scene;
			actionQueue = new List<BasicAction> ();
			actionsByID = new Dictionary<int, BasicAction> ();
			uiButtons = new List<UserInteraction> ();
			uiGroups = new Dictionary<string, UserInteractionGroup> ();

			SetupDefaultActions ();
		}
		
		List<BasicAction> actionQueue;
		private Dictionary<int, BasicAction> actionsByID;
		List<UserInteraction> uiButtons;
		public int lastId = 0;
		public Dictionary<string, UserInteractionGroup> uiGroups;
		
		/**
		 * Enumerates all the actions that are of type T or derived from T
		 * returns enumeratable
		 */
		public IEnumerable<T> EnumerateActions<T> () where T : BasicAction
		{
			foreach (BasicAction action in actionQueue) {
				if (action is T) {
					yield return (T) action;
				}
			}
		}

		/**
		 * Enumerates all the actions
		 * returns enumeratable
		 */
		public IEnumerable<BasicAction> EnumerateActions ()
		{
			foreach (BasicAction action in actionQueue) {
				yield return action;
			}
		}

		public void SetupDefaultActions ()
		{
			AddAction (new SuccessionAction (scene, 0));
			AddAction (new PlantsAction (scene, 1));
		}

		/**
		 * Adds action to action queue
		 */
		public void AddAction (BasicAction action)
		{
			actionQueue.Add (action);
			if (action.id >= lastId) {
				lastId = action.id + 1;
			}
			actionsByID.Add (action.id, action);
		}
		
		/**
		 * Creates a new action of type T (derived from BasicAction) and
		 * adds it to queue. The new action will have some defaults filled
		 * in and is returned.
		 */
		public T CreateAction<T> () where T : BasicAction
		{
			T action = (T)System.Activator.CreateInstance (typeof(T), scene);
			AddAction (action);
			return action;
		}
		
		/**
		 * Creates a new action of type actionType (must be derived from
		 * BasicAction). The new instance will be added to the queue and
		 * returned. The new action will have some default values set.
		 */
		public BasicAction CreateAction (System.Type actionType)
		{
			BasicAction action = System.Activator.CreateInstance (actionType, scene) as BasicAction;
			if (action == null) {
				throw new System.ArgumentException ("Can't create action of type '" + actionType.ToString () + "'");
			}
			AddAction (action);
			return action;
		}
		
		/**
		 * Moves action higher up the queue (to the front of the queue)
		 * If action was already at the front, nothing is changed.
		 */
		public void MoveActionUp (BasicAction action)
		{
			int index = actionQueue.IndexOf (action);
			if (index > 0) {
				actionQueue.RemoveAt (index);
				actionQueue.Insert (index - 1, action);
			}
		}
		
		/**
		 * Removes action from queue. This will also remove all
		 * UserInteractions.
		 */
		public void RemoveAction (BasicAction action)
		{
			actionQueue.Remove (action);
		}
		
		/**
		 * Retreive action using its id.
		 * returns the matching action
		 * or null if not found
		 * UpdateReferences must have been called if actions/userinteractions have changed
		 */
		public BasicAction GetAction (int id)
		{
			BasicAction result;
			if (actionsByID.TryGetValue (id, out result)) {
				return result;
			}
			return null;
		}
		
		/**
		 * Gets UserInteraction with given name, or null
		 * if not found. If multiple instances are defined
		 * with the same name one of them is returned, but
		 * care should be taken to not have duplicates.
		 * UpdateReferences must have been called if actions/userinteractions have changed
		 */
		public UserInteraction GetUIByName (string name)
		{
			name = name.ToLower ();
			foreach (UserInteraction ag in uiButtons) {
				if (ag.name.ToLower () == name)
					return ag;
			}
			return null;
		}
		
		/**
		 * Enumerates over all UserInteractions
		 * UpdateReferences must have been called if actions/userinteractions have changed
		 */
		public IEnumerable<UserInteraction> EnumerateUI ()
		{
			foreach (UserInteraction ui in uiButtons) {
				yield return ui;
			}
		}
		
		/**
		 * Calculates the estimated expenses for the year by going over all UserInterfaces
		 * asking the estimatedTotalCostsForYear of the UserInterface and summing this.
		 */
		public long GetYearExpenses () {
			long expenses = 0L;
			foreach (BasicAction action in actionQueue) {
				foreach (UserInteraction ui in action.uiList) {
					expenses += ui.estimatedTotalCostForYear;
				}
			}
			return expenses;
		}
		
		private void Load (XmlTextReader reader)
		{
			actionQueue = new List<BasicAction>();
			actionsByID = new Dictionary<int, BasicAction>();
			uiGroups = new Dictionary<string, UserInteractionGroup>();

			while (reader.Read()) {
				XmlNodeType nType = reader.NodeType;
				if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == DialogAction.XML_ELEMENT)) {
					BasicAction action = DialogAction.Load (scene, reader);
					actionQueue.Add (action);
					actionsByID.Add (action.id, action);
				} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ScriptAction.XML_ELEMENT)) {
					BasicAction action = ScriptAction.Load (scene, reader);
					actionQueue.Add (action);
					actionsByID.Add (action.id, action);
				} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == WaterAction.XML_ELEMENT)) {
					BasicAction action = WaterAction.Load (scene, reader);
					actionQueue.Add (action);
					actionsByID.Add (action.id, action);
				} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ConversionAction.XML_ELEMENT)) {
					BasicAction action = ConversionAction.Load (scene, reader);
					actionQueue.Add (action);
					actionsByID.Add (action.id, action);
				} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == AreaAction.XML_ELEMENT)) {
					BasicAction action = AreaAction.Load (scene, reader);
					actionQueue.Add (action);
					actionsByID.Add (action.id, action);
				} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == InventarisationAction.XML_ELEMENT)) {
					BasicAction action = InventarisationAction.Load (scene, reader);
					actionQueue.Add (action);
					actionsByID.Add (action.id, action);
				} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == MarkerAction.XML_ELEMENT)) {
					BasicAction action = MarkerAction.Load (scene, reader);
					actionQueue.Add (action);
					actionsByID.Add (action.id, action);
				} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == SuccessionAction.XML_ELEMENT)) {
					BasicAction action = SuccessionAction.Load (scene, reader);
					actionQueue.Add (action);
					actionsByID.Add (action.id, action);
				} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == PlantsAction.XML_ELEMENT)) {
					BasicAction action = PlantsAction.Load (scene, reader);
					actionQueue.Add (action);
					actionsByID.Add (action.id, action);
				} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteractionGroup.XML_ELEMENT)) {
					UserInteractionGroup grp = UserInteractionGroup.Load (scene, this, reader);
					uiGroups.Add (grp.category, grp);
				} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == "actions")) {
					break;
				}
			}
			
		}
		
		public static ActionMgr Load (string path, Scene scene)
		{
			ActionMgr mgr = new ActionMgr (scene);
			if (File.Exists (path + "actions.xml")) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path + "actions.xml"));
				try {
					while (reader.Read()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "actions")) {
							mgr.assemblyId = reader.GetAttribute ("assemblyid");
							mgr.Load (reader);
						}
					}
				} finally {
					reader.Close ();
				}
			}
			return mgr;
		}
		
		public void Save (string path)
		{
			System.CodeDom.Compiler.CompilerErrorCollection errors;
			bool success = EcoScript.Compiler.CompileScripts(scene, out errors, false);
			if (!success) {
				Log.LogError ("Failed to compile scripts");
			}
			string assetsDirPath = path + "Scripts";
			if (!Directory.Exists (assetsDirPath)) {
				Directory.CreateDirectory (assetsDirPath);
			}
			XmlTextWriter writer = new XmlTextWriter (path + "actions.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement ("actions");
			if (assemblyId != null) {
				writer.WriteAttributeString ("assemblyid", assemblyId);
			}
			
			foreach (BasicAction action in actionQueue) {
				action.Save (writer);
				action.LoadScript ();
				action.SaveScriptIfNeeded (path);
			}
			
			foreach (UserInteractionGroup grp in uiGroups.Values) {
				grp.Save (writer);
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();		
		}
		
		/**
		 * Updates indexes and tables. Must be called when Actions have been added/deleted/moved or
		 * when UserInteractions have been added/deleted or renamed.
		 */
		public void UpdateReferences ()
		{
			uiButtons.Clear ();
			foreach (BasicAction action in actionQueue) {
				if (action.id >= lastId) {
					lastId = action.id + 1;
				}
				int index = 0;
				// process all User Interactions (buttons) for the action
				foreach (UserInteraction ui in action.uiList) {
					ui.index = index++;
					uiButtons.Add (ui);
					ui.icon = scene.assets.GetIcon (ui.iconId);
					ui.activeIcon = scene.assets.GetHighlightedIcon (ui.iconId);
				}
				action.UpdateReferences ();
			}
			// make the two user interface groups (measures and research)
			if (!uiGroups.ContainsKey (UserInteractionGroup.CATEGORY_MEASURES)) {
				uiGroups.Add (UserInteractionGroup.CATEGORY_MEASURES,
					new UserInteractionGroup (scene, this, UserInteractionGroup.CATEGORY_MEASURES));
			}
			if (!uiGroups.ContainsKey (UserInteractionGroup.CATEGORY_RESEARCH)) {
				uiGroups.Add (UserInteractionGroup.CATEGORY_RESEARCH,
					new UserInteractionGroup (scene, this, UserInteractionGroup.CATEGORY_RESEARCH));
			}
			// update ui groups
			foreach (UserInteractionGroup grp in uiGroups.Values) {
				grp.UpdateReferences ();
			}
		}
		
		public void PrepareSuccession ()
		{
			foreach (BasicAction action in actionQueue) {
				if (action.isActive) {
					try {
						action.PrepareSuccession ();
					} catch (System.Exception e) {
						Log.LogException (e);
					}
				}
			}
		}

		public void DoSuccession ()
		{
			foreach (BasicAction action in actionQueue) {
				if (action.isActive) {
					try {
						action.DoSuccession ();
					} catch (System.Exception e) {
						Log.LogException (e);
					}
				}
			}
		}

		public void FinalizeSuccession ()
		{
			foreach (BasicAction action in actionQueue) {
				if (action.isActive) {
					try {
						action.FinalizeSuccession ();
					} catch (System.Exception e) {
						Log.LogException (e);
					}
				}
			}
		}
	}
}
