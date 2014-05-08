using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using System.Threading;

namespace Ecosim.Render.BackgroundProcessing
{
	public class ProcessHeightData : Process
	{
		public ProcessHeightData (Scene scene, TerrainCell cell)
		: base(scene, cell)
		{		
			this.heights = new float[(TerrainCell.CELL_SIZE << 2) + 1, (TerrainCell.CELL_SIZE << 2) + 1];
			this.waterHeights = new float[(TerrainCell.CELL_SIZE << 2) + 1, (TerrainCell.CELL_SIZE << 2) + 1];
			this.heightData = scene.progression.heightMap.data;
			this.waterHeightData = scene.progression.calculatedWaterHeightMap.data;
			this.canals = scene.progression.canals; 
		}
		
		private const int CELL_SIZE = TerrainCell.CELL_SIZE;
		public readonly float[,] heights;
		public readonly float[,] waterHeights;
		readonly ushort[] heightData;
		readonly ushort[] waterHeightData;
		public readonly BitMap2 canals;
		public StencilMap water;
		private volatile bool isReady = false;
		private volatile bool isError = false;
		private const float CANAL_DEPTH = -2f / TerrainMgr.VERTICAL_HEIGHT;
		private const float MIN_CANAL_WATER = 0.25f / TerrainMgr.VERTICAL_HEIGHT;
		private const float VERTICAL_NORMALIZE = 1f / 65535f;
		private float[] weights = new float[] { 0.25f, 0.75f, 1f, 1f, 0.75f, 0.25f };
		
		public override void StartWork ()
		{
// temp disable unreachable code warning
#pragma warning disable 162
			if (GameSettings.MULTITHREAD_RENDERING) {
				ThreadPool.QueueUserWorkItem (WorkThread, null);
			}
			else {
				WorkThread (null);
			}
#pragma warning restore 162
		}
		
		public override  IEnumerable<bool> TryFinishWork ()
		{
			while (!isReady) {
				yield return false;
			}
			if (isError) {
				throw new EcoException ("Failed to process terrain");
			}
			
			// set the terrain heights
			cell.terrain.terrainData.SetHeights (0, 0, heights);
			
			yield return false;
			
			// create water meshes...
			Mesh[] meshes = water.GetMeshes ();
			foreach (Mesh mesh in meshes) {
				GameObject go = new GameObject ("water");
				Transform cot = go.transform;
				cot.parent = cell.transform;
				cot.localPosition = Vector3.zero;
				cot.localRotation = Quaternion.identity;
				cot.localScale = new Vector3 (TerrainMgr.TERRAIN_SCALE, TerrainMgr.VERTICAL_HEIGHT, TerrainMgr.TERRAIN_SCALE);
				go.layer = Layers.L_WATER;
				MeshFilter filter = go.AddComponent<MeshFilter> ();
				filter.sharedMesh = mesh;
				MeshRenderer render = go.AddComponent<MeshRenderer> ();
				render.sharedMaterial = TerrainMgr.self.waterMaterial;
				cell.AddObject (go);
			}
			yield return true;
		}

		private void AddCanal (int startX, int startY, int x, int y, int canal, float[,] height, BitMap2 canals)
		{
			int xx = x << 2;
			int yy = y << 2;
			float depth = CANAL_DEPTH;
			if (canal >= 3)
				depth = depth + depth;
			bool canalLeft = MakeValid (startX + x - 1, startY + y, canals, 0) != 0;
			bool canalRight = MakeValid (startX + x + 1, startY + y, canals, 0) != 0;
			bool canalDown = MakeValid (startX + x, startY + y - 1, canals, 0) != 0;
			bool canalUp = MakeValid (startX + x, startY + y + 1, canals, 0) != 0;
			height [yy + 1, xx + 1] += depth;
			if (canal > 1) {
				height [yy + 2, xx + 1] += depth;
				height [yy + 2, xx + 2] += depth;
				height [yy + 1, xx + 2] += depth;
				if (canalLeft) {
					height [yy + 1, xx + 0] += depth;
					height [yy + 2, xx + 0] += depth;
				}
				if (canalRight) {
					height [yy + 1, xx + 3] += depth;
					height [yy + 2, xx + 3] += depth;
				}
				if (canalDown) {
					height [yy + 0, xx + 1] += depth;
					height [yy + 0, xx + 2] += depth;
				}
				if (canalUp) {
					height [yy + 3, xx + 1] += depth;
					height [yy + 3, xx + 2] += depth;
				}
			} else {
				if (canalLeft) {
					height [yy + 1, xx + 0] += depth;
				}
				if (canalRight) {
					height [yy + 1, xx + 2] += depth;
					height [yy + 1, xx + 3] += depth;
				}
				if (canalDown) {
					height [yy + 0, xx + 1] += depth;
				}
				if (canalUp) {
					height [yy + 2, xx + 1] += depth;
					height [yy + 3, xx + 1] += depth;
				}
			}
		}
	
