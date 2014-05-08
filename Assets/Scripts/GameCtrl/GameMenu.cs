using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;
using System.Threading;

public class GameMenu : MonoBehaviour
{
	private static GameMenu self;
	public GUISkin skin;
	public Texture2D banner;
	private Scene scene;
	GUIStyle styleLightNormalLeft;
	GUIStyle styleDarkNormalLeft;
	GUIStyle styleLightNormal;
	GUIStyle styleDarkNormal;
	GUIStyle styleDarkOver;
	static int defaultQuality = -1;
	
	public static void ActivateMenu ()
	{
		self.enabled = true;
		self.state = State.Main;
	}
	
	void Awake ()
	{
		self = this;
		GameSettings.Setup ();
		if (defaultQuality < 0) {
			defaultQuality = QualitySettings.GetQualityLevel ();
		}
	}
	
	void OnDestroy ()
	{
		self = null;
	}
	
	void Start ()
	{
		styleLightNormalLeft = skin.FindStyle ("Arial16-50");
		styleDarkNormalLeft = skin.FindStyle ("Arial16-75");
		styleLightNormal = skin.FindStyle ("Arial16-50-Centre");
		styleDarkNormal = skin.FindStyle ("Arial16-75-Centre");
		styleDarkOver = skin.FindStyle ("Arial16-W-Centre");
		player = new PlayerInfo ();
		player.firstName = PlayerPrefs.GetString ("PlayerFirstName", "John");
		player.familyName = PlayerPrefs.GetString ("PlayerFamilyName", "Fisher");
		player.isMale = (PlayerPrefs.GetString ("PlayerGender", "M") == "M");
	}
	
	int sWidth;
	int sHeight;
	int xOffset;
	
	enum State
	{
		Idle,
		Main,
		Settings,
		LoadNew,
		LoadNew2,
		LoadNew3,
		Continue1,
	};
	
	volatile State state = State.Main;
	
