using Color = UnityEngine.Color;
using Texture2D = UnityEngine.Texture2D;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Ecosim;

namespace Ecosim.SceneData
{
	public class TileType
	{
		public const string XML_ELEMENT = "tile";
	
		public class TreeData
		{
		
			public TreeData ()
			{
			}
		
			public TreeData (TreeData source)
			{
				x = source.x;
				y = source.y;
				r = source.r;
				prototypeIndex = source.prototypeIndex;
				minHeight = source.minHeight;
				maxHeight = source.maxHeight;
				minWidthVariance = source.minWidthVariance;
				minWidthVariance = source.maxWidthVariance;
				colorFrom = source.colorFrom;
				colorTo = source.colorTo;
			}
		
			static Color start = new Color (0.5625f, 0.5625f, 0.5625f, 1f);
			static Color end = new Color (0.75f, 0.75f, 0.75f, 1f);
		
			public string GetName ()
			{
				return EcoTerrainElements.GetTreePrototypeNameForIndex(prototypeIndex);
			}
		
			public int prototypeIndex;
			public float x = 0.5f;
			public float y = 0.5f;
			public float r = 0.2f;
			public float minHeight = 0.9f;
			public float maxHeight = 1.1f;
			public float minWidthVariance = 0.9f;
			public float maxWidthVariance = 1.1f;
			public Color colorFrom = start;
			public Color colorTo = end;
		}
	
		public class ObjectData
		{
		
			public ObjectData ()
			{
			}
		
			public ObjectData (ObjectData source)
			{
				x = source.x;
				y = source.y;
				r = source.r;
				angle = source.angle;
				index = source.index;
				minHeight = source.minHeight;
				maxHeight = source.maxHeight;
				minWidthVariance = source.minWidthVariance;
				minWidthVariance = source.maxWidthVariance;
			}
		
			public string GetName ()
			{
				return null; // TODO JElements.GetTileObjectPrefab(index).name;
			}
		
			public int index;
			public float x = 0.5f;
			public float y = 0.5f;
			public float r = 0.0f;
			public float angle = 0f;
			public float minHeight = 0.9f;
			public float maxHeight = 1.1f;
			public float minWidthVariance = 0.9f;
			public float maxWidthVariance = 1.1f;
		}
			
	
		public string name;
		public int index;
		public VegetationType vegetationType;
		private Texture2D icon;
		
		public TileType() {
		}
		
		
		/**
		 * Copies current tile to destinatioon tile dst
		 * dst will be an exact copy of this tile (including index and vegetation link)
		 */
		public void CopyTo(TileType dst) {
			CopyTo(dst, false);
		}
		
		
		/**
		 * Copies current tile to destination tile dst
		 * if keepVegetationLink is true, the index and vegetation type of destination tile is not changed
		 */
		public void CopyTo(TileType dst, bool keepVegetationLink) {
			if (!keepVegetationLink) {
				dst.index = index;
				dst.vegetationType = vegetationType;
			}
			dst.splat0 = splat0;
			dst.splat1 = splat1;
			dst.splat2 = splat2;
			dst.trees = new TreeData[trees.Length];
			for (int i = 0; i < trees.Length; i++) {
				dst.trees[i] = new TreeData(trees[i]);
			}
			dst.objects = new ObjectData[objects.Length];
			for (int i = 0; i < objects.Length; i++) {
				dst.objects[i] = new ObjectData(objects[i]);
			}
			dst.decals = (int[]) decals.Clone();
			dst.detailCounts = (int[]) detailCounts.Clone();
		}
		
		public TileType(VegetationType veg) {
			this.index = veg.tiles.Length;
			this.vegetationType = veg;
			splat0 = 1f;
			trees = new TreeData[0];
			objects = new ObjectData[0];
			decals = new int[0];
			detailCounts = new int[0];
			if (veg.tiles.Length > 0) {
				TileType firstTile = veg.tiles[0];
				splat0 = firstTile.splat0;
				splat1 = firstTile.splat1;
				splat2 = firstTile.splat2;
			}
			List<TileType> tmpTilesList = new List<TileType>(veg.tiles);
			tmpTilesList.Add(this);
			veg.tiles = tmpTilesList.ToArray();
		}
		
