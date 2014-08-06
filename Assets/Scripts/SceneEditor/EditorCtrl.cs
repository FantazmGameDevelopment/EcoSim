using UnityEngine;
using System;
using System.Collections;
using System.Reflection;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneEditor;

public class EditorCtrl : MonoBehaviour
{
	[System.Serializable]
	public class Tool
	{
		public string name;
		public Texture2D icon;
		public Panel panel;
	}
	
	public bool isGameMode = false;
	public Tool[] tools;
	public int activeToolIndex = -1;
	public Texture2D foldedOpen;
	public Texture2D foldedClose;
	public Texture2D foldedOpenSmall;
	public Texture2D foldedCloseSmall;
	public Texture2D warning;
	public Texture2D error;
	public Texture2D script;
	public Texture2D placeholder;
	public Texture2D questionMark;
	public Texture2D editSmall;
	public Texture2D viewSmall;
	public Scene scene;
	public GUISkin skin;
	public static EditorCtrl self;
	Rect winAreaR;
	Rect winAreaSideR;
	Rect infoWinR;
	public bool isOpen = true;
	public bool infoWinIsOpen;
	public GUIStyle icon;
	public GUIStyle selectedIcon;
	public GUIStyle icon12x12;
	public GUIStyle tileIcon;
	public GUIStyle listItem;
	public GUIStyle listItemSelected;
	public GameObject handlePrefab;
	private Texture2D imageToShow;
	private TerrainInfoPanel infoWin;
	
	public delegate void itemSelected (int index);
	public delegate void itemSelectedResult (int index, string result);

	public delegate void action (bool result);
	
	/**
	 * Shows a dialog with message msg and yes, no buttons.
	 * yesAction will be executed when yes is pressed and not set to null.
	 * noAction will be executed when no is pressed and not set to null.
	 */
	public void StartDialog (string msg, action yesAction, action noAction)
	{
		Vector3 mousePos = Input.mousePosition;
		int mx = (int)(mousePos.x);
		int my = (int)(Screen.height - mousePos.y);
		int x = mx - 100;
		if (x < 0)
			x = 0;
		int y = my - 100;
		if (y < 64)
			y = 64;
		if (y + 150 > Screen.height)
			y = Screen.height - 150;
		dialogRect = new Rect (x, y, 200, 150);
		this.yesAction = yesAction;
		this.noAction = noAction;
		this.dialogMsg = msg;
		this.isOkDialog = false;
	}

	/**
	 * Shows a dialog with message msg confirm button.
	 * confirmAction will be executed when ok is pressed and not set to null.
	 * mx, my is current mouse position and is used for determining location
	 */
	public void StartOkDialog (string msg, action confirmAction)
	{
		StartOkDialog (msg, confirmAction, 200, 150);
	}

	/**
	 * Shows a dialog with message msg confirm button.
	 * confirmAction will be executed when ok is pressed and not set to null.
	 * mx, my is current mouse position and is used for determining location
	 */
	public void StartOkDialog (string msg, action confirmAction, int width, int height)
	{
		if (!this.isOkDialog) {
			Vector3 mousePos = Input.mousePosition;
			int mx = (int)(mousePos.x);
			int my = (int)(Screen.height - mousePos.y);
			int x = mx - 100;
			if (x < 0)
				x = 0;
			int y = my - 100;
			if (y < 64)
				y = 64;
			if (y + 150 > Screen.height)
				y = Screen.height - 150;
			dialogRect = new Rect (x, y, 0, 0);
		}
		dialogRect.width = width;
		dialogRect.height = height;

		this.yesAction = null;
		this.noAction = confirmAction;
		this.dialogMsg = msg;
		this.isOkDialog = true;
	}
	
	public void StartIconSelection (int selected, itemSelected result)
	{
		Vector3 mousePos = Input.mousePosition;
		int mx = (int)(mousePos.x);
		int my = (int)(Screen.height - mousePos.y);
		int height = 38 * ((scene.assets.icons.Length + 7) / 8) + 32;
		int x = mx - 100;
		if (x < 0)
			x = 0;
		int y = my - height / 2;
		if (y < 0)
			y = 0;
		if (y + height > Screen.height)
			y = Screen.height - height;
		listBoxRect = new Rect (x, y, 42 * 8 + 16, height);
		// convert items to string[]
		listBoxItems = null;
		listBoxResultFunc = result;
		listBoxScroll = Vector2.zero;
		iconBoxCurrentSelected = selected;
	}
	
