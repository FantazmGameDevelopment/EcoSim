using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;

namespace Ecosim.SceneEditor
{
	public class VegetationPanel : Panel
	{
		class TileState
		{
			public TileState (TileType tile)
			{
				this.tile = tile;
			}
			
			public TileType tile;
		}
	
		class VegetationState
		{
			public VegetationState (VegetationType veg)
			{
				vegetation = veg;
				tiles = new List<TileState> ();
				foreach (TileType t in veg.tiles) {
					tiles.Add (new TileState (t));
				}
			}
			
			public bool isFoldedOpen = false;
			public VegetationType vegetation;
			public List<TileState> tiles;
		}
		
		class SuccessionState
		{
			public SuccessionState (SuccessionType suc)
			{
				succession = suc;
				vegetations = new List<VegetationState> ();
				foreach (VegetationType veg in suc.vegetations) {
					vegetations.Add (new VegetationState (veg));
				}
			}
			
			public bool isFoldedOpen = false;
			public SuccessionType succession;
			public List<VegetationState> vegetations;
		}
		
		/**
		 * Removes tile from vegetation and tile state corresponding to tile from hierarchie
		 */
		public void DeleteTile (TileType tile)
		{
			List<TileType> newTiles = new List<TileType> (tile.vegetationType.tiles);
			newTiles.Remove (tile);
			tile.vegetationType.tiles = newTiles.ToArray ();
			tile.vegetationType.UpdateReferences (scene);
			
			VegetationData data = scene.progression.vegetation;
			data.RemoveTileType (tile);
			TerrainMgr.self.ForceRedraw ();
			foreach (SuccessionState ss in successions) {
				if (ss.succession == tile.vegetationType.successionType) {
					foreach (VegetationState vs in ss.vegetations) {
						if (vs.vegetation == tile.vegetationType) {
							foreach (TileState ts in vs.tiles) {
								if (ts.tile == tile) {
									vs.tiles.Remove (ts);
									return;
								}
							}
						}
					}
				}
			}
		}
		
		/**
		 * Deletes vegetation veg, update vegetation states, and update vegetation data
		 */
		public void DeleteVegetation (VegetationType veg)
		{
			List<VegetationType> newVegs = new List<VegetationType> (veg.successionType.vegetations);
			newVegs.Remove (veg);
			veg.successionType.vegetations = newVegs.ToArray ();
			veg.successionType.UpdateReferences (scene);
			
			VegetationData data = scene.progression.vegetation;
			data.RemoveVegetationType (veg);
			scene.UpdateReferences ();
			TerrainMgr.self.ForceRedraw ();
			foreach (SuccessionState ss in successions) {
				if (ss.succession == veg.successionType) {
					foreach (VegetationState vs in ss.vegetations) {
						if (vs.vegetation == veg) {
							ss.vegetations.Remove (vs);
							return;
						}
					}
				}
			}
		}

		/**
		 * Deletes succession suc, update succession states, and update vegetation data
		 */
		public void DeleteSuccession (SuccessionType suc)
		{
			List<SuccessionType> newSucs = new List<SuccessionType> (scene.successionTypes);
			newSucs.Remove (suc);
			scene.successionTypes = newSucs.ToArray ();
			scene.UpdateReferences ();
			VegetationData data = scene.progression.vegetation;
			data.RemoveSuccessionType (suc);
			scene.UpdateReferences ();
			TerrainMgr.self.ForceRedraw ();
			foreach (SuccessionState ss in successions) {
				if (ss.succession == suc) {
					successions.Remove (ss);
					return;
				}
			}
		}

		ExtraPanel _extraPanel;
		ExtraPanel extraPanel {
			get { return _extraPanel; }
			set {
				if (_extraPanel != null)
					_extraPanel.Dispose ();
				_extraPanel = value;
			}
		}
		Vector2 scrollPos;
		Vector2 scrollPosExtra;
		Scene scene;
		List<SuccessionState> successions;
		EditorCtrl ctrl;
		
		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			if (scene == null)
				return;
			successions = new List<SuccessionState> ();
			for (int i = 0; i < scene.successionTypes.Length; i++) {
				successions.Add (new SuccessionState (scene.successionTypes [i]));
			}
			extraPanel = null;
			StartValExtraPanel.ClearCopyBuffer ();
			RulesExtraPanel.ClearCopyBuffer ();
			AllowedActionsPanel.ClearCopyBuffer ();
		}
		
