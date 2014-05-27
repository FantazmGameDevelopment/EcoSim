using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.Render.BackgroundProcessing;

/**
 * A cell is square of tiles, currently configured to be 256x256 tiles.
 * The TerrainCell class handles the rendering of this square of tiles, using a Unity Terrain object as base.
 */
public class TerrainCell : MonoBehaviour
{
	public bool mustDestroy = false;
	public bool isUpdating = false;
	public bool isVisible = false;
	public Terrain terrain;
	public TerrainMgr mgr;
	public const int CELL_SIZE = TerrainMgr.CELL_SIZE;
	private const float VERTICAL_HEIGHT = TerrainMgr.VERTICAL_HEIGHT;
	private const float VERTICAL_NORMALIZE = 1f / 65535f;
	private const float TERRAIN_SCALE = TerrainMgr.TERRAIN_SCALE;
	public int cellX = 0;
	public int cellY = 0;
	private int qualitySettings = -2; // number not used for actual settings
	private Scene scene;
	public float[,] heights; // convienient to have access to, but will only be valid when rendering is complete.
	public float[,] waterHeights; // convienient to have access to, but will only be valid when rendering is complete.
	
	string CellName ()
	{
		return "Tile " + cellX + "x" + cellY;
	}
	
	private void SetTerrainQuality (Terrain terrain, int ql)
	{
		terrain.treeDistance = 20000f;
		switch (ql) {
		case -1 : // Special for rendering overview tiles
			terrain.basemapDistance = 10000;
			terrain.treeBillboardDistance = 0;
			terrain.treeMaximumFullLODCount = 0;
			terrain.heightmapPixelError = 1.0f;
			terrain.detailObjectDensity = 1f;
			terrain.detailObjectDistance = 0f;
			break;
		case 0 : // Fastest
			terrain.basemapDistance = 250;
			terrain.treeBillboardDistance = 0;
			terrain.treeMaximumFullLODCount = 0;
			terrain.heightmapPixelError = 5.0f;
			terrain.detailObjectDensity = 0.5f;
			terrain.detailObjectDistance = 100f;
			break;
		case 1 : // Fast
			terrain.basemapDistance = 500;
			terrain.treeBillboardDistance = 200;
			terrain.treeMaximumFullLODCount = 4;
			terrain.heightmapPixelError = 2.0f;
			terrain.detailObjectDensity = 0.75f;
			terrain.detailObjectDistance = 200f;
			break;
		case 2 : // Simple
			terrain.basemapDistance = 1000;
			terrain.treeBillboardDistance = 250;
			terrain.treeMaximumFullLODCount = 20;
			terrain.heightmapPixelError = 1.0f;
			terrain.detailObjectDensity = 1f;
			terrain.detailObjectDistance = 250f;
			break;
		case 3 : // Good
			terrain.basemapDistance = 2000;
			terrain.treeBillboardDistance = 300;
			terrain.treeMaximumFullLODCount = 100;
			terrain.heightmapPixelError = 1.0f;
			terrain.detailObjectDensity = 1f;
			terrain.detailObjectDistance = 250f;
			break;
		case 4 : // Beautiful
			terrain.basemapDistance = 2000;
			terrain.treeBillboardDistance = 500;
			terrain.treeMaximumFullLODCount = 300;
			terrain.heightmapPixelError = 1.0f;
			terrain.detailObjectDensity = 1f;
			terrain.detailObjectDistance = 250f;
			break;
		default : //.Fantastic
			terrain.basemapDistance = 2000;
			terrain.treeBillboardDistance = 500;
			terrain.treeMaximumFullLODCount = 300;
			terrain.heightmapPixelError = 1.0f;
			terrain.detailObjectDensity = 1f;
			terrain.detailObjectDistance = 250f;
			break;
		}
		qualitySettings = ql;
	}
	
	public static TerrainCell CreateCell (TerrainMgr mgr)
	{
		TerrainData src = mgr.baseData;
		TerrainData data = new TerrainData ();
		data.heightmapResolution = CELL_SIZE * 4 + 1;
		data.alphamapResolution = CELL_SIZE;
		data.baseMapResolution = CELL_SIZE;
		data.size = new Vector3 (CELL_SIZE * TERRAIN_SCALE, VERTICAL_HEIGHT, CELL_SIZE * TERRAIN_SCALE);
		data.splatPrototypes = src.splatPrototypes;
//		data.detailPrototypes = src.detailPrototypes;
		data.treePrototypes = src.treePrototypes;
		data.wavingGrassAmount = 0.1f;
		
//		Debug.Log("scale = "  + data.heightmapScale);
		GameObject go = Terrain.CreateTerrainGameObject (data);
		TerrainCell cell = go.AddComponent<TerrainCell> ();
		cell.mgr = mgr;
		cell.scene = mgr.scene;
		
		cell.terrain = go.GetComponent<Terrain> ();
		cell.SetTerrainQuality (cell.terrain, mgr.qualitySettings);
		go.layer = Layers.L_TERRAIN;
		cell.terrain.enabled = false;
		go.GetComponent<TerrainCollider> ().enabled = false;
		return cell;
	}
	
	public void UpdateQualitySettings ()
	{
		SetTerrainQuality (terrain, mgr.qualitySettings);
	}
	
	private List<GameObject> objects; // keeps tracks of all objects connected to this terrain (houses, tile objects, decals, ...)
	
