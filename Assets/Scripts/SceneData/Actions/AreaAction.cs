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
	public class AreaAction : BasicAction
	{
		public const string XML_ELEMENT = "area";
		public string areaName;
		public int gridIconId;
		public int invalidTileIconId;
		private Data selectedArea;
		private EditData edit;
		private GridTextureSettings areaGrid = null;
		private Texture2D areaTex = null;
		private Material areaMat = null;
		private Material selectedAreaMat = null;
		private MethodInfo canSelectTileMI = null;
		private int invalidAreaIndex;
		private string description;
		
		public AreaAction (Scene scene, int id) : base (scene, id)
		{
		}

		public AreaAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			description = "Inventarisation " + id;
			areaName = "area" + id.ToString ();
			UserInteraction ui = new UserInteraction (this);
			uiList.Add (ui);
		}
		
		~AreaAction ()
		{
			if (areaMat != null) {
				UnityEngine.Object.Destroy (areaMat);
			}
			if (selectedAreaMat != null) {
				UnityEngine.Object.Destroy (selectedAreaMat);
			}
			if (areaTex != null) {
				UnityEngine.Object.Destroy (areaTex);
			}			
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
			new Ecosim.GameCtrl.GameButtons.AreaActionWindow (ui);
		}
		
		protected override void UnlinkEcoBase ()
		{
			base.UnlinkEcoBase ();
			canSelectTileMI = null;
		}
		
		protected override void LinkEcoBase ()
		{
			base.LinkEcoBase ();
			if (ecoBase != null) {
				canSelectTileMI = ecoBase.GetType ().GetMethod ("CanSelectTile",
				BindingFlags.NonPublic | BindingFlags.Instance, null,
				new Type[] { typeof(int), typeof(int), typeof(UserInteraction) }, null);
			}
		}
		
		public override void UpdateReferences ()
		{
			if (areaMat != null) {
				UnityEngine.Object.DestroyImmediate (areaMat);
			}
			if (selectedAreaMat != null) {
				UnityEngine.Object.DestroyImmediate (selectedAreaMat);
			}
			if (areaTex != null) {
				UnityEngine.Object.DestroyImmediate (areaTex);
			}
			areaMat = new Material (EcoTerrainElements.GetMaterial ("MapGrid100"));
			selectedAreaMat = new Material (EcoTerrainElements.GetMaterial ("ActiveMapGrid100"));
			
			int gridSize = (uiList.Count > 1) ? 4 : 2;
			areaTex = new Texture2D (gridSize * 32, gridSize * 32, TextureFormat.RGBA32, false, true);
			areaTex.filterMode = FilterMode.Point;
			areaTex.wrapMode = TextureWrapMode.Clamp;
			List<Texture2D> icons = new List<Texture2D> ();
			icons.Add (scene.assets.GetIcon (gridIconId));
//			icons.Add (scene.assets.GetIcon (invalidTileIconId));
			foreach (UserInteraction ui in uiList) {
				icons.Add (scene.assets.GetIcon (ui.iconId));
			}
			scene.assets.CopyTexures (icons.ToArray (), areaTex, null);
			scene.assets.CopyTexure (scene.assets.GetIcon (invalidTileIconId), areaTex, null, 32 * (gridSize - 1), 32 * (gridSize - 1));
			areaMat.mainTexture = areaTex;
			selectedAreaMat.mainTexture = areaTex;
			
			areaGrid = new GridTextureSettings (true, 0, gridSize, areaMat, true, selectedAreaMat);
			invalidAreaIndex = gridSize * gridSize - 1;
		}
		
		public override int GetMinUICount ()
		{
			return 1;
		}
		
		public override int GetMaxUICount ()
		{
			return 10;
		}
		
		public bool CanSelectTile (int x, int y, UserInteraction ui)
		{
			if (canSelectTileMI != null) {
				return (bool)canSelectTileMI.Invoke (ecoBase, new object[] { x, y, ui });
			}
			return true;
		}
				
		/**
		 * Called when player starts selecting tile
		 * method will create EditData instance
		 * ui is the user button pressed for doing this action
		 */
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
		
		/**
		 * Called when player finished selecting tile
		 * method will create EditData instance
		 * ui is the user button pressed for doing this action
		 */
		public void FinishSelecting (UserInteraction ui, bool isCanceled)
		{
			if (!isCanceled) {
				// make selection permanent
				edit.CopyData (selectedArea);

				// Remember the last taken measure values
				int selectedTilesCount = 0;
				foreach (ValueCoordinate vc in selectedArea.EnumerateNotZero()) {
					selectedTilesCount++;
				}

				scene.progression.variables [Progression.PredefinedVariables.lastMeasure.ToString()] = this.description;
				scene.progression.variables [Progression.PredefinedVariables.lastMeasureGroup.ToString()] = "Area";
				scene.progression.variables [Progression.PredefinedVariables.lastMeasureCount.ToString()] = selectedTilesCount;

				// Save and update the affected area
				edit.CopyData (AffectedArea);
				scene.progression.AddActionTaken (this.id);
			}

			edit.Delete ();
			edit = null;
			RecalculateEstimates (false);

			if (!isCanceled) {
				scene.actions.MeasureTaken ();
			}
		}
		
		public void RecalculateEstimates (bool checkTileIsValid)
		{
			foreach (UserInteraction ui in uiList) {
				ui.estimatedTotalCostForYear = 0L; // first reset...
			}
			if (scene.progression.HasData (areaName)) {
				selectedArea = scene.progression.GetData (areaName);
				if (checkTileIsValid) {
					selectedArea.ProcessNotZero (delegate(int x, int y, int val, object data) {
						if (!CanSelectTile (x, y, uiList [val - 1])) {
							selectedArea.Set (x, y, 0);
						} else {
							uiList [val - 1].estimatedTotalCostForYear += uiList [val - 1].cost;
						}
					}, null);
				} else {
					selectedArea.ProcessNotZero (delegate(int x, int y, int val, object data) {
						uiList [val - 1].estimatedTotalCostForYear += uiList [val - 1].cost;
					}, null);
				}
			} else {
				selectedArea = null;
			}
		}

		public override void FinalizeSuccession ()
		{
			base.FinalizeSuccession ();
			RecalculateEstimates (true);
		}
		
		public bool IsSelected (UserInteraction ui, int x, int y)
		{
			return (selectedArea != null) && (selectedArea.Get (x, y) == (ui.index + 1));
		}
		
		public override Dictionary<string, string> SaveProgress ()
		{
			return base.SaveProgress ();
		}
		
		public override void LoadProgress (bool initScene, Dictionary <string, string> properties)
		{
			base.LoadProgress (initScene, properties);
			if (scene.progression.HasData (areaName)) {
				selectedArea = scene.progression.GetData (areaName);
			}
			RecalculateEstimates (false);
		}
	
		public static AreaAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			AreaAction action = new AreaAction (scene, id);
			action.description = reader.GetAttribute ("description");
			action.areaName = reader.GetAttribute ("areaname");
			int.TryParse (reader.GetAttribute ("gridicon"), out action.gridIconId);
			int.TryParse (reader.GetAttribute ("invalidicon"), out action.invalidTileIconId);

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
			writer.WriteAttributeString ("gridicon", gridIconId.ToString ());
			writer.WriteAttributeString ("invalidicon", invalidTileIconId.ToString ());
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}	
	}		
}