	/**
	 * Shows a selection list, using mx, my to determine screen position
	 * list exists of items, selected is initially selected item
	 * on selecting an item fn result is called with index of newly selected item.
	 */
	public void StartSelection (object[] items, int selected, itemSelected result)
	{
		listBoxResultFunc = result;
		DoStartSelection (items, selected);
	}

	/**
	 * Shows a selection list, using mx, my to determine screen position
	 * list exists of items, selected is initially selected item
	 * on selecting an item fn result is called with index of newly selected item.
	 */
	public void StartSelection (object[] items, int selected, itemSelectedResult result)
	{
		listBoxStringResultFunc = result;
		DoStartSelection (items, selected);
	}

	private void DoStartSelection (object[] items, int selected)
	{
		Vector3 mousePos = Input.mousePosition;
		int mx = (int)(mousePos.x);
		int my = (int)(Screen.height - mousePos.y);
		int height = items.Length * 19 + 36;
		if (height > 400)
			height = 400;
		int x = mx - 100;
		if (x < 0)
			x = 0;
		int y = my - height / 2;
		if (y < 0)
			y = 0;
		if (y + height > Screen.height)
			y = Screen.height - height;
		listBoxRect = new Rect (x, y, 250, height);
		// convert items to string[]
		listBoxItems = new string[items.Length];
		for (int i = 0; i < items.Length; i++) {
			listBoxItems [i] = (items [i]).ToString ();
		}
		iconBoxCurrentSelected = -1;
		listBoxScroll = Vector2.zero;
		listBoxCurrentSelected = selected;
		if (19 * selected > (height - 20)) {
			listBoxScroll.y = 19 * selected - height / 2;
		}
	}
	
	public void StartShowImage (Texture2D tex)
	{
		imageToShow = tex;
	}
	
	public void StopShowImage ()
	{
		imageToShow = null;
	}
	
	// drop down list stuff:
	itemSelected listBoxResultFunc;
	itemSelectedResult listBoxStringResultFunc;
	string[] listBoxItems;
	Rect listBoxRect;
	Vector2 listBoxScroll;
	int listBoxCurrentSelected;
	int iconBoxCurrentSelected = -1;
	string dialogMsg;
	action yesAction;
	action noAction;
	Rect dialogRect;
	bool isOkDialog;
	
	void HandleDialog (int id)
	{
		GUILayout.BeginVertical (listItemSelected);
		GUILayout.BeginHorizontal ();
		GUILayout.Label (dialogMsg);
		GUILayout.EndHorizontal ();
		GUILayout.FlexibleSpace ();
		GUILayout.BeginHorizontal ();
		if (!isOkDialog) {
			if (GUILayout.Button ("Yes", GUILayout.Width (60))) {
				dialogMsg = null;
				if (yesAction != null) {
					yesAction (true);
				}
			}
		}
		GUILayout.FlexibleSpace ();
		if (GUILayout.Button (isOkDialog ? "OK" : "No", GUILayout.Width (60))) {
			dialogMsg = null;
			isOkDialog = false;
			if (noAction != null) {
				noAction (false);
			}
		}
		GUILayout.EndHorizontal ();
		GUILayout.EndVertical ();
		GUI.DragWindow ();
	}
	
	void HandleListBox (int id)
	{
		listBoxScroll = GUILayout.BeginScrollView (listBoxScroll, skin.box);
		for (int i = 0; i < listBoxItems.Length; i++) {
			GUILayout.BeginHorizontal ();
			if (GUILayout.Button (listBoxItems [i], (i == listBoxCurrentSelected) ? listItemSelected : listItem)) {
				if (listBoxResultFunc != null) listBoxResultFunc (i);
				if (listBoxStringResultFunc != null) listBoxStringResultFunc(i, listBoxItems[i]);
				listBoxItems = null;
				listBoxResultFunc = null;
				listBoxStringResultFunc = null;
				return;
			}
			GUILayout.EndHorizontal ();
		}
		GUILayout.EndScrollView ();
		GUI.DragWindow ();
	}

