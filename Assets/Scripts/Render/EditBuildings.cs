using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.Render;

/**
 * Manages buildings when editing buildings. Normally buildings are rendered by
 * terrainmgr, but as terrainmgr combines meshes, we turn this off and render
 * buildings through the EditBuildings class instead.
 * 
 * Building GameObjects will have a collider and are placed in layer EDIT1
 */
public class EditBuildings : MonoBehaviour, NotifyTerrainChange
{
	public static EditBuildings self;
	protected BuildingInstance selected;
	
	protected virtual void Awake ()
	{
		self = this;
	}
	
	protected virtual void OnDestroy ()
	{
		self = null;
	}
	
	protected class BuildingInstance
	{
		public BuildingInstance (Buildings.Building building)
		{
			this.building = building;
			CalculateCellKey ();
		}
		
		public void CalculateCellKey ()
		{
			int cellX = (int)(building.position.x / (TerrainMgr.CELL_SIZE * TerrainMgr.TERRAIN_SCALE));
			int cellY = (int)(building.position.z / (TerrainMgr.CELL_SIZE * TerrainMgr.TERRAIN_SCALE));
			cellKey = TerrainMgr.CoordToKey (cellX, cellY);
		}
		
		public void Instantiate ()
		{
			if (!instanceGO) {
				// GameObject go = (GameObject)GameObject.Instantiate (building.prefab.prefab, building.position, building.rotation);
				GameObject go = building.prefab.Instantiate (building.position, building.rotation, building.scale);
				go.AddComponent<MeshCollider> ();
				go.layer = Layers.L_EDIT1;
				instanceGO = go;
				EditBuildings.self.dict.Add (go, this);
			}
		}
		
		public void DestroyInstance ()
		{
			if (instanceGO) {
				EditBuildings.self.dict.Remove (instanceGO);
				GameObject.Destroy (instanceGO);
				instanceGO = null;
			}
		}
		
		public int cellKey; // determines in which cell instance belongs
		public readonly Buildings.Building building;
		public GameObject instanceGO;
	}
	
	
	/**
	 * We're lazy and keep all instances for all 'cells' in one list
	 * we use BuildingInstance.cellKey to determine in which cell the instance belongs
	 */
	protected List<BuildingInstance> instances;
	
	/**
	 * Building instances referred by Game Object
	 */
	protected Dictionary<GameObject, BuildingInstance> dict;

	/**
	 * Building has changed and needs to be redrawn (if visible)
	 * Should only be called when started editing buildings (StartEditBuildings)
	 */
	public void BuildingChanged (Buildings.Building building)
	{
		foreach (BuildingInstance instance in instances) {
			if (instance.building == building) {
				if (instance.instanceGO) {
					instance.DestroyInstance ();
					instance.CalculateCellKey ();
					if (TerrainMgr.self.TileIsVisible (instance.cellKey)) {
						instance.Instantiate ();
					}
				}
				break;
			}
		}
	}
	
	/**
	 * Destroys building 'building'
	 * Should only be called when started editing buildings (StartEditBuildings)
	 */
	public void DestroyBuilding (Buildings.Building building)
	{
		foreach (BuildingInstance instance in instances) {
			if (instance.building == building) {
				if (instance == selected) {
					selected = null;
				}
				instance.DestroyInstance ();
				instances.Remove (instance);
				break;
			}
		}
	}
		
	/**
	 * Adds building 'building' (and renders it if neccessary)
	 * Should only be called when started editing buildings (StartEditBuildings)
	 */
	public void AddBuilding (Buildings.Building building)
	{
		BuildingInstance instance = new BuildingInstance (building);
		instance.CalculateCellKey ();
		if (TerrainMgr.self.TileIsVisible (instance.cellKey)) {
			instance.Instantiate ();
		}
		instances.Add (instance);
	}
	
	public Buildings.Building GetBuildingForGO (GameObject go)
	{
		BuildingInstance result;
		if (dict.TryGetValue (go, out result)) {
			return result.building;
		}
		return null;
	}

	public GameObject GetGameObjectForBuilding (Buildings.Building building)
	{
		foreach (KeyValuePair<GameObject, BuildingInstance> pair in dict) {
			if (pair.Value.building == building) {
				return pair.Key;
			}
		}
		return null;
	}
	
	public void MarkBuildingSelected (Buildings.Building building)
	{
		ClearSelection ();
		foreach (BuildingInstance instance in instances) {
			if (instance.building == building) {
				selected = instance;
				return;
			}
		}
	}
	
	public Buildings.Building GetSelection ()
	{
		if (selected != null) {
			return selected.building;
		}
		return null;
	}
	
	public void ClearSelection ()
	{
		if (selected != null) {
			if (selected.instanceGO) {
				ActivateDeactivateRendering (selected.instanceGO, true);
			}
			selected = null;
		}
	}
	
	protected virtual void ActivateDeactivateRendering (GameObject go, bool active)
	{
		if (go == null)
			return;

		foreach (Component c in go.GetComponentsInChildren<MeshRenderer>()) {
			MeshRenderer mr = (MeshRenderer)c;
			mr.enabled = active;
		}
	}
	
	public virtual void StartEditBuildings (Scene scene)
	{
		selected = null;
		instances = new List<BuildingInstance> ();
		foreach (Buildings.Building b in scene.buildings.GetAllBuildings ()) {
			instances.Add (new BuildingInstance (b));
		}
		dict = new Dictionary<GameObject, BuildingInstance> ();

		// temporarely remove buildings from 'buildings' class
		// as we are going to draw all buildings from within this
		// class
		scene.buildings.SetAllBuildings (null);
		TerrainMgr.AddListener (this);
		StartCoroutine (COBlinkSelection());
	}
	
	public virtual void StopEditBuildings (Scene scene)
	{
		selected = null;
		TerrainMgr.RemoveListener (this);
		List<Buildings.Building> buildings = new List<Buildings.Building> ();
		foreach (BuildingInstance bi in instances) {
			buildings.Add (bi.building);
			bi.DestroyInstance ();
		}
		// Add all the other current buildings
		buildings.AddRange (scene.buildings.GetAllBuildings ());
		scene.buildings.SetAllBuildings (buildings);
		instances = null;
		StopAllCoroutines ();
	}
	
	protected virtual IEnumerator COBlinkSelection ()
	{
		while (true) {
			if ((selected != null) && (selected.instanceGO)) {
				ActivateDeactivateRendering (selected.instanceGO, true);
			}
			yield return new WaitForSeconds(0.25f);
			if ((selected != null) && (selected.instanceGO)) {
				ActivateDeactivateRendering (selected.instanceGO, false);
				yield return 0;
			}
		}
	}
	
	/**
	 * Defined in NotifyTerrainChange
	 */
	public void SceneChanged (Scene scene)
	{
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
		int key = TerrainMgr.CoordToKey (cx, cy);
		foreach (BuildingInstance bi in instances) {
			if (bi.cellKey == key) {
				bi.Instantiate ();
			}
		}
	}
		
	/**
	 * Defined in NotifyTerrainChange
	 */
	public void CellChangedToInvisible (int cx, int cy)
	{
		int key = TerrainMgr.CoordToKey (cx, cy);
		// we use instances.ToArray so we can change the instance list within the loop
		foreach (BuildingInstance bi in instances.ToArray()) {
			if (bi.cellKey == key) {
				bi.DestroyInstance ();
			}
		}
	}	
}