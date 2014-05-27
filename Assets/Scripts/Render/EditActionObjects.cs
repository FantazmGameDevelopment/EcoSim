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

	private class ActionObjectGroupsData
	{
		public class BuildingInstanceState
		{
			public BuildingInstance instance;
			public ActionObject actionObject;
			public bool selected;

			public BuildingInstanceState (BuildingInstance instance, ActionObject actionObject)
			{
				this.instance = instance;
				this.actionObject = actionObject;
				selected = false;
			}
		}

		public ActionObjectsGroup group;
		public List<BuildingInstanceState> buildingInstances;

		public ActionObjectGroupsData (ActionObjectsGroup group)
		{
			this.group = group;
			buildingInstances = new List<BuildingInstanceState>();
		}

		public void Dispose ()
		{
			foreach (BuildingInstanceState bs in buildingInstances) {
				bs.instance = null;
				bs.actionObject = null;
			}
			buildingInstances.Clear ();
			buildingInstances = null;
		}
	}

	private List<ActionObjectGroupsData> groupsData;

	//private Dictionary<ActionObjectsGroup, List<BuildingInstance>> objectGroupBuildingInstances;
	//private Dictionary<ActionObjectsGroup, bool> selectionStates;
	private List<BuildingInstance> purchasableObjects;

	public delegate void ClickHandler (ActionObjectsGroup group, ActionObject actionObject, bool state);
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

		// Setup the groups data list
		groupsData = new List<ActionObjectGroupsData>();
		foreach (ActionObjectsGroup group in groups) {
			groupsData.Add (new ActionObjectGroupsData (group));
		}

		// We must loop through all buildings and check whether it's a building associated with an action object
		// if so, we must then check if it's a building that belong to one of our groups
		List<Buildings.Building> staticBuildings = new List<Buildings.Building>();
		foreach (Buildings.Building b in scene.buildings.GetAllBuildings ()) 
		{
			// Check if it's one of our target groups
			ActionObjectGroupsData targetGroupData = null;
			ActionObject targetActionObject = null;

			foreach (ActionObjectGroupsData grD in groupsData) {
				foreach (ActionObject obj in grD.group.actionObjects) {
					if (obj.building == b) 
					{
						targetGroupData = grD;
						targetActionObject = obj;
						break;
					}
				}
			}

			// Check whether the buildings of the group should be combined or loose.
			// If it's an active building and a part of our group then we should create a loose instance
			// for the building so we'll be able to click on it
			if (targetGroupData != null && !b.isActive)
			{
				BuildingInstance bInst = new BuildingInstance (b);
				targetGroupData.buildingInstances.Add (new ActionObjectGroupsData.BuildingInstanceState(bInst, targetActionObject));
				instances.Add (bInst);
			}
			else 
				// We add it to the 'static' buildings
				staticBuildings.Add (b);
		}

		scene.buildings.SetAllBuildings (staticBuildings);
		TerrainMgr.AddListener (this);
		StartCoroutine (COBlinkSelection());
	}

	public override void StopEditBuildings (Scene scene)
	{
		base.StopEditBuildings (scene);

		if (groupsData != null) 
		{
			foreach (ActionObjectGroupsData gd in groupsData) {
				gd.Dispose ();
			}
			groupsData.Clear ();
			groupsData = null;
		}
	}

	protected override IEnumerator COBlinkSelection ()
	{
		ActionObjectGroupsData gd;
		ActionObjectGroupsData.BuildingInstanceState bs;

		while (true) 
		{
			// Blink all non-selected building instances
			for (int x = 0; x < 2; x++)
			{
				// We use the x (only 0 and 1) to prevent double code
				bool active = (x == 0);

				for (int i = 0; i < groupsData.Count; i++) 
				{
					gd = groupsData[i];
					for (int n = 0; n < gd.buildingInstances.Count; n++) 
					{
						bs = gd.buildingInstances[n];
						if (bs.instance.instanceGO && !bs.selected) 
						{
							ActivateDeactivateRendering (bs.instance.instanceGO, active);
						}
					}
				}
				
				if (x == 0) yield return new WaitForSeconds(0.325f);
				else yield return new WaitForSeconds (0.125f);
			}
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
		// Loop through all group datas to find the instance this building belongs to
		foreach (ActionObjectGroupsData grD in groupsData)
		{
			foreach (ActionObjectGroupsData.BuildingInstanceState bs in grD.buildingInstances)
			{
				if (bs.instance.building == building)
				{
					// Toggle the instance or instances according the action group type
					switch (grD.group.groupType)
					{
					case ActionObjectsGroup.GroupType.Combined :
						// Toggle the state of the entire group and enable the rendering if selected
						bs.selected = !bs.selected;
						foreach (ActionObjectGroupsData.BuildingInstanceState b in grD.buildingInstances) 
						{
							b.selected = bs.selected;
							if (b.selected) ActivateDeactivateRendering (b.instance.instanceGO, true);
						}
						break;

					case ActionObjectsGroup.GroupType.Collection :
						// Toggle the state of this building only
						bs.selected = !bs.selected;
						ActivateDeactivateRendering (bs.instance.instanceGO, true);
						break;
					}

					// Return the group the action object and the object's selected state
					if (this.clickHandler != null)
						this.clickHandler (grD.group, bs.actionObject, bs.selected); 
				}
			}
		}
	}

	public void ProcessSelectedObjects ()
	{
		foreach (ActionObjectGroupsData grD in groupsData)
		{
			switch (grD.group.groupType)
			{
			// If it's a combined group and if only one of the building instances is marked as
			// selected that means the entire group is selected
			case ActionObjectsGroup.GroupType.Combined :
				if (grD.buildingInstances.Count > 0)
					grD.group.enabled = grD.buildingInstances[0].selected;
				break;
			
			// We should set the active state for every object seperately
			case ActionObjectsGroup.GroupType.Collection :
				foreach (ActionObjectGroupsData.BuildingInstanceState bs in grD.buildingInstances)
				{
					if (bs.selected) {
						bs.instance.building.isActive = true;
						bs.actionObject.enabled = true;
					}
				}
				break;
			}


		}
	}
}
