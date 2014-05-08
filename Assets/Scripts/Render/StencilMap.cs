using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StencilMap {
	public StencilMap(float[,] heights, int id) {
		this.heights = heights;
		this.id = id;
		meshes = new List<Mesh>();
		if (id >= 0) {
			decal = EcoTerrainElements.GetDecal(id);
			verticalOffset = decal.verticalOffset / TerrainMgr.VERTICAL_HEIGHT;
		}
		else {
			verticalOffset = 0f;
		}
	}
	
	public readonly EcoTerrainElements.DecalPrototype decal;
	public readonly int id;
	
	readonly float[,] heights;
	readonly float verticalOffset;
	
	int[,] vertexIds;
	List<Vector3> vertices;
	List<Vector2> uvs;
	List<Mesh> meshes;
	List<int> indices;
	int index = 0;
	bool isEmpty = true;
	
	int GetIndex(int x, int y) {
		int i = vertexIds[y, x];
		if (i > 0) return i - 1;
		vertices.Add (new Vector3(x, heights[y << 2, x << 2] + verticalOffset, y));
		uvs.Add(new Vector2(x, y));
		vertexIds[y, x] = ++index;
		return index - 1;
	}
	
	public void AddTile(int x, int y) {
		if (isEmpty) {
			vertexIds = new int[TerrainCell.CELL_SIZE + 1, TerrainCell.CELL_SIZE + 1];
			vertices = new List<Vector3>();
			uvs = new List<Vector2>();
			indices = new List<int>();
			index = 0;
			isEmpty = false;
		}
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
		if (index >= 32768) {
			MakeToMesh();
		}
	}
	
	private void MakeToMesh() {
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.triangles = indices.ToArray();
		mesh.Optimize();
		mesh.RecalculateNormals();		
		vertexIds = null;
		vertices = null;
		uvs = null;
		isEmpty = true;
		meshes.Add(mesh);
	}
	
	public Mesh[] GetMeshes() {
		if (!isEmpty) {
			MakeToMesh();
		}
		return meshes.ToArray();
	}
}
