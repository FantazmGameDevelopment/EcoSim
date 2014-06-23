using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Ecosim;

namespace Ecosim.SceneData
{
	/**
	 * Manages extra assets (objects, textures) defined for a scene.
	 * The extra assets must be stored in the Assets folder
	 */
	public class ExtraAssets
	{
		private readonly Scene scene;
						
		public class AssetObjDef
		{
			public string name;
			public Mesh mesh;
			public string textureName;
			public string shaderName;
			public Material material;
		}
		
		private Dictionary<string, AssetObjDef> objects;
		private Dictionary<string, Texture2D> textures;
		private Dictionary<string, Material> materials;
		private Dictionary<string, Mesh> meshes;
		public Texture2D[] icons;
		public Texture2D[] iconsHighlighted;
		public bool hasIconsTexture = false;
		
		public delegate Color32 ProcessColor32 (Color32 col);
				
		public ExtraAssets (Scene scene)
		{
			this.scene = scene;
			objects = new Dictionary<string, AssetObjDef> ();
			ResetCache ();
		}
		
		public Texture2D MakeTexture (Color32[] pixels, int width, int height, int xo, int yo)
		{
			Color32[] dstPixels = new Color32[32 * 32];
			int pDst = 0;
			bool empty = true;
			for (int y = 0; y < 32; y++) {
				int pSrc = (yo * 32 + y) * width + xo * 32;
				for (int x = 0; x < 32; x++) {
					Color32 srcPixel = pixels [pSrc++];
					if (srcPixel.a != 0)
						empty = false;
					dstPixels [pDst++] = srcPixel;
				}
			}
			if (empty) {
				return null;
			} else {
				Texture2D result = new Texture2D (32, 32, TextureFormat.RGBA32, false);
				result.wrapMode = TextureWrapMode.Clamp;
				result.SetPixels32 (dstPixels);
				result.Apply ();
				return result;
			}
		}

		public Texture2D MakeTexture (Color32[] pixels, ProcessColor32 fn, int width, int height, int xo, int yo)
		{
			Color32[] dstPixels = new Color32[32 * 32];
			int pDst = 0;
			bool empty = true;
			for (int y = 0; y < 32; y++) {
				int pSrc = (yo * 32 + y) * width + xo * 32;
				for (int x = 0; x < 32; x++) {
					Color32 srcPixel = pixels [pSrc++];
					if (srcPixel.a != 0)
						empty = false;
					dstPixels [pDst++] = fn (srcPixel);
				}
			}
			if (empty) {
				return null;
			} else {
				Texture2D result = new Texture2D (32, 32, TextureFormat.RGBA32, false);
				result.wrapMode = TextureWrapMode.Clamp;
				result.SetPixels32 (dstPixels);
				result.Apply ();
				return result;
			}
		}
		
		public void CopyTexure (Texture2D src, Texture2D dst, ProcessColor32 fn, int offsetX, int offsetY)
		{
			Color32[] dstPixels = dst.GetPixels32 ();
			Color32[] srcPixels = src.GetPixels32 ();
			int pSrc = 0;
			int w = src.width;
			int h = src.height;
			for (int y = 0; y < h; y++) {
				int pDst = (y + offsetY) * dst.width + offsetX;
				for (int x = 0; x < w; x++) {
					dstPixels [pDst++] = srcPixels [pSrc++];
				}
			}
			dst.SetPixels32 (dstPixels);
			dst.Apply ();
		}
		
		/**
		 * Copies textures src to dst, starting from lower left corner, adding left to right,
		 * bottom to top. All src textures should be equal sizes and dst size must be a multiple
		 * of src sizes.
		 */
		public void CopyTexures (Texture2D[] src, Texture2D dst, ProcessColor32 fn)
		{
			if (src.Length == 0)
				return;
			int sw = src [0].width;
			int sh = src [0].height;
			int dw = dst.width;
			int dh = dst.height;
			
			int cellsX = dw / sw;
			int cellsY = dh / sh;
			
			int len = src.Length;
			int maxLen = cellsX * cellsY;
			if (len > maxLen) {
				Debug.LogError ("Can't fit textures in texture map! " + len + " > " + maxLen);
				len = maxLen;
			}

			Color32[] dstPixels = dst.GetPixels32 ();
			
			for (int i = 0; i < src.Length; i++) {
				Color32[] srcPixels = src [i].GetPixels32 ();
				int offsetX = (i % cellsX) * sw;
				int offsetY = (i / cellsX) * sh;
				int pSrc = 0;
				for (int y = 0; y < sh; y++) {
					int pDst = (y + offsetY) * dw + offsetX;
					for (int x = 0; x < sw; x++) {
						if (fn != null) {
							dstPixels [pDst++] = fn (srcPixels [pSrc++]);
						} else {
							dstPixels [pDst++] = srcPixels [pSrc++];
						}
					}
				}
			}
			dst.SetPixels32 (dstPixels);
			dst.Apply ();
		}
		
