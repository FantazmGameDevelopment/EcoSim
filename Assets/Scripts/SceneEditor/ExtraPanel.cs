using UnityEngine;
using System.Collections;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor
{
	public interface ExtraPanel
	{
		/**
		 * Called every frame when this extra panel is active
		 * returns false when panel must be closed
		 */
		bool Render(int mx, int my);
		
		/**
		 * Called every frame when this extra panel is active
		 * returns false when panel must be closed
		 */
		bool RenderSide(int mx, int my);

		void Dispose ();
	}
}