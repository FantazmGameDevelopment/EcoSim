using UnityEngine;
using Ecosim;
using System.Collections;

public class Animal : MonoBehaviour {
	public AnimalMgr.AnimalData data;
	
	int cx;
	int cy;
	
	
	public void SetupPosition () {
		Vector3 pos = transform.position;
		pos.x = (0.5f + data.x) * TerrainMgr.TERRAIN_SCALE;
		pos.z = (0.5f + data.y) * TerrainMgr.TERRAIN_SCALE;
		Ray ray = new Ray(pos, Vector3.down);
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, 2000f, Layers.M_TERRAIN)) {
			pos = hit.point + 0.5f * Vector3.up;
		}
		transform.position = pos;
		cx = data.x / TerrainMgr.CELL_SIZE;
		cy = data.y / TerrainMgr.CELL_SIZE;
	}
	
	public void CommitPosition () {
		Vector3 pos = transform.position;
		int x = (int) (pos.x / TerrainMgr.TERRAIN_SCALE);
		int y = (int) (pos.z / TerrainMgr.TERRAIN_SCALE);
		int newCx = x / TerrainMgr.CELL_SIZE;
		int newCy = y / TerrainMgr.CELL_SIZE;
		AnimalMgr mgr = AnimalMgr.self;
		if (!mgr.isCellVisible (newCx, newCy)) {
			// walking of scene or into invisible cell, reset to old position
			SetupPosition ();
			return;
		}
		data.x = x;
		data.y = y;
		if ((newCx != cx) || (newCy != cy)) {
			AnimalMgr.self.AnimalMovesCell(data, cx, cy, newCx, newCy);
		}
	}
}
