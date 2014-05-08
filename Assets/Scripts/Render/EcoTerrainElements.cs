using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneData;

public class EcoTerrainElements : MonoBehaviour
{
	[System.Serializable]
	public class AnimalPrototype
	{
		public string name;
		public GameObject prefab;
	}
	
	public enum EBuildingCategories
	{
		CITY,
		INDUSTRIAL,
		RURAL,
		SPECIAL,
		EXTRA,
		ROAD
	};
	
	[System.Serializable]
	public class BuildingPrototype
	{
		public string name;
		public EBuildingCategories category;
		public GameObject prefab;
		public PrefabContainer prefabContainer;
		
		public override string ToString ()
		{
			return name;
		}
	}
	
	[System.Serializable]
	public class DecalPrototype
	{
		public string name;
		public bool useWaterHeights;
		public float verticalOffset = 0.25f;
		public Material material;		
	}
	
	[System.Serializable]
	public class Shaders
	{
		public string name;
		public string buildinShaderName; // hack as you don't seem to be able to link buildin shaders to shader property
		public Shader shader;
	}
	
	public Texture2D placeholderIcon;
	public AnimalPrototype[] animals;
	public BuildingPrototype[] buildings;
	public TerrainData terrainData;
	public DecalPrototype[] decals;
	public GameObject[] tileObjects;
	public GameObject[] roadPrefabs;
	public static EcoTerrainElements self;
	public Material[] materials;
	public Shaders[] shaders;
	private PrefabContainer[] tileObjectCL;
	
	/**
	 * Simple container for prefabs
	 * Constructor is not thread safe
	 */
	public class PrefabContainer
	{
		
		private static Dictionary<Material, int> materialDict = new Dictionary<Material, int> ();
		private static int materialIdGenerator = 0;
		
		public PrefabContainer (Mesh mesh, Material mat)
		{
			this.prefab = null;
			this.material = mat;
			this.mesh = mesh;
			
			// see if we already know this material, otherwise make a new id for it....
			int id;
			if (materialDict.TryGetValue (material, out id)) {
				materialId = id;
			} else {
				materialId = ++ materialIdGenerator;
				materialDict.Add (material, materialId);
			}
			vertexCount = mesh.vertexCount;
		}
		
		public PrefabContainer (GameObject prefab)
		{
			this.prefab = prefab;
			this.material = prefab.GetComponent<MeshRenderer> ().sharedMaterial;
			this.mesh = prefab.GetComponent<MeshFilter> ().sharedMesh;
			
			// see if we already know this material, otherwise make a new id for it....
			int id;
			if (materialDict.TryGetValue (material, out id)) {
				materialId = id;
			} else {
				materialId = ++ materialIdGenerator;
				materialDict.Add (material, materialId);
			}
			vertexCount = mesh.vertexCount;
		}
		
		public GameObject Instantiate (Vector3 position, Quaternion rotation, Vector3 scale)
		{
			if (prefab != null) {
				// just use the prefab...
				GameObject go = (GameObject)GameObject.Instantiate (prefab, position, rotation);
				go.transform.localScale = scale;
				return go;
			} else {
				GameObject go = new GameObject ("dynamicinstance");
				go.transform.localPosition = position;
				go.transform.localRotation = rotation;
				go.transform.localScale = scale;
				MeshFilter filter = go.AddComponent<MeshFilter> ();
				filter.sharedMesh = mesh;
				MeshRenderer render = go.AddComponent<MeshRenderer> ();
				render.sharedMaterial = material;
				return go;
			}
		}
		
		
		/**
		 * Clean up material list so elements can be reclaimed by GC
		 */
		public static void OnDestroy ()
		{
			materialDict.Clear ();
			materialIdGenerator = 0;
		}
		
		public readonly GameObject prefab;
		public readonly Material material;
		public readonly int materialId;
		public readonly Mesh mesh;
		public readonly int vertexCount;
	}
	
	void Awake ()
	{
		self = this;
		tileObjectCL = new PrefabContainer[tileObjects.Length];
		for (int i = 0; i < tileObjects.Length; i++) {
			tileObjectCL [i] = new PrefabContainer (tileObjects [i]);
		}
		foreach (BuildingPrototype building in buildings) {
			building.prefabContainer = new PrefabContainer (building.prefab);
		}
	}
	
	void OnDestroy ()
	{
		self = null;
		PrefabContainer.OnDestroy ();
	}
	
