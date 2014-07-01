using System.Collections.Generic;
using System.IO;
using System.Xml;
using Ecosim.SceneData.Action;
using Ecosim.EcoScript.Eval;

namespace Ecosim.SceneData
{
	public class Scene
	{		
		public const string XML_ELEMENT = "scene";
		public const int OVERVIEW_TEXTURE_SIZE = 1024;
		public string sceneName; // name of scene
		public string description; // scene description
		public int slotNr = -1; // name of savegame slot
	
		public SuccessionType[] successionTypes;
		public PlantType[] plantTypes;
		public ActionObjectsGroup[] actionObjectGroups;
		public AnimalType[] animalTypes;
		public CalculatedData.Calculation[] calculations;

		public ActionMgr actions;
		public ReportsMgr reports;

		public Progression progression;
		public PlayerInfo playerInfo;
		public Buildings buildings;
		public Roads roads;
		public ExtraAssets assets;
		public Articles articles;
		public readonly EcoExpression expression;
		public readonly int width;
		public readonly int height;
		public UnityEngine.Texture2D[,] overview;

		public event System.Action onGameEnd;
		
		public Progression GetHistoricProgression (int year)
		{
			if (progression != null) {
				if (year == progression.year) {
					return progression;
				} else if (year < progression.startYear) {
					year = 0; // we have to load default progression info
				}
			}
			return Progression.Load (this, year);
		}

		/**
		 * Deletes savegame with given slotnr
		 */
		public static void DeleteSaveGame (int slotNr)
		{
			string path = GameSettings.GetPathForSlotNr (slotNr);
			if (Directory.Exists (path)) {
				Directory.Delete (path, true);
			}
		}

		/**
		 * We do InitActions separate from StartNewGame / LoadExistingGame
		 * to make sure all contexts (like scene variable) have been completely
		 * set up and scripts will work correctly when accessing scene or progression
		 * properties.
		 */
		public void InitActions (bool isNewGame) {
			progression.InitActions (isNewGame);
			if (isNewGame) {
				progression.Save ();
			}
		}

		/**
		 * We do InitActions separate from StartNewGame / LoadExistingGame
		 * to make sure all contexts (like scene variable) have been completely
		 * set up and scripts will work correctly when accessing scene or progression
		 * properties.
		 */
		public void InitReports (bool isNewGame) {
			if (isNewGame) {
				reports.Init ();
			}
		}
		
		/**
		 * Start a new game with scene named name, progress of the game is saved
		 * at slot slotNr.
		 */
		public static Scene StartNewGame (string name, int slotNr, PlayerInfo playerInfo)
		{
			// first try loading scene
			Scene scene = Load (name);
			if (scene != null) {
				// now create the save game...
				scene.slotNr = slotNr;
				// we delete old save game information
				DeleteSaveGame (slotNr);
				scene.progression = Progression.Load (scene, 0); // loads initial progression data
				scene.playerInfo = playerInfo;
				
				scene.progression.year = scene.progression.startYear; // start with first year
				scene.SetupListeners ();

				// we make an initial save in the slot
				SaveGame saveGame = new SaveGame (scene);
				saveGame.Save (scene);
				scene.UpdateReferences ();
				bool success = EcoScript.Compiler.LoadAssemblyFromDisk (scene);
				if (!success) {
					Log.LogError ("Failed to load EcoScript assembly");
				}
//				scene.progression.InitActions (true);
//				scene.progression.Save ();				
			}
			return scene;
		}
		
		/**
		 * Load a saved game at stored at slotNr, if previewOnly == true, only basic scene information is loaded
		 * for showing info about saved game
		 */
		public static Scene LoadExistingGame (int slotNr)
		{
			SaveGame saveGame = SaveGame.Load (slotNr);
			if (saveGame == null) {
				Log.LogError ("Slot " + slotNr + " does not contain a valid savegame");
				return null;
			}
			Scene scene = Load (saveGame.sceneName);
			if (scene == null) {
				Log.LogError ("Scene '" + saveGame.sceneName + "' not found.");
				return null;
			}
			scene.playerInfo = saveGame.playerInfo;
			scene.slotNr = slotNr;
			scene.progression = Progression.Load (scene, saveGame.year);
			if (scene.progression == null) {
				Log.LogError ("Progression not found.");
			}
			scene.SetupListeners ();
			// Check if the game was already ended
			if (scene.progression.gameEnded == true) {
				if (scene.onGameEnd != null)
					scene.onGameEnd ();
			}
			scene.UpdateReferences ();
			bool success = EcoScript.Compiler.LoadAssemblyFromDisk (scene);
			if (!success) {
				Log.LogError ("Failed to load EcoScript assembly");
			}
//			scene.progression.InitActions (false);

			return scene;
		}
		