		public void ResetCache ()
		{
			string path = GameSettings.GetPathForScene (scene.sceneName);
			textures = new Dictionary<string, Texture2D> ();
			materials = new Dictionary<string, Material> ();
			meshes = new Dictionary<string, Mesh> ();
			List<Texture2D> iconsList = new List<Texture2D> ();
			List<Texture2D> iconsHighlightedList = new List<Texture2D> ();

			
			foreach (KeyValuePair<string, AssetObjDef> kv in objects) {
				AssetObjDef def = kv.Value;
				Mesh mesh = TryLoadMesh (path, def.name);
				def.material = TryLoadMaterial (path, def.textureName, def.shaderName);
				def.mesh = mesh;
			}
			
			hasIconsTexture = false;
			List<string> names = null;
			if (Directory.Exists (path + "Assets")) {
				names = new List<string> (Directory.GetFiles (path + "Assets", "icons*.png"));
			}
			else {
				names = new List<string> ();
			}
			names.Sort ();
			foreach (string name in names) {
				Texture2D iconsTex = TryLoadTexture2D (path, Path.GetFileNameWithoutExtension (name));
				if (iconsTex != null) {
					Color32[] pixels = iconsTex.GetPixels32 (0);
					int width = iconsTex.width;
					int height = iconsTex.height;
					int index = 0;
					for (int y = 0; y < (height / 32); y++) {
						for (int x = 0; x < (width / 32); x++) {
							Texture2D tex = MakeTexture (pixels, width, height, x, y);
							Texture2D texHl = MakeTexture (pixels, delegate(Color32 col) {
								return new Color32 ((byte)(255 - col.r), (byte)(255 - col.g), (byte)(255 - col.b), col.a);
							}, width, height, x, y);
							if (tex != null) {
								iconsList.Add (tex);
								iconsHighlightedList.Add (texHl);
								hasIconsTexture = true;
							}
							index ++;
						}
					}
				}
			}
			if (iconsList.Count == 0) {
				iconsList.Add (EcoTerrainElements.self.placeholderIcon);
				iconsHighlightedList.Add (EcoTerrainElements.self.placeholderIcon);
			}
			this.icons = iconsList.ToArray ();
			this.iconsHighlighted = iconsHighlightedList.ToArray ();
		}
		
		public Texture2D GetIcon (int index)
		{
			if ((index < 0) || (index >= icons.Length)) {
				return EcoTerrainElements.self.placeholderIcon;
			}
			return icons [index];
		}

		public Texture2D GetHighlightedIcon (int index)
		{
			if ((index < 0) || (index >= iconsHighlighted.Length)) {
				return EcoTerrainElements.self.placeholderIcon;
			}
			return iconsHighlighted [index];
		}
		
		public AssetObjDef GetObjectDef (string name)
		{
			AssetObjDef obj;
			if (objects.TryGetValue (name, out obj)) {
				return obj;
			}
			return null;
		}
		
		/**
		 * Tries to load a mesh object (in .obj format) or returns null on failure.
		 * path is the path to the scene, name is name of object (minus extension)
		 */
		Mesh TryLoadMesh (string path, string name)
		{
			Mesh mesh;
			if (meshes.TryGetValue (name, out mesh)) {
				return mesh;
			}
			try {
				string objectPath = path + "Assets" + Path.DirectorySeparatorChar + name + ".obj";
				mesh = ObjImporter.ImportFile (objectPath, Vector3.one * 0.01f); // So it matches the default Unity mesh import settings
				if (mesh != null) {
					meshes.Add (name, mesh);
				}
				return mesh;
			} catch (System.Exception e) {
				Debug.LogException (e);
				return null;
			}
		}
		
