using UnityEngine;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using System.Threading;

namespace Ecosim.Render.BackgroundProcessing
{
	public class ProcessTiles : Process
	{
		public ProcessTiles (Scene scene, TerrainCell cell, float[,] heights, float[,] waterHeights)
		: base(scene, cell)
		{		
			this.heightMap = heights;
			this.waterHeights = waterHeights;
			vegetation = scene.progression.vegetation.data;
			alpha = new float[TerrainCell.CELL_SIZE, TerrainCell.CELL_SIZE, 4];
			trees = new List<TreeInstance> ();
			objectDict = new Dictionary<int, List<CombinedMeshesData>> ();
			stencilMaps = new StencilMap[EcoTerrainElements.self.decals.Length];
			detailMaps = SetupDetailMaps ();
		}

		public readonly float[,] heightMap;
		public readonly float[,] waterHeights;
		private volatile bool isReady = false;
		private volatile bool isError = false;
		private readonly ushort[] vegetation;
		private readonly float[,,] alpha;
		private readonly List<TreeInstance> trees;
		DetailMap[] detailMaps;
		DetailPrototype[] detailPrototypes;
		private const float CANAL_DEPTH = -2f / TerrainMgr.VERTICAL_HEIGHT;
		private const float MIN_CANAL_WATER = 0.25f / TerrainMgr.VERTICAL_HEIGHT;
		private const float VERTICAL_NORMALIZE = 1f / 65535f;

		private readonly Dictionary<int, List<CombinedMeshesData>> objectDict;
		private readonly StencilMap[] stencilMaps;

		
		private class DetailMap
		{
			public int[,] map;
			public DetailPrototype proto;
			public bool isEmpty;
		}
		
		DetailMap[] SetupDetailMaps ()
		{
			DetailPrototype[] details = TerrainMgr.self.baseData.detailPrototypes;
			DetailMap[] detailMaps = new DetailMap[details.Length];
			for (int i = 0; i < details.Length; i++) {
				DetailMap d = new DetailMap ();
				// d.map = new int[size, size];
				d.isEmpty = true;
				d.proto = details [i];
				detailMaps [i] = d;
			}
			return detailMaps;
		}
		
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
		
		public override IEnumerable<bool> TryFinishWork ()
		{
			while (!isReady) {
				yield return false;
			}
			if (isError) {
				throw new EcoException ("Failed to process terrain");
			}
			
			TerrainData terrainData = cell.terrain.terrainData;
			yield return false;
			terrainData.treeInstances = trees.ToArray ();
			yield return false;
			terrainData.SetAlphamaps (0, 0, alpha);
			yield return false;

			terrainData.detailPrototypes = detailPrototypes;
			terrainData.SetDetailResolution (TerrainMgr.CELL_SIZE, 16);
			for (int i = 0; i < detailMaps.Length; i++) {
				terrainData.SetDetailLayer (0, 0, i, detailMaps [i].map);
				yield return false;
			}
			
			yield return false;
			cell.terrain.Flush ();
			yield return false;
			
			int objectCount = 0;
			
			foreach (StencilMap stencil in stencilMaps) {
				if (stencil != null) {
					Mesh[] meshes = stencil.GetMeshes ();
					foreach (Mesh mesh in meshes) {
						GameObject go = new GameObject ("stencil " + stencil.decal.name);
						Transform cot = go.transform;
						cot.parent = cell.transform;
						cot.localPosition = Vector3.zero;
						cot.localRotation = Quaternion.identity;
						cot.localScale = new Vector3 (TerrainMgr.TERRAIN_SCALE, TerrainMgr.VERTICAL_HEIGHT, TerrainMgr.TERRAIN_SCALE);
						go.layer = Layers.L_DECALS;
						MeshFilter filter = go.AddComponent<MeshFilter> ();
						filter.sharedMesh = mesh;
						MeshRenderer render = go.AddComponent<MeshRenderer> ();
						render.sharedMaterial = stencil.decal.material;
						cell.AddObject (go);
						objectCount++;
						if (objectCount % 10 == 0)
							yield return false;
					}
				}
			}
			
			yield return false;
			
			List<Buildings.Building> buildings = scene.buildings.GetBuildingsForCell (cell.cellX, cell.cellY);
			if (buildings != null) 
			{
				Vector3 localOffset = new Vector3 (offsetX * TerrainMgr.TERRAIN_SCALE, 0f, offsetY * TerrainMgr.TERRAIN_SCALE);
				foreach (Buildings.Building building in buildings) 
				{
					if (building.isActive)
					{
						EcoTerrainElements.PrefabContainer prefab = building.prefab;
						CombinedMeshesData cmd = new CombinedMeshesData ();
						cmd.prefab = prefab;
						cmd.pos = building.position - localOffset;
						//Debug.Log("pos = " + cmd.pos + " local offset = " + localOffset + " gridx = " + gridX + " gridy = " + gridY);
						cmd.rotation = building.rotation;
						cmd.scale = building.scale;

						List<CombinedMeshesData> list = null;
						if (!objectDict.TryGetValue (prefab.materialId, out list)) 
						{
							list = new List<CombinedMeshesData> ();
							objectDict.Add (prefab.materialId, list);
						}
						list.Add (cmd);
					}
				}
			}
		
			foreach (KeyValuePair<int, List<CombinedMeshesData>> keyval in objectDict) 
			{
				int count = keyval.Value.Count;
				if (count == 0) {
					Log.LogError ("Shouldn't happen!");
				} else {
					Material baseMaterial = keyval.Value [0].prefab.material;
				
					List<CombineInstance> combineInstances = new List<CombineInstance> ();
					int totalVertices = 0;
					foreach (CombinedMeshesData cmd in keyval.Value) 
					{
						EcoTerrainElements.PrefabContainer prefab = cmd.prefab;
						if (totalVertices + prefab.vertexCount >= 65000) {
							cell.AddObject (MakeGOOfMeshInstances (cell.transform, "objects " + baseMaterial.name,
							combineInstances, baseMaterial, Vector3.zero, Layers.L_TERRAIN));
							combineInstances.Clear ();
							totalVertices = 0;
						}
						Matrix4x4 t = new Matrix4x4 ();
						Vector3 pos = cmd.pos;
						//pos.Scale(scaleVector);
						t.SetTRS (pos, cmd.rotation, cmd.scale);
						CombineInstance i = new CombineInstance ();
						i.mesh = prefab.mesh;
						i.subMeshIndex = 0;
						i.transform = t;
						combineInstances.Add (i);
						totalVertices += prefab.vertexCount;
					}
					cell.AddObject (MakeGOOfMeshInstances (cell.transform, "objects " + baseMaterial.name,
					combineInstances, baseMaterial, Vector3.zero, Layers.L_TERRAIN));
					objectCount++;
					if (objectCount % 10 == 0)
						yield return false;
				}			
			}
			
			yield return true;
		}
		
