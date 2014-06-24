using UnityEngine;
using System.Collections;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor
{
	public class ReportsPanel : Panel
	{
		private Vector2 scrollPos;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;
		
		public Scene scene;
		public EditorCtrl ctrl;
		
		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			if (scene == null)
				return;

			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
		}

		/**
		 * Called every frame when this panel is active
		 * It is guaranteed that setup has been called with
		 * a valid scene before the first call of Render.
		 */
		public bool Render(int mx, int my) 
		{ 

			return false;
		}
		
		/* Called for extra edit sub-panel, will be called after Render */
		public void RenderExtra(int mx, int my) 
		{ 
		}
		
		/* Called for extra side edit sub-panel, will be called after RenderExtra */
		public void RenderSide(int mx, int my) 
		{ 
		}
		
		/* Returns true if a side panel is needed. Won't be called before RenderExtra has been called */
		public bool NeedSidePanel() 
		{ 
			return false;
		}
		
		/* True if panel can be used */
		public bool IsAvailable() 
		{ 
			return (scene != null);
		}
		
		/**
		 * Panel is activated...
		 */
		public void Activate() 
		{ 
		}
		/**
		 * Panel is deactivated
		 */
		public void Deactivate()  
		{ 
		}
		
		/**
		 * For Unity.Update like stuff
		 */
		public void Update()  
		{ 
		}
	}
}
