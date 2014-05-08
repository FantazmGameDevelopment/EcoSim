using UnityEngine;
using System.Collections;
using System;
using Ecosim;
using Ecosim.SceneData;

namespace Ecosim.GameCtrl.GameButtons
{
	public class Menu : GameButtonHandler
	{
		public override void OnClick ()
		{
			GameMenu.ActivateMenu ();
			GameControl.DeactivateGameControl ();
			base.OnClick ();
		}
	}
}