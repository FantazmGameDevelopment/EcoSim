using UnityEngine;
using System.Collections;

namespace Ecosim.SceneEditor.Helpers
{
	public interface PanelHelper
	{
		bool Render (int mx, int my);
		
		void Disable();
		
		void Update();
	}
}