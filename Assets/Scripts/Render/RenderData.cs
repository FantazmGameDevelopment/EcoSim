using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.Render;
using Ecosim.SceneData;

/**
 * Visualises (Ecosim.SceneData.)Data on terrain 
 */
public class RenderData : MonoBehaviour, NotifyTerrainChange
{
	
	private const int CELL_SIZE = TerrainCell.CELL_SIZE;
	private const float TERRAIN_SCALE = TerrainMgr.TERRAIN_SCALE;
	private const float VERTICAL_HEIGHT = TerrainMgr.VERTICAL_HEIGHT;
	private const float VERTICAL_OFFSET = 1.0f;
	
	public GridTextureSettings gridSettings;
	public Data data;
	Dictionary<int, List<GameObject>> cells;
	
	public void SceneChanged (Scene scene)
	{
		// we destroy the renderdata if scene has changed, as it's unlikely we still
		// want to show the same data...
		Destroy (gameObject);
	}
		
	public void SuccessionCompleted ()
	{
		// we destroy the renderdata if succession is completed, as it's unlikely we still
		// want to show the same data...
		Destroy (gameObject);
	}
	
	void OnDestroy ()
	{
		// destroy all cells
		foreach (List<GameObject> goList in cells.Values) {
			foreach (GameObject go in goList) {
				if (go) {
					Destroy(go);
				}
			}
		}
		TerrainMgr.RemoveListener (this);
	}
	
	void Awake ()
	{
		cells = new Dictionary<int, List<GameObject>> ();
	}
	
	void Start ()
	{
		TerrainMgr.AddListener (this);
	}
	
	public static RenderData CreateRenderData (string name, Data data, GridTextureSettings gridSettings)
	{
		GameObject go = new GameObject ("RenderData " + name);
		go.transform.parent = TerrainMgr.self.transform;
		RenderData rd = go.AddComponent<RenderData> ();
		rd.gridSettings = gridSettings;
		rd.data = data;
		return rd;
	}
	
	GameObject GenerateGameObject (int cx, int cy, Mesh mesh)
	{
		GameObject go = new GameObject ("mesh " + cx + "," + cy);
		go.transform.parent = transform;
		go.transform.localPosition = new Vector3 (cx * TERRAIN_SCALE * CELL_SIZE, VERTICAL_OFFSET, cy * TERRAIN_SCALE * CELL_SIZE);
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;
		MeshFilter filter = go.AddComponent<MeshFilter> ();
		filter.mesh = mesh;
		MeshRenderer render = go.AddComponent<MeshRenderer> ();
		render.sharedMaterial = gridSettings.material;
		return go;
	}
	
	List<GameObject> GenerateMesh (int cx, int cy, float[,] heightData)
	{
		List<GameObject> meshObjects = new List<GameObject> ();
		bool showZero = gridSettings.showZero;
		int offset = gridSettings.offset;
		int elementsPerRow = gridSettings.elementsPerRow;
		float uvStep = 1f / elementsPerRow;
		
		List<Vector3> vertices = new List<Vector3> ();
		List<Vector2> uv = new List<Vector2> ();
		List<int> indices = new List<int> ();
		int startX = cx * CELL_SIZE;
		int startY = cy * CELL_SIZE;
		int count = 0;
		for (int y = 0; y < CELL_SIZE; y++) {
			int yy = y << 2; // terrain coord
			for (int x = 0; x < CELL_SIZE; x++) {
				int xx = x << 2; // terrain coord
				int val = data.Get (startX + x, startY + y);
				if (showZero || (val > 0)) {
					val += offset;
					int uvX = val % elementsPerRow;
					int uvY = val / elementsPerRow;
					uv.Add (new Vector2 (uvStep * uvX, uvStep * uvY));
					uv.Add (new Vector2 (uvStep * (uvX + 1), uvStep * uvY));
					uv.Add (new Vector2 (uvStep * uvX, uvStep * (uvY + 1)));
					uv.Add (new Vector2 (uvStep * (uvX + 1), uvStep * (uvY + 1)));
					vertices.Add (new Vector3 (TERRAIN_SCALE * x, heightData [yy, xx] * VERTICAL_HEIGHT, TERRAIN_SCALE * y));
					vertices.Add (new Vector3 (TERRAIN_SCALE * (x + 1), heightData [yy, xx + 4] * VERTICAL_HEIGHT, TERRAIN_SCALE * y));
					vertices.Add (new Vector3 (TERRAIN_SCALE * x, heightData [yy + 4, xx] * VERTICAL_HEIGHT, TERRAIN_SCALE * (y + 1)));
					vertices.Add (new Vector3 (TERRAIN_SCALE * (x + 1), heightData [yy + 4, xx + 4] * VERTICAL_HEIGHT, TERRAIN_SCALE * (y + 1)));
					indices.Add (count + 1);
					indices.Add (count);
					indices.Add (count + 2);
					indices.Add (count + 1);
					indices.Add (count + 2);
					indices.Add (count + 3);
					count += 4;
					if (count >= 65530) {
						Mesh mesh = new Mesh ();
						mesh.vertices = vertices.ToArray ();
						mesh.uv = uv.ToArray ();
						mesh.triangles = indices.ToArray ();
						mesh.Optimize ();
						mesh.RecalculateNormals ();	
						meshObjects.Add (GenerateGameObject (cx, cy, mesh));
						vertices.Clear ();
						uv.Clear ();
						indices.Clear ();
						count = 0;
					}
				}	
			}
		}
		if (count > 0) {
			Mesh mesh = new Mesh ();
			mesh.vertices = vertices.ToArray ();
			mesh.uv = uv.ToArray ();
			mesh.triangles = indices.ToArray ();
			mesh.Optimize ();
			mesh.RecalculateNormals ();	
			meshObjects.Add (GenerateGameObject (cx, cy, mesh));
		}
		if (meshObjects.Count > 0) {
			return meshObjects;
		} else
			return null;
	}
		
	public void CellChangedToVisible (int cx, int cy, TerrainCell cell)
	{
		int key = TerrainMgr.CoordToKey (cx, cy);
		if (cells.ContainsKey (key)) {
			List<GameObject> meshObjects = cells[key];
			foreach (GameObject go in meshObjects) {
				Destroy (go);
			}
			cells.Remove (key);
		}
		List<GameObject> newMeshObjects = GenerateMesh (cx, cy, cell.heights);
		if (newMeshObjects != null) {
			cells.Add (key, newMeshObjects);
		}
		
	}
		
	public void CellChangedToInvisible (int cx, int cy)
	{
		int key = TerrainMgr.CoordToKey (cx, cy);
		if (cells.ContainsKey (key)) {
			List<GameObject> meshObjects = cells[key];
			foreach (GameObject go in meshObjects) {
				Destroy (go);
			}
			cells.Remove (key);
		}
	}
	
}