		public Texture2D GetIcon ()
		{
			if (icon == null) {
				icon = RenderTileIcons.RenderTile(this);
			}
			return icon;
		}
	
		public void SetIcon (Texture2D icon)
		{
			this.icon = icon;
		}
	
		public float splat0; // for splat map (tile colour)
		public float splat1;
		public float splat2;
		public TreeData[] trees;
		public ObjectData[] objects;
		public int[] decals;
		public int[] detailCounts;
	
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
					
			writer.WriteAttributeString ("splat0", splat0.ToString ());
			writer.WriteAttributeString ("splat1", splat1.ToString ());
			writer.WriteAttributeString ("splat2", splat2.ToString ());					
					
			foreach (int decal in decals) {
				writer.WriteStartElement ("tilelayer");
				writer.WriteAttributeString ("name", EcoTerrainElements.GetDecalNameForIndex(decal));
				writer.WriteAttributeString ("index", decal.ToString());
				writer.WriteEndElement (); // ~tilelayer
			}
			foreach (TreeData td in trees) {						
				writer.WriteStartElement ("tree");
						
				writer.WriteAttributeString ("x", td.x.ToString ());
				writer.WriteAttributeString ("y", td.y.ToString ());
				writer.WriteAttributeString ("r", td.r.ToString ());
				writer.WriteAttributeString ("prototypeName", td.GetName ());
				writer.WriteAttributeString ("prototype", td.prototypeIndex.ToString ());

				writer.WriteAttributeString ("minHeight", td.minHeight.ToString ());
				writer.WriteAttributeString ("maxHeight", td.maxHeight.ToString ());
				writer.WriteAttributeString ("minWidthVariance", td.minWidthVariance.ToString ());
				writer.WriteAttributeString ("maxWidthVariance", td.maxWidthVariance.ToString ());
				writer.WriteAttributeString ("fromColour", StringUtil.ColorToString (td.colorFrom));
				writer.WriteAttributeString ("toColour", StringUtil.ColorToString (td.colorTo));
						
				writer.WriteEndElement (); // ~tree
			}
			foreach (ObjectData od in objects) {						
				writer.WriteStartElement ("object");
						
				writer.WriteAttributeString ("x", od.x.ToString ());
				writer.WriteAttributeString ("y", od.y.ToString ());
				writer.WriteAttributeString ("r", od.r.ToString ());
				writer.WriteAttributeString ("angle", od.angle.ToString ());
				writer.WriteAttributeString ("name", EcoTerrainElements.GetObjectNames () [od.index]);
				writer.WriteAttributeString ("objectIndex", od.index.ToString ());

				writer.WriteAttributeString ("minHeight", od.minHeight.ToString ());
				writer.WriteAttributeString ("maxHeight", od.maxHeight.ToString ());
				writer.WriteAttributeString ("minWidthVariance", od.minWidthVariance.ToString ());
				writer.WriteAttributeString ("maxWidthVariance", od.maxWidthVariance.ToString ());
						
				writer.WriteEndElement (); // ~object
			}
			int index = 0;
			foreach (int count in detailCounts) {
				if (count > 0) {
					writer.WriteStartElement ("detail");
					writer.WriteAttributeString ("name", EcoTerrainElements.GetDetailNameForIndex(index));
					writer.WriteAttributeString ("index", index.ToString ());
					writer.WriteAttributeString ("count", count.ToString ());

					writer.WriteEndElement (); // ~detail
				}
				index++;
			}
			writer.WriteEndElement (); // ~tile
			
		}
		
