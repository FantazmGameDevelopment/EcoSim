using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim;

public class RoadInstance : MonoBehaviour
{
	private Mesh mesh;
	public Vector3[] shape;
	public float uvScale = 0.01f;
		
	public class Point
	{
		public Vector3 point;
		public Vector3 c1;
		public Vector3 c2;
	}

	const float smooth_value = 0.5f;
	private static Mesh emptyMesh = null;
	public Roads.Road roadData;
	
	Vector3 FindGroundPoint (Vector3 pos)
	{
		Vector3 checkPos = pos;
		checkPos.y = 1000f;
		Ray ray = new Ray (checkPos, -Vector3.up);
		
		RaycastHit hit;
		if (Physics.Raycast (ray, out hit, 1001f, Layers.M_TERRAIN)) {
			pos.y = hit.point.y;
		} else {
//			Debug.Log ("Didn't hit the floor " + pos);
		}
		return pos;
	}

	void CalculateCtrlPointsTest (Point p0, Point p1, Point p2, Point p3)
	{
		p1.c2 = Vector3.Lerp (p1.point, p2.point, 0.25f);
		p2.c1 = Vector3.Lerp (p1.point, p2.point, 0.75f);
	}
	
	// calculate ctrl points between p1 and p2
	// 
	void CalculateCtrlPoints (Point p0, Point p1, Point p2, Point p3)
	{
		if (p0 == null) {
			p0 = new Point ();
			p0.point = p1.point + p1.point - p2.point;
		}
		if (p3 == null) {
			p3 = new Point ();
			p3.point = p2.point + p2.point - p1.point;
		}
		Vector3 c1 = (p0.point + p1.point) * 0.5f;
		Vector3 c2 = (p1.point + p2.point) * 0.5f;
		Vector3 c3 = (p2.point + p3.point) * 0.5f;
		
		float len1 = Vector3.Distance (p0.point, p1.point);
		float len2 = Vector3.Distance (p1.point, p2.point);
		float len3 = Vector3.Distance (p2.point, p3.point);
		
		float k1 = len1 / (len1 + len2);
		float k2 = len2 / (len2 + len3);
		
		Vector3 m1 = c1 + (c2 - c1) * k1;
		Vector3 m2 = c2 + (c3 - c2) * k2;
		Vector3 ctrl1 = m1 + (c2 - m1) * smooth_value + p1.point - m1;
		Vector3 ctrl2 = m2 + (c2 - m2) * smooth_value + p2.point - m2;
		p1.c2 = ctrl1;
		p2.c1 = ctrl2;
	}
	
	Vector3 GetSegmentPoint (Point p1, Point p2, float t)
	{
		Vector3 ab = Vector3.Lerp (p1.point, p1.c2, t);
		Vector3 bc = Vector3.Lerp (p1.c2, p2.c1, t);
		Vector3 cd = Vector3.Lerp (p2.c1, p2.point, t);
		Vector3 abbc = Vector3.Lerp (ab, bc, t);
		Vector3 bccd = Vector3.Lerp (bc, cd, t);
		return FindGroundPoint (Vector3.Lerp (abbc, bccd, t));
	}

	void AddSegmentPoints (List<Vector3> segmentPoints, Point p1, Point p2)
	{
		int steps = 7;
		float delta = 1f / steps;
		float t = 0f;
		for (int i = 1; i < steps; i++) {
			t += delta;
			segmentPoints.Add (GetSegmentPoint (p1, p2, t));
		}
		segmentPoints.Add (p2.point);
	}
	
	
	void Awake ()
	{
		mesh = new Mesh ();
		MeshFilter filter = GetComponent<MeshFilter> ();
		filter.sharedMesh = mesh;
		// points = new List<Vector3>();
	}
	
	public void DeleteRoad ()
	{
		Destroy (gameObject);
	}
	
	/**
	 * Moves point to position
	 * dir is the outgoing direction of the node, has
	 * only a meaning for start and end note. If dir is Vector3.zero
	 * the direction is determined by the relative position of the
	 * point next in line.
	 * 
	 */
	public void MoveNodeTo (int index, Vector3 position, Vector3 dir)
	{
		int count = roadData.points.Count;
		roadData.points [index] = position;
		if (index == 0) {
			if (dir != Vector3.zero) {
				roadData.startCtrl = dir;
			}
			else {
				dir = (roadData.points[1] - roadData.points[0]).normalized;
			}
		}
		if (index == (count - 1)) {
			if (dir != Vector3.zero) {
				roadData.endCtrl = dir;
			}
			else {
				dir = (roadData.points[count - 2] - roadData.points[count - 1]).normalized;
			}
		}
		
		UpdatePath ();
	}
	
	/**
	 * returns true if index is the index of start node or end node, otherwise false
	 */
	public bool IndexIsEndNode(int index) {
		return (index == 0) || (index == roadData.points.Count - 1);
	}
	
	public int AddNode (Vector3 position)
	{
		roadData.points.Add (position);
		UpdatePath ();
		return roadData.points.Count - 1;
	}
	
	public Vector3 GetNodePosition (int index)
	{
		return roadData.points [index];
	}
	
	public int GetNodeCount ()
	{
		return roadData.points.Count;
	}
	
	public void DeleteNode (int index)
	{
		roadData.points.RemoveAt (index);
		UpdatePath ();
	}
	
