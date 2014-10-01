using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.Render;

public class TerrainMgr : MonoBehaviour
{
	
	public const int CELL_SIZE = 128;
	public const float VERTICAL_HEIGHT = HeightMap.VERTICAL_HEIGHT; // makes one unit == 0.01 meter
	public const float HEIGHT_PER_UNIT = HeightMap.HEIGHT_PER_UNIT; // one unit is 0.01 meter
	public const float TERRAIN_SCALE = 20.0f;
	public const int CACHE_SIZE = 5;
	public int range = 1;
	public TerrainData baseData;
	public Material waterMaterial;
	public Transform follow;
	public Vector3 followPosition;
	public Vector3 referencePos;
	public int tx = 0;
	public int ty = 0;
	public Scene scene;
	public static TerrainMgr self;
	private Dictionary<int, TerrainCell> renderingTiles;
	private Dictionary<int, TerrainCell> activeTiles;
	private List<NotifyTerrainChange> notifyList = new List<NotifyTerrainChange> ();
	private GameObject[,] overviewTiles;
	public Mesh overviewTileMesh;
	public Material overviewTileMaterial;
	public Texture2D overviewTilePlaceholderTex;
	public bool disableRendering = false;
	public int qualitySettings = -1;
	private bool isUpdatingMap = false;
	private TerrainCell[,] cellGrid;
	private List<TerrainCell> cache;
	public int cellCount = 0; // total #cells
	public int renderCount = 0; // cells that are currently rendering
	
	/**
	 * Gets a cell from the cache, or if cache is empty, a cell in
	 * the grid of cells that is invisible. If no cells are available
	 * returns null.
	 */
	private TerrainCell FindUnusedCell ()
	{
		if (cache.Count > 0) {
			TerrainCell result = cache [cache.Count - 1];
			cache.RemoveAt (cache.Count - 1);
			return result;
		}
		foreach (TerrainCell cell in cellGrid) {
			if ((cell != null) && (!cell.isVisible) && (!cell.isUpdating)) {
				cellGrid [cell.cellY, cell.cellX] = null;
				cell.MarkUnused ();
				return cell;
			}
		}
		return null;
	}
	
	private TerrainCell CreateCellAt (int cx, int cy)
	{
		if (!IsInRange (cx, cy))
			return null;
		
		if ((cache.Count > 0) || (cellCount >= CACHE_SIZE)) {
			TerrainCell cell = FindUnusedCell ();
			if (cell != null) {
				cellGrid [cy, cx] = cell;
				cell.ActivateAt (cx, cy);
				return cell;
			}
		}
		TerrainCell cell2 = TerrainCell.CreateCell (this);
		cell2.ActivateAt (cx, cy);
		cellCount ++;
		cellGrid [cy, cx] = cell2;
		return cell2;
		
	}

	public void CheckCellsForOverviewRendering ()
	{
		if (cache.Count == 0) {
			for (int y = 0; y < cellGrid.GetLength (0); y++) {
				for (int x = 0; x < cellGrid.GetLength (1); x++) {
					if (cellGrid [y, x] == null) {
						CreateCellAt (x, y);
					}
				}
			}
		}
	}

	public void DestroyCellAt (int cx, int cy)
	{
		if (!IsInRange (cx, cy))
			return;
		TerrainCell cell = cellGrid [cy, cx];
		if (cell != null) {
			HideCellAt (cx, cy);
			if (cell.isUpdating) {
				// cell must be destroyed but can't be done immediately as it is
				// rendering (and using threads that need to finish first)
				cell.DestroyCell (false);
				cellCount --; // one less cell so lower count
				cellGrid [cy, cx] = null;
			} else {
				// we can reuse the cell later
				cellGrid [cy, cx] = null;
				cell.MarkUnused ();
				cache.Add (cell);
			}
		}
	}
	
	public void ShowCellAt (int cx, int cy)
	{
		if (!IsInRange (cx, cy))
			return;
		if (cellGrid [cy, cx] != null) {
			if (!(cellGrid [cy, cx].isVisible)) {
				cellGrid [cy, cx].MakeVisible ();
			}
		} else {
			CreateCellAt (cx, cy);
		}
	}
	
	public void HideCellAt (int cx, int cy)
	{
		if (!IsInRange (cx, cy))
			return;
		if (cellGrid [cy, cx] != null) {
			if (cellGrid [cy, cx].isVisible) {
				cellGrid [cy, cx].MakeInvisible ();
				foreach (NotifyTerrainChange c in notifyList) {
					c.CellChangedToInvisible (cx, cy);
				}
			}
		}
	}
	
	public void CellIsVisible (int cx, int cy)
	{
		TerrainCell cell = cellGrid [cy, cx];
		if (cell == null) {
			Debug.LogError ("Cell marked visible, but not found in grid " + cx + " " + cy);
		} else {
			foreach (NotifyTerrainChange c in notifyList) {
				c.CellChangedToVisible (cx, cy, cell);
			}
		}
	}
	
