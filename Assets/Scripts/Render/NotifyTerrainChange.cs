using UnityEngine;
using System.Collections;

using Ecosim.SceneData;

/**
 * Interface to have rendered objects informed about changes in visibility
 * of terrain cells and also when a new scene is loaded.
 * 
 * Classes can register (and deregister) to the TerrainMgr to get notifications
 */
namespace Ecosim.Render
{
	public interface NotifyTerrainChange
	{
		/**
		 * Notifies a change in scene, new scene can be null if no scene is active anymore
		 */
		void SceneChanged (Scene scene);
		
		/**
		 * Notifies succession has been done
		 */
		void SuccessionCompleted();
		
		/**
		 * A terrain cell has become visible, when registering class to TerrainMgr for
		 * notifications, this method will be called for all cells currenty visible while
		 * registering.
		 */
		void CellChangedToVisible(int cx, int cy, TerrainCell cell);
		
		/**
		 * A terrain cell has become invisible
		 */
		void CellChangedToInvisible(int cx, int cy);
	}
}