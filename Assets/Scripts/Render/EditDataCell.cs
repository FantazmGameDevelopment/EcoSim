using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneData;

/**
 * A 'cell' for EditData
 * The cell size of editdatacell is smaller than the one for terraincell (64 vs 256)
 * The reason is to be able to use only one mesh instead of multiple meshes.
 * (256x256 tiles would create a mesh > 65536 vertices as corners can't be shared)
 * 
 * The EditDataCell also handles drawing 'selection area'
 */
public class EditDataCell
{
	
	public const int CELL_SIZE = 64;
	private const int TERRAIN_CELL_SIZE = TerrainMgr.CELL_SIZE;
	private const float TERRAIN_SCALE = TerrainMgr.TERRAIN_SCALE;
	private const float VERTICAL_HEIGHT = TerrainMgr.VERTICAL_HEIGHT;
	private const float VERTICAL_OFFSET = 1.0f;
	Mesh mesh; // mesh of displayed data (can be null if no data is displayed for this cell)
	GameObject go; // gameobject of displayed data (can be null)
	Mesh selectionMesh; // mesh for selection (can be null)
	GameObject selectionGo; // gameobject for selection (can be null)
	private int cx; // cell x index (0, 1, ...)
	private int cy; // cell y index (0, 1, ...)
	private float[,] heightData = null; // height data from terrain cell
	private float[,] waterHeightData = null;
	private EditData parent; // the EditData this cell is part of
	private byte[] data; // data values for this cell (CELL_SIZE * CELL_SIZE values, index by x + CELL_SIZE * y)
	private bool isEmpty = true; // data only contains 0 values
	private bool isVisible = false; // the cell is visible (underlaying terrain cell is visible)
	
	/**
	 * Constructor, cx and cy are cell indexes, initialData is used to fill data can be
	 * null (resulting in 0 values for data)
	 */
	public EditDataCell (int cx, int cy, EditData parent, Data initialData)
	{
		this.parent = parent;
		this.cx = cx;
		this.cy = cy;
		data = new byte[CELL_SIZE * CELL_SIZE];
		
		if (initialData != null) {
			// copy initial data into data if not null
			int startX = cx * CELL_SIZE;
			int startY = cy * CELL_SIZE;
			int p = 0;
			for (int y = 0; y < CELL_SIZE; y++) {
				for (int x = 0; x < CELL_SIZE; x++) {
					int val = initialData.Get (startX + x, startY + y);
					if (val != 0) isEmpty = false;
					data [p++] = (byte)val;
				}
			}
			
		}
	}
	
	void DestroyGO ()
	{
		if (go) {
			GameObject.Destroy (go);
			go = null;
		}
		if (mesh) {
			GameObject.Destroy (mesh);
			mesh = null;
		}
	}
	
	void DestroySelectionGO ()
	{
		if (selectionGo) {
			GameObject.Destroy (selectionGo);
			selectionGo = null;
		}
		if (selectionMesh) {
			GameObject.Destroy (selectionMesh);
			selectionMesh = null;
		}
	}
	
	
	/**
	 * Gets tile value within this cell (x : 0..CELLSIZE - 1, y : 0..CELLSIZE - 1)
	 * There is no overflow checking, x and y has to stay within defined range
	 */
	public int GetValueAt (int x, int y) {
		return data[y * CELL_SIZE + x];
	}
	
	/**
	 * Deletes gameobject/mesh if they exist, makes that cell won't be rendered
	 */
	public void SetInvisible ()
	{
		DestroyGO ();
		DestroySelectionGO ();
		isVisible = false;
	}
	
	/**
	 * Makes cell renderable and will be rendered if needed
	 * heights is 2-dimensional array of heightmap values from the underlying terrain cell
	 * waterHeights is 2-dimensional array of heightmap values from the underlying terrain cell
	 * note that edit cells are smaller than terrain cells
	 */
	public void SetVisible (float[,] heights, float[,] waterHeights)
	{
		this.heightData = heights;
		this.waterHeightData = waterHeights;
		UpdateMesh ();
		isVisible = true;
	}
	
	/**
	 * Copies local data into toData (at the right offsets)
	 */
	public void CopyData (Data toData)
	{
		int startX = cx * CELL_SIZE;
		int startY = cy * CELL_SIZE;
		int min = toData.GetMin ();
		int max = toData.GetMax ();
		int p = 0;
		for (int y = 0; y < CELL_SIZE; y++) {
			for (int x = 0; x < CELL_SIZE; x++) {
				int val = (int)(data [p++]);
				val = (val < min) ? min : ((val > max) ? max : val);
				toData.Set (x + startX, y + startY, val);
			}
		}
	}

