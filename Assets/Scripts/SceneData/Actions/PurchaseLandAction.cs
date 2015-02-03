using System;
using System.Reflection;
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

		public EditData.NotifyTileChanged OnTileChanged;

		public string areaName;
		public int invalidTileIconId;
		public int undoTileIconId;

		private EditData edit;
		private Data selectedArea;
		private GridTextureSettings areaGrid = null;
		private Texture2D areaTex = null;
		private Material areaMat = null;
		private Material selectedAreaMat = null;

		private MethodInfo canSelectTileMI = null;

		public PurchaseLandAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public PurchaseLandAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			UserInteraction ui = new UserInteraction (this);
			this.areaName = "area" + this.id.ToString ();
			this.uiList.Add (ui);
		}

		~PurchaseLandAction ()
		{
			if (this.areaMat != null) {
				UnityEngine.Object.Destroy (this.areaMat);
			}
			if (this.selectedAreaMat != null) {
				UnityEngine.Object.Destroy (this.selectedAreaMat);
			}
			if (this.areaTex != null) {
				UnityEngine.Object.Destroy (this.areaTex);
			}			
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

		public bool CanSelectTile (int x, int y, UserInteraction ui)
		{
			if (canSelectTileMI != null) {
				return (bool)canSelectTileMI.Invoke (ecoBase, new object[] { x, y, ui });
			}
			return true;
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
			if (this.selectedArea == null && this.scene.progression.HasData (this.areaName))
				this.selectedArea = this.scene.progression.GetData (this.areaName);
			else if (this.selectedArea == null) {
				this.selectedArea = new SparseBitMap8 (this.scene);
				this.scene.progression.AddData (this.areaName, this.selectedArea);
			}

			// Get the selectable area
			Data selectableArea = new BitMap16 (this.scene);
			this.scene.progression.purchasableArea.CopyTo (selectableArea);

			// Remove all managed area from the purchaseable area
			foreach (ValueCoordinate vc in selectableArea.EnumerateNotZero ()) {
				if (this.scene.progression.managedArea.Get (vc) > 0) {
					selectableArea.Set (vc, 0);
					continue;
				}
				// Update selectable area
				if (this.selectedArea.Get (vc) == 0)
					this.selectedArea.Set (vc, vc.v);
			}

			// Get price classes count for choosing correct icons
			int priceClasses = this.scene.progression.priceClasses.Count;

			// Create edit data
			this.edit = EditData.CreateEditData ("action", this.selectedArea, selectableArea, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) 
			{
				// Show undo icon
				if (shift) return 0;
				// Show selected icon
				return this.CanSelectTile (x,y,ui) ? selectableArea.Get (x,y) + priceClasses : this.invalidTileIconId;
			}, 
			this.areaGrid);

			// Final brush
			this.edit.SetFinalBrushFunction (delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) 
			{
				// Show default icon
				if (shift) return selectableArea.Get (x,y);
				// Show selected icon
				return this.CanSelectTile (x,y,ui) ? selectableArea.Get (x,y) + priceClasses : -1;
			});

			// Set area mode selection
			this.edit.SetModeAreaSelect ();

			// Set tile changed handler
			this.edit.AddTileChangedHandler (delegate(int x, int y, int oldV, int newV) 
			{
				// Update
				if (this.OnTileChanged != null) {
					this.OnTileChanged (x,y,oldV,newV);
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
			// Check if not cancelled
			if (!isCanceled) {

				// make selection permanent
				this.edit.CopyData (this.selectedArea);
				
				// Remember the last taken measure values
				// Convert selected area to managed area
				int selectedTilesCount = 0;
				foreach (ValueCoordinate vc in this.selectedArea.EnumerateNotZero()) {
					selectedTilesCount++;
				}

				// Save predefined variables
				scene.progression.variables [Progression.PredefinedVariables.lastMeasure.ToString()] = this.GetDescription ();
				scene.progression.variables [Progression.PredefinedVariables.lastMeasureGroup.ToString()] = "Area";
				scene.progression.variables [Progression.PredefinedVariables.lastMeasureCount.ToString()] = selectedTilesCount;
			}

			// Delete
			this.edit.Delete ();
			this.edit = null;

			// Fire the measure taken methods
			if (!isCanceled) {
				scene.actions.MeasureTaken ();
			}
		}

		public override void UpdateReferences ()
		{
			// Destroy references first
			if (this.areaMat != null) {
				UnityEngine.Object.DestroyImmediate (this.areaMat);
			}
			if (this.selectedAreaMat != null) {
				UnityEngine.Object.DestroyImmediate (this.selectedAreaMat);
			}
			if (this.areaTex != null) {
				UnityEngine.Object.DestroyImmediate (this.areaTex);
			}

			// Area grid
			int gridSize = this.scene.progression.priceClasses.Count + 2;
			this.areaTex = new Texture2D (gridSize * 32, gridSize * 32, TextureFormat.RGBA32, false, true);
			this.areaTex.filterMode = FilterMode.Point;
			this.areaTex.wrapMode = TextureWrapMode.Clamp;
			this.areaGrid = new GridTextureSettings (true, 0, gridSize, areaMat, true, selectedAreaMat);

			this.areaMat = new Material (EcoTerrainElements.GetMaterial ("MapGrid100"));
			this.selectedAreaMat = new Material (EcoTerrainElements.GetMaterial ("ActiveMapGrid100"));

			// Icons
			List<Texture2D> icons = new List<Texture2D> ();
			icons.Add (this.scene.assets.GetIcon (this.undoTileIconId));
			foreach (Progression.PriceClass pc in this.scene.progression.priceClasses) {
				icons.Add (this.scene.assets.GetIcon (pc.normalIconId));
			}
			foreach (Progression.PriceClass pc in this.scene.progression.priceClasses) {
				icons.Add (this.scene.assets.GetIcon (pc.selectedIconId));
			}
			icons.Add (this.scene.assets.GetIcon (this.invalidTileIconId));

			this.scene.assets.CopyTexures (icons.ToArray (), areaTex, null);
			icons.Clear ();

			this.areaMat.mainTexture = this.areaTex;
			this.selectedAreaMat.mainTexture = this.areaTex;

			this.areaGrid = new GridTextureSettings (true, 0, gridSize, this.areaMat, true, this.selectedAreaMat);
		}

		public static PurchaseLandAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			PurchaseLandAction action = new PurchaseLandAction (scene, id);
			action.areaName = reader.GetAttribute ("areaname");
			if (string.IsNullOrEmpty (action.areaName))
				action.areaName = "action" + id;
			action.undoTileIconId = (!string.IsNullOrEmpty (reader.GetAttribute ("undotileid"))) ?
				int.Parse (reader.GetAttribute ("undotileid")) : 0;
			action.invalidTileIconId = (!string.IsNullOrEmpty (reader.GetAttribute ("invalidtileid"))) ?
				int.Parse (reader.GetAttribute ("invalidtileid")) : 0;
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
			writer.WriteAttributeString ("id", this.id.ToString ());
			writer.WriteAttributeString ("areaname", this.areaName.ToString ());
			writer.WriteAttributeString ("undotileid", this.undoTileIconId.ToString ());
			writer.WriteAttributeString ("invalidtileid", this.invalidTileIconId.ToString ());
			foreach (UserInteraction ui in this.uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}		
	}
}