	/**
	 * Destroy all objects connected to this terrain
	 */
	void DestroyObjects ()
	{
		if (objects == null) {
			objects = new List<GameObject> ();
		} else {
			foreach (GameObject go in objects) {
				if (go) {
					DestroyImmediate (go.GetComponent<MeshFilter> ().sharedMesh);
					Destroy (go);
				}
			}
			objects.Clear ();
		}
	}
	
	public void AddObject (GameObject go)
	{
		if (objects == null) {
			objects = new List<GameObject> ();
		}
		objects.Add (go);
	}
	
	
	/**
	 * Makes cell visible, cell is already at correct position and rendered, only
	 * currently hidden, so unhide it.
	 */
	public void MakeVisible ()
	{
		if (!isUpdating) {
			terrain.enabled = true;
			GetComponent<TerrainCollider> ().enabled = true;
			if (mgr.qualitySettings != qualitySettings) {
				UpdateQualitySettings ();
			}
			mgr.CellIsVisible (cellX, cellY);
			gameObject.name = CellName ();
			foreach (GameObject go in objects) {
				if (go) {
					go.SetActive (true);
				}
			}
			isVisible = true;
		}
	}
	
	/**
	 * Makes cell invisible, keep it alive though so it can easily switched back on again
	 */
	public void MakeInvisible ()
	{
		terrain.enabled = false;
		GetComponent<TerrainCollider> ().enabled = false;
		foreach (GameObject go in objects) {
			if (go) {
				go.SetActive (false);
			}
		}
		gameObject.name = CellName () + " Hidden";
		isVisible = false;
	}
	
	public void MarkUnused ()
	{
		terrain.enabled = false;
		GetComponent<TerrainCollider> ().enabled = false;
		// claim back some memory by deleting detail prototype maps
		terrain.terrainData.detailPrototypes = new DetailPrototype[0];
		gameObject.name = "Tile Unused";
		DestroyObjects ();
		waterHeights = null;
		heights = null;
	}
	
	/**
	 * Render cell at position x, y. Cell MUST be build up again, even if position hasn't changed
	 * if cell already exists, is correct but just hidden, use MakeVisible
	 */
	public void ActivateAt (int x, int y)
	{
		// first hide cell
		GetComponent<Terrain> ().enabled = false;
		GetComponent<TerrainCollider> ().enabled = false;
		
		if (mgr.scene != scene) {
			scene = mgr.scene;
		}
		if (isUpdating) {
			Debug.LogError ("Trying to update a busy tile..." + name);
			return;
		}
		if (mgr.qualitySettings != qualitySettings) {
			UpdateQualitySettings ();
		}
		mgr.StartCoroutine (CORender (x, y));
	}

	
	/**
	 * Destroys tile.
	 * markReady, if true the tile is first marked as not being updated anymore
	 */
	public void DestroyCell (bool markReady)
	{
		if ((markReady) && isUpdating) {
			isUpdating = false;
			mgr.renderCount--;
		}
		mustDestroy = true;
		if (isUpdating) {
			return;
		}
		if (objects != null) {
			foreach (GameObject go in objects) {
				if (go) {
					DestroyImmediate (go);
				}
			}
		}
		heights = null;
		waterHeights = null;
		Destroy (gameObject); // really destroy instance
	}
	
	IEnumerator CORender (int gridX, int gridY)
	{
		mgr.renderCount++;
		isVisible = true;
		cellX = gridX;
		cellY = gridY;
		gameObject.name = CellName () + " Rendering";
		terrain.enabled = false;
		GetComponent<TerrainCollider> ().enabled = false;
		isUpdating = true;
		
		yield return 0;
		
		TerrainData terrainData = terrain.terrainData;
		
		Vector3 terrainSize = terrainData.size;
		terrain.transform.localPosition = new Vector3 (
			((float)gridX - mgr.referencePos.x) * terrainSize.x,
			mgr.referencePos.y,
			((float)gridY - mgr.referencePos.z) * terrainSize.z);


		yield return 0;
		if (mustDestroy) {
			DestroyCell (true);
			yield break;
		}
		
		// delete old objects...
		DestroyObjects ();
		
		// first create heightmap
		ProcessHeightData processHeightData = new ProcessHeightData (scene, this);
		processHeightData.StartWork ();
		
		// keep waiting until processing is done
		// this is basically a coroutine that yields until finished, important to yield here to wait for next frame before rechecking
		foreach (bool status in processHeightData.TryFinishWork()) {
			if (!status) yield return 0;
		}
		
		heights = processHeightData.heights;
		waterHeights = processHeightData.waterHeights;
		
		yield return 0;
		if (mustDestroy) {
			DestroyCell (true);
			yield break;
		}
				
		ProcessTiles processBasicTileInfo = new ProcessTiles (scene, this, heights, waterHeights);
		processBasicTileInfo.StartWork ();
		
		// keep waiting until processing is done
		// this is basically a coroutine that yields until finished, important to yield here to wait for next frame before rechecking
		foreach (bool status in processBasicTileInfo.TryFinishWork()) {
			if (!status) yield return 0;
		}
		
		terrain.Flush ();
		yield return 0;
		if (mustDestroy) {
			DestroyCell (true);
			yield break;
		}
				
		isUpdating = false;
		mgr.renderCount--;
		
		if (isVisible) {
			MakeVisible ();
		} else {
			MakeInvisible ();
		}
		yield return 0;
		System.GC.Collect ();
		Resources.UnloadUnusedAssets ();
	}
}