	/**
	 * Copies current data of this cell using setFn to write values (with right x, y offsets).
	 * For every tile in this cell
	 * setFn is called with the actual value of the edit map and x, y offseted by the offsets
	 * of this cell.
	 */
	public void CopyData (EditData.SetValue setFn)
	{
		int startX = cx * CELL_SIZE;
		int startY = cy * CELL_SIZE;
		int p = 0;
		for (int y = 0; y < CELL_SIZE; y++) {
			for (int x = 0; x < CELL_SIZE; x++) {
				setFn (x + startX, y + startY, (int)(data [p++]));
			}
		}
	}
	
	
	/**
	 * Updates data in this cell using fromData, cell is redrawn if needed
	 */
	public void SetData (Data fromData) {
		int startX = cx * CELL_SIZE;
		int startY = cy * CELL_SIZE;
		int p = 0;
		isEmpty = true;
		for (int y = 0; y < CELL_SIZE; y++) {
			for (int x = 0; x < CELL_SIZE; x++) {
				int val = fromData.Get (x + startX, y + startY);				
				val = (val < 0) ? 0 : ((val > 255) ? 255 : val);
				if (val != 0) isEmpty = false;
				data [p++] = (byte) val;
			}
		}
		if (isVisible) {
			UpdateMesh();
		}
	}

	/**
	 * Updates data in this cell using function getFn for values, cell is redrawn if needed
	 */
	public void SetData (EditData.GetValue getFn) {
		int startX = cx * CELL_SIZE;
		int startY = cy * CELL_SIZE;
		int p = 0;
		isEmpty = true;
		for (int y = 0; y < CELL_SIZE; y++) {
			for (int x = 0; x < CELL_SIZE; x++) {
				int val = getFn (x + startX, y + startY);				
				val = (val < 0) ? 0 : ((val > 255) ? 255 : val);
				if (val != 0) isEmpty = false;
				data [p++] = (byte) val;
			}
		}
		if (isVisible) {
			UpdateMesh();
		}
	}
	
	/**
	 * Set data to 0 in this cell, meshes updated (removed) if needed
	 */
	public void ClearData () {
		isEmpty = true;
		for (int i = 0; i < data.Length; i++) {
			data[i] = 0;
		}
		if (isVisible) {
			UpdateMesh();
		}
	}
	
	/**
	 * Clears current selection
	 */
	public void ClearSelection ()
	{
		DestroySelectionGO ();
	}
	
	private float Height (int xx, int yy) {
		float height = heightData[yy, xx];
		float waterHeight = waterHeightData[yy, xx];
		if (height >= waterHeight) return height;
		return waterHeight;
	}
	
