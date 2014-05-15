using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleRoads : PanelHelper
	{
//		readonly MapsPanel parent;
		readonly EditorCtrl ctrl;
		readonly Scene scene;
		Roads roads; // all roads
		Roads.Road activeRoad = null; // which road we are editing
		HandleInstance activeHandle; // which handle of the road is active
		Material handleMaterial; // material for handle
		Material activeHandleMaterial; // material for active handle (== red)
		bool isDragging = false; // handle is being dragged
		bool isCreating = false; // new road is being created
		private Texture2D renderTex;
		
		Dictionary<GameObject, HandleInstance> handles; // all handles for active road
		
		string[] roadNames; // names of different road types
		int activeRoadIndex; // index into road names pointing to active type
		
		private class HandleInstance
		{
			public HandleInstance (int index, GameObject handleGO)
			{
				this.index = index;
				this.handleGO = handleGO;
			}
			
			readonly public int index;
			readonly public GameObject handleGO;
		}
		
		public HandleRoads (EditorCtrl ctrl, MapsPanel parent, Scene scene)
		{
			this.ctrl = ctrl;
//			this.parent = parent;
			this.scene = scene;
			roads = scene.roads;
			
			handles = new Dictionary<GameObject, HandleInstance> ();
			List<string> roadNamesList = new List<string> ();
			foreach (GameObject go in EcoTerrainElements.self.roadPrefabs) {
				roadNamesList.Add (go.name);
			}
			roadNames = roadNamesList.ToArray ();
			renderTex = new Texture2D (360, 300, TextureFormat.RGB24, false);
			Setup ();
		}

		void Setup ()
		{
			handleMaterial = ctrl.handlePrefab.GetComponent<MeshRenderer> ().sharedMaterial;
			activeHandleMaterial = new Material (handleMaterial);
			activeHandleMaterial.color = Color.red;
			RoadHandles.self.ShowAllHandles (scene);
			RenderPreviewTexture ();
		}

		/*void RenderPreviewTexture () {
			(GameObject.FindObjectOfType <MonoBehaviour> () as MonoBehaviour).StartCoroutine (CORenderPreviewTexture());
		}*/

		void RenderPreviewTexture () {
		//IEnumerator CORenderPreviewTexture () {
			RenderTileIcons.RenderSettings rs = new RenderTileIcons.RenderSettings (60f, 30f, 30f, 5f);
			//yield return new WaitForEndOfFrame ();
			RenderTileIcons.self.Render (rs, ref renderTex, scene.successionTypes[0].vegetations[0].tiles[0], null, null,
				EcoTerrainElements.GetRoadPrefab (roadNames [activeRoadIndex]));
		}
		
		public bool Render (int mx, int my)
		{
			GUILayout.Space (8);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", GUILayout.Width (100));
			if (GUILayout.Button (roadNames [activeRoadIndex], GUILayout.Width (140))) {
				ctrl.StartSelection (roadNames, activeRoadIndex,
					newIndex => {
					if (newIndex != activeRoadIndex) {
						activeRoadIndex = newIndex;
						RenderPreviewTexture ();
					}
				});
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			
			if (activeRoad != null) {
				if (isCreating) {
					GUILayout.Label ("Click on terrain to add nodes to the new road. You can move handles by dragging them." +
						"Finish editing road by pressing 'N' again or by connecting road to an object.");
				} else if (activeHandle != null) {
					GUILayout.Label ("You can move handles by dragging them, add handles by right-clicking " +
					 	"(or ctrl-left-clicking), delete handles by pressing 'Delete'. Move the start or end handle " +
					 	"to a connection point on road object to connect it.");
				} else {
					GUILayout.Label ("Click on a node on the road to edit the road. Click on terrain to stop editing the road.");
				}
			} else {
				GUILayout.Label ("Click on a road to edit the road. Press 'N' to create new road of type '" + roadNames [activeRoadIndex] + "'");
			}

			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.Label (renderTex, GUIStyle.none);
			}
			GUILayout.EndVertical ();
			return false;
		}
		
		void DeselectRoad ()
		{
			if (activeRoad != null) {
				foreach (HandleInstance hi in handles.Values) {
					GameObject.Destroy (hi.handleGO);
				}
				handles.Clear ();
				activeRoad = null;
			}
		}
		
		void SelectRoad (GameObject road)
		{
			activeHandle = null;
			DeselectRoad ();
			
			activeRoad = road.GetComponent<RoadInstance> ().roadData;
			activeRoad.instance.UpdatePath ();
			int index = 0;
			foreach (Vector3 pnt in activeRoad.points) {
				GameObject go = (GameObject)GameObject.Instantiate (ctrl.handlePrefab, pnt, Quaternion.identity);
				go.name = "Handle " + index;
				HandleInstance hi = new HandleInstance (index, go);
				handles.Add (go, hi);
				index++;
			}
		}
		
		bool SelectHandle (GameObject handle)
		{
			HandleInstance hi;
			if (handles.TryGetValue (handle, out hi)) {
				if ((hi != activeHandle) && (activeHandle != null)) {
					activeHandle.handleGO.GetComponent<MeshRenderer> ().sharedMaterial = handleMaterial;
				}
				activeHandle = hi;
				activeHandle.handleGO.GetComponent<MeshRenderer> ().sharedMaterial = activeHandleMaterial;
				return true;
			}
			return false;
		}
	
		bool SelectHandleByIndex (int index)
		{
			foreach (HandleInstance hi in handles.Values) {
				if (hi.index == index) {
					activeHandle = hi;
					activeHandle.handleGO.GetComponent<MeshRenderer> ().sharedMaterial = activeHandleMaterial;
					return true;
				}
			}
			return false;
		}

		public void Disable ()
		{
			DeselectRoad ();
			RoadHandles.self.HideAllHandles ();
			UnityEngine.Object.Destroy (renderTex);
		}

		public void Update ()
		{
			bool ctrlKey = Input.GetKey (KeyCode.LeftControl) | Input.GetKey (KeyCode.RightControl);
			Vector3 mousePos = Input.mousePosition;
			if ((CameraControl.MouseOverGUI) ||
			Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt)) {
				return;
			}
			if (Input.GetKeyDown (KeyCode.N)) {
				if (isCreating) {
					// finish road
					isCreating = false;
				} else {
					DeselectRoad ();
					isCreating = true;
					Vector3 dir = Vector3.zero;
					Vector3 pos;
					Ray screenRay = Camera.main.ScreenPointToRay (mousePos);
					RaycastHit hit;
					if (Physics.Raycast (screenRay, out hit, 10000f, Layers.M_TERRAIN | Layers.M_EDIT2)) {
						GameObject go = hit.collider.gameObject;
						if (go.layer == Layers.L_EDIT2) {
							// we clicked on road object (bridge, intersection, ...)
							Transform hitT = hit.transform;
							pos = hitT.position;
							dir = hitT.rotation * Vector3.forward;
						} else {
							pos = hit.point;
						}
						
						
						
						Roads.Road newRoad = new Roads.Road ();
						newRoad.prefab = EcoTerrainElements.GetRoadPrefab (roadNames [activeRoadIndex]);
						newRoad.points = new List<Vector3> ();
						newRoad.points.Add (pos);
						newRoad.startCtrl = dir;
						RoadInstance instance = RenderRoads.self.CreateRoadInstance (newRoad);
						if (instance != null) {
							roads.roads.Add (newRoad);
							SelectRoad (instance.gameObject);
							SelectHandleByIndex (0);
						}
					}
				}
			}
			if (Input.GetMouseButtonDown (0) && !ctrlKey) {
				// start left mouse click (if ctrl is pressed we ignore it, as we handle
				// it later as being a right mouse click for Mac users).
				Ray screenRay = Camera.main.ScreenPointToRay (mousePos);
				RaycastHit hit;
				if (Physics.Raycast (screenRay, out hit, 10000f, Layers.M_TERRAIN | Layers.M_ROADS | Layers.M_EDIT1)) {
					GameObject go = hit.collider.gameObject;
					if (!isCreating && (go.layer == Layers.L_ROADS)) {
						SelectRoad (go);
					} else if (go.layer == Layers.L_EDIT1) {
						HandleInstance oldActive = activeHandle;
						isDragging = SelectHandle (go) && (oldActive == activeHandle);
					} else {
						// clicked on terrain
						if (isCreating) {
							Vector3 pos = hit.point;
							Vector3 dir = Vector3.zero;
							if (Physics.Raycast (screenRay, out hit, 10000f, Layers.M_EDIT2)) {
								// we clicked on road object (bridge, intersection, ...)
								Transform hitT = hit.transform;
								pos = hitT.position;
								dir = hitT.rotation * Vector3.forward;
								isCreating = false;
							}
							int count = activeRoad.instance.AddNode (pos);
							activeRoad.instance.MoveNodeTo (count, pos, dir);
							SelectRoad (activeRoad.instance.gameObject);
							SelectHandleByIndex (count);
						} else {
							DeselectRoad ();
						}
					}
				}
			}
			if (isDragging) {
				if (Input.GetMouseButton (0)) {
					int index = activeHandle.index;
					RoadInstance instance = activeRoad.instance;
					Ray screenRay = Camera.main.ScreenPointToRay (mousePos);
					RaycastHit hit;
					if (instance.IndexIsEndNode (index) && Physics.Raycast (screenRay, out hit, 10000f, Layers.M_EDIT2)) {
						Transform hitT = hit.transform;
						instance.MoveNodeTo (index, hitT.position, hitT.rotation * Vector3.forward);
						activeHandle.handleGO.transform.position = hitT.position;
					} else {
						Vector3 point;
						if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out point)) {
							instance.MoveNodeTo (index, point, Vector3.zero);
							activeHandle.handleGO.transform.position = point;
						}
					}
				} else {
					isDragging = false;
				}
			}
			if (Input.GetMouseButton (0) && ctrlKey) {
				if (activeHandle != null) {
					Vector3 point;
					if (TerrainMgr.TryScreenToTerrainCoord (mousePos, out point)) {
						int index = activeHandle.index;
						RoadInstance instance = activeRoad.instance;
						int count = instance.GetNodeCount ();
						instance.AddNode (point);
						if (index < count - 1) {
							// add node added a node to the end, but if (index < count - 1) we actually want to
							// insert a node, nod append one... To fix this we move nodes after index up
							for (int i = count; i > index; i--) {
								instance.MoveNodeTo (i, instance.GetNodePosition (i - 1), Vector3.zero);
							}
							instance.MoveNodeTo (index + 1, point, Vector3.zero);
						}
						SelectRoad (activeRoad.instance.gameObject);
						SelectHandleByIndex (index + 1);
					}
				}
			}
			if (Input.GetKeyDown (KeyCode.Delete) || Input.GetKeyDown (KeyCode.Backspace)) {
				if (activeHandle != null) {
					RoadInstance instance = activeRoad.instance;
					int count = instance.GetNodeCount ();
					if (count > 2) {
						int index = activeHandle.index;
						instance.DeleteNode (index);
						GameObject roadGO = activeRoad.instance.gameObject;
						DeselectRoad ();
						SelectRoad (roadGO);
						if (index >= count - 1)
							index --;
						SelectHandleByIndex (index);
					}
				} else {
					if (activeRoad != null) {
						roads.roads.Remove (activeRoad);
						GameObject.Destroy (activeRoad.instance.gameObject);
						DeselectRoad ();
					}
				}
			}
		}
	}
}
