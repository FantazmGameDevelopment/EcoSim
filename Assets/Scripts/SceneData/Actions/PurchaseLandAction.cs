using System;
using System.Threading;
using System.Collections.Generic;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.VegetationRules;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class PurchaseLandAction : BasicAction
	{
		public const string XML_ELEMENT = "purchaseland";

		private EditData edit;
		private Data selectedArea;
		private GridTextureSettings areaGrid = null;
		private Texture2D areaTex = null;
		private Material areaMat = null;
		private Material selectedAreaMat = null;

		public PurchaseLandAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public PurchaseLandAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			UserInteraction ui = new UserInteraction (this);
			uiList.Add (ui);
		}
		
		public override string GetDescription ()
		{
			return "Handle Purchase Land";
		}

		public override int GetMinUICount ()
		{
			return 1;
		}
		
		public override int GetMaxUICount ()
		{
			return 1;
		}

		public override void DoSuccession ()
		{
			base.DoSuccession ();
		}

		public override void ActionSelected (UserInteraction ui)
		{
			base.ActionSelected (ui);
			new Ecosim.GameCtrl.GameButtons.PurchaseLandActionWindow (ui);
		}

		public void StartSelecting (UserInteraction ui)
		{
			// New selected area
			this.selectedArea = new SparseBitMap8 (this.scene);
			this.scene.progression.AddData ("purchaseland" + UnityEngine.Time.realtimeSinceStartup, this.selectedArea);

			// Get the selectable area
			Data selectableArea = new BitMap16 (this.scene);
			this.scene.progression.purchasableArea.CopyTo (selectableArea);

			// Remove all managed area from the purchaseable area
			foreach (ValueCoordinate vc in selectableArea.EnumerateNotZero ()) {
				if (this.scene.progression.managedArea.Get (vc) > 0) {
					selectableArea.Set (vc, 0);
				}
			}

			// Create edit data
			this.edit = EditData.CreateEditData ("action", this.selectedArea, selectedArea, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
				if (shift) return 0;
				return ui.index + 1;
			}, this.areaGrid);
			this.edit.SetFinalBrushFunction (delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
				if (shift) return 0;
				return 1; //CanSelectTile (x, y, ui) ? (ui.index + 1) : -1;
			});
			this.edit.SetModeAreaSelect ();

			// TODO
			/*edit.AddTileChangedHandler (delegate (int x, int y, int oldV, int newV) {
				if ((oldV > 0) && (oldV <= uiList.Count)) {
					uiList [oldV - 1].estimatedTotalCostForYear -= uiList [oldV - 1].cost;
				}
				if ((newV > 0) && (newV <= uiList.Count)) {
					uiList [newV - 1].estimatedTotalCostForYear += uiList [newV - 1].cost;
				}
			});
			 */ 

			// Setup unique area name
			/*invAreaName = "_" + this.areaName + "_" + (scene.progression.activeInventarisations.Count).ToString();
			selectedArea = new SparseBitMap8 (scene);
			scene.progression.AddData (invAreaName, selectedArea);
			
			// Get the "selectable area"
			Data selectableArea = scene.progression.managedArea;
			if (ecoBase != null) {
				MethodInfo getSelectableAreaMI = ecoBase.GetType ().GetMethod ("GetSelectableArea",
				                                                               BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof (UserInteraction) }, null);
				if (getSelectableAreaMI != null) {
					try {
						Data newSelectableArea = (Data)getSelectableAreaMI.Invoke (ecoBase, new object[] { ui });
						if (newSelectableArea != null) {
							selectableArea = newSelectableArea;
						}
					} catch (System.Exception ex) { }
				}
			}
			
			edit = EditData.CreateEditData ("action", selectedArea, selectableArea, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
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
			});*/
		}
		
		/**
		 * Called when player finished selecting tile
		 * method will create EditData instance
		 * ui is the user button pressed for doing this action
		 */
		public void FinishSelecting (UserInteraction ui, bool isCanceled)
		{
			/*if (!isCanceled) {
				// make selection permanent
				edit.CopyData (selectedArea);
				
				// Save the selected area in progression
				//scene.progression.AddData (invAreaName, selectedArea);
				
				// Remember the last taken researche values
				int selectedTilesCount = 0;
				foreach (ValueCoordinate vc in selectedArea.EnumerateNotZero()) {
					selectedTilesCount++;
				}
				
				// Save predefined variables
				scene.progression.variables [Progression.PredefinedVariables.lastResearch.ToString()] = this.description;
				scene.progression.variables [Progression.PredefinedVariables.lastResearchGroup.ToString()] = "Inventarisation";
				scene.progression.variables [Progression.PredefinedVariables.lastResearchCount.ToString()] = selectedTilesCount;
			}
			
			edit.Delete ();
			edit = null;
			//RecalculateEstimates (false);
			
			// Fire the research conducted methods
			if (!isCanceled) {
				scene.actions.ResearchConducted ();
			}*/
		}

		public override void UpdateReferences ()
		{
			// Area grid
			int gridSize = this.scene.progression.priceClasses.Count;
			this.areaTex = new Texture2D (gridSize * 32, gridSize * 32, TextureFormat.RGBA32, false, true);
			this.areaTex.filterMode = FilterMode.Point;
			this.areaTex.wrapMode = TextureWrapMode.Clamp;
			this.areaGrid = new GridTextureSettings (true, 0, gridSize, areaMat, true, selectedAreaMat);

			this.areaMat = new Material (EcoTerrainElements.GetMaterial ("MapGrid100"));
			this.selectedAreaMat = new Material (EcoTerrainElements.GetMaterial ("ActiveMapGrid100"));

			// Icons
			List<Texture2D> icons = new List<Texture2D> ();
			//icons.Add (scene.assets.GetIcon (gridIconId));
			foreach (Progression.PriceClass pc in this.scene.progression.priceClasses) {
				icons.Add (this.scene.assets.GetIcon (pc.iconId));
			}

			this.scene.assets.CopyTexures (icons.ToArray (), areaTex, null);
			icons.Clear ();

			// TODO this.scene.assets.CopyTexure (scene.assets.GetIcon (invalidTileIconId), areaTex, null, 32 * (gridSize - 1), 32 * (gridSize - 1));
			this.areaMat.mainTexture = this.areaTex;
			this.selectedAreaMat.mainTexture = this.areaTex;

			this.areaGrid = new GridTextureSettings (true, 0, gridSize, this.areaMat, true, this.selectedAreaMat);
		}

		public static PurchaseLandAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			PurchaseLandAction action = new PurchaseLandAction (scene, id);
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
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}		
	}
}