		/**
		 * Loads in scene 'name' for editing purposes, no player info will be defined and slotnr = -1
		 */
		public static Scene LoadForEditing (string name)
		{
			Scene scene = Load (name);
			scene.progression = Progression.Load (scene, 0); // loads initial progression data
			scene.SetupListeners ();
			scene.UpdateReferences ();
			bool success = EcoScript.Compiler.LoadAssemblyFromDisk (scene);
			if (!success) {
				Log.LogError ("Failed to load EcoScript assembly");
			}
			scene.progression.InitActions (true);
			return scene;
		}
		
		public static Scene CreateNewScene (string name, int width, int height, SuccessionType[] successionTypes, PlantType[] plantTypes, AnimalType[] animalTypes, ActionObjectsGroup[] actionObjectGroups, CalculatedData.Calculation[] calculations)
		{
			Scene scene = new Scene (name, width, height, successionTypes, plantTypes, animalTypes, actionObjectGroups, calculations);
			scene.assets = new ExtraAssets (scene);
			scene.actions = new ActionMgr (scene);
			scene.reports = new ReportsMgr (scene);
			scene.buildings = new Buildings (scene);
			scene.roads = new Roads (scene);
			scene.progression = new Progression (scene);
			scene.articles = new Articles (scene);
			
			// add the basic data to progression
			// we start with land at 2m above absolute 0
			// we start with water at 1m above absolute 0
			HeightMap heightMap = new HeightMap (scene);
			heightMap.AddAll (200);
			HeightMap waterHeightMap = new HeightMap (scene);
			waterHeightMap.AddAll (100);
			scene.progression.CreateBasicData ();
			scene.UpdateReferences ();
			scene.progression.InitActions (true);
			scene.SetupListeners ();
			scene.Save (scene.sceneName);
			return scene;
		}
		
		/**
		 * Forces all current data is loaded into memory
		 * This is useful if an edited scenario is to be saved under
		 * a different name
		 */
		public void ForceLoadAllData ()
		{
			List<string> keys = progression.GetAllDataNames ();
			foreach (string key in keys) {
				// force load if not already loaded
				progression.GetData (key);
			}
		}
		
		/**
		 * Gives back array with scene preview of all available scenes
		 */
		public static Scene[] ListAvailableScenarios ()
		{
			List<Scene> list = new List<Scene> ();
			string[] dirs = Directory.GetDirectories (GameSettings.ScenePath);
			foreach (string dir in dirs) {
				try {
					string name = Path.GetFileName (dir);
					Scene scene = LoadForPreview (name);
					if (scene != null) {
						list.Add (scene);
					}
				} catch (System.Exception) {
				}
			}
			
			return list.ToArray ();
		}
		
		/**
		 * Gives back array with slotNrs as index, for used slots a preview of the scene is given, otherwise null
		 */
		public static SaveGame[] ListSaveGames ()
		{
			SaveGame[] saveGames = new SaveGame[GameSettings.MAX_SAVEGAME_SLOTS];
			for (int i = 0; i < saveGames.Length; i++) {
				try {
					saveGames [i] = SaveGame.Load (i);
				} catch (System.Exception) {
				}
			}
			return saveGames;
		}
		
		void LoadTiles (string path)
		{
			path = path + "Map" + Path.DirectorySeparatorChar;
			for (int y = 0; y < overview.GetLength (0); y++) {
				for (int x = 0; x < overview.GetLength (1); x++) {
					string filePath = path + "tile" + x + "x" + y + ".png";
					if (File.Exists (filePath)) {
						byte[] bytes = File.ReadAllBytes (filePath);
						UnityEngine.Texture2D tex = new UnityEngine.Texture2D (OVERVIEW_TEXTURE_SIZE, OVERVIEW_TEXTURE_SIZE, UnityEngine.TextureFormat.RGB24, false);
						tex.LoadImage (bytes);
						tex.wrapMode = UnityEngine.TextureWrapMode.Clamp;
						overview [y, x] = tex;
					}
				}
			}
		}
		
		void SaveTiles (string path)
		{
			path = path + "Map" + Path.DirectorySeparatorChar;
			if (!Directory.Exists (path)) {
				Directory.CreateDirectory (path);
			}
			if (Directory.Exists (path)) {
				for (int y = 0; y < overview.GetLength (0); y++) {
					for (int x = 0; x < overview.GetLength (1); x++) {
						if (overview [y, x] != null) {
							byte[] bytes = overview [y, x].EncodeToPNG ();
							File.WriteAllBytes (path + "tile" + x + "x" + y + ".png", bytes);
						}
					}
				}
			}
		}
		
