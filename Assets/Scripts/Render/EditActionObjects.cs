using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.Render;

public class EditActionObjects : EditBuildings
{
	// Note: 'instances' list is the list with all toggleable objects

	// TODO: Make a difference between Combined and Collection groups

	public static EditActionObjects instance;

	private List<ActionObjectsGroup> objectGroups;
	private Dictionary<ActionObjectsGroup, List<BuildingInstance>> objectGroupBuildingInstances;
	private Dictionary<ActionObjectsGroup, bool> selectionStates;
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

	public void StartEditActionObjects (Scene scene, ActionObjectsGroup[] groups, ClickHandler handler)
	{
		// Necessary for base class
		instances = new List<BuildingInstance>();
		dict = new Dictionary<GameObject, BuildingInstance> ();

		this.clickHandler = handler;
		
		objectGroups = new List<ActionObjectsGroup>(groups);
		objectGroupBuildingInstances = new Dictionary<ActionObjectsGroup, List<BuildingInstance>>();
		selectionStates = new Dictionary<ActionObjectsGroup, bool>();

		// We must loop through all buildings and check whether it's a building associated with an action object
		// if so, we must then check if it's a building that belong to one of our groups
		List<Buildings.Building> staticBuildings = new List<Buildings.Building>();
		foreach (Buildings.Building b in scene.buildings.GetAllBuildings ()) 
		{
			// Check if it's one of our groups
			ActionObjectsGroup matchingGroup = null;
			foreach (ActionObjectsGroup g in objectGroups) {
				foreach (ActionObject obj in g.actionObjects) {
					if (obj.building == b) {
						matchingGroup = g;
						break;
					}
				}
			}

			if (matchingGroup != null && !b.isActive)
			{
				if (!objectGroupBuildingInstances.ContainsKey (matchingGroup)) 
				{
					objectGroupBuildingInstances.Add (matchingGroup, new List<BuildingInstance>());
					selectionStates.Add (matchingGroup, false);
				}

				BuildingInstance newInst = new BuildingInstance(b);
				objectGroupBuildingInstances[matchingGroup].Add (newInst);
				instances.Add (newInst);
			}
			else 
				staticBuildings.Add (b);
		}

		scene.buildings.SetAllBuildings (staticBuildings);
		TerrainMgr.AddListener (this);
		StartCoroutine (COBlinkSelection());
	}

	protected override IEnumerator COBlinkSelection ()
	{
		while (true) 
		{
			// Make the others blink
			foreach (KeyValuePair<ActionObjectsGroup, bool> pair in selectionStates) {
				foreach (BuildingInstance bInst in objectGroupBuildingInstances[pair.Key]) {
					if (bInst.instanceGO && !pair.Value) 
						ActivateDeactivateRendering (bInst.instanceGO, false);
				}
			}

			yield return new WaitForSeconds(0.125f);

			foreach (KeyValuePair<ActionObjectsGroup, bool> pair in selectionStates) {
				foreach (BuildingInstance bInst in objectGroupBuildingInstances[pair.Key]) {
					if (bInst.instanceGO && !pair.Value) 
						ActivateDeactivateRendering (bInst.instanceGO, true);
				}
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
		// Get the matching building instance
		foreach (BuildingInstance instance in instances) 
		{
			// Find the list linked to the action object group where this instance belongs to
			if (instance.building == building) 
			{
				foreach (KeyValuePair<ActionObjectsGroup, List<BuildingInstance>> pair in objectGroupBuildingInstances)
				{
					if (pair.Value.Contains (instance))
					{
						// Toggle the action object group state
						bool newState = !selectionStates[pair.Key];
						selectionStates [pair.Key] = newState;

						if (clickHandler != null)
							clickHandler (pair.Key, newState);

						// Enable all instances if we're enabled
						if (newState)
						{
							foreach (BuildingInstance bInst in pair.Value) {
								if (bInst.instanceGO)
									ActivateDeactivateRendering (bInst.instanceGO, true);
							}
						}
						return;
					}
				}
			}
		}

		/*foreach (BuildingInstance instance in instances) 
		{
			if (instance.building == building) 
			{
				ActionObjectsGroup group = GetActionObjectGroup (building);
				if (group != null)
				{
					// Get all instances belonging to the group


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
		}*/
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
		foreach (KeyValuePair<ActionObjectsGroup, bool> pair in selectionStates) 
		{
			if (pair.Value == true)
			{
				pair.Key.enabled = true;

				foreach (BuildingInstance bInst in objectGroupBuildingInstances[pair.Key])
				{
					bInst.building.isActive = true;
				}
			}
		}
	}
}
