using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.Render;

public class AnimalMgr : MonoBehaviour, NotifyTerrainChange
{
	
	public class AnimalData
	{
		public AnimalData (int id, int x, int y)
		{
			this.id = id;
			this.x = x;
			this.y = y;
		}
		
		public readonly int id;
		public int x;
		public int y;
		public Animal animal;
	}
	
	class AnimalCell
	{
		public AnimalCell (int cx, int cy)
		{
			this.cx = cx;
			this.cy = cy;
			animals = new List<AnimalData> ();
		}
		
		public readonly int cx;
		public readonly int cy;
		public bool isVisible = false;
		public List<AnimalData> animals;
	}
	
	private int cWidth;
	private int cHeight;
	private AnimalCell[,] cells;
	private Scene scene;
	public bool editMode = false;
	public static AnimalMgr self;
	
	void Awake ()
	{
		self = this;
	}
	
	void Start ()
	{
		TerrainMgr.AddListener (this);
	}
	
	void OnDestroy ()
	{
		self = null;
		TerrainMgr.RemoveListener (this);
	}
	
	public void ForceRefresh() {
		DeleteAnimals ();
		TerrainMgr.RemoveListener (this);
		SceneChanged (scene);
		SuccessionCompleted ();
		TerrainMgr.AddListener (this);
	}

	void DeleteAnimals ()
	{
		if (cells != null) {
			foreach (AnimalCell cell in cells) {
				foreach (AnimalData ad in cell.animals) {
					if (ad.animal) {
						Object.Destroy (ad.animal.gameObject);
						ad.animal = null;
					}
				}
				cell.animals.Clear ();
			}
		}
	}
	
	public void SceneChanged (Scene scene)
	{
		DeleteAnimals ();
		cells = null;
		this.scene = scene;
		if (scene != null) {
			if (!editMode && !scene.progression.HasData (Progression.ANIMAL_ID)) {
				return; // no animals so don't bother...
			}
			cWidth = scene.width / TerrainMgr.CELL_SIZE;
			cHeight = scene.height / TerrainMgr.CELL_SIZE;
			cells = new AnimalCell[cHeight, cWidth];
			for (int cy = 0; cy < cHeight; cy++) {
				for (int cx = 0; cx < cWidth; cx++) {
					cells [cy, cx] = new AnimalCell (cx, cy);
				}
			}
			SuccessionCompleted ();
		}
	}

	public void SuccessionCompleted ()
	{
		DeleteAnimals ();
		if (!scene.progression.HasData (Progression.ANIMAL_ID)) {
			return;
		}
		Data data = scene.progression.GetData (Progression.ANIMAL_ID);
		int maxId = EcoTerrainElements.self.animals.Length;
		// go over all defined animals
		foreach (ValueCoordinate vc in data.EnumerateNotZero ()) {
			int x = vc.x;
			int y = vc.y;
			int id = vc.v - 1;
			if (id < maxId) {
				// found an animal, id is valid (animal type exists in EcoTerrainElements)
				AnimalData ad = new AnimalData (id, x, y);
				int cx = x / TerrainMgr.CELL_SIZE;
				int cy = y / TerrainMgr.CELL_SIZE;
				cells [cy, cx].animals.Add (ad);
			}
		}
	}

	public void CellChangedToVisible (int cx, int cy, TerrainCell cell)
	{
		if (cells == null)
			return;
		AnimalCell aCell = cells [cy, cx];
		aCell.isVisible = true;
		foreach (AnimalData ad in aCell.animals) {
			if (ad.animal == null) {
				// animal needs to be created
				CreateAnimal (ad);
			}
		}
	}

	public void CellChangedToInvisible (int cx, int cy)
	{
		if (cells == null)
			return;
		AnimalCell aCell = cells [cy, cx];
		foreach (AnimalData ad in aCell.animals) {
			if (ad.animal) {
				Object.Destroy (ad.animal.gameObject);
				ad.animal = null;
			}
		}
		
		aCell.isVisible = false;
	}
	
	public void RemoveAnimalAt(int x, int y) {
		int cx = x / TerrainMgr.CELL_SIZE;
		int cy = y / TerrainMgr.CELL_SIZE;
		if (isCellVisible(cx, cy)) {
			AnimalCell cell = cells[cy, cx];
			foreach (AnimalData ad in cell.animals) {
				if ((ad.x == x) && (ad.y == y)) {
					if (ad.animal) {
						Destroy (ad.animal.gameObject);
					}
					cell.animals.Remove (ad);
					break;
				}
			}
			if (scene.progression.HasData (Progression.ANIMAL_ID)) {
				Data map = scene.progression.GetData(Progression.ANIMAL_ID);
				map.Set (x, y, 0);
			}
		}
	}
	
	public void AddAnimalAt(int id, int x, int y) {
		int cx = x / TerrainMgr.CELL_SIZE;
		int cy = y / TerrainMgr.CELL_SIZE;
		if (isCellVisible(cx, cy)) {
			AnimalCell cell = cells[cy, cx];
			foreach (AnimalData ad in cell.animals) {
				if ((ad.x == x) && (ad.y == y)) {
					if (ad.animal) {
						Destroy (ad.animal.gameObject);
					}
					cell.animals.Remove (ad);
					break;
				}
			}
			AnimalData newAd = new AnimalData(id, x, y);
			cell.animals.Add (newAd);
			CreateAnimal (newAd);
			Data map;
			if (!scene.progression.HasData (Progression.ANIMAL_ID)) {
				scene.progression.AddData(Progression.ANIMAL_ID, new SparseBitMap8(scene));
			}
			map = scene.progression.GetData(Progression.ANIMAL_ID);
			map.Set (x, y, id + 1);
		}
	}
	
	public void CreateAnimal (AnimalData data)
	{
		EcoTerrainElements.AnimalPrototype[] animalPrototypes = EcoTerrainElements.self.animals;
		Vector3 pos = new Vector3 ((0.5f + data.x) * TerrainMgr.TERRAIN_SCALE, 2000f, (0.5f + data.y) * TerrainMgr.TERRAIN_SCALE);
		GameObject go = (GameObject)Instantiate (animalPrototypes [data.id].prefab);
		go.transform.parent = transform;
		go.transform.localPosition = pos;
		go.transform.localRotation = Quaternion.Euler (0f, Random.Range (0f, 360f), 0f);
		Animal animalScript = go.GetComponent<Animal> ();
		animalScript.data = data;
		if (editMode) {
			go.transform.localScale *= 4f;
		}
		data.animal = animalScript;
	}
	
	public bool isCellVisible(int cx, int cy) {
		if ((cx < 0) || (cx >= cWidth) || (cy < 0) || (cy >= cHeight)) {
			return false; // outside scene!
		}
		return cells[cy, cx].isVisible;
	}
	
	public void AnimalMovesCell (AnimalData ad, int cx, int cy, int newCx, int newCy)
	{
		AnimalCell oldCell = cells[cy, cx];
		oldCell.animals.Remove (ad);
		AnimalCell newCell = cells[newCy, newCx];
		newCell.animals.Add (ad);
		if (!newCell.isVisible) {
			Object.Destroy (ad.animal.gameObject);
			ad.animal = null;
		}
	}
}
