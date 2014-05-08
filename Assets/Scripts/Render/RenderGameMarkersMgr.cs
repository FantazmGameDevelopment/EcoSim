using UnityEngine;
using System.Collections.Generic;
using Ecosim;
using Ecosim.Render;
using Ecosim.SceneData;

public class RenderGameMarkersMgr : MonoBehaviour, NotifyTerrainChange
{

	public GameObject markerPrefab;
	public Scene scene;
	public HeightMap heights;
	private string showText = null;
	private float showTextTimeout;
	private Rect showTextR;
	
	public delegate void ClickHandler (Coordinate c);
	
	
	private static RenderGameMarkersMgr self;

	void Awake ()
	{
		self = this;
		queue = new List<MarkerInfo> ();
		dict = new Dictionary<string, RenderGameMarkers> ();
	}
	
	void OnDestroy ()
	{
		TerrainMgr.RemoveListener (this);
	}
	
	void Start ()
	{
		TerrainMgr.AddListener (this);
	}
	
	class MarkerInfo
	{
		public bool isAdding = true;
		public string mapName;
		public string[] models;
		public ClickHandler handler;
	}
	
	private List<MarkerInfo> queue;
	private Dictionary<string, RenderGameMarkers> dict;
	private volatile int queueCounter = 0;
	
	public static void AddGameMarkers (string mapName, string[] models, ClickHandler handler)
	{
		MarkerInfo mi = new MarkerInfo ();
		mi.mapName = mapName;
		mi.models = models;
		mi.handler = handler;
		lock (self.queue) {
			self.queue.Add (mi);
		}
		self.queueCounter++;
	}

	public static void RemoveGameMarkers (string mapName)
	{
		MarkerInfo mi = new MarkerInfo ();
		mi.mapName = mapName;
		mi.isAdding = false;
		lock (self.queue) {
			self.queue.Add (mi);
		}
		self.queueCounter++;
	}
	
	void DoAddRemoveGameMarkers (MarkerInfo markerInfo)
	{
		if (markerInfo.isAdding) {
			if (scene.progression.HasData (markerInfo.mapName)) {
				Data map = scene.progression.GetData (markerInfo.mapName);
				ExtraAssets.AssetObjDef[] objects = null;
				if (markerInfo.models != null) {
					objects = new ExtraAssets.AssetObjDef[markerInfo.models.Length];
					for (int i = 0; i < markerInfo.models.Length; i++) {
						objects[i] = scene.assets.GetObjectDef (markerInfo.models[i]);
					}
				}
				dict.Add (markerInfo.mapName, AddGamerMarkersInstance (map, objects, markerInfo.handler));
			}
		} else {
			GameObject.Destroy (dict [markerInfo.mapName].gameObject);
			dict.Remove (markerInfo.mapName);
		}
	}
	
	public RenderGameMarkers AddGamerMarkersInstance (Data map, ExtraAssets.AssetObjDef[] objects, ClickHandler fn)
	{
		GameObject go = new GameObject ("gamemarkers");
		go.transform.parent = transform;
		go.transform.localPosition = Vector3.zero;
		RenderGameMarkers rgm = go.AddComponent <RenderGameMarkers> ();
		rgm.parent = this;
		rgm.map = map;
		rgm.objects = objects;
		rgm.handler = fn;
		return rgm;
	}

	#region NotifyTerrainChange implementation
	void NotifyTerrainChange.SceneChanged (Scene scene)
	{
		foreach (RenderGameMarkers rgm in dict.Values) {
			GameObject.Destroy (rgm.gameObject);
		}
		this.scene = scene;
		if (scene != null) {
			heights = scene.progression.GetData <HeightMap> (Progression.HEIGHTMAP_ID);
		}
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
	
	void Update ()
	{
		if ((scene != null) && (queueCounter > 0)) {
			MarkerInfo mi;
			lock (queue) {
				mi = queue [0];
				queue.RemoveAt (0);
				queueCounter --;
			}
			DoAddRemoveGameMarkers (mi);
		}
		if (CameraControl.IsNear && Camera.main) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast (ray, out hit, Mathf.Infinity, Layers.M_EDIT2 | Layers.L_TERRAIN)) {
				Transform hitT = hit.transform;
				int x = (int)(hitT.position.x / TerrainMgr.TERRAIN_SCALE);
				int y = (int)(hitT.position.z / TerrainMgr.TERRAIN_SCALE);
				RenderGameMarkers rgm = null;
				if (hitT.parent != null) {
					rgm = hitT.parent.gameObject.GetComponent <RenderGameMarkers> ();
				}
				if (rgm != null) {
					showText = rgm.GetTextAtCoordinate (x, y);
					if (showText != null) {
						float mx = Input.mousePosition.x + 20;
						float my = (Screen.height - Input.mousePosition.y) - 150;
						if (mx > Screen.width - 250) {
							mx -= 240;
						}
						my = Mathf.Clamp (my, 200, Screen.height - 400);
						showTextR = new Rect (mx, my, 200, 300);
						showTextTimeout = Time.timeSinceLevelLoad + 2f;
					}
					if ((Input.GetMouseButtonDown (0)) && (rgm.handler != null)) {
						rgm.HandleMouseDown (new Coordinate(x, y));
					}
				}
				else {
					foreach (RenderGameMarkers rgm2 in GetComponentsInChildren <RenderGameMarkers> ()) {
						rgm2.HandleMouseDown (new Coordinate (x, y));
					}
				}
			}
		}
	}
	
	void OnGUI ()
	{
		if (showText != null) {
			GUISkin skin = GameControl.self.skin;
			GUI.skin = skin;
			GUI.depth = 101;
			GUI.Label (showTextR, showText, skin.FindStyle ("Arial12-50-formatted"));
			if (Time.timeSinceLevelLoad > showTextTimeout) {
				showText = null;
			}
		}
	}
}
