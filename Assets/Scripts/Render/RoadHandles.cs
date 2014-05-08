using UnityEngine;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;

public class RoadHandles : MonoBehaviour
{
	
	public GameObject handlePrefab;
	public static RoadHandles self;
	
	void Awake ()
	{
		self = this;
	}
	
	void OnDestroy ()
	{
		self = null;
	}
	
	private List<GameObject> handles;
		
	public void ShowAllHandles (Scene scene)
	{
		List<Buildings.Building> buildings = scene.buildings.GetAllBuildings ();
		handles = new List<GameObject> ();
		foreach (Buildings.Building b in buildings) {
			Vector3 pos = b.position;
			Quaternion rot = b.rotation;
			Vector3 scale = b.scale;
			if (b.prefab.prefab != null) {
				// b.prefab can be null if object is an runtime imported one
				foreach (Transform t in b.prefab.prefab.transform) {
					Vector3 handlePos = t.localPosition;
					Quaternion handleRot = t.localRotation;
					handlePos.Scale (scale);
					handlePos = pos + rot * handlePos;
					handleRot = rot * handleRot;
					GameObject handle = (GameObject)Instantiate (handlePrefab, handlePos, handleRot);
					handle.layer = Layers.L_EDIT2;
					handles.Add (handle);
				}
			}
		}
	}
	
	public void HideAllHandles ()
	{
		foreach (GameObject go in handles) {
			Destroy (go);
		}
		handles = null;
	}
}