		public bool Render (int mx, int my)
		{
			if (scene == null)
				return false;
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			GUILayout.BeginVertical ();
			int successionIndex = 0;
			foreach (SuccessionState st in successions) {
				GUILayout.BeginVertical (ctrl.skin.box);
				GUILayout.BeginHorizontal ();
				if (GUILayout.Button (st.isFoldedOpen ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) {
					st.isFoldedOpen = !st.isFoldedOpen;
				}
				GUILayout.Label (successionIndex.ToString (), GUILayout.Width (40));
				st.succession.name = GUILayout.TextField (st.succession.name);
//				GUILayout.FlexibleSpace ();
				if ((successionIndex > 0) && GUILayout.Button ("-", GUILayout.Width (20))) {
					SuccessionType tmpST = st.succession;
					ctrl.StartDialog ("Delete succession '" + tmpST.name + "'?", newVal => {
						DeleteSuccession (tmpST);
					}, null);
				}
				GUILayout.EndHorizontal ();
				if (st.isFoldedOpen) {
					int vegetationIndex = 0;
					foreach (VegetationState vs in st.vegetations) {
						GUILayout.BeginVertical (ctrl.skin.box);
						GUILayout.BeginHorizontal ();
						if (GUILayout.Button (vs.isFoldedOpen ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) {
							vs.isFoldedOpen = !vs.isFoldedOpen;
						}
						GUILayout.Label (vegetationIndex.ToString (), GUILayout.Width (40));
						vs.vegetation.name = GUILayout.TextField (vs.vegetation.name);
						// GUILayout.FlexibleSpace ();
						if ((st.vegetations.Count > 1) && GUILayout.Button ("-", GUILayout.Width (20))) {
							VegetationType tmpVeg = vs.vegetation;
							ctrl.StartDialog ("Delete vegetation '" + tmpVeg.name + "'?", newVal => {
								DeleteVegetation (tmpVeg);
							}, null);
						}
						GUILayout.EndHorizontal ();
						if (vs.isFoldedOpen) {
							GUILayout.BeginHorizontal ();
							if (GUILayout.Button ("Start values")) {
								extraPanel = new StartValExtraPanel (ctrl, vs.vegetation);
							}
							if (GUILayout.Button ("Rules")) {
								extraPanel = new RulesExtraPanel (ctrl, vs.vegetation);
							}
							if (GUILayout.Button ("Allowed actions")) {
								extraPanel = new AllowedActionsPanel (ctrl, vs.vegetation);
							}
							GUILayout.EndHorizontal ();
							GUILayout.BeginHorizontal ();
							int tsCount = 0;
							foreach (TileState ts in vs.tiles) {
								if ((tsCount > 0) && (tsCount % 5 == 0)) {
									GUILayout.FlexibleSpace ();
									GUILayout.EndHorizontal ();
									GUILayout.BeginHorizontal ();
								}
								if (GUILayout.Button (ts.tile.GetIcon (), ctrl.tileIcon)) {
									extraPanel = new TileExtraPanel (ctrl, ts.tile, this);									
								}
								tsCount++;
							}
							if (GUILayout.Button ("<size=24>+</size>")) {
								TileType newTile = new TileType (vs.vegetation);
								vs.vegetation.tiles [0].CopyTo (newTile, true);
								TileState tileState = new TileState (newTile);
								vs.tiles.Add (tileState);
							}
							GUILayout.FlexibleSpace ();
							GUILayout.EndHorizontal ();
						}
						GUILayout.EndVertical (); // Vegetation entry
						vegetationIndex++;
					}
					// add vegetation type button...
					GUILayout.BeginHorizontal ();
					if (GUILayout.Button ("+")) {
						// add new vegetation
						VegetationType v = new VegetationType (st.succession);
						// we have to update states as well...
						VegetationState state = new VegetationState (v);
						st.vegetations.Add (state);
						st.succession.UpdateReferences (scene);
					}
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
					
				}
				GUILayout.EndVertical (); // Succession entry
				successionIndex++;
			}
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button ("+")) {
				// add new succession
				SuccessionType s = new SuccessionType (scene);
				// we have to update state as well...
				SuccessionState state = new SuccessionState (s);
				successions.Add (state);
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.FlexibleSpace ();
			GUILayout.EndVertical ();
			GUILayout.EndScrollView ();
			return (extraPanel != null);
		}
		
		public void RenderExtra (int mx, int my)
		{
			if (extraPanel != null) {
				bool keepRendering = extraPanel.Render (mx, my);
				if (!keepRendering) {
					extraPanel = null;
				}
			}
		}

		public void RenderSide (int mx, int my)
		{
			if (extraPanel != null) {
				extraPanel.RenderSide (mx, my);
			}
		}

		public bool NeedSidePanel ()
		{
			return (extraPanel is TileExtraPanel);
		}

		public bool IsAvailable ()
		{
			return (scene != null);
		}
		
		public void Activate ()
		{
		}
		
		public void Deactivate ()
		{
		}

		public void Update ()
		{
			if ((CameraControl.MouseOverGUI) ||
			Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt)) {
				return;
			}
			Vector3 mousePos = Input.mousePosition;
			if (Input.GetMouseButton (1)) {
				Vector3 hitPoint;
				if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out hitPoint)) {
					int x = (int)(hitPoint.x / TerrainMgr.TERRAIN_SCALE);
					int y = (int)(hitPoint.z / TerrainMgr.TERRAIN_SCALE);
					if ((x >= 0) && (y >= 0) && (x < scene.width) && (y < scene.height)) {
						TileType tile = scene.progression.vegetation.GetTileType (x, y);
						extraPanel = new TileExtraPanel (ctrl, tile, this);
						SuccessionType selectedSuccession = tile.vegetationType.successionType;
						VegetationType selectedVegetation = tile.vegetationType;
						foreach (SuccessionState ss in successions) {
							if (ss.succession == selectedSuccession) {
								ss.isFoldedOpen = true;
								foreach (VegetationState vs in ss.vegetations) {
									if (vs.vegetation == selectedVegetation) {
										vs.isFoldedOpen = true;
									}
								}
							}
						}
					}
				}
			}
		}
	}	
}