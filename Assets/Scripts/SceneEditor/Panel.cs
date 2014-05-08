using UnityEngine;
using System.Collections;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor
{
	public interface Panel
	{
		/**
		 * Called when scene is set or changed in Editor
		 */
		void Setup(EditorCtrl ctrl, Scene scene);


		/**
		 * Called every frame when this panel is active
		 * It is guaranteed that setup has been called with
		 * a valid scene before the first call of Render.
		 */
		bool Render(int mx, int my);
		
		/* Called for extra edit sub-panel, will be called after Render */
		void RenderExtra(int mx, int my);

		/* Called for extra side edit sub-panel, will be called after RenderExtra */
		void RenderSide(int mx, int my);
		
		/* Returns true if a side panel is needed. Won't be called before RenderExtra has been called */
		bool NeedSidePanel();
		
		/* True if panel can be used */
		bool IsAvailable();
		
		/**
		 * Panel is activated...
		 */
		void Activate();
		
		/**
		 * Panel is deactivated
		 */
		void Deactivate();
		
		/**
		 * For Unity.Update like stuff
		 */
		void Update();
	
	}
}