	public void AddExtraBuildings (ExtraAssets assets)
	{
		List<BuildingPrototype> newBuildings = new List<BuildingPrototype> ();
		foreach (BuildingPrototype bp in buildings) {
			if (bp.category != EBuildingCategories.EXTRA) {
				newBuildings.Add (bp);
			}
		}
		foreach (ExtraAssets.AssetObjDef def in assets.GetAllObjects()) {
			BuildingPrototype bp = new BuildingPrototype ();
			bp.name = def.name;
			bp.category = EBuildingCategories.EXTRA;
			bp.prefab = null;
			bp.prefabContainer = new PrefabContainer (def.mesh, def.material);
			newBuildings.Add (bp);
		}
		buildings = newBuildings.ToArray ();
	}
	
	public static int GetIndexOfTreePrototype (string name)
	{
		name = name.ToLower ();
		int i = 0;
		foreach (TreePrototype tp in self.terrainData.treePrototypes) {
			if (tp.prefab.name.ToLower () == name)
				return i; 
			i++;
		}
		return -1;
	}
	
	public static int GetIndexOfObject (string name)
	{
		name = name.ToLower ();
		for (int i = 0; i < self.tileObjects.Length; i++) {
			if (self.tileObjects [i].name.ToLower () == name)
				return i;
		}
		return -1;
	}
	
	public static int GetIndexOfDetailPrototype (string name)
	{
		name = name.ToLower ();
		for (int i = self.terrainData.detailPrototypes.Length - 1; i >= 0; i--) {
			if (name == GetDetailNameForIndex (i).ToLower ())
				return i;
		}
		return -1;
	}
	
	public static int GetIndexOfDecal (string name)
	{
		name = name.ToLower ();
		for (int i = self.decals.Length - 1; i >= 0; i--) {
			if (name == self.decals [i].name.ToLower ())
				return i;
		}
		return -1;
	}
	
	public static string GetDetailNameForIndex (int index)
	{
		DetailPrototype dp = self.terrainData.detailPrototypes [index];
		if (dp.prototype != null) {
			return dp.prototype.name;
		} else {
			return dp.prototypeTexture.name;
		}
	}
	
	public static string GetDecalNameForIndex (int index)
	{
		return self.decals [index].name;
	}

	public static DecalPrototype GetDecal (int index)
	{
		return self.decals [index];
	}
	
	public static string GetTreePrototypeNameForIndex (int index)
	{
		return self.terrainData.treePrototypes [index].prefab.name;
	}
	
	public static string[] GetTreeNames ()
	{
		string[] names = new string[self.terrainData.treePrototypes.Length];
		for (int i = 0; i < names.Length; i++) {
			names [i] = self.terrainData.treePrototypes [i].prefab.name;
		}
		return names;
	}

	public static string[] GetObjectNames ()
	{
		string[] names = new string[self.tileObjects.Length];
		for (int i = 0; i < names.Length; i++) {
			names [i] = self.tileObjects [i].name;
		}
		return names;
	}
	
	public static string[] GetDecalNames ()
	{
		string[] names = new string[self.decals.Length];
		for (int i = 0; i < names.Length; i++) {
			names [i] = self.decals [i].name;
		}
		return names;
	}
	
	public static string[] GetDetailNames ()
	{
		string[] names = new string[self.terrainData.detailPrototypes.Length];
		for (int i = 0; i < names.Length; i++) {
			GameObject go = self.terrainData.detailPrototypes [i].prototype;
			if (go != null) {
				names [i] = go.name;
			} else {
				names [i] = self.terrainData.detailPrototypes [i].prototypeTexture.name;
			}
			
		}
		return names;
	}
	
	public static PrefabContainer GetTileObjectPrefab (int index)
	{
		return self.tileObjectCL [index];
	}
	
	public static BuildingPrototype GetBuilding (string name)
	{
		foreach (BuildingPrototype building in self.buildings) {
			if (building.name == name)
				return building;
		}
		return null;
	}
	
	public static GameObject GetRoadPrefab (string name)
	{
		name = name.ToLower ();
		foreach (GameObject prefab in self.roadPrefabs) {
			if (prefab.name.ToLower () == name)
				return prefab;
		}
		return null;
	}
	
	public static Material GetMaterial (string name)
	{
		name = name.ToLower ();
		foreach (Material mat in self.materials) {
			if (mat.name.ToLower () == name) {
				return mat;
			}
		}
		Debug.LogError ("Material '" + name + "' not found!");
		return null;
	}
	
	public static Shader GetShader (string name)
	{
		name = name.ToLower ();
		foreach (Shaders s in self.shaders) {
			if (s.name.ToLower () == name) {
				if (s.shader == null) {
					s.shader = Shader.Find (s.buildinShaderName);
					if (s.shader == null) {
						Debug.LogError ("Can't find Shader '" + s.buildinShaderName + "'");
					}
				}
				return s.shader;
			}
		}
		Shader result = Shader.Find (name);
		if (result == null) {
			Debug.LogError ("Can't find shader named '" + name + "'");
		}
		return result;
	}
}
