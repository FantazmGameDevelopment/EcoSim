using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim;
	
public class RenderTileIcons : MonoBehaviour {

	private class DetailMap {
		public int[,] map;
		public DetailPrototype proto;
		public bool isEmpty;
	}
		
	public struct RenderSettings {
		public RenderSettings(float angleH, float angleV, float distance, float offsetV) {
			this.angleH = angleH;
			this.angleV = angleV;
			this.distance = distance;
			this.offsetV = offsetV;
			
			emptySurroundings = true;
		}
		public float angleH;
		public float angleV;
		public float distance;
		public float offsetV;		
		public bool emptySurroundings;
	}
	
	public static RenderTileIcons self;
	
	public Texture2D placeholderIcon;
	public Camera renderCamera;
	
	public UnityEngine.TerrainData originalTerrainData;
	
	UnityEngine.TerrainData data;
	
	public const int TERRAIN_SIZE = 32;
	public const float TERRAIN_SCALE = 20f;
	
	RenderSettings defaultSettings = new RenderSettings(60f, 30f, 30f, 4f);
	private DetailMap[] detailMaps;
	List<GameObject> objects = new List<GameObject>();
	
	// used to check if the tile layout has changed or not.
	private long renderId = 1;
	
	RenderTileIconStencil[] stencils;
	
	/**
	 * Adds tile to icon generation queue, returns placeholder icon to show till the icon is generated
	 */
	public static Texture2D RenderTile(TileType tile) {
		if (self == null) return null;
		QueueEntry entry = new QueueEntry();
		entry.tile = tile;
		if (self.tail != null) {
			self.tail.next = entry;
		}
		if (self.head == null) {
			self.head = entry;
		}
		self.tail = entry;
		return self.placeholderIcon;
	}
	
	class QueueEntry {
		public TileType tile;
		public QueueEntry next;
	}
	
	QueueEntry head;
	QueueEntry tail;
	
	void Awake() {
		self = this;
	}
	
	void OnDestroy() {
		self = null;
	}

	public void SetupDetailMap(UnityEngine.TerrainData data) {
		DetailPrototype[] details = data.detailPrototypes;
		detailMaps = new DetailMap[details.Length];
		for (int i = 0; i < details.Length; i++) {
			DetailMap d = new DetailMap();
			d.map = new int[TERRAIN_SIZE, TERRAIN_SIZE];
			d.proto = details[i];
			detailMaps[i] = d;
		}
	}
	
	void Start() {
//		UnityEngine.TerrainData originalTerrainData = GameObject.Find ("Terrain").GetComponent<Terrain>().terrainData;
//		Terrain terrain = gameObject.AddComponent<Terrain>();
		data = new UnityEngine.TerrainData();
		data.baseMapResolution = TERRAIN_SIZE;
		data.alphamapResolution = TERRAIN_SIZE;
		data.SetDetailResolution(TERRAIN_SIZE, 64);
		data.heightmapResolution = TERRAIN_SIZE;
		data.size = new Vector3(TERRAIN_SIZE * TERRAIN_SCALE, 100f, TERRAIN_SIZE * TERRAIN_SCALE);
//		terrain.terrainData = data;
		data.splatPrototypes = originalTerrainData.splatPrototypes;
		data.treePrototypes = originalTerrainData.treePrototypes;
		data.detailPrototypes = originalTerrainData.detailPrototypes;
		GameObject terrainGO = Terrain.CreateTerrainGameObject(data);
		terrainGO.transform.parent = transform;
		terrainGO.transform.localPosition = Vector3.zero;
		terrainGO.transform.localRotation = Quaternion.identity;
		terrainGO.transform.localScale = Vector3.one;
		terrainGO.layer = Layers.L_GUI;
		SetupDetailMap(data);
//		TerrainCollider collider = gameObject.AddComponent<TerrainCollider>();
//		collider.terrainData = data;
		StartCoroutine(COHandleThumbnails());
		stencils = new RenderTileIconStencil[EcoTerrainElements.self.decals.Length];
	}
	
	
	IEnumerator COHandleThumbnails() {
		yield return new WaitForSeconds(1f);
		while (true) {
			if (head != null) {
				TileType tt = head.tile;
//				TileType surroundings = tt.vegetationType.tiles[0];
				Texture2D icon = new Texture2D(64, 64, TextureFormat.RGB24, false, false);
				Render(defaultSettings, ref icon, tt);
				tt.SetIcon(icon);
				head = head.next;
				if (head == null) tail = null;
				yield return new WaitForSeconds(0.1f);
			}
			else {
				yield return new WaitForSeconds(0.5f);
			}
		}
	}

