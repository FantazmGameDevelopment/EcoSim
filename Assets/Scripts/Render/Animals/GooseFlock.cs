using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;

public class GooseFlock : MonoBehaviour
{
	
	public Vector3 target;
	Vector3 position;
	Quaternion rot;
	Transform myTransform;
	Transform camT;
	public float distanceMultiplier = 1f;
	bool forceFlyUp = false;
	
	void Start ()
	{
		myTransform = transform;
		rot = myTransform.localRotation;
		position = myTransform.localPosition;
		camT = CameraControl.self.nearCamera.transform;
		;
		Vector3 pos = camT.position + (camT.forward * -100f + camT.up * -600f + camT.right * Random.Range (-200, 200)) * distanceMultiplier;
		pos.y = Mathf.Clamp (camT.position.y, 50f, 150f);
		myTransform.position = pos;
		position = pos;
		SelectTarget ();
		StartCoroutine (CORandomNewTarget ());
		StartCoroutine (COPreventCrash ());
	}
	
	IEnumerator CORandomNewTarget ()
	{
		while (true) {
			yield return new WaitForSeconds(Random.Range(1, 5));
			Vector3 cameraPos = camT.position;
			cameraPos.y = myTransform.localPosition.y;
			float distance = Vector3.Distance (cameraPos, myTransform.position);
			if ((distance > 2500) && (Random.value < 0.1f)) {
				position = cameraPos - camT.forward * 500f + Random.Range (-250f, 250f) * camT.right;
				myTransform.localPosition = position;
			}
			if (distance > 750) {
				SelectTarget ();
			}
		}
	}
	
	IEnumerator COPreventCrash ()
	{
		while (true) {
			yield return new WaitForSeconds(0.5f);
			Scene scene = TerrainMgr.self.scene;
			if (scene != null) {
				Vector3 pos = transform.localPosition;
				HeightMap map = scene.progression.heightMap;
				float minHeight = map.GetInterpolatedHeight (pos.x, pos.z);
				if (pos.y < minHeight + 50) {
					forceFlyUp = true;
				} else {
					forceFlyUp = false;
				}
			}
		}
	}
	
	void SelectTarget ()
	{
		Vector3 pos;
		if (Random.value < 0.8f) {
			pos = camT.position + (camT.forward * Random.Range (-10f, 100f) + camT.right * Random.Range (-50f, 50f)) * distanceMultiplier;
			pos.y = Mathf.Clamp (pos.y + Random.Range (-5f, 25f), 20f, 500f);
		}
		else {
			Quaternion rot = Quaternion.Euler (0f, Random.Range (0f, 360f), 0f);
			pos = camT.position + rot * (700f * Vector3.forward);
		}
		
		Scene scene = TerrainMgr.self.scene;
		if (scene != null) {
			HeightMap map = scene.progression.heightMap;
			float minHeight = map.GetInterpolatedHeight (pos.x, pos.z);
			if (pos.y < minHeight + 25)
				pos.y = minHeight + 25;
		}
		target = pos;
	}
	
	void Update ()
	{
		float deltaTime = Time.deltaTime;
		Vector3 direction = (target - position).normalized;
		if (forceFlyUp) {
			direction.y = 1f;
		}
		rot = Quaternion.RotateTowards (rot, Quaternion.LookRotation (direction, Vector3.up), 15 * deltaTime);
		myTransform.localRotation = rot;
		Vector3 dir = myTransform.forward;
		if (forceFlyUp) {
			dir.y = Mathf.Max (dir.y, 0f);
		}
		position += deltaTime * 30 * dir;
		myTransform.localPosition = position;
		if ((position - target).sqrMagnitude < 10000) {
			SelectTarget ();
		}
	}
}