	/**
	 * renders, if necessary, the selection mesh, using fn for getting the data values
	 * data values < 0 will result in tiles not drawn (not selected)
	 * minX, minY, maxX, maxY is selection boundary, used for quick calculation if
	 * anything need to be drawn here
	 * Note that minX, maxX, minY, maxY are absolute coordinates, not cell coordinates
	 */
	public void UpdateSelection (EditData.GetValue fn, int minX, int minY, int maxX, int maxY)
	{
		if (!isVisible)
			return; // cell not visible
		int startX = cx * CELL_SIZE;
		int startY = cy * CELL_SIZE;
		
		int x0 = Mathf.Max (minX - startX, 0);
		int y0 = Mathf.Max (minY - startY, 0);
		int x1 = Mathf.Min (maxX - startX, CELL_SIZE - 1);
		int y1 = Mathf.Min (maxY - startY, CELL_SIZE - 1);
		if ((x0 > x1) || (y0 > y1)) {
			// no selection visible in this cell
			DestroySelectionGO ();
			return;
		}
		
		int count = 0;
		bool showZero = parent.gridSettings.activeShowZero;
		int offset = parent.gridSettings.offset;
		int elementsPerRow = parent.gridSettings.elementsPerRow;
		float uvStep = 1f / elementsPerRow;

		List<Vector3> vertices = new List<Vector3> ();
		List<Vector2> uv = new List<Vector2> ();
		List<int> indices = new List<int> ();
		
		int heightsOffsetX = startX % TERRAIN_CELL_SIZE;
		int heightsOffsetY = startY % TERRAIN_CELL_SIZE;
		
		for (int y = 0; y < CELL_SIZE; y++) {
			int yy = (heightsOffsetY + y) << 2; // terrain coord
			for (int x = 0; x < CELL_SIZE; x++) {
				int xx = (heightsOffsetX + x) << 2; // terrain coord
				int val = fn (startX + x, startY + y);
				if ((showZero && (val >= 0)) || (val > 0)) {
					val += offset;
					int uvX = val % elementsPerRow;
					int uvY = val / elementsPerRow;
					uv.Add (new Vector2 (uvStep * uvX, uvStep * uvY));
					uv.Add (new Vector2 (uvStep * (uvX + 1), uvStep * uvY));
					uv.Add (new Vector2 (uvStep * uvX, uvStep * (uvY + 1)));
					uv.Add (new Vector2 (uvStep * (uvX + 1), uvStep * (uvY + 1)));
					vertices.Add (new Vector3 (TERRAIN_SCALE * x, Height (xx, yy) * VERTICAL_HEIGHT, TERRAIN_SCALE * y));
					vertices.Add (new Vector3 (TERRAIN_SCALE * (x + 1), Height (xx + 4, yy) * VERTICAL_HEIGHT, TERRAIN_SCALE * y));
					vertices.Add (new Vector3 (TERRAIN_SCALE * x, Height (xx, yy + 4) * VERTICAL_HEIGHT, TERRAIN_SCALE * (y + 1)));
					vertices.Add (new Vector3 (TERRAIN_SCALE * (x + 1), Height (xx + 4, yy + 4) * VERTICAL_HEIGHT, TERRAIN_SCALE * (y + 1)));
					indices.Add (count + 1);
					indices.Add (count);
					indices.Add (count + 2);
					indices.Add (count + 1);
					indices.Add (count + 2);
					indices.Add (count + 3);
					count += 4;
				}	
			}
		}
		if (count > 0) {
			if (!selectionMesh) {
				selectionMesh = new Mesh ();
			}
			else {
				selectionMesh.Clear();
			}
			selectionMesh.vertices = vertices.ToArray ();
			selectionMesh.uv = uv.ToArray ();
			selectionMesh.triangles = indices.ToArray ();
			selectionMesh.Optimize ();
			selectionMesh.RecalculateNormals ();
			if (!selectionGo) {
				selectionGo = GenerateSelectionGameObject (selectionMesh);
			}
		} else {
			DestroySelectionGO ();
		}
		
	}
	
	/**
	 * Updates values within coordinate range (minX, minY) - (maxX, maxY) using function fn
	 * Note that minX, maxX, minY, maxY are absolute coordinates, not cell coordinates
	 */
	public void UpdateValues (EditData.GetValue fn, int minX, int minY, int maxX, int maxY)
	{
		bool hasTileChangedEventHandler = parent.HasTileChangedEventHandler ();
		int startX = cx * CELL_SIZE;
		int startY = cy * CELL_SIZE;
		
		int x0 = Mathf.Max (minX - startX, 0);
		int y0 = Mathf.Max (minY - startY, 0);
		int x1 = Mathf.Min (maxX - startX, CELL_SIZE - 1);
		int y1 = Mathf.Min (maxY - startY, CELL_SIZE - 1);
		if ((x0 > x1) || (y0 > y1))
			return; // outside our cell
		for (int y = y0; y <= y1; y++) {
			int p = (y * CELL_SIZE) + x0;
			for (int x = x0; x <= x1; x++) {
				int val = fn (x + startX, y + startY);
				if (val >= 0) {
					if (hasTileChangedEventHandler) {
						parent.FireTileChangedEvent(x + startX, y + startY, (int) (data[p]), val);
					}
					data [p] = (byte) val;
				}
				p++;
			}
		}
		
		// check if data is still empty...
		int len = CELL_SIZE * CELL_SIZE;
		isEmpty = true;
		for (int i = 0; i < len; i++) {
			if (data [i] != 0) {
				isEmpty = false;
				break;
			}
		}
		if (isVisible) {
			UpdateMesh ();
		}
	}
	
