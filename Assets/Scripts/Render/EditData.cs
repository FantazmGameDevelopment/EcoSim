using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.Render;
using Ecosim.SceneData;
using Ecosim;

/**
 * Visualises and edits (Ecosim.SceneData.)Data on terrain 
 */
public class EditData : MonoBehaviour, NotifyTerrainChange
{
	private const int TERRAIN_CELL_SIZE = TerrainCell.CELL_SIZE;
	private const int EDIT_CELL_SIZE = EditDataCell.CELL_SIZE;
	private const float TERRAIN_SCALE = TerrainMgr.TERRAIN_SCALE;
	private const float VERTICAL_HEIGHT = TerrainMgr.VERTICAL_HEIGHT;
	private const float VERTICAL_OFFSET = 1.0f;

	public GridTextureSettings gridSettings;
	private BrushValue fn; // used for calculate write values during selecting
	private BrushValue fnFinal; // if null uses fn for determining values when completing selection, otherwise fnFinal

	private event RightMouseButtonHndlr rmbHndlr;
	public event NotifyTileChanged tileChangeHndlr;

	EditDataCell[,] cells;
	Scene scene;
	
	public enum EditMode
	{
		NoEdit,
		AreaSelect,
		Brush
	}
	
	EditMode editMode = EditMode.NoEdit;
	
	public delegate int GetValue (int x,int y);

	public delegate void SetValue (int x,int y,int v);

	public delegate int RemapValue (int x,int y,int v);
	
	public delegate void NotifyTileChanged (int x,int y,int oldV,int newV);
	
	/**
	 * delegate function to calculate what values must be shown/set at x, y.
	 * currentVal is value at x, y. Param Strength is brush strength at x, y.
	 * Param shift and ctrl is true when respective buttons are being pressed
	 */
	public delegate int BrushValue (int x,int y,int currentVal,float strength,bool shift,bool ctrl);
	
	public delegate void RightMouseButtonHndlr (int x,int y,int v);
	
	/**
	 * Normally the brush function used when creating EditData is used for determining
	 * values of tiles in active selection area and the values written to the data set
	 * when completing selection. Sometimes however it's useful to have a different
	 * function for writing the completed selection. One case scenario would be
	 * that the selection function uses a special index for tiles not allowed to be
	 * selected by player, and thus when writing the definitive selection the values
	 * for these tiles shouldn't be set.
	 */
	public void SetFinalBrushFunction (BrushValue fnFinal)
	{
		this.fnFinal = fnFinal;
	}
	
	public void AddRightMouseHandler (RightMouseButtonHndlr hndlr)
	{
		rmbHndlr += hndlr;
	}
	
	/**
	 * Notifies when a tile has changed for every tile in the selection when completed.
	 */
	public void AddTileChangedHandler (NotifyTileChanged hndlr)
	{
		tileChangeHndlr += hndlr;
	}
	
	public bool HasTileChangedEventHandler ()
	{
		return (tileChangeHndlr != null);
	}
	
	public void FireTileChangedEvent (int x, int y, int oldV, int newV)
	{
		if (tileChangeHndlr != null) {
			tileChangeHndlr (x, y, oldV, newV);
		}
	}
	
	/**
	 * Call back handler (called by terrainmgr)
	 */
	public void SceneChanged (Scene scene)
	{
		// we destroy the renderdata if scene has changed, as it's unlikely we still
		// want to show the same data...
		Destroy (gameObject);
	}
		
	/**
	 * Call back handler (called by terrainmgr)
	 */
	public void SuccessionCompleted ()
	{
		// we destroy the renderdata if succession is completed, as it's unlikely we still
		// want to show the same data...
		Destroy (gameObject);
	}
	
	void OnDestroy ()
	{
		foreach (EditDataCell cell in cells) {
			if (cell != null) {
				cell.SetInvisible ();
			}
		}
		TerrainMgr.RemoveListener (this);
	}
		
	void Start ()
	{
		TerrainMgr.AddListener (this);
	}
	
