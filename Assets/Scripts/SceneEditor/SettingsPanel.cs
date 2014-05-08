using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim;

namespace Ecosim.SceneEditor
{
	public class SettingsPanel : Panel
	{
		
		
//		Scene scene;
		EditorCtrl ctrl;
		private Vector2 scrollPos;
		public static float perlinScale = 0.01f;
		
		/**
		 * Called when scene is set or changed in Editor
		 */
		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
//			this.scene = scene;
			
			scenePath = GameSettings.ScenePath;
			saveGamesPath = GameSettings.SaveGamesPath;
			monoPath = GameSettings.MonoPath;
		}
		
		private string scenePath;
		private string saveGamesPath;
		private string monoPath;
		
		/**
		 * Called every frame when this panel is active
		 * It is guaranteed that setup has been called with
		 * a valid scene before the first call of Render.
		 */
		public bool Render (int mx, int my)
		{
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			GUILayout.BeginVertical ();
			GUILayout.Label ("Version " + GameSettings.VERSION_STR);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Quality", GUILayout.Width (65));
			int currentQuality = QualitySettings.GetQualityLevel ();
			int newQuality = (int)GUILayout.HorizontalSlider (currentQuality, 0, 5, GUILayout.Width (100));
			GUILayout.Label (newQuality.ToString ());
			if (newQuality != currentQuality) {
				QualitySettings.SetQualityLevel (newQuality);
				TerrainMgr.self.UpdateQualitySettings ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Terrain range", GUILayout.Width (65));
			int currentRange = TerrainMgr.self.range;
			int newRange = (int)GUILayout.HorizontalSlider (currentRange, 64, 256, GUILayout.Width (100));
			GUILayout.Label (newRange.ToString ());
			if (newRange != currentRange) {
				if ((currentRange <= 128) && (newRange > 128)) {
					ctrl.StartDialog ("Values > 128 should only used on powerful systems in 64-bit mode. Continue?", result => {
						TerrainMgr.self.range = newRange;
					}, null);
				} else {
					TerrainMgr.self.range = newRange;
				}
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			GUILayout.Space (4);

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Scenes", GUILayout.Width (65));
			string newScenePath = GUILayout.TextField (scenePath, GUILayout.Width (200));
			if (newScenePath != scenePath) {
				GameSettings.ScenePath = newScenePath;
				scenePath = newScenePath;
			}
			if (GUILayout.Button ("Desktop", GUILayout.Width (50))) {
				scenePath = GameSettings.DesktopPath;
				GameSettings.ScenePath = scenePath;
			}
			if (GUILayout.Button ("Default", GUILayout.Width (50))) {
				scenePath = GameSettings.DefaultScenePath;
				GameSettings.ScenePath = scenePath;
			}
			// GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal ();
			GUILayout.Space (4);

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Savegames", GUILayout.Width (65));
			string newSaveGamesPath = GUILayout.TextField (saveGamesPath, GUILayout.Width (200));
			if (newSaveGamesPath != saveGamesPath) {
				GameSettings.SaveGamesPath = newSaveGamesPath;
				saveGamesPath = newSaveGamesPath;
			}
			if (GUILayout.Button ("Desktop", GUILayout.Width (50))) {
				saveGamesPath = GameSettings.DesktopPath;
				GameSettings.SaveGamesPath = saveGamesPath;
			}
			if (GUILayout.Button ("Default", GUILayout.Width (50))) {
				saveGamesPath = GameSettings.DefaultSaveGamesPath;
				GameSettings.SaveGamesPath = saveGamesPath;
			}
			// GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal ();
			GUILayout.Space (4);

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("MONO", GUILayout.Width (65));
			string newMonoPath = GUILayout.TextField (monoPath, GUILayout.Width (200));
			if (newMonoPath != monoPath) {
				GameSettings.MonoPath = newMonoPath;
				monoPath = newMonoPath;
			}
			if (GUILayout.Button ("Default", GUILayout.Width (50))) {
				monoPath = GameSettings.DefaultMonoPath;
				GameSettings.MonoPath = monoPath;
			}
			// GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal ();
			GUILayout.Space (8);
			
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Perlin scale", GUILayout.Width (65));
			perlinScale = GUILayout.HorizontalSlider (perlinScale, 0.001f, 1f, GUILayout.Width (200));
			GUILayout.Label (perlinScale.ToString ("0.000"));
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Debug scripts", GUILayout.Width (65));
			GameSettings.DebugScripts = GUILayout.Toggle (GameSettings.DebugScripts, "save generated scripts to desktop");
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			
			if (!ctrl.infoWinIsOpen) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Info window", GUILayout.Width (65));
				if (GUILayout.Button ("Reopen")) {
					ctrl.infoWinIsOpen = true;
				}
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
			}
			
			GUILayout.Space (16);
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Restart Ecosim", GUILayout.Width (65));
			if (GUILayout.Button ("RESTART")) {
				Application.LoadLevel ("Startup");
			}
			GUILayout.EndHorizontal ();
			GUILayout.FlexibleSpace ();
			GUILayout.EndVertical ();
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
			return true;
		}

		public void Activate ()
		{
		}
		
		public void Deactivate ()
		{
		}

		public void Update ()
		{
		}
	}
}