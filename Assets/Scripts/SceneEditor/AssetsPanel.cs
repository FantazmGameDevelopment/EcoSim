using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim;

namespace Ecosim.SceneEditor
{
	public class AssetsPanel : Panel
	{
		
		
		Scene scene;
		EditorCtrl ctrl;
		private Vector2 scrollPos;
		private Texture2D renderTex = null;
		
		/**
		 * Called when scene is set or changed in Editor
		 */
		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			if (scene != null) {
				string path = GameSettings.GetPathForScene (scene.sceneName) + "Assets" + Path.DirectorySeparatorChar;
				string[] objectFiles;
				
				if (Directory.Exists (path)) {
					objectFiles = Directory.GetFiles (path, "*.obj");
					textureFiles = Directory.GetFiles (path, "*.png");
				} else {
					objectFiles = new string[0];
					textureFiles = new string[0];
				}		
				
				for (int i = 0; i < objectFiles.Length; i++) {
					string str = objectFiles [i];
					str = str.Substring (str.LastIndexOf (Path.DirectorySeparatorChar) + 1);
					str = str.Substring (0, str.Length - 4);
					objectFiles [i] = str;
				}
				for (int i = 0; i < textureFiles.Length; i++) {
					string str = textureFiles [i];
					str = str.Substring (str.LastIndexOf (Path.DirectorySeparatorChar) + 1);
					str = str.Substring (0, str.Length - 4);
					textureFiles [i] = str;
				}
				objects = new ObjectInfo[objectFiles.Length];
				for (int i = 0; i < objectFiles.Length; i++) {
					ObjectInfo info = new ObjectInfo ();
					info.name = objectFiles [i];
					info.def = scene.assets.GetObjectDef (info.name);
					if (info.def != null) {
						info.textureIndex = TextureNameToIndex (info.def.textureName);
						info.shaderIndex = ShaderNameToIndex (info.def.shaderName);
					}
					objects [i] = info;
				}
			}
		}
		
		string[] textureFiles;
		string[] shaders = new string[] { "Opaque", "Transparent", "Cutout" };
		ObjectInfo[] objects;
		
		int TextureNameToIndex (string name)
		{
			name = name.ToLower ();
			for (int i = 0; i < textureFiles.Length; i++) {
				if (textureFiles [i].ToLower () == name)
					return i;
			}
			return 0;
		}
		
		int ShaderNameToIndex (string name)
		{
			for (int i = 0; i < shaders.Length; i++) {
				if (shaders [i] == name)
					return i;
			}
			return 0;
		}
		
		private class ObjectInfo
		{
			public string name;
			public ExtraAssets.AssetObjDef def;
			public int textureIndex;
			public int shaderIndex;
		}

		/*void RenderPreviewTexture (Mesh mesh, Material mat) {
			(GameObject.FindObjectOfType <MonoBehaviour> () as MonoBehaviour).StartCoroutine (CORenderPreviewTexture(mesh, mat));
		}*/

		//IEnumerator CORenderPreviewTexture (Mesh mesh, Material mat) {
		void RenderPreviewTexture (Mesh mesh, Material mat) {
			if (renderTex == null) {
				renderTex = new Texture2D (380, 300, TextureFormat.RGB24, false);
			}
			RenderTileIcons.RenderSettings rs = new RenderTileIcons.RenderSettings (60f, 30f, 120f, 24f);
			//yield return new WaitForEndOfFrame ();
			RenderTileIcons.self.Render (rs, ref renderTex, scene.successionTypes[0].vegetations[0].tiles[0], mesh, mat);
		}
		
		/**
		 * Called every frame when this panel is active
		 * It is guaranteed that setup has been called with
		 * a valid scene before the first call of Render.
		 */
		public bool Render (int mx, int my)
		{
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			GUILayout.BeginVertical ();
			if (scene.assets.hasIconsTexture) {
				GUILayout.Label ("icons from icons<i>number</i>.png files");
				GUILayout.BeginHorizontal ();
				int count = 0;
				foreach (Texture2D tex in scene.assets.icons) {
					if ((count > 0) && (count % 10 == 0)) {
						GUILayout.FlexibleSpace ();
						GUILayout.EndHorizontal ();
						GUILayout.BeginHorizontal ();
					}
					GUILayout.Label (tex);
					count++;
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			} else {
				GUILayout.Label ("No valid icons<i>number</i>.png in Assets folder");
			}
			GUILayout.Space (10);
			GUILayout.Label ("objects from assets");
			for (int i = 0; i < objects.Length; i++) {
				ObjectInfo info = objects [i];
				GUILayout.BeginHorizontal ();
				GUILayout.Label (info.name, GUILayout.Width (100));
				if (GUILayout.Button (textureFiles [info.textureIndex], GUILayout.Width (90))) {
					ctrl.StartSelection (textureFiles, info.textureIndex, newIndex => {
						if (newIndex != info.textureIndex) {
							info.textureIndex = newIndex;
						}
					});
				}
				if (GUILayout.Button (shaders [info.shaderIndex], GUILayout.Width (90))) {
					ctrl.StartSelection (shaders, info.shaderIndex, newIndex => {
						if (newIndex != info.shaderIndex) {
							info.shaderIndex = newIndex;
						}
					});
				}
				if (info.def == null) {
					if (GUILayout.Button ("Add", GUILayout.Width (60))) {
						info.def = scene.assets.AddObject (GameSettings.GetPathForScene (scene.sceneName),
							info.name, textureFiles [info.textureIndex], shaders [info.shaderIndex]);
						EcoTerrainElements.self.AddExtraBuildings (scene.assets);
					}
				} else {
					if (GUILayout.Button ("Remove", GUILayout.Width (60))) {
						scene.assets.RemoveObject (info.name);
						info.def = null;
						EcoTerrainElements.self.AddExtraBuildings (scene.assets);
					}
					if (GUILayout.Button ("[]")) {
						RenderPreviewTexture (info.def.mesh, info.def.material);
					}
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
			
			GUILayout.Space (16);
			if (renderTex != null) {
				GUILayout.Label (renderTex, GUIStyle.none);
			}
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("", GUILayout.Width (100));
			if (GUILayout.Button ("Reread data", GUILayout.Width (80))) {
				scene.assets.ResetCache ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			
			GUILayout.EndVertical ();
			GUILayout.FlexibleSpace ();
			GUILayout.EndScrollView ();
			return false;
		}
		
		/* Called for extra edit sub-panel, will be called after Render */
		public void RenderExtra (int mx, int my)
		{
		}

		/* Called for extra side edit sub-panel, will be called after RenderExtra */
		public void RenderSide (int mx, int my)
		{
		}
		
		/* Returns true if a side panel is needed. Won't be called before RenderExtra has been called */
		public bool NeedSidePanel ()
		{
			return false;
		}
		
		public bool IsAvailable ()
		{
			return (scene != null);
		}

		public void Activate ()
		{
		}
		
		public void Deactivate ()
		{
			if (renderTex != null) {
				UnityEngine.Object.Destroy (renderTex);
			}
		}

		public void Update ()
		{
		}
	}
}