	/**
	 * Call back handler for updating visibility of cells (called by terrainmgr)
	 */
	public void CellChangedToVisible (int cx, int cy, TerrainCell cell)
	{
		// for every terraincell we need several editdatacells....
		int minX = cx * TERRAIN_CELL_SIZE;
		int minY = cy * TERRAIN_CELL_SIZE;
		int maxX = (cx + 1) * TERRAIN_CELL_SIZE;
		int maxY = (cy + 1) * TERRAIN_CELL_SIZE;
		int ecMinX = minX / EDIT_CELL_SIZE;
		int ecMaxX = maxX / EDIT_CELL_SIZE;
		int ecMinY = minY / EDIT_CELL_SIZE;
		int ecMaxY = maxY / EDIT_CELL_SIZE;
		
		for (int y = ecMinY; y < ecMaxY; y++) {
			for (int x = ecMinX; x < ecMaxX; x++) {
				EditDataCell c = cells [y, x];
				c.SetVisible (cell.heights, cell.waterHeights);
			}
		}		
	}
	
	/**
	 * Call back handler for updating visibility of cells (called by terrainmgr)
	 */
	public void CellChangedToInvisible (int cx, int cy)
	{
		// for every terraincell we need several editdatacells....
		int minX = cx * TERRAIN_CELL_SIZE;
		int minY = cy * TERRAIN_CELL_SIZE;
		int maxX = (cx + 1) * TERRAIN_CELL_SIZE;
		int maxY = (cy + 1) * TERRAIN_CELL_SIZE;
		int ecMinX = minX / EDIT_CELL_SIZE;
		int ecMaxX = maxX / EDIT_CELL_SIZE;
		int ecMinY = minY / EDIT_CELL_SIZE;
		int ecMaxY = maxY / EDIT_CELL_SIZE;
		
		for (int y = ecMinY; y < ecMaxY; y++) {
			for (int x = ecMinX; x < ecMaxX; x++) {
				EditDataCell c = cells [y, x];
				c.SetInvisible ();
			}
		}		
	}

