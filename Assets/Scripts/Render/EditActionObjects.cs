using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.Render;

public class EditActionObjects : EditBuildings
{
	// Note: 'instances' list is the list with all toggleable objects

	public static EditActionObjects instance;

	private List<ActionObjectsGroup> objectGroups;
	private Dictionary<BuildingInstance, bool> selectionStates;
	private List<BuildingInstance> purchasableObjects;

	public delegate void ClickHandler (ActionObjectsGroup group, bool state);
	public ClickHandler clickHandler;

	protected override void Awake ()
	{
		base.Awake ();
		instance = this;
	}

	protected override void OnDestroy ()
	{
		base.OnDestroy ();
		instance = null;
	}

	public void StartEditActionObjects (Scene scene, string[] groups, ClickHandler handler)
	{
		this.clickHandler = handler;

		// TODO: Use the string[] array to filter the groups
		objectGroups = new List<ActionObjectsGroup>();
		foreach (ActionObjectsGroup group in scene.actionObjectGroups) {
			objectGroups.Add (group);
		}

		// TODO: Check for the correct action object groups
		selectionStates = new Dictionary<BuildingInstance, bool>();
		instances = new List<BuildingInstance> ();

		List<Buildings.Building> currentBuildings = new List<Buildings.Building>();
		foreach (Buildings.Building b in scene.buildings.GetAllBuildings ()) 
		{
			// Check if the building is part of the groups
			bool correctGroup = false;
			foreach (ActionObjectsGroup g in objectGroups) {
				foreach (ActionObject obj in g.actionObjects) {
					if (obj.building == b) {
						correctGroup = true;
						break;
					}
				}
				if (correctGroup) break;
			}

			if (correctGroup && !b.isActive) 
			{
				instances.Add (new BuildingInstance (b));
				selectionStates.Add (instances[instances.Count - 1], false);
			}
			else 
				currentBuildings.Add (b);
		}

		dict = new Dictionary<GameObject, BuildingInstance> ();

		scene.buildings.SetAllBuildings (currentBuildings);
		TerrainMgr.AddListener (this);

		StartCoroutine (COBlinkSelection());
	}

	protected override IEnumerator COBlinkSelection ()
	{
		while (true) 
		{
			// Make the others blink
			foreach (KeyValuePair<BuildingInstance, bool> pair in selectionStates) {
				if (pair.Key.instanceGO && !pair.Value) 
					ActivateDeactivateRendering (pair.Key.instanceGO, false);
			}

			yield return new WaitForSeconds(0.125f);

			foreach (KeyValuePair<BuildingInstance, bool> pair in selectionStates) {
				if (pair.Key.instanceGO && !pair.Value) 
					ActivateDeactivateRendering (pair.Key.instanceGO, true);
			}

			yield return new WaitForSeconds(0.125f);
		}
	}

	protected override void ActivateDeactivateRendering (GameObject go, bool active)
	{
		base.ActivateDeactivateRendering (go, active);
	}

	void Update ()
	{
		if (instances == null) return;

		if (CameraControl.IsNear && Camera.main)
		{
			if (Input.GetMouseButtonDown (0))
			{
				Ray screenRay = Camera.main.ScreenPointToRay (Input.mousePosition);
				RaycastHit hit;

				if (Physics.Raycast (screenRay, out hit, Mathf.Infinity, Layers.M_EDIT1)) 
				{
					GameObject go = hit.collider.gameObject;
					Buildings.Building building = self.GetBuildingForGO (go);
					BuildingClicked (building);
				}
			}
		}
	}

	protected void BuildingClicked (Buildings.Building building)
	{
		foreach (BuildingInstance instance in instances) 
		{
			if (instance.building == building) 
			{
				ActionObjectsGroup group = GetActionObjectGroup (building);
				if (group != null)
				{
					// Toggle the state of the building
					bool currState = selectionStates [instance];
					currState = !currState;

					// We save the state, we set the isActive of the building in the FinishedSelection
					selectionStates [instance] = currState;

					if (clickHandler != null)
						clickHandler (group, currState);

					// TODO: Save the building state in xml
					if (currState && instance.instanceGO)
						ActivateDeactivateRendering (instance.instanceGO, true);
				}
			}
		}
	}

	private ActionObjectsGroup GetActionObjectGroup (Buildings.Building building)
	{
		foreach (ActionObjectsGroup g in objectGroups) {
			foreach (ActionObject obj in g.actionObjects) {
				if (obj.building == building) {
					return g;
				}
			}
		}
		return null;
	}

	public void ProcessSelectedObjects ()
	{
		foreach (KeyValuePair<BuildingInstance, bool> pair in selectionStates) {
			if (pair.Value == true)
				pair.Key.building.isActive = true;
		}
	}
}