	void HandleIconBox (int id)
	{
//		listBoxScroll = GUILayout.BeginScrollView (listBoxScroll, skin.box);
		GUILayout.BeginHorizontal ();
		for (int i = 0; i < scene.assets.icons.Length; i++) {
			if ((i > 0) && (i % 8 == 0)) {
				GUILayout.FlexibleSpace ();
				GUILayout.EndHorizontal ();
				GUILayout.BeginHorizontal ();
			}
			if (GUILayout.Button (scene.assets.icons [i], (i == iconBoxCurrentSelected) ? listItemSelected : listItem)) {
				iconBoxCurrentSelected = -1;
				listBoxResultFunc (i);
			}
		}
		GUILayout.FlexibleSpace ();
		GUILayout.EndHorizontal ();
//		GUILayout.EndScrollView ();
		GUI.DragWindow ();
	}
	
	void HandleInfoWin (int id)
	{
		if (GUI.Button (new Rect (infoWinR.width - 37, 3, 24, 12), "X")) {
			infoWinIsOpen = false;
		}
		
		Vector3 mousePos = Input.mousePosition;
		infoWin.Render ((int)mousePos.x, (int)mousePos.y);
		GUI.DragWindow ();
	}
	
	void Awake ()
	{
		GameSettings.Setup ();		
		self = this;
	}
	
	void OnDestroy ()
	{
		self = null;
	}
	
	public void SceneIsLoaded (Scene scene)
	{
		this.scene = scene;
		if ((activeToolIndex >= 0) && (tools [activeToolIndex].panel != null)) {
			tools [activeToolIndex].panel.Deactivate ();
		}
		foreach (Tool t in tools) {
			if (t.panel != null) {
				t.panel.Setup (this, scene);
			}
		}
		if ((activeToolIndex < 0) || !tools[activeToolIndex].panel.IsAvailable ()) {
			activeToolIndex = 0;
			foreach (Tool t in tools) {
				if (t.panel.IsAvailable ()) {
					break;
				}
				activeToolIndex++;
			}
		}
		if (tools [activeToolIndex].panel != null) {
			tools [activeToolIndex].panel.Activate ();
		}
	}
	
	public void HandleSuccession () {
		if (tools [activeToolIndex].panel != null) {
			tools [activeToolIndex].panel.Deactivate ();
			tools [activeToolIndex].panel.Activate ();
		}
	}
	
	void Start ()
	{
		foreach (Tool tool in tools) {
			string name = tool.name;
			if (name != "") {
				Type t = Type.GetType ("Ecosim.SceneEditor." + name);
				if (t != null) {
					Panel panel = (Panel)t.GetConstructor (new Type[] {}).Invoke (new object[] {});
					tool.panel = panel;
				} else {
					Ecosim.Log.LogError ("Can't find class '" + name + "'");
				}
			}
		}
		
		SceneIsLoaded (scene);
		CameraControl.SetupCamera (scene);
		
		winAreaR = new Rect (0f, 0f, 400f, Screen.height);
		winAreaSideR = new Rect (400f, (Screen.height * 0.5f) - 5f, 400f, (Screen.height * 0.5f) + 5f);
		icon = skin.FindStyle ("Icon");
		selectedIcon = skin.FindStyle ("IconSelected");
		icon12x12 = skin.FindStyle ("Icon12x12");
		tileIcon = skin.FindStyle ("IconTile");
		listItem = skin.FindStyle ("ListItem");
		listItemSelected = skin.FindStyle ("ListItemSelected");
		infoWinR = new Rect (Screen.width - 200, 0, 200, 340);
		infoWin = new TerrainInfoPanel (this);
		
		infoWinIsOpen = !isGameMode;
		isOpen = !isGameMode;
	}
	
	void OnClosedGUI (Vector3 mousePos)
	{
		Vector3 screenMousePos = new Vector3 (mousePos.x, Screen.height - mousePos.y, 0f);
		if ((screenMousePos.x < 32) && (screenMousePos.y < 32))
			CameraControl.MouseOverGUI = true;
		if (GUILayout.Button (foldedClose, icon)) {
			isOpen = true;
			listBoxItems = null;
			listBoxResultFunc = null;
			listBoxStringResultFunc = null;
		}
	}
	