	/**
	 * name is just for easier debugging (used for naming game objects)
	 * data is the current value set, if null, editData will be initialized with 0 values for data
	 * area is used to determine which tiles are editable
	 * fn is used to get initial values when adding areas to edit set
	 * gridSettings is used to determine material and uv mapping
	 */
	public static EditData CreateEditData (string name, Data data, Data editableArea, BrushValue fn, GridTextureSettings gridSettings)
	{
		GameObject go = new GameObject ("EditData " + name);
		go.transform.parent = TerrainMgr.self.transform;
		EditData ed = go.AddComponent<EditData> ();
		ed.gridSettings = gridSettings;
		Scene scene = TerrainMgr.self.scene;
		ed.scene = scene;
		ed.fn = fn;
		int w = scene.width / EDIT_CELL_SIZE;
		int h = scene.height / EDIT_CELL_SIZE;
		ed.cells = new EditDataCell[h, w];
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				ed.cells [y, x] = new EditDataCell (x, y, ed, data, editableArea);
			}
		}
		return ed;
	}

	/**
	 * Constructor, T determines which Data implementation is used (1 bit, 2 bit, ...)
	 * name is just for easier debugging (used for naming game objects)
	 * data is the current value set, if null, editData will be initialized with 0 values for data
	 * fn is used to get initial values when adding areas to edit set
	 * gridSettings is used to determine material and uv mapping
	 */
	public static EditData CreateEditData (string name, Data data, BrushValue fn, GridTextureSettings gridSettings)
	{
		return CreateEditData (name, data, null, fn, gridSettings);
	}
	
	/**
	 * Sets data in edit area to values from fromData
	 */
	public void SetData (Data fromData)
	{
		ClearSelection ();
		int w = scene.width / EDIT_CELL_SIZE;
		int h = scene.height / EDIT_CELL_SIZE;
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				cells [y, x].SetData (fromData);
			}
		}	
	}

	/**
	 * Sets data in edit area using function getFn
	 */
	public void SetData (GetValue getFn)
	{
		ClearSelection ();
		int w = scene.width / EDIT_CELL_SIZE;
		int h = scene.height / EDIT_CELL_SIZE;
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				cells [y, x].SetData (getFn);
			}
		}	
	}
	
	
	/**
	 * Clears data in edit area (all values set to 0)
	 */
	public void ClearData ()
	{
		ClearSelection ();
		int w = scene.width / EDIT_CELL_SIZE;
		int h = scene.height / EDIT_CELL_SIZE;
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				cells [y, x].ClearData ();
			}
		}	
	}
	
	/**
	 * Deletes this editdata and it's gameobjects
	 */
	public void Delete ()
	{
		Destroy (gameObject);
	}
	
	/**
	 * Copies current values of editdata to data
	 */
	public void CopyData (Data toData)
	{
		int w = cells.GetLength (1);
		int h = cells.GetLength (0);
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				cells [y, x].CopyData (toData);
			}
		}
	}

	/**
	 * Copies current data using setFn to write values.
	 * For every tile in the whole edit area (size of map)
	 * setFn is called with the actual value of the edit map.
	 */
	public void CopyData (SetValue setFn)
	{
		int w = cells.GetLength (1);
		int h = cells.GetLength (0);
		for (int y = 0; y < h; y++) {
			for (int x = 0; x < w; x++) {
				cells [y, x].CopyData (setFn);
			}
		}
	}
	
	
	
	/**
	 * data value at given position
	 */
	int GetValueAt (int x, int y)
	{
		int cx = x / EDIT_CELL_SIZE;
		int cy = y / EDIT_CELL_SIZE;
		return cells [cy, cx].GetValueAt (x % EDIT_CELL_SIZE, y % EDIT_CELL_SIZE);
	}
	
	/**
	 * tile mouse is pointing at (or Coordinate.Invalid if not pointing to terrain)
	 */
	Coordinate GetMousePos ()
	{
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, Mathf.Infinity, Layers.M_TERRAIN)) {
			int x = (int)(hit.point.x / TERRAIN_SCALE);
			int y = (int)(hit.point.z / TERRAIN_SCALE);
			// Debug.Log("pos = " + x + ", " + y);
			return new Coordinate (x, y);
		}
		return Coordinate.INVALID;
	}
		
	/**
	 * Clears selection area if any
	 */
	public void ClearSelection ()
	{
		foreach (EditDataCell cell in cells) {
			cell.ClearSelection ();
		}
	}
	
	int radius = 5;
	bool wasShift = false;
	bool wasCtrl = false;
	Coordinate lastCoord;
	
	/**
	 * Handles circular brush
	 */
	void UpdateBrush (Coordinate c)
	{
		if (c != Coordinate.INVALID) {
			bool shift = Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift);
			bool ctrl = Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl);
			bool mouseClick = Input.GetMouseButtonDown (0) && !CameraControl.MouseOverGUI;
			if ((c == lastCoord) && (shift == wasShift) && (ctrl == wasCtrl) && !mouseClick) {
				return; // nothing changed, no need to update
			}
			int minX = Mathf.Max (c.x - radius, 0);
			int minY = Mathf.Max (c.y - radius, 0);
			int maxX = Mathf.Min (c.x + radius, scene.width - 1);
			int maxY = Mathf.Min (c.y + radius, scene.height - 1);
			
			// function to calculate values within selection
			GetValue dSet = delegate(int x, int y) {
				int sqrDist = (c.x - x) * (c.x - x) + (c.y - y) * (c.y - y);
				if (sqrDist <= radius * radius) {
					float pntStrength = 1f;
					if (sqrDist > 0) {
						float normalizedDist = Mathf.Sqrt (sqrDist) / radius;
						pntStrength = (1f - normalizedDist);
					}
					int currentVal = GetValueAt (x, y);
					return fn (x, y, currentVal, pntStrength, shift, ctrl);
				} else {
					return -1;
				}
			};
			
			// function to calculate values when selection is added to data set
			GetValue dFinish;
			
			if (fnFinal != null) {
				dFinish = delegate(int x, int y) {
					int sqrDist = (c.x - x) * (c.x - x) + (c.y - y) * (c.y - y);
					if (sqrDist <= radius * radius) {
						float pntStrength = 1f;
						if (sqrDist > 0) {
							float normalizedDist = Mathf.Sqrt (sqrDist) / radius;
							pntStrength = (1f - normalizedDist);
						}
						int currentVal = GetValueAt (x, y);
						return fnFinal (x, y, currentVal, pntStrength, shift, ctrl);
					} else {
						return -1;
					}
				};
			} else {
				dFinish = dSet;
			}
			
			// update selection brush
			foreach (EditDataCell cell in cells) {
				cell.UpdateSelection (dSet, minX, minY, maxX, maxY);
			}
			
			if (mouseClick) {
				if (Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt) || Input.GetMouseButton(1))
					return; // moving/rotating camera, don't use brush
				// place selection (update actual values)
				foreach (EditDataCell cell in cells) {
					cell.UpdateValues (dFinish, minX, minY, maxX, maxY);
				}
			}
			wasShift = shift;
			wasCtrl = ctrl;
		}
		lastCoord = c;
	}
	
	Coordinate startPoint = Coordinate.INVALID;
	private int lastMinX;
	private int lastMinY;
	private int lastMaxX;
	private int lastMaxY;
	
	/**
	 * handle area select
	 */
	void UpdateAreaSelect (Coordinate c)
	{
		if (c != Coordinate.INVALID) {
			bool shift = Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift);
			bool ctrl = Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl);
			bool mouseClickDown = Input.GetMouseButtonDown (0) && !CameraControl.MouseOverGUI;
			
			if (mouseClickDown) {
				if (Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt))
					return; // rotating/moving camera

				// start selecting area
				startPoint = c;
				lastMinX = c.x;
				lastMinY = c.y;
				lastMaxX = c.x;
				lastMaxY = c.y;
			}
			
			if (Input.GetKeyDown (KeyCode.Escape)) {
				startPoint = Coordinate.INVALID; // stop selecting
				ClearSelection ();
			}
			if (startPoint == Coordinate.INVALID) {
				return; // not selecting yet (not started pressing left mouse button)
			}
			bool mouseClickUp = Input.GetMouseButtonUp (0);
			if ((c == lastCoord) && (shift == wasShift) && (ctrl == wasCtrl) && !mouseClickUp) {
				return; // nothing changed, no need to update
			}
			int minX = Mathf.Min (c.x, startPoint.x);
			int minY = Mathf.Min (c.y, startPoint.y);
			int maxX = Mathf.Max (c.x, startPoint.x);
			int maxY = Mathf.Max (c.y, startPoint.y);
			
			// function for calculating the values within the selection area
			GetValue dSet = delegate(int x, int y) {
				if ((x >= minX) && (x <= maxX) && (y >= minY) && (y <= maxY)) {
					int currentVal = GetValueAt (x, y);
					return fn (x, y, currentVal, 1f, shift, ctrl);
				} else {
					return -1;
				}
			};

			// function to calculate values when selection is added to data set
			GetValue dFinish;
			
			if (fnFinal != null) {
				dFinish = delegate(int x, int y) {
					if ((x >= minX) && (x <= maxX) && (y >= minY) && (y <= maxY)) {
						int currentVal = GetValueAt (x, y);
						return fnFinal (x, y, currentVal, 1f, shift, ctrl);
					} else {
						return -1;
					}
				};
			} else {
				dFinish = dSet;
			}
			
			if (mouseClickUp) {
				// finish area select
				foreach (EditDataCell cell in cells) {
					cell.UpdateValues (dFinish, minX, minY, maxX, maxY);
				}
				startPoint = Coordinate.INVALID;
				ClearSelection ();
			} else {
				// update shown area selection
				foreach (EditDataCell cell in cells) {
					
					cell.UpdateSelection (dSet, (minX < lastMinX) ? minX : lastMinX, (minY < lastMinY) ? minY : lastMinY,
						(maxX > lastMaxX) ? maxX : lastMaxX, (maxY > lastMaxY) ? maxY : lastMaxY);
				}
				lastMinX = minX;
				lastMinY = minY;
				lastMaxX = maxX;
				lastMaxY = maxY;
			}
			wasShift = shift;
			wasCtrl = ctrl;
		}
		lastCoord = c;
	}
	
	public void SetModeBrush (int radius)
	{
		editMode = EditMode.Brush;
		this.radius = radius;
		ClearSelection ();
	}
	
	public void SetModeAreaSelect ()
	{
		editMode = EditMode.AreaSelect;
		ClearSelection ();
	}
	
	public void SetModeNone ()
	{
		editMode = EditMode.NoEdit;
		ClearSelection ();
	}
	
	void Update ()
	{
		Coordinate c = GetMousePos ();
		if (c != Coordinate.INVALID && (rmbHndlr != null) && (Input.GetMouseButtonDown (1))) {
			rmbHndlr (c.x, c.y, GetValueAt (c.x, c.y));
		}
		switch (editMode) {
		case EditMode.Brush :
			UpdateBrush (c);
			break;
		case EditMode.AreaSelect :
			UpdateAreaSelect (c);
			break;
		}
	}
}