		/**
		 * Tries  to load texture from Assets folder in scene
		 * path is the path to the scene
		 * texName is  name of texture to load (minus extension)
		 * Texture file must be either an .png or .jpng, .png has precedence.
		 * returns null if texture not found or fails to load
		 */
		Texture2D TryLoadTexture2D (string path, string texName)
		{
			Texture2D result = null;
			if (textures.TryGetValue (texName, out result)) {
				return result;
			}
			try {
				byte[] data = null;
				if (File.Exists (path + "Assets" + Path.DirectorySeparatorChar + texName + ".png")) {
					data = File.ReadAllBytes (path + "Assets" + Path.DirectorySeparatorChar + texName + ".png");
				} else if (File.Exists (path + "Assets" + Path.DirectorySeparatorChar + texName + ".jpg")) {
					data = File.ReadAllBytes (path + "Assets" + Path.DirectorySeparatorChar + texName + ".jpg");
				} else {
					Debug.LogError ("No texture found in Assets with name '" + texName + "'");
					return null;
				}
				Texture2D tex = new Texture2D (2, 2);
				tex.LoadImage (data);
				textures.Add (texName, tex);
				return tex;
			} catch (System.Exception e) {
				Debug.LogException (e);
				return null;
			}
		}
		
		/**
		 * Tries to load material from the Assets folder scene
		 * path is the path to the scene
		 * texName is  name of texture to load (minus extension)
		 * shaderStr is either a build-in shader or a name defined in EcoTerrainElements
		 * Texture file must be either an .png or .jpng, .png has precedence.
		 * returns null if texture/shader is not found or fails to load
		 */
		Material TryLoadMaterial (string path, string texName, string shaderStr)
		{
			string key = shaderStr + "." + texName;
			Material result = null;
			if (materials.TryGetValue (key, out result)) {
				return result;
			}
			Texture2D tex = TryLoadTexture2D (path, texName);
			if (tex == null) {
				return null;
			}
			Shader shader = EcoTerrainElements.GetShader (shaderStr);
			if (shader == null) {
				Debug.LogError ("Shader not found for '" + shaderStr + "'");
				return null;
			}
			result = new Material (shader);
			result.mainTexture = tex;
			return result;
		}
		
		public AssetObjDef AddObject (string path, string name, string textureName, string shader)
		{
			AssetObjDef def = new AssetObjDef ();
			def.name = name;
			def.textureName = textureName;
			def.shaderName = shader;
			def.mesh = TryLoadMesh (path, name);
			def.material = TryLoadMaterial (path, textureName, shader);
			if ((def.mesh == null) || (def.material == null))
				return null;
			objects.Add (name, def);
			return def;
		}
		
		public List<AssetObjDef> GetAllObjects ()
		{
			List<AssetObjDef> result = new List<AssetObjDef> ();
			foreach (AssetObjDef def in objects.Values) {
				result.Add (def);
			}
			return result;
		}
		
		public void RemoveObject (string name)
		{
			objects.Remove (name);
		}
				
		void LoadObject (XmlTextReader reader, string path)
		{			
			AssetObjDef def = new AssetObjDef ();
			def.name = reader.GetAttribute ("name");
			def.textureName = reader.GetAttribute ("texturename");
			def.shaderName = reader.GetAttribute ("shader").ToLower ();
			IOUtil.ReadUntilEndElement (reader, "object");
			
			Mesh mesh = TryLoadMesh (path, def.name);
			def.material = TryLoadMaterial (path, def.textureName, def.shaderName);
			if (mesh != null) {
				def.mesh = mesh;
				objects.Add (def.name, def);
			}
		}
		
		public static ExtraAssets Load (string path, Scene scene)
		{
			ExtraAssets ea = new ExtraAssets (scene);
			if (!File.Exists (path + "assets.xml")) {
				Debug.LogError ("No assets file!");
				return ea;
			}
			XmlTextReader reader = new XmlTextReader (new StreamReader (path + "assets.xml"));
			while (reader.Read()) {
				XmlNodeType nType = reader.NodeType;
				if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "object")) {
					ea.LoadObject (reader, path);	
				}
			}
			reader.Close ();
			return ea;
		}

		public void Save (string path)
		{
			string assetsDirPath = path + "Assets";
			if (!Directory.Exists (assetsDirPath)) {
				Directory.CreateDirectory (assetsDirPath);
			}
			XmlTextWriter writer = new XmlTextWriter (path + "assets.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement ("assets");
			foreach (AssetObjDef obj in objects.Values) {
				writer.WriteStartElement ("object");
				writer.WriteAttributeString ("name", obj.name);
				writer.WriteAttributeString ("texturename", obj.textureName);
				writer.WriteAttributeString ("shader", obj.shaderName);
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();		
		}
	}
}
