using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;
using Ecosim.GameCtrl.GameButtons;
using System;
using System.IO;
using Ecosim.SceneEditor;

public class CheatsWindow : GameWindow
{
	public static CheatsWindow instance;

	private GUIStyle textArea;
	private string cheatInput;
	private string response;

	public CheatsWindow () : base (-1, -1, 512, null)
	{
		this.canCloseManually = true;

		this.textArea = GameControl.self.skin.FindStyle ("TextArea B16-75");
		this.cheatInput = "Enter password here...";
	}

	public override void Render ()
	{
		base.Render ();

		GUILayout.BeginArea (new Rect (this.xOffset, this.yOffset, this.width, Screen.height));

		GUILayout.BeginHorizontal ();
		{
			GUILayout.Space (33); // Close button
			GUILayout.Label ("Password", header, GUILayout.Height (32), GUILayout.Width (this.width));
		}
		GUILayout.EndHorizontal ();
		GUILayout.Space (1);

		string newCheatInput = GUILayout.TextField (cheatInput, textArea, GUILayout.Height (32), GUILayout.Width (this.width));
		if (newCheatInput != cheatInput) response = null;
		cheatInput = newCheatInput;

		GUILayout.Space (1);
		GUILayout.BeginHorizontal ();
		{
			GUILayout.Label ("", header, GUILayout.Height (32), GUILayout.Width (this.width - 100));
			if (GUILayout.Button ("Enter", entry, GUILayout.Height (32), GUILayout.Width (100))) 
			{
				response = null;

				if (string.IsNullOrEmpty (cheatInput)) {
					response = "No password entered";
				}
				else {
					// Check all cheatsactions if we have a valid cheat
					bool correctCheat = false;
					foreach (BasicAction action in GameControl.self.scene.actions.EnumerateActions ()) {
						if (action is CheatsAction && action.isActive) {
							CheatsAction ca = (CheatsAction)action;
							bool correct = ca.HandleCheat (cheatInput);
							if (!correctCheat && correct) correctCheat = correct;
						}
					}

					if (correctCheat) {
						response = "Correct password entered";
					}
					else {
						response = "Incorrect password entered";
					}
				}
			}
		}
		GUILayout.EndHorizontal ();

		if (!string.IsNullOrEmpty (response))
		{
			GUILayout.Space (1);
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label (response, entry, GUILayout.Height (32), GUILayout.Width (this.width));
			}
			GUILayout.EndHorizontal ();
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