	/**
	 * converts screenPos to position on terrain, returning value into terrainCoord. If screenPos is not
	 * over terrain it will return false, else true. The calculation is done with a raycast so if part
	 * of the terrain isn't currently drawn points over it will result in false.
	 */
	public static bool TryScreenToTerrainCoord (Vector3 screenPos, out Vector3 terrainCoord)
	{
		Ray screenRay = Camera.main.ScreenPointToRay (screenPos);
		RaycastHit hit;
		if (Physics.Raycast (screenRay, out hit, 10000f, Layers.M_TERRAIN)) {
			terrainCoord = hit.point;
			return true;
		}
		terrainCoord = Vector3.zero;
		return false;
	}

	/**
	 * Calculates height by using raycast from pos down (pos.y will be set first high enough to be above terrain).
	 */
	public static bool TryGetTerrainHeight (Vector3 pos, out float height)
	{
		pos.y = 2000f;
		Ray ray = new Ray (pos, Vector3.down);
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, 10000f, Layers.M_TERRAIN)) {
			height = hit.point.y;
			return true;
		}
		height = 0f;
		return false;
	}
	
	
	/**
	 * Adds a listener for changes in terrain tiles. After adding the listener method CellChangedToVisible will
	 * be called immediately for all cells that are already visible
	 */
	public static void AddListener (NotifyTerrainChange receiver)
	{
		if (self != null) {
			self.notifyList.Add (receiver);
			if (self.scene != null) {
				// tell receiver which cells are currently visible.
				foreach (TerrainCell cell in self.cellGrid) {
					if ((cell != null) && (cell.isVisible) && (!cell.isUpdating)) {
						receiver.CellChangedToVisible (cell.cellX, cell.cellY, cell);
					}
				}
			}
		}
	}
	
	/**
	 * Removes a listener for terrain cell changes
	 */
	public static void RemoveListener (NotifyTerrainChange receiver)
	{
		if (self != null) {
			if (self.notifyList.Contains (receiver)) {
				self.notifyList.Remove (receiver);
			}
		}
	}
	
	/**
	 * Makes an cx, cy tile coordinate into one int.
	 * tile coordinates are limited to be 0 and 2^15.
	 */
	public static int CoordToKey (int cx, int cy)
	{
		return cx | (cy << 16);
	}
	
	/**
	 * Returns true if still rendering
	 */
	public static bool IsRendering {
		get {
			return self.isUpdatingMap || (self.renderCount > 0);
		}
	}
	
	void Awake ()
	{
		cache = new List<TerrainCell> ();
		qualitySettings = QualitySettings.GetQualityLevel ();
		self = this;
	}
	
	void OnDestroy ()
	{
		self = null;
	}
	
	/**
	 * Recalculates quality settings on all terrain cells
	 */
	public void UpdateQualitySettings ()
	{
		qualitySettings = QualitySettings.GetQualityLevel ();
		UpdateQualitySettings (qualitySettings);
	}
	
	/**
	 * Recalculates quality settings on all terrain cells
	 * level is quality level, value -1 is special level for updating overview tiles
	 */
	public void UpdateQualitySettings (int level)
	{
		qualitySettings = level;
		if (cellGrid != null) {
			foreach (TerrainCell cell in cellGrid) {
				if (cell != null) {
					cell.UpdateQualitySettings ();
				}
			}
		}
	}
	
	/**
	 * Must be called after ascene is loaded, setup terrain
	 */
	public void SetupTerrain (Scene scene)
	{
		StopAllCoroutines ();
		this.scene = scene;
		
		cache.Clear ();
		if (cellGrid != null) {
			foreach (TerrainCell cell in cellGrid) {
				if ((cache.Count < CACHE_SIZE) && (cell != null) && (!cell.isUpdating)) {
					cell.MarkUnused ();
					cache.Add (cell);
				} else {
					if (cell != null) {
						cell.DestroyCell (false);
					}
				}
			}
		}
		if (scene != null) {
			cellGrid = new TerrainCell[scene.height / CELL_SIZE, scene.width / CELL_SIZE];
		} else {
			cellGrid = null;
		}
		cellCount = 0;
		
		Resources.UnloadUnusedAssets ();
		if (scene != null) {
			StartCoroutine (COUpdateTiles ());
		}
		foreach (NotifyTerrainChange c in notifyList) {
			c.SceneChanged (scene);
		}
		CreateOverviewTiles ();
	}
	
	public void CreateOverviewTiles ()
	{
		if (overviewTiles != null) {
			foreach (GameObject overviewTile in overviewTiles) {
				if (overviewTile)
					Destroy (overviewTile);
			}
		}
		if (scene != null) {
			int cw = scene.width / CELL_SIZE;
			int ch = scene.height / CELL_SIZE;
			float scale = CELL_SIZE * TERRAIN_SCALE;
			overviewTiles = new GameObject[ch, cw];
			for (int y = 0; y < ch; y++) {
				for (int x = 0; x < cw; x++) {
					Texture2D overviewTex = scene.overview [y, x];
					if (overviewTex == null) {
						overviewTex = overviewTilePlaceholderTex;
					}
					if (overviewTex != null) {
						GameObject go = new GameObject ("overview" + x + "x" + y);
						go.layer = Layers.L_OVERVIEW;
						go.transform.parent = transform;
						go.transform.localPosition = new Vector3 ((0.5f + x) * scale, 0f, (0.5f + y) * scale);
						go.transform.localScale = new Vector3 (scale, 1f, scale);
						MeshFilter filter = go.AddComponent <MeshFilter> ();
						filter.sharedMesh = overviewTileMesh;
						MeshRenderer render = go.AddComponent <MeshRenderer> ();
						Material renderMat = new Material (overviewTileMaterial);
						renderMat.mainTexture = overviewTex;
						render.material = renderMat;
						overviewTiles [y, x] = go;
					}
					
				}
			}
		}
	}
	
	/**
	 * returns true if cx and cy are valid terrain cell values
	 */
	public bool IsInRange (int cx, int cy)
	{
		return ((cx >= 0) && (cy >= 0) && (cx < (scene.width / CELL_SIZE)) && (cy < (scene.height / CELL_SIZE)));
	}
	
	/**
	 * returns true if cx and cy are valid terrain cell values and cell is active
	 */
	public bool TileIsVisible (int cx, int cy)
	{
		return IsInRange (cx, cy) && (cellGrid [cy, cx] != null) && (cellGrid [cy, cx].isVisible) && !(cellGrid [cy, cx].isUpdating) ;
	}

	public bool TileIsVisible (int key)
	{
		int cx = key & 0xffff;
		int cy = (key >> 16) & 0xffff;
		return IsInRange (cx, cy) && (cellGrid [cy, cx] != null) && (cellGrid [cy, cx].isVisible) && !(cellGrid [cy, cx].isUpdating) ;
	}
	
	int oldTx = int.MaxValue;
	int oldTy = int.MaxValue;
	
	IEnumerator COUpdateTiles ()
	{
		oldTx = int.MaxValue;
//		float terrainSize = CELL_SIZE * TERRAIN_SCALE;
		while (true) {
			if (disableRendering || !CameraControl.IsNear) {
				foreach (TerrainCell cell in cellGrid) {
					if ((cell != null) && (cell.isVisible)) {
						HideCellAt (cell.cellX, cell.cellY);
					}
				}
				while (disableRendering || !CameraControl.IsNear) {
					yield return 0;
				}
				oldTx = int.MaxValue;
			}
			
			Vector3 position = followPosition;
			tx = Mathf.FloorToInt ((position.x - referencePos.x) / TERRAIN_SCALE);
			ty = Mathf.FloorToInt ((position.z - referencePos.z) / TERRAIN_SCALE);
			
			if ((tx != oldTx) || (ty != oldTy)) {
				isUpdatingMap = true;
				oldTx = tx;
				oldTy = ty;
				
				for (int y = 0; y < cellGrid.GetLength (0); y++) {
					for (int x = 0; x < cellGrid.GetLength (1); x++) {
						int cellCentreX = (x * CELL_SIZE) + CELL_SIZE / 2;
						int cellCentreY = (y * CELL_SIZE) + CELL_SIZE / 2;
						if ((cellCentreX < tx - range) || (cellCentreX > tx + range) ||
						(cellCentreY < ty - range) || (cellCentreY > ty + range)) {
							HideCellAt (x, y);
						} else {
							ShowCellAt (x, y);
						}
					}
				}
				while (renderCount > 0) {
					yield return 0;
				}
				isUpdatingMap = false;
			}
			yield return 0;
		}		
	}
	
	/**
	 * Forces redraw of terrain
	 */
	public void ForceRedraw ()
	{
		if (scene != null) {
			ForceRedraw (0, 0, scene.width - 1, scene.height - 1);
		}
	}
	
	/**
	 * Force redraw of visible terrain tiles that are (partially) overlapped
	 * by the area defined by [(minX, minY) ... (maxX, maxY)]
	 */
	public void ForceRedraw (int minX, int minY, int maxX, int maxY)
	{
		int cxMin = minX / CELL_SIZE;
		int cyMin = minY / CELL_SIZE;
		int cxMax = maxX / CELL_SIZE;
		int cyMax = maxY / CELL_SIZE;
		for (int y = cyMin; y <= cyMax; y++) {
			for (int x = cxMin; x <= cxMax; x++) {
				TerrainCell cell = cellGrid [y, x];
				if (cell != null) {
					if ((cache.Count >= CACHE_SIZE) || (cell.isUpdating)) {
						cell.DestroyCell (false);
						cellCount--;
					} else {
						cell.MarkUnused ();
						cache.Add (cell);
					}
					cellGrid [y, x] = null;
				}
			}
		}
		oldTx = int.MaxValue;		
	}	
}
