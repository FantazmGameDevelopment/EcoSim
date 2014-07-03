using UnityEngine;
using System.Collections.Generic;

namespace Ecosim.GameCtrl.GameButtons
{
	/**
	 * Simple window manager and window base class for implementing in game windows
	 */
	public class GameWindow
	{
		/**
		 * Sets up windows, closes all currently open windows (OnClose will _not_ be called!)
		 */
		public static void Reset ()
		{
			if (windows != null) {
				foreach (GameWindow window in windows) {
					if (window.instance) {
						GameObject.DestroyImmediate (window.instance.gameObject);
					}
				}
			}
			windows = new List<GameWindow> ();
			if (black != null) return; // already did stuff below
			black = GameControl.self.skin.FindStyle ("BGBlack");
			white = GameControl.self.skin.FindStyle ("BGWhite");
			title = GameControl.self.skin.FindStyle ("ArialB16-75");
			titleNoText = GameControl.self.skin.FindStyle ("75");
			header = GameControl.self.skin.FindStyle ("ArialB16-50");
			entry = GameControl.self.skin.FindStyle ("Arial16-75");
			entrySelected = GameControl.self.skin.FindStyle ("Arial16-W");
			formatted = GameControl.self.skin.FindStyle ("Arial16-50-formatted");
			closeIcon = Resources.Load ("Icons/cross_w") as Texture2D;
			closeIconH = Resources.Load ("Icons/cross_zw") as Texture2D;
		}
				
		private static void UpdateDepth () {
			int i = 0;
			foreach (GameWindow window in windows) {
				window.depth = i++;
			}
		}
		
		private static List<GameWindow> windows;
		protected static Texture2D closeIcon;
		protected static Texture2D closeIconH;
		
		protected static GUIStyle black;
		protected static GUIStyle white;
		protected static GUIStyle title;
		protected static GUIStyle titleNoText;
		protected static GUIStyle header;
		protected static GUIStyle entry;
		protected static GUIStyle entrySelected;
		protected static GUIStyle formatted;
		
		protected readonly Texture2D icon;
		protected int xOffset;
		protected int yOffset;
		protected readonly int width;
		protected int height;
		private bool isDragging;
		private Vector2 mouseDrag;
		public int depth;
		private GameWindowInstance instance;
		protected bool canCloseManually = true;
		
		public GameWindow (int x, int y, int width, Texture2D icon) {
			this.icon = icon;
			this.width = width;
			height = 33;
			if ((x < 0) || (y < 0)) {
				int sWidth = Screen.width;
				x = 0;
				if (EditorCtrl.self.isOpen) {
					x = 400;
					sWidth -= 400;
				}
				x += (sWidth - width) / 2;
				y = 128;	
			}
			xOffset = x;
			yOffset = y;
			
			windows.Insert (0, this);
			UpdateDepth ();
			GameObject go = new GameObject ("window");
			instance = go.AddComponent <GameWindowInstance>();
			instance.window = this;
		}
		
		/**
		 * Rendering the window, should be overridden but base must be called to implement drag and draw close
		 * button and icon.
		 * As this base implementation of Render will prevent mouse clicks when over the window area
		 * derived classes should call base.Render () at the end of their Render implementations
		 */
		virtual public void Render ()
		{
			Vector3 mousePos = Input.mousePosition;
			Vector2 guiMousePos = new Vector2 (mousePos.x, Screen.height - mousePos.y);
			if (canCloseManually && SimpleGUI.Button (new Rect (xOffset, yOffset, 32, 32), closeIcon, closeIconH, black, white)) {
				Close ();
			}
			if (icon != null)
				SimpleGUI.Label (new Rect (xOffset + 33, yOffset, 32, 32), icon, titleNoText);
			// handle window dragging...
			if (Event.current.type == EventType.MouseUp) {
				isDragging = false;
			} else if (isDragging) {
				xOffset += (int)(guiMousePos.x - mouseDrag.x);
				yOffset += (int)(guiMousePos.y - mouseDrag.y);
				mouseDrag = guiMousePos;
			} else if (Event.current.type == EventType.MouseDown) {
				if (new Rect (xOffset + 32, yOffset, width - 32, 32).Contains (guiMousePos)) {
					// move window to front...
					windows.Remove (this);
					windows.Insert (0, this);
					UpdateDepth ();
					mouseDrag = guiMousePos;
					isDragging = true;
					Event.current.Use (); // prevent mouse down event from cascading down
				}
			}
			if ((Event.current.type == EventType.MouseDown) && (new Rect (xOffset, yOffset, width, height).Contains (guiMousePos))) {
				// mouse click on window area, prevent event from cascading down
				Event.current.Use ();
			}
		}
				
		/**
		 * close the window
		 */
		public void Close ()
		{
			OnClose ();
			windows.Remove (this);
			UpdateDepth ();
			GameObject.DestroyImmediate (instance.gameObject);
		}
		
		/**
		 * Called when window is closed (except on Reset)
		 */
		protected virtual void OnClose ()
		{
		}
		
	}
}