		private void MakeWaterMeshFromCoords (int x, int y, int val, System.Object args)
		{
			water.AddTile (x, y);
		}
		
		private void ProcessStrip (int startX, int startY, int width, int height)
		{
			for (int y = startY; y < startY + height; y++) {
				int p = (y + offsetY) * scene.width + offsetX + startX;
				for (int x = startX; x < startX + width; x++) {
					
					int yy = y << 2;
					float heightVal = heightData [p] * VERTICAL_NORMALIZE;
					float waterHeightVal = waterHeightData [p] * VERTICAL_NORMALIZE;
					
					for (int subY = -1; subY <= 4; subY++) {
						int yp = yy + subY;
						if ((yp >= 0) && (yp <= CELL_SIZE << 2)) {
							float weightY = weights [subY + 1];
							int xx = x << 2;
							for (int subX = -1; subX <= 4; subX++) {
								int xp = xx + subX;
								if ((xp >= 0) && (xp <= CELL_SIZE << 2)) {
									float weight = weights [subX + 1] * weightY;
									heights [yy + subY, xx + subX] += weight * heightVal;
									waterHeights [yy + subY, xx + subX] += weight * waterHeightVal;
								}
							}
						}
					}
					p++;
				}
			}
		}
	
		private void WorkThread (System.Object args)
		{
			Thread.CurrentThread.Priority = System.Threading.ThreadPriority.BelowNormal;
			try {
				BitMap1 waterTiles = new BitMap1 (scene, CELL_SIZE, CELL_SIZE);
		
				for (int y = 0; y < CELL_SIZE; y++) {
					int p = (y + offsetY) * scene.width + offsetX;
					for (int x = 0; x < CELL_SIZE; x++) {
						int canal = canals.Get (offsetX + x, offsetY + y);
						int yy = y << 2;
						int yRangeStart = (y > 0) ? -1 : 0;
						float heightVal = heightData [p] * VERTICAL_NORMALIZE;
						float waterHeightVal = waterHeightData [p] * VERTICAL_NORMALIZE;
						bool isWaterVisible = (waterHeightVal >= heightVal);
						if (canal > 0) {
							isWaterVisible = true;
							AddCanal (offsetX, offsetY, x, y, canal, heights, canals);
							waterHeightVal = Mathf.Max (waterHeightVal, heightVal - MIN_CANAL_WATER);
						}
						if (isWaterVisible) {
							int xrMin = Mathf.Max (0, x - 1);
							int yrMin = Mathf.Max (0, y - 1);
							int xrMax = Mathf.Min (TerrainCell.CELL_SIZE - 1, x + 1);
							int yrMax = Mathf.Min (TerrainCell.CELL_SIZE - 1, y + 1);
							
							for (int yr = yrMin; yr <= yrMax; yr++) {
								for (int xr = xrMin; xr <= xrMax; xr++) {
									waterTiles.SetToTrue (xr, yr);
								}
							}
						}
						for (int subY = yRangeStart; subY <= 4; subY++) {
							float weightY = weights [subY + 1];
							int xx = x << 2;
							int xRangeStart = (x > 0) ? -1 : 0;
							for (int subX = xRangeStart; subX <= 4; subX++) {
								float weight = weights [subX + 1] * weightY;
								heights [yy + subY, xx + subX] += weight * heightVal;
								waterHeights [yy + subY, xx + subX] += weight * waterHeightVal;
							}
						}
						p++;
					}
				}
			
				bool hasLeftBorder = (offsetX > 0);
				bool hasRightBorder = (offsetX + CELL_SIZE < totalWidth);
				bool hasBottomBorder = (offsetY > 0);
				bool hasTopBorder = (offsetY + CELL_SIZE < totalHeight);

				int stripHeight = CELL_SIZE + ((hasBottomBorder) ? 1 : 0) + ((hasTopBorder) ? 1 : 0);
				int stripOffsetY = (hasBottomBorder ? -1 : 0);
				if (hasLeftBorder) {
					ProcessStrip (-1, stripOffsetY, 1, stripHeight);
				}
				if (hasRightBorder) {
					ProcessStrip (CELL_SIZE, stripOffsetY, 1, stripHeight);
				}
				if (hasBottomBorder) {
					ProcessStrip (0, -1, CELL_SIZE, 1);
				}
				if (hasTopBorder) {
					ProcessStrip (0, CELL_SIZE, CELL_SIZE, 1);
				}
			
			
				water = new StencilMap (waterHeights, -1);
				waterTiles.ProcessNotZero (MakeWaterMeshFromCoords, null);
			} catch (System.Exception e) {
				Log.LogException (e);
				isError = true;
			}
			isReady = true;
		}	
	}
}