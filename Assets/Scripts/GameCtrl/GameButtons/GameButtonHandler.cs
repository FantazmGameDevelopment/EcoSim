using UnityEngine;
using System.Collections;
using System;
using Ecosim;
using Ecosim.SceneData;

namespace Ecosim.GameCtrl.GameButtons
{
	public class GameButtonHandler
	{
		public static GameButtonHandler GetHandlerByName (string name)
		{
			Type t = Type.GetType ("Ecosim.GameCtrl.GameButtons." + name);
			if (t != null) {
				GameButtonHandler hdlr = (GameButtonHandler)t.GetConstructor (new Type[] {}).Invoke (new object[] {});
				return hdlr;
			}
			Log.LogError ("Can't find button handler '" + name + "'");
			return null;
		}
		
		/**
		 * Called when button is clicked
		 */
		public virtual void OnClick ()
		{
		}
		
		/**
		 * Called every frame from moment mouse is over this button until false is returned or mouse is over
		 * other button
		 */
		public virtual bool SelectRender (GameButton button)
		{
			return false;
		}
		
		/**
		 * Called every frame if GameButton.alwaysRender = true for this button
		 */
		public virtual void DefaultRender ()
		{
		}
		
		/**
		 * Called when scene is changed (or more specifically when game control is
		 * activated through ActivateGameControl) and when succession has completed
		 */
		public virtual void UpdateScene (Scene scene, GameButton button) {
		}
		
		public virtual void UpdateState (GameButton button) {
			button.isVisible = true;
			button.alwaysRender = false;
		}		
	}
}
