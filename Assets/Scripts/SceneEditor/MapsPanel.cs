using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim.SceneEditor.Helpers;

namespace Ecosim.SceneEditor
{
	public class MapsPanel : Panel
	{
		public Scene scene;
		public EditorCtrl ctrl;
		private Vector2 scrollPos;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;
		public Texture2D texture;
		private PanelHelper helper;
		
		private enum ETabs
		{
			AREAS,
			HEIGHTMAP,
			PARAMETERS,
			VEGETATION,
			PLANTS,
			OBJECTS,
		};
		
		private ETabs tab;
		
		/**
		 * Called when scene is set or changed in Editor
		 */
		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
		}
		
		private string imagePath = GameSettings.DesktopPath + "map.png";
		
		public void RenderLoadTexture ()
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.Label ("Get data from image");
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("Image path", GUILayout.Width (60));
					imagePath = GUILayout.TextField (imagePath, GUILayout.Width (225));
					if (GUILayout.Button ("Load")) { //, GUILayout.Width (45))) {
						if (System.IO.File.Exists (imagePath)) {
							try {
								byte[] data = System.IO.File.ReadAllBytes (imagePath);
								texture = new Texture2D (4, 4, TextureFormat.ARGB32, false);
								texture.filterMode = FilterMode.Point;
								if (!texture.LoadImage (data)) {
									texture = null;
									ctrl.StartOkDialog ("Invalid image. Please choose another image and try again.", null);
								}
								
							} catch (System.Exception e) {
								Debug.LogException (e);
							}
						} else {
							ctrl.StartOkDialog ("Image could not be found, please check the path and try again.", null);
						}
					}
					//GUILayout.FlexibleSpace ();
				}
				GUILayout.EndHorizontal ();
				if (texture != null) 
				{
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Label ("Size " + texture.width + " x " + texture.height, GUILayout.Width (80));
						GUILayout.Label ("Horizontal offset ");
						offsetXStr = GUILayout.TextField (offsetXStr, GUILayout.Width (40));
						GUILayout.Label (" Vertical offset ");
						offsetYStr = GUILayout.TextField (offsetYStr, GUILayout.Width (40));
						int.TryParse (offsetXStr, out offsetX);
						int.TryParse (offsetYStr, out offsetY);
						//GUILayout.FlexibleSpace ();
					}
					GUILayout.EndHorizontal ();
					GUILayout.Label (texture, GUILayout.Width (380), GUILayout.Height (380));
				}
			}
			GUILayout.EndVertical ();
		}
		
		string offsetXStr = "0";
		string offsetYStr = "0";
		int offsetX = 0;
		int offsetY = 0;
		
		public float GetFromImage (int x, int y)
		{
			if (texture == null)
				return 0f;
			int xx = x + offsetX;
			if ((xx < 0) || (xx >= texture.width))
				return 0f;
			int yy = y + offsetY;
			if ((yy < 0) || (yy >= texture.height))
				return 0f;
			return texture.GetPixel (x + offsetX, y + offsetY).r;
		}

		
		bool RenderObjects (int mx, int my)
		{
			return false;
		}
		
		public void ResetEdit ()
		{
			if (helper != null) {
				helper.Disable ();
				helper = null;
			}
		}

		private void StartAreas ()
		{
			ResetEdit ();
			helper = new HandleAreas (ctrl, this, scene);
		}

		private void StartHeightmap ()
		{
			ResetEdit ();
			helper = new HandleHeightmap (ctrl, this, scene);
		}
		
		private void StartParameters ()
		{
			ResetEdit ();
			helper = new HandleParameters (ctrl, this, scene, null);
		}

		private void StartVegetation ()
		{
			ResetEdit ();
			helper = new HandleVegetation (ctrl, this, scene);
		}

		private void StartPlants ()
		{
			ResetEdit ();
			helper = new HandlePlants (ctrl, this, scene);
		}

		void StartObjects ()
		{
			ResetEdit ();
			helper = new  HandleObjects (ctrl, this, scene);
		}
		
		/**
		 * Called every frame when this panel is active
		 * It is guaranteed that setup has been called with
		 * a valid scene before the first call of Render.
		 */
		public bool Render (int mx, int my)
		{
			float width = 60f;

			GUILayout.BeginHorizontal ();
			{
				if (GUILayout.Button ("Areas", (tab == ETabs.AREAS) ? tabSelected : tabNormal, GUILayout.Width (width))) {
					tab = ETabs.AREAS;
					StartAreas ();
				}
				if (GUILayout.Button ("Heightmap", (tab == ETabs.HEIGHTMAP) ? tabSelected : tabNormal, GUILayout.Width (width))) {
					tab = ETabs.HEIGHTMAP;
					StartHeightmap ();
				}
				if (GUILayout.Button ("Parameters", (tab == ETabs.PARAMETERS) ? tabSelected : tabNormal, GUILayout.Width (width))) {
					tab = ETabs.PARAMETERS;
					StartParameters ();
				}
				if (GUILayout.Button ("Vegetation", (tab == ETabs.VEGETATION) ? tabSelected : tabNormal, GUILayout.Width (width))) {
					tab = ETabs.VEGETATION;
					StartVegetation ();
				}
				if (GUILayout.Button("Plants", (tab == ETabs.PLANTS) ? tabSelected : tabNormal, GUILayout.Width (width))) {
					tab = ETabs.PLANTS;
					StartPlants();
				}
				if (GUILayout.Button ("Objects", (tab == ETabs.OBJECTS) ? tabSelected : tabNormal, GUILayout.Width (width))) {
					tab = ETabs.OBJECTS;
					StartObjects ();
				}

				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndHorizontal ();
			GUILayout.Space (4);
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			GUILayout.BeginVertical ();
			bool result = helper.Render (mx, my);
			GUILayout.FlexibleSpace ();
			
			GUILayout.EndVertical ();
			GUILayout.EndScrollView ();
			return result;
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
			switch (tab) {
			case ETabs.AREAS :
				StartAreas ();
				break;
			case ETabs.HEIGHTMAP :
				StartHeightmap ();
				break;
			case ETabs.PARAMETERS :
				StartParameters ();
				break;
			case ETabs.VEGETATION :
				StartVegetation ();
				break;
			case ETabs.PLANTS : 
				StartPlants ();
				break;
			case ETabs.OBJECTS :
				StartObjects ();
				break;
			}
		}
		
		public void Deactivate ()
		{
			ResetEdit ();
		}
		
		public void Update() {
			if (helper != null) {
				helper.Update();
			}
		}
	}
}