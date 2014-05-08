using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EditorUtility  {

	[MenuItem("Ctrl-J/Add Decals")]
	static void AddDecals() {
		EcoTerrainElements ete = GameObject.Find("EcoTerrainElements").GetComponent<EcoTerrainElements>();
		int count = 0;
	
		List<EcoTerrainElements.DecalPrototype> decals = new List<EcoTerrainElements.DecalPrototype>();
		foreach (Object o in Selection.GetFiltered(typeof(Material), SelectionMode.DeepAssets)) {
			Debug.Log(o.name);
			EcoTerrainElements.DecalPrototype d = new EcoTerrainElements.DecalPrototype();
			d.material = (Material) o;
			d.verticalOffset = 0.1f;
			d.useWaterHeights = false;
			d.name = d.material.name;
			decals.Add(d);
			count++;
		}
		
		decals.Sort((x, y) => string.Compare(x.name, y.name));
		
		ete.decals = decals.ToArray();
	}


	[MenuItem("Ctrl-J/Add Buildings")]
	static void AddBuildings() {
		EcoTerrainElements ete = GameObject.Find("EcoTerrainElements").GetComponent<EcoTerrainElements>();
		int count = 0;
	
		List<EcoTerrainElements.BuildingPrototype> buildings = new List<EcoTerrainElements.BuildingPrototype>(ete.buildings);
		foreach (Object o in Selection.GetFiltered(typeof(GameObject), SelectionMode.DeepAssets)) {
			Debug.Log(o.name);
			EcoTerrainElements.BuildingPrototype b = new EcoTerrainElements.BuildingPrototype();
			b.name = o.name;
			b.prefab = (GameObject) o;
			b.category = EcoTerrainElements.EBuildingCategories.ROAD;
			if (b.name.StartsWith("schuur")) b.category = EcoTerrainElements.EBuildingCategories.RURAL;
			if (b.name.StartsWith("kas")) b.category = EcoTerrainElements.EBuildingCategories.RURAL;
			if (b.name.StartsWith("boer")) b.category = EcoTerrainElements.EBuildingCategories.RURAL;
			if (b.name.StartsWith("bezoek")) b.category = EcoTerrainElements.EBuildingCategories.RURAL;
			if (b.name.StartsWith("zadel")) b.category = EcoTerrainElements.EBuildingCategories.RURAL;
			if (b.name.StartsWith("kerk")) b.category = EcoTerrainElements.EBuildingCategories.SPECIAL;
			if (b.name.StartsWith("kathedraal")) b.category = EcoTerrainElements.EBuildingCategories.SPECIAL;
			if (b.name.StartsWith("kantoor")) b.category = EcoTerrainElements.EBuildingCategories.INDUSTRIAL;
			if (b.name.StartsWith("steen")) b.category = EcoTerrainElements.EBuildingCategories.INDUSTRIAL;
			foreach (EcoTerrainElements.BuildingPrototype proto in buildings) {
				if (proto.name == b.name) {
					buildings.Remove(proto);
					break;
				}
			}
			buildings.Add(b);
			count++;
		}
		
		buildings.Sort((x, y) => string.Compare(x.name, y.name));
		
		ete.buildings = buildings.ToArray();
	}

	[MenuItem("Ctrl-J/Add Roads")]
	static void AddRoads() {
		EcoTerrainElements ete = GameObject.Find("EcoTerrainElements").GetComponent<EcoTerrainElements>();
		int count = 0;
	
		List<GameObject> roads = new List<GameObject>(ete.roadPrefabs);
		foreach (Object o in Selection.GetFiltered(typeof(GameObject), SelectionMode.DeepAssets)) {
			GameObject go = (GameObject) o;
			Debug.Log(go.name);
			roads.Add(go);
			count++;
		}
		
		roads.Sort((x, y) => string.Compare(x.name, y.name));
		
		ete.roadPrefabs = roads.ToArray();
	}
}