		private float TreeHeight (float x, float y)
		{
			int cx = (int)(x * 4);
			int cy = (int)(y * 4);
			float xs = (x * 4) - cx;
			float ys = (y * 4) - cy;
			float h00 = heightMap [cy, cx];
			float h01 = heightMap [cy, cx + 1];
			float h10 = heightMap [cy + 1, cx + 1];
			float h11 = heightMap [cy + 1, cx + 1];
		
			return h00 * (1 - xs) * (1 - ys) + h01 * xs * (1 - ys) + h10 * (1 - xs) * ys + h11 * xs * ys;
		}
	
		private void WorkThread (System.Object args)
		{
			Thread.CurrentThread.Priority = System.Threading.ThreadPriority.BelowNormal;
			
			try {
				DetailMap[] tmpDetailMaps = detailMaps;
			
				string xxx = "Array " + alpha;
				for (int y = 0; y < TerrainCell.CELL_SIZE; y++) {
					int p = (y + offsetY) * scene.width + offsetX;
					for (int x = 0; x < TerrainCell.CELL_SIZE; x++) {
						System.Random rnd = new System.Random (p * 55);
						int vegetationInt = vegetation [p++];
						int successionId = (vegetationInt >> VegetationData.SUCCESSION_SHIFT) & VegetationData.SUCCESSION_MASK;
						int vegetationId = (vegetationInt >> VegetationData.VEGETATION_SHIFT) & VegetationData.VEGETATION_MASK;
						int tileId = (vegetationInt >> VegetationData.TILE_SHIFT) & VegetationData.TILE_MASK;
						SuccessionType s = scene.successionTypes [successionId];
						VegetationType v = s.vegetations [vegetationId];
						TileType tile = v.tiles [tileId];
				
						// first handle terrain colour
						try {
						alpha [y, x, 0] = tile.splat0;
						alpha [y, x, 1] = tile.splat1;
						alpha [y, x, 2] = tile.splat2;
						alpha [y, x, 3] = 1f - tile.splat0 - tile.splat1 - tile.splat2;
						} catch (System.Exception e) {
							// testing issue with data corruption
							Debug.Log (xxx + " message " + e.Message);
							Debug.Log ("Error Array " + alpha + " x = " + x + " y = " + y + " this = " + this + " thread " + Thread.CurrentThread);
							Debug.Log ("Dimension 0 " + alpha.GetLength (0));
							Debug.Log ("Dimension 1 " + alpha.GetLength (1));
							Debug.Log ("Dimension 2 " + alpha.GetLength (2));
						}
							
						// place trees
						foreach (TileType.TreeData treeData in tile.trees) {
							TreeInstance ti = new TreeInstance ();
							float tx = treeData.x;
							float ty = treeData.y;
							float rad = treeData.r;
							if (rad > 0f) {
								float angle = (float)rnd.NextDouble () * Mathf.PI * 2;
								rad = rad * (float)rnd.NextDouble ();
								tx += Mathf.Sin (angle) * rad;
								ty += Mathf.Cos (angle) * rad;
							}
							tx = x + Mathf.Clamp (tx, 0f, 0.999f);
							ty = y + Mathf.Clamp (ty, 0f, 0.999f);
							float th = TreeHeight (tx, ty);
							ti.position = new Vector3 (tx / TerrainCell.CELL_SIZE, th, ty / TerrainCell.CELL_SIZE);
							ti.prototypeIndex = treeData.prototypeIndex;
							ti.heightScale = RndUtil.RndRange (ref rnd, treeData.minHeight, treeData.maxHeight);
							ti.widthScale = ti.heightScale * RndUtil.RndRange (ref rnd, treeData.minWidthVariance, treeData.maxWidthVariance);
							Color c = RndUtil.RndRange (ref rnd, treeData.colorFrom, treeData.colorTo);
							ti.color = c;
							ti.lightmapColor = Color.white;
							try {
								trees.Add (ti);
							}
							catch (System.Exception e) {
								// testing issue with data corruption
								Debug.LogException (e);
								Debug.Log ("capacity = " + trees.Capacity + " count = " + trees.Count);
								Debug.Log ("TreeInstance = " + ti);
							}
						}
				
						// details
						for (int i = 0; i < tile.detailCounts.Length; i++) {
							int dc = tile.detailCounts [i];
							if (dc > 0) {
								DetailMap dMap = tmpDetailMaps [i];
								if (dMap.isEmpty) {
									dMap.isEmpty = false;
									dMap.map = new int[TerrainCell.CELL_SIZE, TerrainCell.CELL_SIZE];
								}
								dMap.map [y, x] = dc;
							}	
						}
					
						// objects
						foreach (TileType.ObjectData objData in tile.objects) {
							CombinedMeshesData cm = new CombinedMeshesData ();
							EcoTerrainElements.PrefabContainer pc = EcoTerrainElements.GetTileObjectPrefab (objData.index);
							cm.prefab = pc;
							float tx = objData.x;
							float ty = objData.y;
							float rad = objData.r;
							cm.rotation = Quaternion.Euler (0f, objData.angle, 0f);
							if (rad > 0f) {
								float angle = (float)rnd.NextDouble () * Mathf.PI * 2;
								rad = rad * (float)rnd.NextDouble ();
								tx += Mathf.Sin (angle) * rad;
								ty += Mathf.Cos (angle) * rad;
							}
							tx = x + Mathf.Clamp (tx, 0f, 0.999f);
							ty = y + Mathf.Clamp (ty, 0f, 0.999f);
							float objH = TreeHeight (tx, ty) * TerrainMgr.VERTICAL_HEIGHT;
							cm.pos = new Vector3 (tx * TerrainMgr.TERRAIN_SCALE, objH, ty * TerrainMgr.TERRAIN_SCALE);
							float heightScale = RndUtil.RndRange (ref rnd, objData.minHeight, objData.maxHeight);
							float widthScale = heightScale * RndUtil.RndRange (ref rnd, objData.minWidthVariance, objData.maxWidthVariance);
							cm.scale = new Vector3 (widthScale, heightScale, widthScale);
							List<CombinedMeshesData> gos;
							if (objectDict.TryGetValue (pc.materialId, out gos)) {
								gos.Add (cm);
							} else {
								gos = new List<CombinedMeshesData> ();
								gos.Add (cm);
								objectDict.Add (pc.materialId, gos);
							}
						}
						// stencils/decals
						foreach (int decalID in tile.decals) {
							StencilMap stencilMap = stencilMaps [decalID];
							if (stencilMap == null) {
								// we don't have a map for this decal type, create one...
								EcoTerrainElements.DecalPrototype decal = EcoTerrainElements.GetDecal (decalID);
								stencilMap = new StencilMap ((decal.useWaterHeights) ? waterHeights : heightMap, decalID);
								stencilMaps [decalID] = stencilMap;
							}
							stencilMap.AddTile (x, y);
						}
					}
				}
		
				// make new detailmap array with only details that have non-empty map...
				List<DetailMap> nonEmptyDetails = new List<DetailMap> ();
				List<DetailPrototype> nonEmptyPrototypes = new List<DetailPrototype> ();
				foreach (DetailMap map in tmpDetailMaps) {
					if (!map.isEmpty) {
						nonEmptyDetails.Add (map);
						nonEmptyPrototypes.Add (map.proto);
					}
				}
				this.detailMaps = nonEmptyDetails.ToArray ();
				detailPrototypes = nonEmptyPrototypes.ToArray ();
			} catch (System.Exception e) {
				Log.LogException (e);
				isError = true;
			}
			isReady = true;
		}

		private static GameObject MakeGOOfMeshInstances (Transform parent, string name, List<CombineInstance> combineInstances, Material mat, Vector3 localPos, int layer)
		{
			GameObject go = new GameObject (name);
			Transform cot = go.transform;
			cot.parent = parent;
			cot.localPosition = localPos;
			cot.localRotation = Quaternion.identity;
			cot.localScale = Vector3.one;
			go.layer = layer;
			MeshFilter filter = go.AddComponent<MeshFilter> ();
			Mesh comesh = new Mesh ();
			comesh.CombineMeshes (combineInstances.ToArray (), true, true);
			filter.sharedMesh = comesh;
			MeshRenderer render = go.AddComponent<MeshRenderer> ();
			render.sharedMaterial = mat;
			return go;
		}
	}
}