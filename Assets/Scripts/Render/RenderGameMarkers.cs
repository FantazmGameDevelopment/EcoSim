using System;
using UnityEngine;
using System.Collections.Generic;
using Ecosim;
using Ecosim.Render;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;

public class RenderGameMarkers : MonoBehaviour, NotifyTerrainChange
{
	public RenderGameMarkersMgr parent;
	public Data map;
	public ExtraAssets.AssetObjDef[] objects = null;
	public RenderGameMarkersMgr.ClickHandler handler = null;
	private Dictionary<Coordinate, GameObject> dict;
		
	void Start ()
	{
		TerrainMgr.AddListener (this);
		dict = new Dictionary<Coordinate, GameObject> ();
		CreateMarkers ();
	}
	
	void CreateMarker (Coordinate c, int val)
	{
		ExtraAssets.AssetObjDef obj = null;
		if ((objects != null) && (val - 1 < objects.Length)) {
			obj = objects [val - 1];
		}
		GameObject marker = null;
		if (obj != null) {
			marker = new GameObject (obj.name);
			MeshFilter filter = marker.AddComponent<MeshFilter> ();
			filter.sharedMesh = obj.mesh;
			MeshRenderer render = marker.AddComponent<MeshRenderer> ();
			render.sharedMaterial = obj.material;
			BoxCollider col = marker.AddComponent<BoxCollider> ();
			col.center = obj.mesh.bounds.center;
			col.extents = obj.mesh.bounds.extents;
		} else {
			marker = (GameObject)Instantiate (parent.markerPrefab);
		}
		marker.transform.parent = transform;
		float x = (0.5f + c.x) * TerrainMgr.TERRAIN_SCALE;
		float y = (0.5f + c.y) * TerrainMgr.TERRAIN_SCALE;
		float h = parent.heights.GetInterpolatedHeight (x, y);
		marker.transform.localPosition = new Vector3 (x, h, y);
		marker.layer = Layers.L_EDIT2;
		dict.Add (c, marker);
	}
	
	void CreateMarkers ()
	{
		foreach (ValueCoordinate c in map.EnumerateNotZero ()) {
			CreateMarker (c, c.v);
		}
	}
	
	public void HandleMouseDown (Coordinate c)
	{
		if (handler != null) {
			GameObject go;
			if (dict.TryGetValue (c, out go)) {
				Destroy (go);
				dict.Remove (c);
			}
			handler (c);
			int val = map.Get (c);
			if (val > 0) {
				CreateMarker (c, val);
			}
		}
	}
	
	void OnDestroy ()
	{
		TerrainMgr.RemoveListener (this);
	}
	
	/**
	 * if there is a marker at x, y and marker has text, this text
	 * will be returned otherwise null
	 */
	public string GetTextAtCoordinate (int x, int y)
	{
		if (map is TextBitMap) {
			return ((TextBitMap)map).GetString (x, y);
		}
		return null;
	}
	
	#region NotifyTerrainChange implementation
	public void SceneChanged (Scene scene)
	{
	}

	public void SuccessionCompleted ()
	{
	}

	public void CellChangedToVisible (int cx, int cy, TerrainCell cell)
	{
	}

	public void CellChangedToInvisible (int cx, int cy)
	{
	}
	#endregion
}
