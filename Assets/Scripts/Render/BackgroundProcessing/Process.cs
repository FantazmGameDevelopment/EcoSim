using System.Collections.Generic;
using Ecosim.SceneData;

namespace Ecosim.Render.BackgroundProcessing
{
	/**
	 *  Base class for processing render setup in background threads
	 */
	public abstract class Process {
		
		public readonly TerrainCell cell;
		public readonly Scene scene;
		public readonly int offsetX;
		public readonly int offsetY;
		protected readonly int totalWidth;
		protected readonly int totalHeight;
		
		public Process(Scene scene, TerrainCell cell) {
			this.scene = scene;
			this.cell = cell;
			offsetX = cell.cellX * TerrainCell.CELL_SIZE;
			offsetY = cell.cellY * TerrainCell.CELL_SIZE;
			totalWidth = scene.width;
			totalHeight = scene.height;
		}

		
		public abstract void StartWork();
		
		public abstract IEnumerable<bool> TryFinishWork();
		
		protected int MakeValid(int x, int y, Data data, int val) {
			return ((x >= 0) && (y >= 0) && (x < totalWidth) && (y < totalHeight))?(data.Get (x, y)):val;
		}
		
	}
}