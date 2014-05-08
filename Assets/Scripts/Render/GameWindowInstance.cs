using UnityEngine;
using System.Collections;
using Ecosim.GameCtrl.GameButtons;

public class GameWindowInstance : MonoBehaviour {
	
	public GameWindow window;
	
	void OnGUI () {
		GUI.depth = window.depth + 1;
		window.Render ();
	}
}