	public long Render(RenderSettings settings, ref Texture2D resultTex, TileType tile, Mesh mesh, Material material, GameObject road) {
		return ReRender(settings, 0L, ref resultTex, tile, mesh, material, road);
	}
	
	public long Render(RenderSettings settings, ref Texture2D resultTex, TileType tile, Mesh mesh, Material material) {
		return ReRender(settings, 0L, ref resultTex, tile, mesh, material, null);
	}

	public long Render(RenderSettings settings, ref Texture2D resultTex, TileType tile) {
		return ReRender(settings, 0L, ref resultTex, tile, null, null, null);
	}
		
	public long ReRender(RenderSettings settings, long currentId, ref Texture2D resultTex, TileType tile) {
		return ReRender(settings, currentId, ref resultTex, tile, null, null, null);
	}
	
	public long ReRender(RenderSettings settings, long currentId, ref Texture2D resultTex, TileType tile, Mesh mesh, Material material) {
		return ReRender(settings, 0L, ref resultTex, tile, mesh, material, null);
	}
	
	public long ReRender(RenderSettings settings, long currentId, ref Texture2D resultTex, TileType tile, Mesh mesh, Material material,
		GameObject road) {
		if (currentId != renderId) {
			TileType surroundings = (settings.emptySurroundings)?(tile.vegetationType.tiles[0]):tile;
			// first generate the terrain...
			Generate(tile, surroundings);
			if (mesh != null) {
				// we have an object to show on terrain (something like a building)
				GameObject go = new GameObject("extra");
				go.transform.parent = transform;
				go.transform.localPosition = new Vector3 ((TERRAIN_SIZE / 2) * TERRAIN_SCALE, 0f, (TERRAIN_SIZE / 2) * TERRAIN_SCALE);
				go.transform.localRotation = Quaternion.identity;
				go.transform.localScale = Vector3.one;
				go.AddComponent<MeshFilter>().sharedMesh = mesh;
				go.AddComponent<MeshRenderer>().sharedMaterial = material;
				objects.Add(go);
			}
			if (road != null) {
				GameObject go = (GameObject) Instantiate (road);
				go.transform.parent = transform;
				go.transform.localPosition = new Vector3 ((TERRAIN_SIZE / 2) * TERRAIN_SCALE, 0f, (TERRAIN_SIZE / 2) * TERRAIN_SCALE);
				go.transform.localRotation = Quaternion.identity;
				go.transform.localScale = Vector3.one;
				RoadInstance ri = go.GetComponent <RoadInstance> ();
				Roads.Road rdata = new Roads.Road ();
				rdata.points = new List<Vector3> ();
				rdata.points.Add (new Vector3 (-(TERRAIN_SIZE / 2) * TERRAIN_SCALE, 0f, 0f));
				rdata.points.Add (new Vector3 (0, 0, 0));
				rdata.points.Add (new Vector3 ((TERRAIN_SIZE / 2) * TERRAIN_SCALE, 0f, (TERRAIN_SIZE / 2) * TERRAIN_SCALE));
				ri.Setup (rdata);
				objects.Add(go);
			}
			renderId++;
		}
		
		float offset = (0.5f + TERRAIN_SCALE) * TERRAIN_SIZE / 2;
		Transform cameraT = renderCamera.transform;
		float distH = Mathf.Cos(Mathf.Deg2Rad * settings.angleV) * settings.distance;
		float distV = Mathf.Sin(Mathf.Deg2Rad * settings.angleV) * settings.distance;
		cameraT.localPosition = new Vector3(offset +
			distH * Mathf.Cos(Mathf.Deg2Rad * settings.angleH),
			distV + settings.offsetV,
			offset + distH * Mathf.Sin(Mathf.Deg2Rad * settings.angleH)
			);
		
		Vector3 centreP = transform.position + new Vector3(offset, 0f, offset);
		cameraT.LookAt(centreP + new Vector3(0f, settings.offsetV, 0f), Vector3.up);
		
		if ((resultTex == null) || (resultTex.format != TextureFormat.RGB24)) {
			if (resultTex != null) {
				Destroy(resultTex);
			}
			resultTex = new Texture2D(128, 128, TextureFormat.RGB24, false);
		}
		RenderTexture rt =  RenderTexture.GetTemporary(resultTex.width, resultTex.height, 24, RenderTextureFormat.ARGB32);
		rt.useMipMap = false;
		rt.wrapMode = TextureWrapMode.Clamp;
		renderCamera.targetTexture = rt;
		renderCamera.Render();
		RenderTexture.active = rt;
		resultTex.ReadPixels(new Rect(0, 0, resultTex.width, resultTex.height), 0, 0, false);
		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary(rt);
		resultTex.Apply();
		Resources.UnloadUnusedAssets();
		System.GC.Collect();
		return renderId;
	}
	
