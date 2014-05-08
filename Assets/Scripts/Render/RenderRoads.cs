using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Ecosim.SceneData;
using Ecosim.Render;
using Ecosim;

public class RenderRoads : MonoBehaviour, NotifyTerrainChange
{
	
	private Roads roads;
	public static RenderRoads self;
	
	void Awake ()
	{
		self = this;
	}
	
	void Start ()
	{
		TerrainMgr.AddListener (this);
	}
	
	void OnDestroy ()
	{
		self = null;
		TerrainMgr.RemoveListener (this);
	}
	
	/**
	 * Defined in NotifyTerrainChange
	 */
	public void SceneChanged (Scene scene)
	{
		StopAllCoroutines ();
		if (roads != null) {
			// remove old roads...
			foreach (Roads.Road data in roads.roads) {
				RoadInstance instance = data.instance;
				if (instance != null) {
					Destroy (instance.gameObject);
				}
			}
			roads = null;
		}
		if (scene != null) {
			this.roads = scene.roads;
			// For now, we just render all the roads at once...
			RenderAllRoads ();
		}
	}

	/**
	 * Defined in NotifyTerrainChange
	 */
	public void SuccessionCompleted ()
	{
	}
		
	/**
	 * Defined in NotifyTerrainChange
	 */
	public void CellChangedToVisible (int cx, int cy, TerrainCell cell)
	{
	}
		
	/**
	 * Defined in NotifyTerrainChange
	 */
	public void CellChangedToInvisible (int cx, int cy)
	{
	}
	
	/**
	 * Creates road 'data', returns the instance
	 */
	public RoadInstance CreateRoadInstance (Roads.Road data)
	{
		GameObject go = (GameObject)Instantiate (data.prefab);
		go.layer = Layers.L_ROADS;
		Transform t = go.transform;
		t.parent = transform;
		t.localPosition = Vector3.zero;
		RoadInstance instance = go.GetComponent<RoadInstance> ();
		if (instance == null) {
			Debug.LogError ("Failed to find roadinstance in prefab '" + data.prefab.name + "'");
			Destroy (go);
		} else {
			instance.Setup (data);
			data.instance = instance;
		}
		return instance;
	}
	
	void RenderAllRoads ()
	{
		foreach (Roads.Road data in roads.roads) {
			RoadInstance instance = data.instance;
			if (!instance) {
				CreateRoadInstance (data);
			}
		}
	}
}
