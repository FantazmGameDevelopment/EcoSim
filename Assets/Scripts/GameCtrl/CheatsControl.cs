using UnityEngine;
using Ecosim.SceneData.Action;
using System.Collections.Generic;

public class CheatsControl : MonoBehaviour
{
	void OnGUI ()
	{
		// Some checks...
		if (GameControl.self == null ||
		    GameControl.self.scene == null ||
			GameControl.self.hideToolBar ||
		    GameControl.self.isProcessing)
			return;

		Event e = Event.current;
		if (e.type == EventType.KeyDown)
		{
			// Check cheats keyboard combination
			if (e.alt &&
			    e.shift &&
			    e.keyCode == KeyCode.C)
			{
				// Check if we have a cheats action
				bool enabled = false;
				foreach (BasicAction action in GameControl.self.scene.actions.EnumerateActions ()) {
					if (action is CheatsAction && action.isActive) {
						enabled = true;
						break;
					}
				}

				if (!enabled) 
					return;

				// Create new cheats window or update it
				if (CheatsWindow.instance == null) {
					// New cheats window
					new CheatsWindow ();
				}
				else {
					CheatsWindow.instance.SetFocus ();
				}
			}
		}
	}
}