	void Generate(TileType centre, TileType surroundings) {
		int centrePos = TERRAIN_SIZE / 2;
		
		// first remove old objects from previous generates...
		foreach (GameObject obj in objects) {
			if (obj) DestroyImmediate(obj);
		}
		objects.Clear();
		
		// remove old stencils
		for (int i = 0; i < stencils.Length; i++) {
			if (stencils[i] != null) {
				RenderTileIconStencil.DestroyStencil(stencils[i]);
				stencils[i] = null;
			}
		}
		
		foreach (DetailMap d in detailMaps) {
			if (!d.isEmpty) {
				d.isEmpty = true;
				d.map = new int[TERRAIN_SIZE, TERRAIN_SIZE];
			}
		}
		

		// alpha map (terrain ground colours)
		float[,,] alpha = new float[TERRAIN_SIZE, TERRAIN_SIZE, 4];
		List<TreeInstance> treeList = new List<TreeInstance>();
		GameObject[] prefabs = EcoTerrainElements.self.tileObjects;
		
		int randomSeed = 1;
		bool noRandom = false;
		
		for (int y = 0; y < TERRAIN_SIZE; y++) {
			for (int x = 0; x < TERRAIN_SIZE; x++) {
				
				System.Random rnd = new System.Random((randomSeed + x + 3333 * y) | x | y);
				TileType tile = ((x == centrePos) && (y == centrePos))?centre:surroundings;
				
				// first handle terrain colour
				alpha[y, x, 0] = tile.splat0;
				alpha[y, x, 1] = tile.splat1;
				alpha[y, x, 2] = tile.splat2;
				alpha[y, x, 3] = 1f - tile.splat0 - tile.splat1 - tile.splat2;
				
				// place trees
				foreach (TileType.TreeData treeData in tile.trees) {
					TreeInstance ti = new TreeInstance();
					float tx = treeData.x;
					float ty = treeData.y;
					float rad = treeData.r;
					if ((rad > 0f) && (!noRandom)) {
						float angle = (float) rnd.NextDouble() * Mathf.PI * 2;
						rad = rad * (float) rnd.NextDouble();
						tx += Mathf.Sin(angle) * rad;
						ty += Mathf.Cos(angle) * rad;
					}
					// ti.position = TreePos(ref heightMap, x + Mathf.Clamp(tx, 0f, 0.999f), y + Mathf.Clamp(ty, 0f, 0.999f));
					tx = x + Mathf.Clamp(tx, 0f, 0.999f);
					ty = y + Mathf.Clamp(ty, 0f, 0.999f);
					ti.position = new Vector3(tx / TERRAIN_SIZE, 0f, ty / TERRAIN_SIZE);
					ti.prototypeIndex = treeData.prototypeIndex;
					if (noRandom) {
						ti.heightScale = 0.5f * (treeData.minHeight + treeData.maxHeight);
						ti.widthScale = ti.heightScale * 0.5f * (treeData.minWidthVariance + treeData.maxWidthVariance);
						Color c = treeData.colorTo;
						ti.color = c;
					}
					else {
						ti.heightScale = RndUtil.RndRange(ref rnd, treeData.minHeight, treeData.maxHeight);
						ti.widthScale = ti.heightScale * RndUtil.RndRange(ref rnd, treeData.minWidthVariance, treeData.maxWidthVariance);
						Color c = RndUtil.RndRange(ref rnd, treeData.colorFrom, treeData.colorTo);
						ti.color = c;
					}
					ti.lightmapColor = Color.white;
					treeList.Add(ti);
				}
				
				// objects
				foreach (TileType.ObjectData objData in tile.objects) {
					GameObject go = (GameObject) GameObject.Instantiate(prefabs[objData.index]);
					go.layer = Layers.L_GUI;
					Transform t = go.transform;
					t.parent = transform;
					float tx = objData.x;
					float ty = objData.y;
					float rad = objData.r;
					t.localRotation = Quaternion.Euler(0f, objData.angle, 0f);
					if ((rad > 0f) && (!noRandom)) {
						float angle = (float) rnd.NextDouble() * Mathf.PI * 2;
						rad = rad * (float) rnd.NextDouble();
						tx += Mathf.Sin(angle) * rad;
						ty += Mathf.Cos(angle) * rad;
					}
					tx = x + Mathf.Clamp(tx, 0f, 0.999f);
					ty = y + Mathf.Clamp(ty, 0f, 0.999f);
					t.localPosition = new Vector3(tx * TERRAIN_SCALE, 0f, ty * TERRAIN_SCALE);
					if (noRandom) {
						float heightScale = 0.5f * (objData.minHeight + objData.maxHeight);
						float widthScale = heightScale * 0.5f * (objData.minWidthVariance + objData.maxWidthVariance);
						t.localScale = new Vector3(widthScale, heightScale, widthScale);
					}
					else {
						float heightScale = RndUtil.RndRange(ref rnd, objData.minHeight, objData.maxHeight);
						float widthScale = heightScale * RndUtil.RndRange(ref rnd, objData.minWidthVariance, objData.maxWidthVariance);
						t.localScale = new Vector3(widthScale, heightScale, widthScale);
					}
					objects.Add(go);
				}
				
				
				// detail
				for (int i = 0; i < tile.detailCounts.Length; i++) {
					int dc = tile.detailCounts[i];
					if (dc > 0) {
						DetailMap dMap = detailMaps[i];
						if (dMap.isEmpty) {
							dMap.isEmpty = false;
						}
						dMap.map[y, x] = dc;
					}
				}	
				
				for (int i = 0; i < tile.decals.Length; i++) {
					int id = tile.decals[i];
					RenderTileIconStencil stencil = stencils[id];
					if (stencil == null) {
						stencil = RenderTileIconStencil.CreateStencil(transform, id);
						stencils[id] = stencil;
					}
					stencil.AddTile(x, y);
				}
			}
		}
		data.SetAlphamaps(0, 0, alpha);
		data.treeInstances = treeList.ToArray();

		List<DetailPrototype> detailPrototypes = new List<DetailPrototype>();
		foreach (DetailMap d in detailMaps) {
			if (!d.isEmpty) {
				detailPrototypes.Add(d.proto);
			}
		}
		data.SetDetailResolution(TERRAIN_SIZE, 32);
		data.detailPrototypes = detailPrototypes.ToArray();
		int j = 0;
		foreach (DetailMap d in detailMaps) {
			if (!d.isEmpty) {
				data.SetDetailLayer(0, 0, j++, d.map);
			}
		}
		for (int i = 0; i < stencils.Length; i++) {
			if (stencils[i] != null) {
				stencils[i].GenerateMesh();
			}
		}
	}
}