		public static TileType Load (XmlTextReader reader, Scene scene)
		{
			TileType tt = new TileType ();
			tt.splat0 = float.Parse (reader.GetAttribute ("splat0"));
			tt.splat1 = float.Parse (reader.GetAttribute ("splat1"));
			tt.splat2 = float.Parse (reader.GetAttribute ("splat2"));
			int maxDetailIndex = -1;
			List<int> tileLayers = new List<int> ();
			if (!reader.IsEmptyElement) {
				List<TreeData> treeList = new List<TreeData> ();
				List<ObjectData> objectList = new List<ObjectData> ();
				Dictionary<int, int> detailDict = new Dictionary<int, int> ();
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "image")) {
						nType = reader.NodeType;
						IOUtil.ReadUntilEndElement (reader, "image");
					}
				
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "tree")) {
						TreeData tree = new TreeData ();
						tree.x = float.Parse (reader.GetAttribute ("x"));
						tree.y = float.Parse (reader.GetAttribute ("y"));
						tree.r = float.Parse (reader.GetAttribute ("r"));
						int index = -1;
						string prototypeName = reader.GetAttribute ("prototypeName");
						if (prototypeName != null) {
							index = EcoTerrainElements.GetIndexOfTreePrototype (prototypeName);
						}
						if (index >= 0) {
							// index = int.Parse(reader.GetAttribute("prototype"));
							tree.prototypeIndex = index;
							tree.minHeight = float.Parse (reader.GetAttribute ("minHeight"));
							tree.maxHeight = float.Parse (reader.GetAttribute ("maxHeight"));
							tree.minWidthVariance = float.Parse (reader.GetAttribute ("minWidthVariance"));
							tree.maxWidthVariance = float.Parse (reader.GetAttribute ("maxWidthVariance"));
							tree.colorFrom = StringUtil.StringToColor (reader.GetAttribute ("fromColour"));
							tree.colorTo = StringUtil.StringToColor (reader.GetAttribute ("toColour"));
							treeList.Add (tree);
							IOUtil.ReadUntilEndElement (reader, "tree");
						}
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "object")) {
						ObjectData obj = new ObjectData ();
						obj.x = float.Parse (reader.GetAttribute ("x"));
						obj.y = float.Parse (reader.GetAttribute ("y"));
						obj.r = float.Parse (reader.GetAttribute ("r"));
						obj.angle = float.Parse (reader.GetAttribute ("angle"));
						int index = -1;
						string prototypeName = reader.GetAttribute ("name");
						if (prototypeName != null) {
							index = EcoTerrainElements.GetIndexOfObject (prototypeName);
						}
						if (index >= 0) {
							// index = int.Parse(reader.GetAttribute("objectIndex"));
							obj.index = index;
							obj.minHeight = float.Parse (reader.GetAttribute ("minHeight"));
							obj.maxHeight = float.Parse (reader.GetAttribute ("maxHeight"));
							obj.minWidthVariance = float.Parse (reader.GetAttribute ("minWidthVariance"));
							obj.maxWidthVariance = float.Parse (reader.GetAttribute ("maxWidthVariance"));
							objectList.Add (obj);
						}
						else {
							Log.LogError("Can't find tile object '" + prototypeName + "'");
						}
						IOUtil.ReadUntilEndElement (reader, "object");
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "detail")) {
						int index = -1;
						string detailName = reader.GetAttribute ("name");
						if (detailName != null) {
							index = EcoTerrainElements.GetIndexOfDetailPrototype (detailName);
						}
						if (index >= 0) {
							// index = int.Parse(reader.GetAttribute("index"));
							int count = int.Parse (reader.GetAttribute ("count"));
							maxDetailIndex = System.Math.Max (maxDetailIndex, index);
							if (!detailDict.ContainsKey (index)) {
								detailDict.Add (index, count);
							}
						}
						IOUtil.ReadUntilEndElement (reader, "detail");
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "tilelayer")) {
						string name = reader.GetAttribute ("name");
						int id = EcoTerrainElements.GetIndexOfDecal (name);
						if (id >= 0) {
							// id = int.Parse(reader.GetAttribute("index"));
							tileLayers.Add (id);
						}
						IOUtil.ReadUntilEndElement (reader, "tilelayer");
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			
				tt.trees = treeList.ToArray ();
				tt.objects = objectList.ToArray ();
				tt.decals = tileLayers.ToArray ();
				tt.detailCounts = new int[maxDetailIndex + 1];
				foreach (KeyValuePair<int, int> v in detailDict) {
					tt.detailCounts [v.Key] = v.Value;
				}
			} else {
				tt.trees = new TreeData[0];
				tt.detailCounts = new int[0];
				tt.objects = new ObjectData[0];
				tt.decals = new int[0];
			}
			return tt;
		}

		public void UpdateLinks (Scene scene, VegetationType veg) {
			vegetationType = veg;
		}
		
	}
	
}