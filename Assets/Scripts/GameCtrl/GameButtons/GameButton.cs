using UnityEngine;
using System.Collections;

namespace Ecosim.GameCtrl.GameButtons
{
	[System.Serializable]
	public class GameButton
	{
		public string name;
		public string code;
		public string description;
		public string help;
		public Texture2D icon;
		public Texture2D iconH;
		public bool isVisible = true;
		public bool isGameAction = false;
		
		[System.NonSerializedAttribute]
		public Rect position;
		public bool alwaysRender = false;
		
		public GameButtonHandler hdlr;
	}
}
