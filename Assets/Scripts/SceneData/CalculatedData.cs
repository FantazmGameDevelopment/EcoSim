using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneData
{
	/**
	 * Data values are calculated by a function in the ConversionAction
	 */
	public class CalculatedData : Data
	{
	
		public CalculatedData (Scene scene, string name) :
		base(scene)
		{
			this.name = name;
		}
	
		private readonly string name;
		private MethodInfo getValueMI;
		
		private ConversionAction.GetFn getFn;
		

		public override void Save (BinaryWriter writer, Progression progression)
		{
			writer.Write ("CalculatedData");
			writer.Write (width);
			writer.Write (height);
			writer.Write (name);
		}
		
		static CalculatedData LoadInternal (BinaryReader reader, Progression progression)
		{
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			string name = reader.ReadString ();
			
			EnforceValidSize(progression.scene, width, height);
			
			CalculatedData result = new CalculatedData (progression.scene, name);
			return result;
		}

		/**
		 * Sets all values in bitmap to zero
		 */
		public override void Clear ()
		{
			throw new System.NotSupportedException("operation not supported on calculated data");
		}
		
		/**
		 * set data value val at x, y
		 */
		public override void Set (int x, int y, int val)
		{
			throw new System.NotSupportedException ("Can't set values on calculated data"); 
		}

		public override int Get (int x, int y)
		{
			if (getFn == null) {
				if (scene.progression.conversionHandler != null) {
					getFn = scene.progression.conversionHandler.GetDataDelegate (name);
					if (getFn == null) {
						throw new System.NotSupportedException("no valid function '" + name + "' defined in conversion action.");
					}
				}
				else {
					return 0;
				}
			}
			try {
				return getFn(x, y);
			}
			catch (TargetException te) {
				Log.LogException (te);
				getFn = null;
				return 0;
			}
		}
		public override int GetMin() { return 0; }
		public override int GetMax() { return 255; }
		
		public override Data CloneAndResize(Progression targetProgression, int offsetX, int offsetY) {
			
			return new CalculatedData (targetProgression.scene, name);
		}
	}
}