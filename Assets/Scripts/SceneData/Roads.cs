using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

using Ecosim;
using UnityEngine;

namespace Ecosim.SceneData
{
	public class Roads
	{
		public class Road
		{
			public GameObject prefab;
			public Vector3[] path;
			public List<Vector3> points;
			public Vector3 startCtrl;
			public Vector3 endCtrl;
			public Bounds bounds;
			public RoadInstance instance;
		}	
		
		public readonly List<Road> roads;
		private const long FILE_VERSION = 100;
		
		public Roads (Scene scene)
		{
			roads = new List<Road> ();
		}
		
		public void Save (string path)
		{
			FileStream stream = new FileStream (path + "roads.dat", FileMode.Create);
			BinaryWriter writer = new BinaryWriter (stream);
			writer.Write (FILE_VERSION);
			writer.Write (roads.Count);
			foreach (Road road in roads) {
				writer.Write (road.prefab.name);
				writer.Write (road.points.Count);
				foreach (Vector3 v in road.points) {
					IOUtil.WriteVector3 (writer, v);
				}
				IOUtil.WriteVector3 (writer, road.startCtrl);
				IOUtil.WriteVector3 (writer, road.endCtrl);
				writer.Write (road.path.Length);
				foreach (Vector3 v in road.path) {
					IOUtil.WriteVector3 (writer, v);
				}
			}
			writer.Close ();
			stream.Close ();	
		}
		
		/**
		 * Save roads to directory 'path' and transform/clip roads using offsets and width/height
		 * Original data will remain unchanged
		 */
		public void SaveAndClip (string path, int offsetX, int offsetY, int newWidth, int newHeight) {
			FileStream stream = new FileStream (path + "roads.dat", FileMode.Create);
			
			Vector3 offset = new Vector3(TerrainMgr.TERRAIN_SCALE * offsetX, 0f, TerrainMgr.TERRAIN_SCALE * offsetY);
			Vector3 size = new Vector3(TerrainMgr.TERRAIN_SCALE * newWidth, 0f, TerrainMgr.TERRAIN_SCALE * newHeight);
			Bounds bounds = new Bounds(offset + 0.5f * size, new Vector3(size.x, 10000f, size.z));
			List<Road> saveList = new List<Road>();
			foreach (Road road in roads) {
				foreach (Vector3 v in road.points) {
					if (bounds.Contains(v)) {
						saveList.Add(road);
						break;
					}
				}
			}
			
			BinaryWriter writer = new BinaryWriter (stream);
			writer.Write (FILE_VERSION);
			writer.Write (saveList.Count);
			foreach (Road road in saveList) {
				writer.Write (road.prefab.name);
				writer.Write (road.points.Count);
				foreach (Vector3 v in road.points) {
					IOUtil.WriteVector3 (writer, v - offset);
				}
				IOUtil.WriteVector3 (writer, road.startCtrl);
				IOUtil.WriteVector3 (writer, road.endCtrl);
				writer.Write (road.path.Length);
				foreach (Vector3 v in road.path) {
					IOUtil.WriteVector3 (writer, v - offset);
				}
			}
			writer.Close ();
			stream.Close ();	
		}
		
		public static Roads Load (string path, Scene scene)
		{
			Roads roads = new Roads (scene);
			if (!File.Exists (path + "roads.dat"))
				return roads;
			FileStream stream = new FileStream (path + "roads.dat", FileMode.Open);
			BinaryReader reader = new BinaryReader (stream);
			long header = reader.ReadInt64 ();
			if (header != FILE_VERSION) {
				throw new System.Exception ("incompatible road version");
			}
			int count = reader.ReadInt32 ();
			for (int i = 0; i < count; i++) {
				string prefabName = reader.ReadString ();
				Road data = new Road ();
				GameObject prefab = EcoTerrainElements.GetRoadPrefab (prefabName);
				data.prefab = prefab;
				data.points = new List<Vector3> ();
				int count2 = reader.ReadInt32 ();
				for (int j = 0; j < count2; j++) {
					data.points.Add (IOUtil.ReadVector3 (reader));
				}
				data.startCtrl = IOUtil.ReadVector3 (reader);
				data.endCtrl = IOUtil.ReadVector3 (reader);
				count2 = reader.ReadInt32 ();
				data.path = new Vector3[count2];
				Vector3 min = Vector3.zero;
				Vector3 max = Vector3.zero;
				for (int j = 0; j < count2; j++) {
					Vector3 v = IOUtil.ReadVector3 (reader);
					data.path [j] = v;
					if (j == 0) {
						min = v;
						max = v;
					} else {
						min.x = Mathf.Min (min.x, v.x);
						min.y = Mathf.Min (min.y, v.y);
						min.z = Mathf.Min (min.z, v.z);
						max.x = Mathf.Max (min.x, v.x);
						max.y = Mathf.Max (min.y, v.y);
						max.z = Mathf.Max (min.z, v.z);
					}
				}
				data.bounds = new Bounds ((min + max) / 2, (max - min));
				if (prefab != null) {
					roads.roads.Add (data);
				} else {
					Debug.LogError ("Can't find road prefab '" + prefabName + "'");
				}
			}
			reader.Close ();
			return roads;
		}
	}
}