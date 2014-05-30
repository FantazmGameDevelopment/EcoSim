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
	public class InventarisationAction : BasicAction
	{
		public const string XML_ELEMENT = "inventarisation";
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
		private Texture2D inventarisationTex = null;
		private Material inventarisationMat = null;
		private GridTextureSettings inventarisationGrid = null;
		private int invalidAreaIndex;
		private string description;
		
		public class InventarisationValue
		{
			public int iconId;
			public string name;
		}
		
		public const int MAX_VALUE_INDEX = 14;
		public InventarisationValue[] valueTypes;
				
		public InventarisationAction (Scene scene, int id) : base (scene, id)
		{
			valueTypes = new InventarisationValue[MAX_VALUE_INDEX + 1];
		}

		public InventarisationAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			description = "Inventarisation " + id;
			areaName = "area" + id.ToString ();
			UserInteraction ui = new UserInteraction (this);
			uiList.Add (ui);
			valueTypes = new InventarisationValue[MAX_VALUE_INDEX + 1];
			valueTypes [0] = new InventarisationValue ();
			valueTypes [0].name = "Result1";
			valueTypes [1] = new InventarisationValue ();
			valueTypes [1].name = "Result2";
			valueTypes [2] = new InventarisationValue ();
			valueTypes [2].name = "Result3";
		}
		
		~InventarisationAction ()
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
			if (inventarisationMat != null) {
				UnityEngine.Object.Destroy (inventarisationMat);
			}
			if (inventarisationTex != null) {
				UnityEngine.Object.Destroy (inventarisationTex);
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
			new Ecosim.GameCtrl.GameButtons.InventarisationActionWindow (ui);
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
			
			icons.Clear ();
			for (int i = 0; i <= MAX_VALUE_INDEX; i++) {
				InventarisationValue iv = valueTypes [i];
				if (iv != null) {
					icons.Add (scene.assets.GetIcon (iv.iconId));
				} else {
					icons.Add (scene.assets.GetIcon (gridIconId));
				}
			}
			inventarisationTex = new Texture2D (4 * 32, 4 * 32, TextureFormat.RGBA32, false, true);
			inventarisationTex.filterMode = FilterMode.Point;
			inventarisationTex.wrapMode = TextureWrapMode.Clamp;
			scene.assets.CopyTexures (icons.ToArray (), inventarisationTex, null);
			inventarisationMat = new Material (EcoTerrainElements.GetMaterial ("MapGrid100"));
			inventarisationMat.mainTexture = inventarisationTex;
			inventarisationGrid = new GridTextureSettings (false, -1, 4, inventarisationMat);
		}

		public override int GetMinUICount ()
		{
			return 1;
		}
		
		public override int GetMaxUICount ()
		{
			return 15;
		}
		
		public bool CanSelectTile (int x, int y, UserInteraction ui)
		{
			if (canSelectTileMI != null) {
				return (bool)canSelectTileMI.Invoke (ecoBase, new object[] { x, y, ui });
			}
			return true;
		}
		
		public EditData GetInventarisationMap (Data data)
		{
			return EditData.CreateEditData ("inv", data, null, inventarisationGrid);
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

				// Remember the last taken researche values
				int selectedTilesCount = 0;
				foreach (ValueCoordinate vc in selectedArea.EnumerateNotZero()) {
					selectedTilesCount++;
				}
				
				scene.progression.variables [Progression.PredefinedVariables.lastResearch.ToString()] = this.description;
				scene.progression.variables [Progression.PredefinedVariables.lastResearchGroup.ToString()] = "Inventarisation";
				scene.progression.variables [Progression.PredefinedVariables.lastResearchCount.ToString()] = selectedTilesCount;
			}
			edit.Delete ();
			edit = null;
			RecalculateEstimates (false);

			if (!isCanceled) {
				scene.actions.ResearchConducted ();
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
		}
	
		public static InventarisationAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			InventarisationAction action = new InventarisationAction (scene, id);
			action.description = reader.GetAttribute ("description");
			action.areaName = reader.GetAttribute ("areaname");
			int.TryParse (reader.GetAttribute ("gridicon"), out action.gridIconId);
			int.TryParse (reader.GetAttribute ("invalidicon"), out action.invalidTileIconId);

			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteraction.XML_ELEMENT)) {
						action.uiList.Add (UserInteraction.Load (action, reader));
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "valuetype")) {
						int index = int.Parse (reader.GetAttribute ("index"));
						string name = reader.GetAttribute ("name");
						int iconid = int.Parse (reader.GetAttribute ("icon"));
						InventarisationValue iv = new InventarisationValue ();
						iv.iconId = iconid;
						iv.name = name;
						action.valueTypes [index] = iv;
						IOUtil.ReadUntilEndElement (reader, "valuetype");
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
			for (int i = 0; i < valueTypes.Length; i++) {
				if (valueTypes [i] != null) {
					writer.WriteStartElement ("valuetype");
					writer.WriteAttributeString ("index", i.ToString ());
					writer.WriteAttributeString ("name", valueTypes [i].name);
					writer.WriteAttributeString ("icon", valueTypes [i].iconId.ToString ());
					writer.WriteEndElement ();
				}
			}
			writer.WriteEndElement ();
		}	
	}		
}