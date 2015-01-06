using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;
using System;
using System.IO;
using Ecosim.SceneEditor;

public class ReportBaseWindow : GameWindow
{
	protected System.Action onFinished;
	protected Vector2 scrollPosition;

	protected float width;
	protected float height;
	protected float left;
	protected float top;
	protected GUILayoutOption defaultOption;
	
	protected static GUIStyle black;
	protected static GUIStyle white;
	protected static GUIStyle headerDark;
	protected static GUIStyle textArea;
	//protected static GUIStyle titleNoText;
	protected static GUIStyle headerLight;
	protected static GUIStyle button;
	//protected static GUIStyle formatted;

	public static void Reset ()
	{
		if (black != null) return; // already did stuff below
		black = GameControl.self.skin.FindStyle ("BGBlack");
		white = GameControl.self.skin.FindStyle ("BGWhite");
		headerDark = GameControl.self.skin.FindStyle ("ArialB16-95");
		headerLight = GameControl.self.skin.FindStyle ("Arial16-75");
		textArea = GameControl.self.skin.FindStyle ("TextArea B16-100");
		button = GameControl.self.skin.FindStyle ("Arial16-95"); // -> Make this one darker
		//formatted = GameControl.self.skin.FindStyle ("Arial16-50-formatted");
	}
	
	public ReportBaseWindow (System.Action onFinished) : base(-1, -1, 650, null)
	{
		this.onFinished = onFinished;
		this.canCloseManually = false;
		Reset ();
	}
	
	public virtual void Render ()
	{
		float editorWidth = 0f;
		if (EditorCtrl.self.isOpen) {
			editorWidth = 400;
		}
		width = 650;//Screen.width * 0.65;
		height = Screen.height * 0.5f;
		left = this.xOffset;// ((Screen.width - width) * 0.5f) + editorWidth;
		top = this.yOffset;//(Screen.height - height) * 0.5f;
		defaultOption = GUILayout.MinWidth (0);//GUILayout.MinHeight (28f);//GUILayout.ExpandHeight (true);

		base.Render ();
	}

	public virtual void Dispose ()
	{
		this.onFinished = null;
		this.defaultOption = null;

		this.Close ();
	}
}

