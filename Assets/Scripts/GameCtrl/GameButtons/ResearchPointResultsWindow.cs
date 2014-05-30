using UnityEngine;
using System.Globalization;
using System.Collections;
using Ecosim.SceneData.Action;
using Ecosim.SceneData;

namespace Ecosim.GameCtrl.GameButtons
{
	public class ResearchPointResultsWindow : GameWindow
	{
		const int winWidth = 240;

		public ResearchPoint researchPoint;
		private string message;
		private Vector2 position;
		private int newLinesCount;

		private float size = 14;
		private float windowHeight = 400;

		public ResearchPointResultsWindow () : base (-1, -1, winWidth, null)
		{
			this.canCloseManually = false;
		}

		public void UpdateMessage (string message, Vector2 position)
		{
			this.message = message;
			this.position = position;
			this.newLinesCount = message.Split('\n').Length - 1;

			size = 14;
			windowHeight = Mathf.Infinity;
			while (windowHeight > Screen.height && size > 1) {
				windowHeight = 10 + (size * 1.06f) * (float)((float)newLinesCount + 1.5f);
				size--;
			}
		}
		
		public override void Render ()
		{
			float w = winWidth;
			float x = (position.x < Screen.width - winWidth) ? (position.x + 16) : (position.x - w - 16);
			float y = Mathf.Clamp(Screen.height - position.y - windowHeight / 2, 10, Screen.height - windowHeight - 10);

			SimpleGUI.Label (new Rect (x, y, w, windowHeight), string.Format("<size={0}>{1}</size>", size, message), entry);

			base.Render ();
		}
		
		protected override void OnClose ()
		{
			base.OnClose ();
		}
		
	}
}
