using System.Collections.Generic;
using System.Xml;
using System.IO;
using Ecosim;
using UnityEngine;

namespace Ecosim.SceneData
{
	public class Buildings
	{
		public class Building
		{
			public Building (int id)
			{
				this.id = id;
			}
			
			public string name;
			public EcoTerrainElements.PrefabContainer prefab;
			public readonly int id;
			public Vector3 position;
			public Quaternion rotation;
			public Vector3 scale;
			public bool startsActive = true;
			public bool isActive = true;
		}
		
//		private readonly Scene scene;
		Dictionary<int, Building> buildings;
		private List<Building>[,] grid;
		private int nextId = 0;
		
		public static string GetMapNameForBuildingId (int id) {
			return "_bld" + id;
		}
		
		public Buildings (Scene scene)
		{
//			this.scene = scene;
			grid = new List<Building>[scene.height / TerrainMgr.CELL_SIZE, scene.width / TerrainMgr.CELL_SIZE];
			buildings = new Dictionary<int, Building> ();
		}
		
		public List<Building> GetBuildingsForCell (int cx, int cy)
		{
			return grid [cy, cx];
		}
		
		/**
		 * Returns a list with all buildings
		 */
		public List<Building> GetAllBuildings ()
		{
			return new List<Building> (buildings.Values);
		}
		
		/**
		 * Returns building with id 'id', or null if building doesn't exist
		 */
		public Building GetBuilding (int id)
		{
			Building result = null;
			if (buildings.TryGetValue (id, out result)) {
				return result;
			}
			return null;
		}
		
		public int GetNewBuildingID ()
		{
			return nextId++;
		}
		
		/**
		 * The complete list of buildings is replaced with buildingList
		 * if buildingList == null, the buildings will end up with no buildings
		 */
		public void SetAllBuildings (List<Building> buildingList)
		{
			buildings.Clear ();
			for (int y = 0; y < grid.GetLength(0); y++) {
				for (int x = 0; x < grid.GetLength(1); x++) {
					grid [y, x] = null;
				}
			}
			if (buildingList == null)
				return;

			foreach (Building b in buildingList) {
				int cellX = (int)(b.position.x / (TerrainMgr.CELL_SIZE * TerrainMgr.TERRAIN_SCALE));
				int cellY = (int)(b.position.z / (TerrainMgr.CELL_SIZE * TerrainMgr.TERRAIN_SCALE));
				buildings.Add (b.id, b);
				List<Building> bl = grid [cellY, cellX];
				if (bl == null) {
					bl = new List<Building> ();
					grid [cellY, cellX] = bl;
				}
				bl.Add (b);
			}
		}
		
		void LoadBuilding (XmlTextReader reader)
		{
			// Get ID
			int id = -1;
			if (!string.IsNullOrEmpty (reader.GetAttribute ("id")))
				id = int.Parse (reader.GetAttribute ("id"));
			else id = nextId;

			string name = reader.GetAttribute ("name");
			EcoTerrainElements.BuildingPrototype prefab = EcoTerrainElements.GetBuilding (name);
			if (prefab == null) {
				Log.LogError ("Building '" + name + "' not found.");
				IOUtil.ReadUntilEndElement (reader, "building");
				return;			
			}
			Building b = new Building (id);
			b.name = name;
			b.prefab = prefab.prefabContainer;
			nextId = Mathf.Max (nextId, id + 1);
			b.position = StringUtil.StringToVector3 (reader.GetAttribute ("position"));
			b.rotation = Quaternion.Euler (StringUtil.StringToVector3 (reader.GetAttribute ("rotation")));
			b.scale = StringUtil.StringToVector3 (reader.GetAttribute ("scale"));

			int cellX = (int)(b.position.x / (TerrainMgr.CELL_SIZE * TerrainMgr.TERRAIN_SCALE));
			int cellY = (int)(b.position.z / (TerrainMgr.CELL_SIZE * TerrainMgr.TERRAIN_SCALE));
			
			string isActive = reader.GetAttribute ("active");
			b.startsActive = ((isActive == null) || (isActive == "true"));
			b.isActive = b.startsActive;

			if ((cellX < 0) || (cellY < 0) || (cellX >= grid.GetLength (1)) || (cellY >= grid.GetLength (0))) {
				Debug.Log ("building out of bounds [" + cellX + ", " + cellY + "] (" + b.position.x + ", " + b.position.z + ")");
			} else {
				buildings.Add (id, b);
				
				List<Building> bl = grid [cellY, cellX];
				if (bl == null) {
					bl = new List<Building> ();
					grid [cellY, cellX] = bl;
				}
				bl.Add (b);
			}
			IOUtil.ReadUntilEndElement (reader, "building");
		}
		
		public static Buildings Load (string path, Scene scene)
		{
			Buildings b = new Buildings (scene);
			XmlTextReader reader = new XmlTextReader (new StreamReader (path + "buildings.xml"));
			while (reader.Read()) {
				XmlNodeType nType = reader.NodeType;
				if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "building")) {
					b.LoadBuilding (reader);
				}
			}
			reader.Close ();
			return b;
		}
		
		public void Save (string path)
		{
			XmlTextWriter writer = new XmlTextWriter (path + "buildings.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement ("buildings");
			foreach (Building b in buildings.Values) {
				writer.WriteStartElement ("building");
				writer.WriteAttributeString ("id", b.id.ToString ());
				writer.WriteAttributeString ("name", b.name);
				writer.WriteAttributeString ("position", StringUtil.Vector3ToString (b.position));
				writer.WriteAttributeString ("rotation", StringUtil.Vector3ToString (b.rotation.eulerAngles));
				writer.WriteAttributeString ("scale", StringUtil.Vector3ToString (b.scale));
				writer.WriteAttributeString ("active", b.startsActive ? "true" : "false");
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();
			
		}
		
		/**
		 * Save buildings to directory 'path' and transform/clip roads using offsets and width/height
		 * Original data will remain unchanged
		 */
		public void SaveAndClip (string path, int offsetX, int offsetY, int newWidth, int newHeight)
		{
			Vector3 offset = new Vector3 (TerrainMgr.TERRAIN_SCALE * offsetX, 0f, TerrainMgr.TERRAIN_SCALE * offsetY);
			Vector3 size = new Vector3 (TerrainMgr.TERRAIN_SCALE * newWidth, 0f, TerrainMgr.TERRAIN_SCALE * newHeight);
			Bounds bounds = new Bounds (offset + 0.5f * size, new Vector3 (size.x, 10000f, size.z));
			XmlTextWriter writer = new XmlTextWriter (path + "buildings.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement ("buildings");
			foreach (Building b in buildings.Values) {
				if (bounds.Contains (b.position)) {
					writer.WriteStartElement ("building");
					writer.WriteAttributeString ("id", b.id.ToString ());
					writer.WriteAttributeString ("name", b.name);
					writer.WriteAttributeString ("position", StringUtil.Vector3ToString (b.position - offset));
					writer.WriteAttributeString ("rotation", StringUtil.Vector3ToString (b.rotation.eulerAngles));
					writer.WriteAttributeString ("scale", StringUtil.Vector3ToString (b.scale));
					writer.WriteAttributeString ("active", b.startsActive ? "true" : "false");
					writer.WriteEndElement ();
				}
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();
		}		
	}
}