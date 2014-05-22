using UnityEngine;
using System.Collections.Generic;
using Ecosim;
using Ecosim.Render;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;

public class RenderResearchPointsMgr : MonoBehaviour, NotifyTerrainChange
{
	public ResearchPointMarker researchPointPrefab;
	public Scene scene;
	public HeightMap heights;

	private static RenderResearchPointsMgr self;

	private List<ResearchPoint> researchPoints {
		get { 
			if (scene != null) {
				return scene.progression.researchPoints;
			} else return new List<ResearchPoint>();
		}
	}
	private Dictionary<ResearchPoint, ResearchPointMarker> markers;

	void Awake ()
	{
		self = this;

		markers = new Dictionary<ResearchPoint, ResearchPointMarker>();
	}
	
	void OnDestroy ()
	{
		TerrainMgr.RemoveListener (this);
	}
	
	void Start ()
	{
		TerrainMgr.AddListener (this);
	}

	public static ResearchPoint GetResearchPointAt (int x, int y)
	{
		foreach (ResearchPoint rp in self.researchPoints) {
			if (rp.x == x && rp.y == y) {
				return rp;
			}
		}

		// Make a new one
		ResearchPoint newRP = new ResearchPoint (x, y);
		self.researchPoints.Add (newRP);
		self.AddNewMarkerInstance (newRP);
		return newRP;
	}

	public static void DeleteResearchPoint (ResearchPoint rp)
	{
		ResearchPointMarker marker = GetMarkerOf (rp);
		self.markers.Remove (rp);
		self.DestroyMarkerInstance (marker);
		self.researchPoints.Remove (rp);
	}

	public static ResearchPointMarker GetMarkerOf (ResearchPoint rp)
	{
		return self.markers [rp];
	}

	public static void AddResearchPoints ()
	{

	}
	
	public static void RemoveResearchPoints ()
	{

	}
	
	#region NotifyTerrainChange implementation
	void NotifyTerrainChange.SceneChanged (Scene scene)
	{
		this.scene = scene;

		if (researchPoints != null)
		{
			// Remove all markers
			foreach (KeyValuePair<ResearchPoint, ResearchPointMarker> pair in markers) {
				DestroyMarkerInstance (pair.Value);
			}
			markers.Clear ();
		}

		if (scene != null)
		{
			heights = scene.progression.GetData <HeightMap> (Progression.HEIGHTMAP_ID);

			ShowMarkers ();
		}

		/*StopAllCoroutines ();
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
		 */ 

	}
	
	void NotifyTerrainChange.SuccessionCompleted ()
	{
	}
	
	void NotifyTerrainChange.CellChangedToVisible (int cx, int cy, TerrainCell cell)
	{
	}
	
	void NotifyTerrainChange.CellChangedToInvisible (int cx, int cy)
	{
	}
	#endregion

	private void ShowMarkers ()
	{
		foreach (ResearchPoint rp in researchPoints)
		{
			AddNewMarkerInstance (rp);
		}
	}
	
	private void AddNewMarkerInstance (ResearchPoint rp)
	{
		ResearchPointMarker marker = ((GameObject)GameObject.Instantiate (researchPointPrefab.gameObject)).GetComponent <ResearchPointMarker>();
		marker.researchPoint = rp;
		marker.transform.parent = transform;

		float x = (0.5f + rp.x) * TerrainMgr.TERRAIN_SCALE;
		float y = (0.5f + rp.y) * TerrainMgr.TERRAIN_SCALE;
		float h = heights.GetInterpolatedHeight (x, y) + 1f;

		marker.transform.localPosition = new Vector3 (x, h, y);
		markers.Add (rp, marker);
		//marker.layer = Layers.L_EDIT2;
	}

	private void DestroyMarkerInstance (ResearchPoint rp) {
		DestroyMarkerInstance( markers[rp] );
	}
	private void DestroyMarkerInstance (ResearchPointMarker marker) {
		GameObject.Destroy (marker.gameObject);
	}

	private Vector3 oldCameraPos;
	private Vector3 oldCameraFwd;
	private Transform cameraTransform;

	void Update() 
	{
		if (CameraControl.IsNear && scene != null)
		{
			if (cameraTransform == null)
				cameraTransform = CameraControl.self.nearCamera.transform;

			if ((cameraTransform.position != oldCameraPos) || (cameraTransform.forward != oldCameraFwd)) 
			{
				oldCameraPos = cameraTransform.position;
				oldCameraFwd = cameraTransform.forward;
				UpdateRotations(oldCameraPos, oldCameraFwd);
			}
		}
	}

	void UpdateRotations (Vector3 pos, Vector3 fwd) 
	{
		foreach (KeyValuePair<ResearchPoint, ResearchPointMarker> marker in markers) {
			marker.Value.UpdateRotation (pos, fwd);
		}
	}

	#region GUI

	private ResearchPointResultsWindow resultsWindow;

	public static void SetMessage(ResearchPoint rp, string message, Vector3 position) 
	{
		if (self.resultsWindow == null) {
			self.resultsWindow = new ResearchPointResultsWindow ();
		}

		Vector3 screenPos = Camera.main.WorldToScreenPoint(position);
		self.resultsWindow.UpdateMessage (message, screenPos);
		self.resultsWindow.researchPoint = rp;
	}
	
	public static void ClearMessage(ResearchPoint rp) 
	{
		if (self.resultsWindow != null && self.resultsWindow.researchPoint == rp) {
			self.resultsWindow.Close ();
			self.resultsWindow = null;
		}
	}

	#endregion GUI
}