		/**
		 * Save the scene (only for editor use, for game play use SaveProgress, although this actually doesn't
		 * save the scene itself, as it doesn't change during gameplay (but Progression does change))
		 */
		public void Save (string newSceneName)
		{
			if (!isSameScene (sceneName, newSceneName)) {
				CopyIfNeeded (sceneName, newSceneName, "Scripts");
				CopyIfNeeded (sceneName, newSceneName, "Assets");
				CopyIfNeeded (sceneName, newSceneName, "ArticleData");
				sceneName = newSceneName;
			}
			string path = GameSettings.GetPathForScene (sceneName);
			if (!Directory.Exists (path)) {
				Directory.CreateDirectory (path);
			}
			
			XmlTextWriter writer = new XmlTextWriter (path + "scene.xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("width", width.ToString ());
			writer.WriteAttributeString ("height", height.ToString ());
			writer.WriteAttributeString ("description", description);
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();
			
			SaveTiles (path);
			
			LoadSaveVegetation.Save (path, successionTypes, this);
			LoadSavePlants.Save (path, plantTypes, this);
			LoadSaveAnimals.Save (path, animalTypes, this);
			ActionObjectsGroup.Save (path, actionObjectGroups, this);
			CalculatedData.Calculation.Save (path, calculations, this);

			assets.Save (path);
			reports.Save (path);
			buildings.Save (path);
			roads.Save (path);
			progression.Save ();
			articles.Save (path);
			actions.Save (path);
		}

		static Scene Load (string name, XmlTextReader reader)
		{
//			budget = long.Parse (reader.GetAttribute ("budget"));
			int width = int.Parse (reader.GetAttribute ("width"));
			int height = int.Parse (reader.GetAttribute ("height"));
			Scene scene = new Scene (name, width, height, new SuccessionType[0], new PlantType[0], new AnimalType[0], new ActionObjectsGroup[0], new CalculatedData.Calculation[0]);
			scene.description = reader.GetAttribute ("description");
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			return scene;
		}
		
		/**
		 * Loads only basic scene info for preview in start new game
		 */
		static Scene LoadForPreview (string name)
		{
			string path = GameSettings.GetPathForScene (name);
			
			Scene scene = null;
			XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path + "scene.xml"));
			try {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == XML_ELEMENT)) {
						scene = Scene.Load (name, reader);
					}
				}
			} finally {
				reader.Close ();
			}
			return scene;
		}
		
		static Scene Load (string name)
		{
			string path = GameSettings.GetPathForScene (name);
			
			Scene scene = null;
			XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path + "scene.xml"));
			try {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == XML_ELEMENT)) {
						scene = Scene.Load (name, reader);
					}
				}
			} finally {
				reader.Close ();
			}
			if (scene != null) {
				scene.LoadTiles (path);
				scene.assets = ExtraAssets.Load (path, scene);
				scene.successionTypes = LoadSaveVegetation.Load (path, scene);
				scene.plantTypes = LoadSavePlants.Load (path, scene);
				scene.animalTypes = LoadSaveAnimals.Load (path, scene);
				scene.calculations = CalculatedData.Calculation.LoadAll (path, scene);
				EcoTerrainElements.self.AddExtraBuildings (scene.assets);
				scene.buildings = Buildings.Load (path, scene);
				scene.actionObjectGroups = ActionObjectsGroup.Load (path, scene);
				scene.roads = Roads.Load (path, scene);
				scene.articles = Articles.Load (path, scene);
				scene.actions = ActionMgr.Load (path, scene);
				scene.reports = ReportsMgr.Load (path, scene);
			}
			return scene;
		}
				
		/**
		 * Create a new scene with given name, size and optional vegetation (successionTypes)
		 * if vegetation == null a default Succession with one vegetation will be created
		 */
		Scene (string name, int width, int height, SuccessionType[] successionTypes, PlantType[] plantTypes, AnimalType[] animalTypes, ActionObjectsGroup[] actionObjectGroups, CalculatedData.Calculation[] calculations)
		{
			sceneName = name;
			description = "";
			this.width = width;
			this.height = height;

			if (successionTypes != null) {
				this.successionTypes = successionTypes;
			} else {
				this.successionTypes = new SuccessionType[] { };
				SuccessionType suc = new SuccessionType (this);
				new VegetationType (suc);
			}

			this.plantTypes = (plantTypes != null) ? plantTypes : new PlantType[] { };
			this.animalTypes = (animalTypes != null) ? animalTypes : new AnimalType[] { };
			this.actionObjectGroups = (actionObjectGroups != null) ? actionObjectGroups : new ActionObjectsGroup[] { };
			this.calculations = (calculations != null) ? calculations : new CalculatedData.Calculation[] { };

			overview = new UnityEngine.Texture2D[height / TerrainMgr.CELL_SIZE, width / TerrainMgr.CELL_SIZE];
			expression = new EcoExpression (this);
		}
	
		
		/**
		 * Needs to be called after handling succession
		 * Stores the progress to the gameslot for this game
		 * year must be set to the new year otherwise the progression
		 * will be stored under incorrect year nr.
		 */
		public void SaveProgress ()
		{
			progression.Advance ();
		}

		public void UpdateReferences ()
		{
			actions.UpdateReferences ();
			reports.UpdateReferences ();
			int i = 0;
			foreach (SuccessionType s in successionTypes) {
				s.index = i++;
			}
			foreach (SuccessionType s in successionTypes) {
				s.UpdateReferences (this);
			}

			i = 0;
			foreach (PlantType p in plantTypes) {
				p.index = i++;
			}
			foreach (PlantType p in plantTypes) {
				p.UpdateReferences (this);
			}

			i = 0;
			foreach (AnimalType a in animalTypes) {
				a.index = i++;
			}
			foreach (AnimalType a in animalTypes) {
				a.UpdateReferences (this);
			}

			i = 0;
			foreach (ActionObjectsGroup g in actionObjectGroups) {
				g.index = i++;
			}
			foreach (ActionObjectsGroup g in actionObjectGroups) {
				g.UpdateReferences (this);
			}

			foreach (CalculatedData.Calculation c in calculations) {
				c.UpdateReferences (this);
			}
		}
		
		public static bool isSameScene (string scene1Name, string scene2Name)
		{
			string path1 = GameSettings.GetPathForScene (scene1Name);
			string path2 = GameSettings.GetPathForScene (scene2Name);
			return (Path.GetFullPath (path1) == Path.GetFullPath (path2));
		}
		
		private static void CopyIfNeeded (string oldScene, string newScene, string dir)
		{
			if (isSameScene (oldScene, newScene))
				return;
			string srcPath = GameSettings.GetPathForScene (oldScene) + dir + Path.DirectorySeparatorChar;
			string dstPath = GameSettings.GetPathForScene (newScene) + dir + Path.DirectorySeparatorChar;
			
			if (!Directory.Exists (srcPath))
				return;
			IOUtil.DirectoryCopy (srcPath, dstPath, true);
		}
		
		public Scene ResizeTo (string newSceneName, int offsetX, int offsetY, int newWidth, int newHeight)
		{
			Scene newScene = new Scene (newSceneName, newWidth, newHeight, successionTypes, plantTypes, animalTypes, actionObjectGroups, calculations);
			Progression newProgression = new Progression (newScene);
			newScene.progression = newProgression;
			newScene.SetupListeners ();
			foreach (string name in progression.GetAllDataNames()) {
				Data data = progression.GetData (name);
				Data newData = data.CloneAndResize (newProgression, offsetX, offsetY);
				newProgression.AddData (name, newData);
			}
			CopyIfNeeded (sceneName, newSceneName, "Scripts");
			CopyIfNeeded (sceneName, newSceneName, "Assets");
			CopyIfNeeded (sceneName, newSceneName, "ArticleData");
			
			string newScenePath = GameSettings.GetPathForScene (newSceneName);
			actions.Save (newScenePath);
			newScene.actions = ActionMgr.Load (newScenePath, newScene);
			reports.Save (newScenePath);
			newScene.reports = ReportsMgr.Load (newScenePath, newScene);
			assets.Save (newScenePath);
			articles.Save (newScenePath);
			newScene.assets = ExtraAssets.Load (newScenePath, newScene);
			
			roads.SaveAndClip (newScenePath, offsetX, offsetY, newWidth, newHeight);
			newScene.roads = Roads.Load (newScenePath, newScene);
			
			buildings.SaveAndClip (newScenePath, offsetX, offsetY, newWidth, newHeight);
			newScene.buildings = Buildings.Load (newScenePath, newScene);
			
			newScene.Save (newSceneName);
			return newScene;
		}

		private void SetupListeners ()
		{
			if (progression != null) {
				progression.onGameEndChanged -= OnGameEndChanged;
				progression.onGameEndChanged += OnGameEndChanged;
			}
		}

		private void OnGameEndChanged (bool value)
		{
			if (value && this.onGameEnd != null)
				this.onGameEnd ();
		}
	}

}