	public void UpdatePath ()
	{
		int count = roadData.points.Count;
		if (count < 2) {
			roadData.path = new Vector3[0];
			GenerateMesh ();
			return;
		}
//		if (roadData.startCtrl != Vector3.zero) count++;
//		if (roadData.endCtrl != Vector3.zero) count++;
		Point[] splinePoints = new Point[count];
		for (int i = 0; i < count; i++) {
			Point p = new Point ();
			Vector3 pos = roadData.points [i];
			if (((i == 0) && (roadData.startCtrl != Vector3.zero)) ||
				((i == count - 1) && (roadData.endCtrl != Vector3.zero))) {
				// linked start/end point, don't look for ground
			} else {
				pos = FindGroundPoint (pos);
			}
			p.point = pos;
			splinePoints [i] = p;
		}
		Point p0 = null;
		Point p1 = splinePoints [0];
		Point p2 = splinePoints [1];
		for (int i = 2; i <= count; i++) {
			Point p3 = (i < count) ? (splinePoints [i]) : null;
			CalculateCtrlPoints (p0, p1, p2, p3);
			p0 = p1;
			p1 = p2;
			p2 = p3;
		}
		if (roadData.startCtrl != Vector3.zero) {
			splinePoints [0].c2 = splinePoints [0].point + 20 * roadData.startCtrl;
		}
		if (roadData.endCtrl != Vector3.zero) {
			splinePoints [count - 1].c1 = splinePoints [count - 1].point + 20 * roadData.endCtrl;
		}
		
		List<Vector3> segmentPoints = new List<Vector3> ();
		
		
		p1 = splinePoints [0];
		segmentPoints.Add (p1.point);
		for (int i = 1; i < count; i++) {
			p2 = splinePoints [i];
			AddSegmentPoints (segmentPoints, p1, p2);
			p1 = p2;
		}
		roadData.path = segmentPoints.ToArray ();
		GenerateMesh ();
	}
	
	void GenerateMesh ()
	{
		mesh.Clear ();
		Vector3[] path = roadData.path;
		int count = (path != null) ? (path.Length) : 0;
		
		Vector3[] vertices;
		Vector2[] uv;
		int[] triangles;
		if (count < 2) {
			vertices = new Vector3[0];
			uv = new Vector2[0];
			triangles = new int[0];
		} else {
			int pointsPerSegment = shape.Length;
			
			// the basic triangle layout (indices) for a segment
			int triangleCount = (pointsPerSegment - 1) * 6;
			int[] triangleIndices = new int[triangleCount];
			for (int i = 0; i < pointsPerSegment - 1; i++) {
				triangleIndices [i * 6 + 0] = i;
				triangleIndices [i * 6 + 1] = i + pointsPerSegment + 1;
				triangleIndices [i * 6 + 2] = i + 1;
				triangleIndices [i * 6 + 3] = i;
				triangleIndices [i * 6 + 4] = i + pointsPerSegment;
				triangleIndices [i * 6 + 5] = i + pointsPerSegment + 1;
			}
			
			vertices = new Vector3[count * pointsPerSegment];
			uv = new Vector2[count * pointsPerSegment];
			triangles = new int[triangleCount * (count - 1)];
			
			int p = 0;
			float uvProgress = 0f;
			for (int i = 0; i < count; i++) {
				Vector3 fwd;
				Vector3 current = path [i];
				if (i == 0) {
					if (roadData.startCtrl != Vector3.zero) {
						fwd = roadData.startCtrl;
					}
					else {
						fwd = (path[i + 1] - current).normalized;
					}
				} else {
					fwd = (current - path[i - 1]);
					uvProgress += fwd.magnitude * uvScale;
					if ((i == count - 1) && (roadData.endCtrl != Vector3.zero)) {
						fwd = -roadData.endCtrl;
					}
					fwd = fwd.normalized;
				}
				Quaternion q = Quaternion.LookRotation (fwd, Vector3.up);
				float rotation = q.eulerAngles.y;
				fwd = Quaternion.Euler (0f, rotation, 0f) * Vector3.forward;
				
				Vector3 right = Vector3.Cross (fwd, Vector3.up);
				Vector3 up = Vector3.Cross (right, fwd);
				
				for (int j = 0; j < pointsPerSegment; j++) {				
					uv [p] = new Vector2 (uvProgress, shape [j].z);
					vertices [p++] = current + (right * shape [j].x + up * shape [j].y);
				}
			}
			p = 0;
			for (int i = 0; i < count - 1; i++) {
				foreach (int j in triangleIndices) {
					triangles [p++] = (i * pointsPerSegment) + j;
				}
			}
		}
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.Optimize ();
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
		MeshCollider col = GetComponent<MeshCollider> ();
		if (col != null) {
			if (col.sharedMesh != null) {
				// hack to force collider to be recalculated
				if (emptyMesh == null) {
					emptyMesh = new Mesh ();
				}
				col.sharedMesh = emptyMesh;
			}
			col.sharedMesh = mesh;
		}
	}
	
	public void Setup (Roads.Road data)
	{
		roadData = data;
		data.instance = this;
		if (data.path != null) {
			// we have already calculated path, use this...
			GenerateMesh ();
		} else {
			UpdatePath ();
		}
		MeshCollider col = GetComponent<MeshCollider> ();
		if (col != null) {
			col.sharedMesh = mesh;
		}
	}
}
