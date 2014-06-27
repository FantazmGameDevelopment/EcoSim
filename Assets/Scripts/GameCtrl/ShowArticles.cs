using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneData;

/**
 * Monobehaviour class that manages a queue of articles, letters, ... to be shown
 * to the player. While showing an article the user interface will be blocked until
 * the queue is empty.
 */
public class ShowArticles : MonoBehaviour {
	
//	private static ShowArticles self;
	private Texture2D articleTex;
	private bool isShowing = false;
	private float yPos = 0;
	private float targetYPos = 0;
	private static volatile bool hasUnreadMessages = false;
	public static bool HasUnreadMessages {
		get { return hasUnreadMessages; }
	}
	
	/**
	 * Notify ShowArticles there are messages that need to be displayed
	 */
	public static void NotifyUnreadMessages () {
		hasUnreadMessages = true;
	}
	
	void Awake () {
//		self = this;
		articleTex = new Texture2D (2, 2, TextureFormat.ARGB32, false, true);
		hasUnreadMessages = false;
	}
	
	void OnDestroy () {
//		self = null;
	}
	
	void OnGUI () {

		GameControl ctrl = GameControl.self;
		Scene scene = ctrl.scene;
		if ((scene != null) && hasUnreadMessages && (!isShowing)) {
			if (ctrl.hideToolBar || ctrl.hideSuccessionButton) {
				// toolbar/succession button is hidden, apparently we're busy with something, wait till 
				// we're ready for showing article...
				return;
			}
			RenderFontToTexture.self.RenderNewsArticle (scene.progression.CurrentMessage ().text, scene, articleTex, false);
			isShowing = true;
			yPos = (float) Screen.height;
			targetYPos = Screen.height - articleTex.height;// Mathf.Min (Screen.height - articleTex.height, 200);
			ctrl.hideToolBar = true;
			ctrl.hideSuccessionButton = true;
		}
		else if (isShowing) {
			yPos = Mathf.Max (targetYPos, yPos - 1200f * Time.deltaTime);
			int sWidth = Screen.width;
			int xOffset = 0;
			if (EditorCtrl.self.isOpen) {
				sWidth -= 400;
				xOffset += 400;
			}
			GUI.depth = 100;
			CameraControl.MouseOverGUI = true;
			SimpleGUI.Label (new Rect ((sWidth - articleTex.width ) / 2, yPos, articleTex.width, articleTex.height), articleTex, GUIStyle.none);
			if (Event.current.type == EventType.MouseDown) {
				Event.current.Use ();
				ctrl.hideToolBar = false;
				ctrl.hideSuccessionButton = false;
				isShowing = false;
				hasUnreadMessages = ctrl.scene.progression.ToNextMessage ();
			}
		}
	}
}
