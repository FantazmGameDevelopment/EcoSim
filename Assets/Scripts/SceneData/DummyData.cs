using UnityEngine;
using System.Collections;
using System.IO;

namespace Ecosim.SceneData
{
	/**
	 * Just a dummy implementation of Data. Is used to prevent things from breaking when references to the right
	 * data hasn't been found (for example in vegetation rule is an incorrect name for data used).
	 */
	public class DummyData : Data
	{		
		public DummyData(Scene scene) : base(scene) {
		}

		public override void Clear()
		{
		}
		
		public override int Get (int x, int y)
		{
			return 0;
		}
		
		public override void Set(int x, int y, int val) {
		}
		
		public override void Save(BinaryWriter writer, Progression progression) {
			UnityEngine.Debug.LogError ("trying to write dummy data");
		}
		
		public override int GetMin() { return 0; }
		public override int GetMax() { return 255; }

		public override Data CloneAndResize(Progression targetProgression, int offsetX, int offsetY) {
			return new DummyData(targetProgression.scene);
		}
	}

}