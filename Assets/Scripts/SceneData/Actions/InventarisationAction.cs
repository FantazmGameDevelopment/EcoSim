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
		public class BiasRange
		{
			public enum RoundTypes
			{
				RoundUp,
				RoundDown,
				RoundAutomatic
			}

			public RoundTypes roundType;
			public float min;
			public float max;

			public BiasRange () { }
			public BiasRange (float min, float max)
			{
				this.min = min;
				this.max = max;
			}
		}

		public const string XML_ELEMENT = "inventarisation";
		public string areaName;
		public string invAreaName;
		public int gridIconId;
		public int invalidTileIconId;
		public List<BiasRange> biasses;

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
			biasses = new List<BiasRange>();
		}

		public InventarisationAction (Scene scene) : base(scene, scene.actions.lastId)
		{
			description = "Inventarisation " + id;
			areaName = "area" + id.ToString ();
			invAreaName = "";
			UserInteraction ui = new UserInteraction (this);
			uiList.Add (ui);
			biasses = new List<BiasRange>() { new BiasRange () };
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

		public BiasRange GetBias (int index)
		{
			if (index < biasses.Count)
				return biasses [index];

			while (index >= biasses.Count) {
				biasses.Add (new BiasRange (1f, 1f));
			}
			return biasses [index];
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
			// Setup unique area name
			invAreaName = "_" + this.areaName + "_" + (scene.progression.activeInventarisations.Count).ToString();
			selectedArea = new SparseBitMap8 (scene);
			scene.progression.AddData (invAreaName, selectedArea);

			/*if (selectedArea == null) {
				if (uiList.Count > 1) {
					selectedArea = new SparseBitMap8 (scene);
				} else {
					selectedArea = new SparseBitMap1 (scene);
				}
				scene.progression.AddData (areaName, selectedArea);
			}*/

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
			}
		}

		/// <summary>
		/// Recalculates the estimates. This one is for now just used to reset all estimated costs.
		/// </summary>
		public void RecalculateEstimates (bool checkTileIsValid)
		{
			foreach (UserInteraction ui in uiList) {
				ui.estimatedTotalCostForYear = 0L; // first reset...
			}

			// Check if we have the areaname
			/*string dataName = invAreaName;// areaName;
			if (!string.IsNullOrEmpty (invAreaName) && scene.progression.HasData (dataName)) {
				selectedArea = scene.progression.GetData (dataName);
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
			}*/
		}

		public override void FinalizeSuccession ()
		{
			base.FinalizeSuccession ();
			//RecalculateEstimates (true);

			// Reset costs
			foreach (UserInteraction ui in uiList) {
				ui.estimatedTotalCostForYear = 0L; // first reset...
			}
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
					} 
					else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "bias")) 
					{
						float min = float.Parse (reader.GetAttribute ("min"));
						float max = float.Parse (reader.GetAttribute ("max"));

						BiasRange newRange = new BiasRange (min, max);
						if (!string.IsNullOrEmpty (reader.GetAttribute ("roundtype"))) {
							newRange.roundType = (BiasRange.RoundTypes)System.Enum.Parse (typeof (BiasRange.RoundTypes), reader.GetAttribute ("roundtype"));
						}
						action.biasses.Add (newRange);

						IOUtil.ReadUntilEndElement (reader, "bias");
					}
					else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
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
			foreach (BiasRange r in biasses) {
				writer.WriteStartElement ("bias");
				writer.WriteAttributeString ("min", r.min.ToString());
				writer.WriteAttributeString ("max", r.max.ToString());
				writer.WriteAttributeString ("roundtype", r.roundType.ToString());
				writer.WriteEndElement ();
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