using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleVegetation : PanelHelper
	{
		private readonly MapsPanel parent;
		private Scene scene;
		private EditorCtrl ctrl;
		private GridTextureSettings gridSettings255 = new GridTextureSettings (false, 0, 16, "MapGrid255", true, "ActiveMapGrid255");
		private int brushWidth;
		private float brushStrength = 1.0f;
		private enum EBrushMode
		{
			Area,
			Circle
		};
		private EBrushMode brushMode;
		private EditData edit;
		private Data backupCopy;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;
		string[] successionNames;
		int selectedSuccession = -1;
		string[] vegetationNames;
		int selectedVegetation = -1;
		TileType[] tiles;
		int selectedTile = -1;
		
		public HandleVegetation (EditorCtrl ctrl, MapsPanel parent, Scene scene)
		{
			this.parent = parent;
			this.ctrl = ctrl;
			
			this.scene = parent.scene;
			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
			
			Setup ();
		}
		
		void Setup ()
		{
			edit = EditData.CreateEditData ("vegetation", null, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
				if (!ctrl) {
					return shift ? 0 : (int)(100 * brushStrength);
				} else {
					strength = strength * brushStrength;
					return Mathf.RoundToInt ((shift ? 0 : (strength * 100)) + ((1 - strength) * currentVal));
				}
			}, gridSettings255);
			edit.SetModeBrush (brushWidth);
			
			edit.AddRightMouseHandler (delegate(int x, int y, int v) {
				
				VegetationData data = scene.progression.vegetation;
				VegetationType vegType = data.GetVegetationType (x, y);
				SuccessionType succession = vegType.successionType;
				List<string> vegNameList = new List<string> ();
				vegNameList.Add ("<Any vegetation>");
				foreach (VegetationType vt in succession.vegetations) {
					vegNameList.Add (vt.name);
				}
				vegetationNames = vegNameList.ToArray ();
				tiles = vegType.tiles;
			});
			
			brushMode = EBrushMode.Circle;
			backupCopy = new VegetationData (scene);
			scene.progression.vegetation.CopyTo (backupCopy);
			
			List<string> successionNameList = new List<string> ();
			successionNameList.Add ("<Any succession>");
			foreach (SuccessionType st in scene.successionTypes) {
				successionNameList.Add (st.name);
			}
			successionNames = successionNameList.ToArray ();
		}
				
		public bool Render (int mx, int my)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("brush mode", GUILayout.Width (100));
			if (GUILayout.Button ("Area select", (brushMode == EBrushMode.Area) ? tabSelected : tabNormal, GUILayout.Width (100))) {
				brushMode = EBrushMode.Area;
				edit.SetModeAreaSelect ();
			}
			if (GUILayout.Button ("Circle brush", (brushMode == EBrushMode.Circle) ? tabSelected : tabNormal, GUILayout.Width (100))) {
				brushMode = EBrushMode.Circle;
				edit.SetModeBrush (brushWidth);
			}
			
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Brush strength", GUILayout.Width (100));
			float newBrushStrength = GUILayout.HorizontalSlider (brushStrength, 0f, 1f, GUILayout.Width (160f));
			GUILayout.Label (brushStrength.ToString ());
			if (newBrushStrength != brushStrength) {
				brushStrength = ((int)(newBrushStrength * 20)) / 20f;
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			if (brushMode == EBrushMode.Circle) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Brush width", GUILayout.Width (100));
				int newBrushWidth = (int)GUILayout.HorizontalSlider (brushWidth, 0f, 10f, GUILayout.Width (160f));
				GUILayout.Label (brushWidth.ToString ());
				if (newBrushWidth != brushWidth) {
					brushWidth = newBrushWidth;
					edit.SetModeBrush (brushWidth);
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
			GUILayout.Space (16);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Succession", GUILayout.Width (100));
			if (GUILayout.Button (successionNames [selectedSuccession + 1], tabNormal, GUILayout.Width (280))) {
				ctrl.StartSelection (successionNames, selectedSuccession + 1, result => {
					selectedSuccession = result - 1;
					selectedVegetation = -1;
					selectedTile = -1;
					if (selectedSuccession >= 0) {
						SuccessionType succession = scene.successionTypes [selectedSuccession];
						List<string> vegNameList = new List<string> ();
						vegNameList.Add ("<Any vegetation>");
						foreach (VegetationType vt in succession.vegetations) {
							vegNameList.Add (vt.name);
						}
						vegetationNames = vegNameList.ToArray ();
					}
				});
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			if (selectedSuccession >= 0) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Vegetation", GUILayout.Width (100));
				if (GUILayout.Button (vegetationNames [selectedVegetation + 1], tabNormal, GUILayout.Width (280))) {
					ctrl.StartSelection (vegetationNames, selectedVegetation + 1, result => {
						selectedVegetation = result - 1;
						selectedTile = -1;
						if (selectedVegetation >= 0) {
							tiles = scene.successionTypes [selectedSuccession].vegetations [selectedVegetation].tiles;
						}
					});
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				
				if (selectedVegetation >= 0) {
					GUILayout.BeginHorizontal ();
					GUILayout.Label ("Tile", GUILayout.Width (100));
					if (GUILayout.Button (ctrl.questionMark, (selectedTile == -1) ? tabSelected : tabNormal, GUILayout.Width (52), GUILayout.Height (52))) {
						selectedTile = -1;
					}
					for (int i = 0; i < tiles.Length; i++) {
						if (((i + 1) % 5) == 0) {
							GUILayout.FlexibleSpace ();
							GUILayout.EndHorizontal ();
							GUILayout.BeginHorizontal ();
							GUILayout.Label ("", GUILayout.Width (100));
						}
						if (GUILayout.Button (tiles [i].GetIcon (), (selectedTile == i) ? tabSelected : tabNormal, GUILayout.Width (52), GUILayout.Height (52))) {
							selectedTile = i;
						}
					}
					GUILayout.FlexibleSpace ();
					GUILayout.EndHorizontal ();
				}
			}
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", GUILayout.Width (100));
			if (GUILayout.Button ("Select", GUILayout.Width (60))) {
				BitMap8 data = new BitMap8 (scene);
				ushort[] vegetation = scene.progression.vegetation.data;
				int p = 0;
				for (int y = 0; y < scene.height; y++) {
					for (int x = 0; x < scene.width; x++) {
						int vegetationInt = vegetation [p];
						int successionId = (vegetationInt >> VegetationData.SUCCESSION_SHIFT) & VegetationData.SUCCESSION_MASK;
						if ((successionId == selectedSuccession) || (selectedSuccession == -1)) {
							int vegetationId = (vegetationInt >> VegetationData.VEGETATION_SHIFT) & VegetationData.VEGETATION_MASK;
							if ((vegetationId == selectedVegetation) || (selectedVegetation == -1)) {
								int tileId = (vegetationInt >> VegetationData.TILE_SHIFT) & VegetationData.TILE_MASK;
								if ((tileId == selectedTile) || (selectedTile == -1)) {
									data.data [p] = 100; // found tile with correct vegetation, set edit value to 100%
								}
							}
						}
						p++;
					}
				}
				edit.SetData (data);
			}
			if (GUILayout.Button ("Set", GUILayout.Width (60))) {
				BitMap8 data = new BitMap8 (scene);
				edit.CopyData (data);
				byte[] bytes = data.data;
				ushort[] vegetation = scene.progression.vegetation.data;
				int p = 0;
				for (int y = 0; y < scene.height; y++) {
					for (int x = 0; x < scene.width; x++) {
						int chance = bytes [p]; // edit area is % chance we need to set the tile to new vegetation
						if ((chance > 0) && (Random.Range (0, 100) < chance)) {
							int successionId = (selectedSuccession >= 0) ? selectedSuccession : (Random.Range (0, scene.successionTypes.Length));
							if (scene.successionTypes [successionId].vegetations.Length > 0) {
								int vegetationId = (selectedVegetation >= 0) ? selectedVegetation : (Random.Range (0, scene.successionTypes [successionId].vegetations.Length));
								int tileId = selectedTile;
								if (tileId < 0) {
									// tile should be chosen random
									int tileLen = scene.successionTypes [successionId].vegetations [vegetationId].tiles.Length;
									if (tileLen == 0) {
										Debug.LogError ("No tiles defined for " + scene.successionTypes [successionId].vegetations [vegetationId].name);
										tileId = 0;
										vegetationId = 0;
										successionId = 0;
									}
									if (tileLen == 1) {
										tileId = 0;
									} else {
										// normally we don't randomly choose tile id 0, as it is special, but we need to have at least 2 tiles before
										// we can choose tile id != 0
										int vegetationInt = vegetation [p];
										int currentTileId = vegetationInt & 0x0f;
										// tile id 0 is special (empty variant), if current tile has id 0, we make new tile id 0 as well...
										tileId = (currentTileId == 0) ? 0 : Random.Range (1, tileLen);
									}
								} 
								vegetation [p] = (ushort)((successionId << VegetationData.SUCCESSION_SHIFT) |
									(vegetationId << VegetationData.VEGETATION_SHIFT) | (tileId << VegetationData.TILE_SHIFT));
								// We mark it as changed so if we change it during the game it saves the changes
								scene.progression.vegetation.hasChanged = true;

							}
						}
						p++;
					}
				}
				TerrainMgr.self.ForceRedraw ();
			}
			if (GUILayout.Button ("Clear", GUILayout.Width (60))) {
				edit.ClearData ();
			}
			if (GUILayout.Button ("Default", GUILayout.Width (60))) {
				Perlin perlin = new Perlin(1234);
				BitMap8 data = new BitMap8 (scene);
				edit.CopyData (data);
				byte[] bytes = data.data;
				VegetationData vegData = scene.progression.vegetation;
				int p = 0;
				float scale = SettingsPanel.perlinScale;
				for (int y = 0; y < scene.height; y++) {
					for (int x = 0; x < scene.width; x++) {
						int chance = bytes [p++]; // edit area is % chance we need to set the tile to new vegetation
						if ((chance > 0) && (Random.Range (0, 100) < chance)) {
							VegetationType v = vegData.GetVegetationType (x, y);
							foreach (ParameterChange pc in v.changes) {
								int offsetX = pc.data.GetHashCode() & 0xfff;
								int offsetY = (pc.data.GetHashCode() >> 12) & 0xfff;
								float noise = 0.5f + 0.5f * perlin.Noise (scale * x - 123.123f + offsetX, scale * y - 345.567f + offsetY);
//								Debug.Log ("pos " + x + ", " + y + " noise = " + noise);
								int val = Mathf.RoundToInt(noise * (pc.highRange - pc.lowRange) + pc.lowRange);
								pc.data.Set (x, y, Mathf.Clamp (val, pc.lowRange, pc.highRange));
							}
						}
					}
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.FlexibleSpace ();
			if (parent.texture != null) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("selection from image", GUILayout.Width (100));
				if (GUILayout.Button ("select")) {
					Data tmpData = new BitMap8 (scene);
					for (int y = 0; y < scene.height; y++) {
						for (int x = 0; x < scene.width; x++) {
							int v = (int)(100 * parent.GetFromImage (x, y));
							tmpData.Set (x, y, v);
						}
					}
					edit.SetData (tmpData);
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
			parent.RenderLoadTexture ();
			return false;
		}
		
		public void Disable ()
		{
			edit.Delete ();
			edit = null;
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
						selectedSuccession = tile.vegetationType.successionType.index;
						selectedVegetation = tile.vegetationType.index;
						selectedTile = tile.index;
						List<string> vegNameList = new List<string> ();
						vegNameList.Add ("<Any vegetation>");
						foreach (VegetationType vt in tile.vegetationType.successionType.vegetations) {
							vegNameList.Add (vt.name);
						}
						vegetationNames = vegNameList.ToArray ();
						tiles = tile.vegetationType.tiles;
					}
				}
			}
		}
	}
}
