using UnityEngine;
using System.Collections.Generic;
using Ecosim;

public class RenderTileIconStencil : MonoBehaviour {
	
	private Mesh mesh;
	private int id;
	private float height;
//	private bool useWaterHeights;
	int[,] vertexIdList;
	List<Vector3> vertices;
	List<Vector2> uvs;
	List<int> indices;
	int index;

	public static RenderTileIconStencil CreateStencil(Transform parent, int id) {
		GameObject go = new GameObject("Stencil" + id);
		go.transform.parent = parent;
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		go.transform.localScale = new Vector3(RenderTileIcons.TERRAIN_SCALE, 1f, RenderTileIcons.TERRAIN_SCALE);
		go.layer = Layers.L_GUI;
		RenderTileIconStencil s = go.AddComponent<RenderTileIconStencil>();
		s.id = id;
		return s;
	}
	
	public static void DestroyStencil(RenderTileIconStencil stencil) {
		if (stencil != null) {
			DestroyImmediate(stencil.gameObject);
		}
	}
	
	public void Awake() {
		// hack to get id:
		id = int.Parse (name.Substring(7));
		MeshFilter filter = gameObject.AddComponent<MeshFilter>();
		MeshRenderer render = gameObject.AddComponent<MeshRenderer>();
		mesh = new Mesh();
		EcoTerrainElements.DecalPrototype decal = EcoTerrainElements.GetDecal(id);
		render.sharedMaterial = decal.material;
		filter.sharedMesh = mesh;
		height = decal.verticalOffset;
//		useWaterHeights = decal.useWaterHeights;
		vertexIdList = new int[RenderTileIcons.TERRAIN_SIZE + 1, RenderTileIcons.TERRAIN_SIZE + 1];
		vertices = new List<Vector3>();
		uvs = new List<Vector2>();
		indices = new List<int>();
		index = 0;
	}
	
	void OnDestroy() {
		Destroy(mesh);
		mesh = null;
	}
	
	
	int GetIndex(int x, int y) {
		int i = vertexIdList[y, x];
		if (i > 0) return i - 1;
		vertices.Add (new Vector3(x, height, y));
		uvs.Add(new Vector2(x, y));
		vertexIdList[y, x] = ++index;
		return index - 1;
	}
	
	public void AddTile(int x, int y) {
		int c0 = GetIndex(x, y);
		int c1 = GetIndex(x + 1, y);
		int c2 = GetIndex(x, y + 1);
		int c3 = GetIndex(x + 1, y + 1);
		indices.Add(c1);
		indices.Add(c0);
		indices.Add(c2);
		indices.Add(c2);
		indices.Add(c3);
		indices.Add(c1);
	}
	
	public void GenerateMesh() {
		mesh.vertices = vertices.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.triangles = indices.ToArray();
		mesh.Optimize();
		mesh.RecalculateNormals();
	}
}
