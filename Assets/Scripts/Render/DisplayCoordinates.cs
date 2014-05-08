using UnityEngine;
using System.Collections;

using Ecosim;

public class DisplayCoordinates : MonoBehaviour
{
	
	const float TERRAIN_SCALE = TerrainMgr.TERRAIN_SCALE;
	
	// Use this for initialization
	void Start ()
	{
	
	}
	
	private int x = -1;
	private int y = -1;
	float height;
	
	void OnGUI ()
	{
		if (x >= 0) {
			GUI.Label (new Rect (Screen.width - 200, Screen.height - 30, 200, 30), "[" + x + ", " + y + "] = " + height.ToString ("0.00") + "m");
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (Camera.main) {
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast (ray, out hit, Mathf.Infinity, Layers.M_TERRAIN)) {
				x = (int)(hit.point.x / TERRAIN_SCALE);
				y = (int)(hit.point.z / TERRAIN_SCALE);
				height = hit.point.y;
			} else {
				x = -1;
				y = -1;
			}
		} else {
			x = -1;
			y = -1;
		}
	}
}