	void MainCtrl (int mx, int my)
	{
		int hheight = sHeight / 2;
		int hwidth = sWidth / 2;
		int index = -3;
		GUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index - 154, 620, 153), banner, GUIStyle.none);
		if (scene != null) {
			if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
				"Back to game", styleDarkNormal, styleDarkOver)) {
				state = State.Idle;
				enabled = false;
				GameControl.ActivateGameControl (scene);
			}
			if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
				"Back to menu", styleDarkNormal, styleDarkOver)) {
				scene = null;
				TerrainMgr.self.SetupTerrain (scene);
				CameraControl.SetupCamera (scene);
				EditorCtrl.self.SceneIsLoaded (scene);
				state = State.Main;				
			}
		} else {
			if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
				"Start new game", styleDarkNormal, styleDarkOver)) {
				scenes = Scene.ListAvailableScenarios ();
				state = State.LoadNew;
			}
			if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
				"Continue existing game", styleDarkNormal, styleDarkOver)) {
				saveGames = Scene.ListSaveGames ();
				state = State.Continue1;
			}
		}
		if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
				"Settings", styleDarkNormal, styleDarkOver)) {
			scenePath = GameSettings.ScenePath;
			savePath = GameSettings.SaveGamesPath;
			state = State.Settings;
		}
		if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
				"Quit", styleDarkNormal, styleDarkOver)) {
			Application.Quit ();
		}
		if (Application.isEditor || GameSettings.ALLOW_EDITOR && (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift))) {
			if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
				"Editor", styleDarkNormal, styleDarkOver)) {
				Application.LoadLevelAsync ("Editor");
				state = State.Idle;
				SimpleSpinner.ActivateSpinner ();
			}
		}
	}
	
	private string scenePath;
	private string savePath;
	
	void Settings (int mx, int my)
	{
		int hheight = sHeight / 2;
		int hwidth = sWidth / 2;
		int index = -3;
		GUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index - 154, 620, 153), banner, GUIStyle.none);
		if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Back", styleDarkNormal, styleDarkOver)) {
			state = State.Main;
		}
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index, 128, 32), "Scene path", styleDarkNormal);
		scenePath = SimpleGUI.TextField (new Rect (xOffset + hwidth - 181, hheight + 33 * index, 361, 32), scenePath, styleLightNormalLeft);
		if (SimpleGUI.Button (new Rect (xOffset + hwidth + 181, hheight + 33 * index, 64, 32), "Set", styleDarkNormal, styleDarkOver)) {
			GameSettings.ScenePath = scenePath;
			scenePath = GameSettings.ScenePath;
		}
		if (SimpleGUI.Button (new Rect (xOffset + hwidth + 246, hheight + 33 * index, 64, 32), "Reset", styleDarkNormal, styleDarkOver)) {
			GameSettings.ScenePath = GameSettings.DefaultScenePath;
			scenePath = GameSettings.ScenePath;
		}
		index++;
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index, 128, 32), "Save path", styleDarkNormal);
		savePath = SimpleGUI.TextField (new Rect (xOffset + hwidth - 181, hheight + 33 * index, 361, 32), savePath, styleLightNormalLeft);
		if (SimpleGUI.Button (new Rect (xOffset + hwidth + 181, hheight + 33 * index, 64, 32), "Set", styleDarkNormal, styleDarkOver)) {
			GameSettings.SaveGamesPath = savePath;
			savePath = GameSettings.SaveGamesPath;
		}
		if (SimpleGUI.Button (new Rect (xOffset + hwidth + 246, hheight + 33 * index, 64, 32), "Reset", styleDarkNormal, styleDarkOver)) {
			GameSettings.SaveGamesPath = GameSettings.DefaultSaveGamesPath;
			savePath = GameSettings.SaveGamesPath;
		}
		index++;
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index, 128, 32), "Graphics", styleDarkNormal);
		int quality = QualitySettings.GetQualityLevel ();
		int newQuality = (int)SimpleGUI.Slider (new Rect (xOffset + hwidth - 181, hheight + 33 * index, 361, 32), quality, 0, 5);
		if (newQuality != quality) {
			QualitySettings.SetQualityLevel (newQuality);
			TerrainMgr.self.UpdateQualitySettings ();
		}
		SimpleGUI.Label (new Rect (xOffset + hwidth + 181, hheight + 33 * index, 64, 32), QualitySettings.names [quality], styleDarkNormal);
		if (SimpleGUI.Button (new Rect (xOffset + hwidth + 246, hheight + 33 * index, 64, 32), "Reset", styleDarkNormal, styleDarkOver)) {
			QualitySettings.SetQualityLevel (defaultQuality);
			TerrainMgr.self.UpdateQualitySettings ();
		}
		index++;
	}
	
	Scene[] scenes;
	SaveGame[] saveGames;
	Scene loadScene;
	PlayerInfo player;
	
	IEnumerator COLoadGame (int slotNr)
	{
		state = State.Idle;
		SimpleSpinner.ActivateSpinner ();
		scene = null;
		yield return 0;
		try {
			scene = Scene.StartNewGame (loadScene.sceneName, slotNr, player);
		} catch (System.Exception e) {
			Log.LogException (e);
		}
		yield return 0;
		TerrainMgr.self.SetupTerrain (scene);
		CameraControl.SetupCamera (scene);
		EditorCtrl.self.SceneIsLoaded (scene);
		yield return 0;
		SimpleSpinner.DeactivateSpinner ();
		if (scene != null) {
			state = State.Idle;
			enabled = false;
			GameControl.ActivateGameControl (scene);
			scene.InitActions (true);
			GameControl.InterfaceChanged ();
		} else {
			state = State.Main;
		}
	}

	IEnumerator COContinueGame (int slotNr)
	{
		state = State.Idle;
		SimpleSpinner.ActivateSpinner ();
		scene = null;
		yield return 0;
		try {
			scene = Scene.LoadExistingGame (slotNr);
		} catch (System.Exception e) {
			Log.LogException (e);
		}
		yield return 0;
		TerrainMgr.self.SetupTerrain (scene);
		CameraControl.SetupCamera (scene);
		EditorCtrl.self.SceneIsLoaded (scene);
		yield return 0;
		SimpleSpinner.DeactivateSpinner ();
		if (scene != null) {
			state = State.Idle;
			enabled = false;
			GameControl.ActivateGameControl (scene);
			scene.InitActions (false);
			GameControl.InterfaceChanged ();
		} else {
			state = State.Main;
		}
	}
	
	void Continue1 (int mx, int my)
	{
		int hheight = sHeight / 2;
		int hwidth = sWidth / 2;
		int index = -3;
		GUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index - 154, 620, 153), banner, GUIStyle.none);
		if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Back", styleDarkNormal, styleDarkOver)) {
			state = State.Main;
		}
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Continue existing game", styleLightNormal);
		int x = 0;
		int slotNr = 0;
		foreach (SaveGame s in saveGames) {
			string slotName = (s == null) ? "<Empty>" : (s.sceneName + '\n' + "<size=12>" + s.playerInfo.firstName + ' ' + s.playerInfo.familyName + "</size>");
			if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310 + 207 * x, hheight + 33 * index, 206, 65),
				slotName, styleDarkNormal, styleDarkOver)) {
				// scene = Scene.StartNewGame (loadScene.sceneName, slotNr, player);
				StartCoroutine (COContinueGame (slotNr));
			}
			slotNr++;
			x ++;
			if (x > 2) {
				index += 2;
				x = 0;
			}
		}
		while (x > 0) {
			SimpleGUI.Label (new Rect (xOffset + hwidth - 310 + 207 * x, hheight + 33 * index, 206, 65),
				"", styleDarkNormal);
			if (x > 2) {
				index += 2;
				x = 0;
			}
		}
	}
	
	void StartNewGame3 (int mx, int my)
	{
		int hheight = sHeight / 2;
		int hwidth = sWidth / 2;
		int index = -3;
		GUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index - 154, 620, 153), banner, GUIStyle.none);
		if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Back", styleDarkNormal, styleDarkOver)) {
			state = State.LoadNew2;
		}
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Save game slot", styleLightNormal);
		int x = 0;
		int slotNr = 0;
		foreach (SaveGame s in saveGames) {
			string slotName = (s == null) ? "<Empty>" : (s.sceneName + '\n' + "<size=12>" + s.playerInfo.firstName + ' ' + s.playerInfo.familyName + "</size>");
			if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310 + 207 * x, hheight + 33 * index, 206, 65),
				slotName, styleDarkNormal, styleDarkOver)) {
				// scene = Scene.StartNewGame (loadScene.sceneName, slotNr, player);
				StartCoroutine (COLoadGame (slotNr));
			}
			slotNr++;
			x ++;
			if (x > 2) {
				index += 2;
				x = 0;
			}
		}
		while (x > 0) {
			SimpleGUI.Label (new Rect (xOffset + hwidth - 310 + 207 * x, hheight + 33 * index, 206, 65),
				"", styleDarkNormal);
			if (x > 2) {
				index += 2;
				x = 0;
			}
		}
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Progress is automatically saved at the start of each year.", styleLightNormal);
	}
	
	void StartNewGame2 (int mx, int my)
	{
		int hheight = sHeight / 2;
		int hwidth = sWidth / 2;
		int index = -3;
		GUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index - 154, 620, 153), banner, GUIStyle.none);
		if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Back", styleDarkNormal, styleDarkOver)) {
			state = State.LoadNew;
		}
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"What's your name?", styleLightNormal);
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index, 128, 32), "Given name", styleLightNormal);
		player.firstName = SimpleGUI.TextField (new Rect (xOffset + hwidth - 181, hheight + 33 * index, 491, 32), player.firstName, styleDarkNormalLeft);
		index ++;
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index, 128, 32), "Family name", styleLightNormal);
		player.familyName = SimpleGUI.TextField (new Rect (xOffset + hwidth - 181, hheight + 33 * index, 491, 32), player.familyName, styleDarkNormalLeft);
		index ++;
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index, 128, 32), "Gender", styleLightNormal);
		if (SimpleGUI.Button (new Rect (xOffset + hwidth - 181, hheight + 33 * index, 245, 32), "Male", player.isMale ? styleDarkOver : styleDarkNormal, styleDarkOver)) {
			player.isMale = true;
		}
		if (SimpleGUI.Button (new Rect (xOffset + hwidth + 65, hheight + 33 * index, 245, 32), "Female", player.isMale ? styleDarkNormal : styleDarkOver, styleDarkOver)) {
			player.isMale = false;
		}
		index++;
		if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Continue", styleDarkNormal, styleDarkOver)) {
			saveGames = Scene.ListSaveGames ();
			state = State.LoadNew3;
			PlayerPrefs.SetString ("PlayerFirstName", player.firstName);
			PlayerPrefs.SetString ("PlayerFamilyName", player.familyName);
			PlayerPrefs.SetString ("PlayerGender", player.isMale ? "M" : "F");
		}
	}
	
	void StartNewGame (int mx, int my)
	{
		int hheight = sHeight / 2;
		int hwidth = sWidth / 2;
		int index = -3;
		GUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index - 154, 620, 153), banner, GUIStyle.none);
		if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Back", styleDarkNormal, styleDarkOver)) {
			state = State.Main;
		}
		SimpleGUI.Label (new Rect (xOffset + hwidth - 310, hheight + 33 * index++, 620, 32),
			"Choose a game", styleLightNormal);
		int x = 0;
		foreach (Scene s in scenes) {
			if (SimpleGUI.Button (new Rect (xOffset + hwidth - 310 + 207 * x, hheight + 33 * index, 206, 65),
				s.sceneName, styleDarkNormal, styleDarkOver)) {
				loadScene = s;
				state = State.LoadNew2;
			}
			x ++;
			if (x > 2) {
				index += 2;
				x = 0;
			}
		}
		while (x > 0) {
			SimpleGUI.Label (new Rect (xOffset + hwidth - 310 + 207 * x, hheight + 33 * index, 206, 65),
				"", styleDarkNormal);
			x++;
			if (x > 2) {
				index += 2;
				x = 0;
			}
		}
		
	}
	
	void OnGUI ()
	{
		if (state == State.Idle)
			return;
		sWidth = Screen.width;
		sHeight = Screen.height;
		if (EditorCtrl.self.isOpen) {
			xOffset = 400;
			sWidth -= 400;
		} else {
			xOffset = 0;
		}
		
		Vector2 mousePos = Input.mousePosition;
		int mx = (int)mousePos.x;
		int my = sHeight - (int)mousePos.y;
		
		switch (state) {
		case State.Main :
			MainCtrl (mx, my);
			break;
		case State.LoadNew :
			StartNewGame (mx, my);
			break;
		case State.LoadNew2 :
			StartNewGame2 (mx, my);
			break;
		case State.LoadNew3 :
			StartNewGame3 (mx, my);
			break;
		case State.Continue1 :
			Continue1 (mx, my);
			break;
		case State.Settings :
			Settings (mx, my);
			break;
		}
	}
}
