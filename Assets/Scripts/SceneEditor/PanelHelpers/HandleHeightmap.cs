using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleHeightmap : PanelHelper
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
		private bool doWater = false;
		private string[] heightModes = new string[] { "Set", "Add", "Smooth", "Set relative", "Set Image data", "Add Image data" };
		private int heightMode = 1;
		private int heightRangeIndex = 9;
		private float[] heightRangeVals = new float[] {
			0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f,
			1.5f, 2f, 2.5f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f,
			12.5f, 15f, 17.5f, 20f, 25f, 30f, 35f, 40f, 50f,
			60f, 70f, 80f, 90f, 100f, 150f, 200f, 250f, 300f, 400f, 500f
		};
		private float heightRange = 1.0f;
		private string heightRangeStr = "1.0";
		private string loadMultiplier = "600";
		private string loadOffset = "20";
		private EditData edit;
		private Data backupCopy;
		private Data backupCopyExtra;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;
		
		public HandleHeightmap (EditorCtrl ctrl, MapsPanel parent, Scene scene)
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
			parent.ResetEdit ();
			edit = EditData.CreateEditData ("heightmap", null, delegate(int x, int y, int currentVal, float strength, bool shift, bool ctrl) {
				if (!ctrl) {
					return shift ? 0 : (int)(100 * brushStrength);
				} else {
					strength = strength * brushStrength;
					return Mathf.RoundToInt ((shift ? 0 : (strength * 100)) + ((1 - strength) * currentVal));
				}
			}, gridSettings255);
			edit.AddRightMouseHandler (delegate(int x, int y, int v) {
				HeightMap data = doWater ? scene.progression.waterHeightMap:scene.progression.heightMap;
				heightRange = ((float)data.Get (x, y) * TerrainMgr.HEIGHT_PER_UNIT);
				heightRangeStr = heightRange.ToString ();
				heightRangeIndex = 0;
				for (int i = 0; i < heightRangeVals.Length; i++) {
					if (heightRange >= heightRangeVals [i])
						heightRangeIndex = i;
				}
			});
			edit.SetModeBrush (brushWidth);
			brushMode = EBrushMode.Circle;
			backupCopy = new HeightMap (scene);
			backupCopyExtra = new HeightMap (scene);
			scene.progression.heightMap.CopyTo (backupCopy);
			scene.progression.waterHeightMap.CopyTo (backupCopyExtra);
		}

		public bool Render (int mx, int my)
		{
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("height type", GUILayout.Width (100));
			if (GUILayout.Button ("Land", doWater ? tabNormal : tabSelected, GUILayout.Width (100))) {
				doWater = false;
			}
			if (GUILayout.Button ("Water", doWater ? tabSelected : tabNormal, GUILayout.Width (100))) {
				doWater = true;
			}
			
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("height mode", GUILayout.Width (100));
			if (GUILayout.Button (heightModes [heightMode], tabNormal, GUILayout.Width (202))) {
				ctrl.StartSelection (heightModes, heightMode,
					newIndex => {
					heightMode = newIndex;
				});
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
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
			GUILayout.Label ("Height range", GUILayout.Width (100));
			int newHeightRangeIndex = (int)GUILayout.HorizontalSlider (heightRangeIndex, 0f, ((float)heightRangeVals.Length) - 0.99f, GUILayout.Width (160f));
			string newHeightRangeStr = GUILayout.TextField (heightRangeStr, GUILayout.Width (50));
			if (newHeightRangeIndex != heightRangeIndex) {
				heightRangeIndex = newHeightRangeIndex;
				heightRange = ((heightRange < 0) ? -1 : 1) * heightRangeVals [heightRangeIndex];
				heightRangeStr = heightRange.ToString ();
			} else if (newHeightRangeStr != heightRangeStr) {
				heightRangeStr = newHeightRangeStr;
				float parseVal;
				if (float.TryParse (newHeightRangeStr, out parseVal)) {
					heightRange = parseVal;
					float abs = (heightRange < 0) ? -heightRange : heightRange;
					heightRangeIndex = 0;
					for (int i = 0; i < heightRangeVals.Length; i++) {
						if (abs >= heightRangeVals [i])
							heightRangeIndex = i;
					}
				}
			}
			bool switchNeg = GUILayout.Toggle ((heightRange < 0), "negate");
			if (switchNeg != (heightRange < 0)) {
				heightRange = -heightRange;
				heightRangeStr = heightRange.ToString ();
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
			if (GUILayout.Button ("Save to clipboard", GUILayout.Width (100))) {
				scene.progression.heightMap.CopyTo (backupCopy);
				scene.progression.waterHeightMap.CopyTo (backupCopyExtra);
			}
			if (GUILayout.Button ("Restore from clipb.", GUILayout.Width (100))) {
				backupCopy.CopyTo (scene.progression.heightMap);
				backupCopyExtra.CopyTo (scene.progression.waterHeightMap);
				TerrainMgr.self.ForceRedraw (); // redraw terrain
			}
			if (GUILayout.Button ("Apply", GUILayout.Width (50))) {
				// hpu = 0.01m 
				float heightMultiplier = (heightRange / TerrainMgr.HEIGHT_PER_UNIT / 100f);

				if (heightMode == 0) {
					// set height
					ApplyHeightFn (delegate(int height, int waterHeight, int changeVal) {
						int v = (doWater ? waterHeight : height);
						return (changeVal > 0) ? (Mathf.RoundToInt (heightMultiplier * changeVal + (100 - changeVal) * v / 100)) : v;
					}, doWater);
				} else if (heightMode == 1) {
					// add/subtract height
					ApplyHeightFn (delegate(int height, int waterHeight, int changeVal) {
						int h = (doWater) ? waterHeight : height;
						return (changeVal > 0) ? (Mathf.RoundToInt (heightMultiplier * changeVal) + h) : h;
					}, doWater);
				} else if (heightMode == 2) {
					// smooth
					long calcAvgHeightCount = 0;
					long calcAvgTotalCount = 0;
					ApplyHeightFn (delegate(int height, int waterHeight, int changeVal) {
						if (changeVal > 0) {
							calcAvgTotalCount += changeVal;
							calcAvgHeightCount += (doWater ? waterHeight : height) * changeVal;
						}
						
						return height;
					}, false);
					float avgHeight = ((float)calcAvgHeightCount) / calcAvgTotalCount / 100f; // /100f because changeval = 0..100 instead of 0..1
					ApplyHeightFn (delegate(int height, int waterHeight, int changeVal) {
						int v = (doWater ? waterHeight : height);						
						return (changeVal > 0) ? (Mathf.RoundToInt (avgHeight * changeVal + (100 - changeVal) * v / 100)) : v;
					}, doWater);
				} else if (heightMode == 3) {
					// add relative
					ApplyHeightFn (delegate(int height, int waterHeight, int changeVal) {
						return (changeVal > 0) ? (Mathf.RoundToInt (heightMultiplier * changeVal) + (doWater ? height : waterHeight)) : (doWater ? waterHeight : height);
					}, doWater);
				} else if (heightMode == 4) {
					// set height using image (add/subtract)
					ApplyHeightFnXY (delegate(int x, int y, int height, int waterHeight, int changeVal) {
						int v = (doWater ? waterHeight : height);						
						if (changeVal > 0) {
							return (Mathf.RoundToInt (heightMultiplier * changeVal * parent.GetFromImage (x, y) + (100 - changeVal) * v / 100));
						} else {
							return v;
						}
					}, doWater);
				} else if (heightMode == 5) {
					// add height using image (add/subtract)
					ApplyHeightFnXY (delegate(int x, int y, int height, int waterHeight, int changeVal) {
						if (changeVal > 0) {
							return (Mathf.RoundToInt (heightMultiplier * changeVal * parent.GetFromImage (x, y)) + (doWater ? waterHeight : height));
						} else {
							return doWater ? waterHeight : height;
						}
					}, doWater);
				}
			}
			if (GUILayout.Button ("Clear", GUILayout.Width (50))) {
				edit.ClearData ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.FlexibleSpace ();
			GUILayout.Space (16);
			if (parent.texture != null) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("max height", GUILayout.Width (100));
				loadMultiplier = GUILayout.TextField (loadMultiplier, GUILayout.Width (40));
				GUILayout.Label ("height offset", GUILayout.Width (100));
				loadOffset = GUILayout.TextField (loadOffset, GUILayout.Width (40));
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("use image to:", GUILayout.Width (100));
				bool doSetHeights = GUILayout.Button ("set heights");
				bool doSetWaterHeights = GUILayout.Button ("set water heights");
				if (doSetHeights || doSetWaterHeights) {
					int heightMultiplier = 0;
					int heightOffset = 0;
					int.TryParse (loadMultiplier, out heightMultiplier);
					int.TryParse (loadOffset, out heightOffset);
					loadMultiplier = heightMultiplier.ToString ();
					loadOffset = heightOffset.ToString ();
					ushort[] data = doSetHeights ? scene.progression.heightMap.data : scene.progression.waterHeightMap.data;
					int p = 0;
					for (int y = 0; y < scene.height; y++) {
						for (int x = 0; x < scene.width; x++) {
							int height = (int)((heightMultiplier * parent.GetFromImage (x, y) + heightOffset) * 100);
							data [p++] = (ushort)Mathf.Clamp (height, 0, 65535);
						}
					}
					TerrainMgr.self.ForceRedraw ();
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

		delegate int ProcessHeightFn (int height,int waterHeight,int changeVal);

		delegate int ProcessHeightFnXY (int x,int y,int height,int waterHeight,int changeVal);
		
		private void ApplyHeightFn (ProcessHeightFn fn, bool isWater)
		{
			ushort[] height = scene.progression.heightMap.data;
			ushort[] waterHeight = scene.progression.waterHeightMap.data;
			BitMap8 data = new BitMap8 (scene);
			edit.CopyData (data);
			byte[] changeVals = data.data;
			
			int minX = scene.width;
			int maxX = 0;
			int minY = scene.height;
			int maxY = 0;
			
			int p = 0;
			for (int y = 0; y < scene.height; y++) {
				for (int x = 0; x < scene.height; x++) {
					int currentHeight = height [p];
					int currentWaterHeight = waterHeight [p];
					int changeVal = changeVals [p];
					int result = Mathf.Clamp (fn (currentHeight, currentWaterHeight, changeVal), 0, 65535);
					bool isChanged = false;
					if (isWater) {
						if (result != currentWaterHeight) {
							waterHeight [p] = (ushort)result;
							isChanged = true;
						}
					} else {
						if (result != currentHeight) {
							height [p] = (ushort)result;
							isChanged = true;
						}
					}
					if (isChanged) {
						minX = Mathf.Min (x, minX);
						minY = Mathf.Min (y, minY);
						maxX = Mathf.Max (x, maxX);
						maxY = Mathf.Max (y, maxY);
					}
					p++;
				}
			}
			if (minX < maxX) {
				TerrainMgr.self.ForceRedraw (minX, minY, maxX, maxY);
			}
		}

		private void ApplyHeightFnXY (ProcessHeightFnXY fn, bool isWater)
		{
			ushort[] height = scene.progression.heightMap.data;
			ushort[] waterHeight = scene.progression.waterHeightMap.data;
			BitMap8 data = new BitMap8 (scene);
			edit.CopyData (data);
			byte[] changeVals = data.data;
			
			int minX = scene.width;
			int maxX = 0;
			int minY = scene.height;
			int maxY = 0;
			
			int p = 0;
			for (int y = 0; y < scene.height; y++) {
				for (int x = 0; x < scene.height; x++) {
					int currentHeight = height [p];
					int currentWaterHeight = waterHeight [p];
					int changeVal = changeVals [p];
					int result = Mathf.Clamp (fn (x, y, currentHeight, currentWaterHeight, changeVal), 0, 65535);
					bool isChanged = false;
					if (isWater) {
						if (result != currentWaterHeight) {
							waterHeight [p] = (ushort)result;
							isChanged = true;
						}
					} else {
						if (result != currentHeight) {
							height [p] = (ushort)result;
							isChanged = true;
						}
					}
					if (isChanged) {
						minX = Mathf.Min (x, minX);
						minY = Mathf.Min (y, minY);
						maxX = Mathf.Max (x, maxX);
						maxY = Mathf.Max (y, maxY);
					}
					p++;
				}
			}
			if (minX < maxX) {
				TerrainMgr.self.ForceRedraw (minX, minY, maxX, maxY);
			}
		}		

		public void Update() {
		}
	}
}
