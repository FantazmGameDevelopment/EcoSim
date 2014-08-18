using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;
using System;
using System.IO;
using Ecosim.SceneEditor;

public class CheatsWindow : GameWindow
{
	public static CheatsWindow instance;

	public CheatsWindow () : base (-1, -1, 512, null)
	{
		this.canCloseManually = true;
	}

	public override void Render ()
	{
		base.Render ();

		GUILayout.BeginArea (new Rect (this.xOffset, this.yOffset, this.width, Screen.height));

		GUILayout.BeginHorizontal ();
		{
			GUILayout.Space (33); // Close button
			GUILayout.Label ("Cheats...", header, GUILayout.Width (this.width));
		}
		GUILayout.EndHorizontal ();
		GUILayout.Label ("Cheats...", title, GUILayout.Width (this.width));
		GUILayout.Label ("Cheats...", entry, GUILayout.Width (this.width));
		if (GUILayout.Button ("Enter", entry)) {
			Debug.LogError ("CHEAT ENTERED!");
		}

		GUILayout.EndArea ();
	}

	public void SetFocus ()
	{
		this.SetWindowOnTop ();
	}

	protected override void OnClose ()
	{
		base.OnClose ();

		instance = null;
	}
}