	void OnGUI ()
	{
//		CameraControl.MouseOverGUI = false;
		Vector3 mousePos = Input.mousePosition;
		Vector3 screenMousePos = new Vector3 (mousePos.x, Screen.height - mousePos.y, 0f);
		GUI.skin = skin;

		if (infoWinIsOpen) {
			infoWinR = GUI.Window (0x10010, infoWinR, HandleInfoWin, "Info");
		}
		if (!isOpen) {
			if (!isGameMode) {
				OnClosedGUI (mousePos);
			}
			return;
		}
		Tool tool = tools [activeToolIndex];
		if (winAreaR.Contains (screenMousePos))
			CameraControl.MouseOverGUI = true;

		int oldActiveToolIndex = activeToolIndex;

//#if !UNITY_EDITOR
		try {
//#endif
			GUILayout.BeginArea (winAreaR, skin.FindStyle ("BG"));
			GUILayout.BeginVertical ();
			GUILayout.BeginHorizontal ();
			if (!isGameMode) {
				if (GUILayout.Button (foldedOpen, icon)) {
					isOpen = false;
				}
				
				GUILayout.Space (4);
			}
			int i = 0;
			foreach (Tool t in tools) {
				if ((t.panel != null) && (t.panel.IsAvailable ())) {
					if (GUILayout.Button (t.icon, (i == activeToolIndex) ? selectedIcon : icon)) {
						activeToolIndex = i;
					}
				}
				i++;
			}
			GUILayout.EndHorizontal ();
			if (tool.panel != null) {
				GUILayout.BeginVertical (skin.box);
				bool extra = tool.panel.Render ((int)mousePos.x, (int)mousePos.y);
				GUILayout.EndVertical ();
				if (extra) {
					GUILayout.BeginVertical (skin.box, GUILayout.Height (Screen.height * 0.5f));
					tool.panel.RenderExtra ((int)mousePos.x, (int)mousePos.y);
					GUILayout.EndVertical ();
				}
			}
			GUILayout.EndVertical ();
			GUILayout.EndArea ();
		
			if ((tool.panel != null) && (tool.panel.NeedSidePanel ())) {
				if (winAreaSideR.Contains (screenMousePos))
					CameraControl.MouseOverGUI = true;
				GUILayout.BeginArea (winAreaSideR, skin.FindStyle ("BG"));
				GUILayout.BeginVertical (skin.box, GUILayout.Height (Screen.height * 0.5f));
				tool.panel.RenderSide ((int)mousePos.x, (int)mousePos.y);
				GUILayout.EndVertical ();
				GUILayout.EndArea ();
			}
//#if !UNITY_EDITOR
		} catch (System.Exception e) {
			if (!e.StackTrace.Contains ("UnityEngine.GUILayout")) {
				StartOkDialog (e.Message, null);
			}
			Debug.LogWarning ("TRACE :'" + e.StackTrace + "'");
			Debug.LogException (e);
		}
//#endif
		
		if (iconBoxCurrentSelected >= 0) {
			listBoxRect = GUI.Window (0x10001, listBoxRect, HandleIconBox, GUIContent.none);
			// HandleListBox();
		}
		if (listBoxItems != null) {
			listBoxRect = GUI.Window (0x10001, listBoxRect, HandleListBox, GUIContent.none);
			// HandleListBox();
		}
		if (dialogMsg != null) {
			dialogRect = GUI.Window (0x10002, dialogRect, HandleDialog, GUIContent.none);
			// HandleListBox();
		}
		if (oldActiveToolIndex != activeToolIndex) {
			if (tools [oldActiveToolIndex].panel != null) {
				tools [oldActiveToolIndex].panel.Deactivate ();
			}
			if (tools [activeToolIndex].panel != null) {
				tools [activeToolIndex].panel.Activate ();
			}
		}
		
		if (imageToShow) {
			int width = imageToShow.width;
			int height = imageToShow.height;
			Rect rect = new Rect ((Screen.width - 400 - width) / 2 + 400, (Screen.height - height) / 2, width, height); 
			GUI.DrawTexture (rect, imageToShow);
		}

		// Tool tip (always on top)
		if (GUI.tooltip.Length > 0) 
		{
			int offset = 15;
			GUILayout.BeginArea (new Rect (Input.mousePosition.x + offset, Screen.height - Input.mousePosition.y + offset, 200, Screen.height));
			{
				GUILayout.BeginVertical (skin.box);
				{
					GUILayout.Label ("<size=11>" + GUI.tooltip + "</size>");
				}
				GUILayout.EndVertical ();
			}
			GUILayout.EndArea ();
		}
	}
	
	void Update ()
	{
		if ((Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl)) && Input.GetKeyDown (KeyCode.E)) {
			isOpen = !isOpen;
			GameControl.InterfaceChanged ();
		}
		if (infoWinIsOpen) {
			infoWin.Update ();
		}
		if (tools [activeToolIndex].panel != null) {
			tools [activeToolIndex].panel.Update ();
		}
	}
}