	/**
	 * Calculates mesh, and if needed creates or destroys (if mesh would be empty) gameobject to hold mesh.
	 */
	void UpdateMesh ()
	{
		int count = 0;
		bool showZero = parent.gridSettings.showZero;
		List<Vector3> vertices = null;
		List<Vector2> uv = null;
		List<int> indices = null;
		
		if (!isEmpty || showZero) {
			// if we don't have non-zero data we only have to render
			// if we want to show zero's, otherwise
			// we can skip this (count will stay zero, so no mesh/gameobject will be created)
			int offset = parent.gridSettings.offset;
			int elementsPerRow = parent.gridSettings.elementsPerRow;
			float uvStep = 1f / elementsPerRow;

			vertices = new List<Vector3> ();
			uv = new List<Vector2> ();
			indices = new List<int> ();
			int startX = cx * CELL_SIZE;
			int startY = cy * CELL_SIZE;
		
			int heightsOffsetX = startX % TERRAIN_CELL_SIZE;
			int heightsOffsetY = startY % TERRAIN_CELL_SIZE;
		
			int p = 0;
			for (int y = 0; y < CELL_SIZE; y++) {
				int yy = (heightsOffsetY + y) << 2; // terrain coord
				for (int x = 0; x < CELL_SIZE; x++) {
					int xx = (heightsOffsetX + x) << 2; // terrain coord
					int val = data[p++];
					if (showZero || (val > 0)) {
						val += offset;
						int uvX = val % elementsPerRow;
						int uvY = val / elementsPerRow;
						uv.Add (new Vector2 (uvStep * uvX, uvStep * uvY));
						uv.Add (new Vector2 (uvStep * (uvX + 1), uvStep * uvY));
						uv.Add (new Vector2 (uvStep * uvX, uvStep * (uvY + 1)));
						uv.Add (new Vector2 (uvStep * (uvX + 1), uvStep * (uvY + 1)));
						vertices.Add (new Vector3 (TERRAIN_SCALE * x, Height (xx, yy) * VERTICAL_HEIGHT, TERRAIN_SCALE * y));
						vertices.Add (new Vector3 (TERRAIN_SCALE * (x + 1), Height (xx + 4, yy) * VERTICAL_HEIGHT, TERRAIN_SCALE * y));
						vertices.Add (new Vector3 (TERRAIN_SCALE * x, Height (xx, yy + 4) * VERTICAL_HEIGHT, TERRAIN_SCALE * (y + 1)));
						vertices.Add (new Vector3 (TERRAIN_SCALE * (x + 1), Height (xx + 4, yy + 4) * VERTICAL_HEIGHT, TERRAIN_SCALE * (y + 1)));
						indices.Add (count + 1);
						indices.Add (count);
						indices.Add (count + 2);
						indices.Add (count + 1);
						indices.Add (count + 2);
						indices.Add (count + 3);
						count += 4;
					}	
				}
			}
		}
		if (count > 0) {
			if (!mesh) {
				mesh = new Mesh ();
			}
			else {
				mesh.Clear();
			}
			mesh.vertices = vertices.ToArray ();
			mesh.uv = uv.ToArray ();
			mesh.triangles = indices.ToArray ();
			mesh.Optimize ();
			mesh.RecalculateNormals ();
			if (!go) {
				go = GenerateGameObject (mesh);
			}
		} else {
			DestroyGO ();
		}
	}
	
	GameObject GenerateGameObject (Mesh mesh)
	{
		GameObject go = new GameObject ("mesh " + cx + "," + cy);
		go.transform.parent = parent.transform;
		go.transform.localPosition = new Vector3 (cx * TERRAIN_SCALE * CELL_SIZE, VERTICAL_OFFSET, cy * TERRAIN_SCALE * CELL_SIZE);
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;
		MeshFilter filter = go.AddComponent<MeshFilter> ();
		filter.sharedMesh = mesh;
		MeshRenderer render = go.AddComponent<MeshRenderer> ();
		render.sharedMaterial = parent.gridSettings.material;
		return go;
	}

	GameObject GenerateSelectionGameObject (Mesh mesh)
	{
		GameObject go = new GameObject ("selectionmesh " + cx + "," + cy);
		go.transform.parent = parent.transform;
		go.transform.localPosition = new Vector3 (cx * TERRAIN_SCALE * CELL_SIZE, VERTICAL_OFFSET, cy * TERRAIN_SCALE * CELL_SIZE);
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;
		MeshFilter filter = go.AddComponent<MeshFilter> ();
		filter.sharedMesh = mesh;
		MeshRenderer render = go.AddComponent<MeshRenderer> ();
		render.sharedMaterial = parent.gridSettings.activeMaterial;
		return go;
	}
	
}
