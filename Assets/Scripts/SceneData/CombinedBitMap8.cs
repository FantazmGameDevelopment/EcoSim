using System.Collections.Generic;
using System.IO;

namespace Ecosim.SceneData
{
	/**
	 * Can be used to calculate the combined results of several datasets using multipliers.
	 * The datasets are currently limited to be of type ParameterData.
	 */
	public class CombinedBitMap8 : Data
	{
	
		public CombinedBitMap8 (Scene scene) :
		base(scene)
		{
		}
	
		public class Factor
		{
			public float multiplier;
			public string name;
			public byte[] data;
		}
		public float offset;
		public Factor[] factors;

		public override void Save (BinaryWriter writer, Progression progression)
		{
			writer.Write ("CombinedBitMap8");
			writer.Write (width);
			writer.Write (height);
			writer.Write (offset);
			writer.Write (factors.Length);
			foreach (Factor f in factors) {
				writer.Write (f.multiplier);
				writer.Write (f.name);
			}
		}
		
		void LoadFactors (Progression progression)
		{
			foreach (Factor f in factors) {
				BitMap8 data = progression.GetData<BitMap8> (f.name);
				if ((data.width == width) && (data.height == height)) {
					f.data = data.data;
				}
			}
		}
		
		static CombinedBitMap8 LoadInternal (BinaryReader reader, Progression progression)
		{
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			
			EnforceValidSize(progression.scene, width, height);
			
			float offset = reader.ReadSingle ();
			int len = reader.ReadInt32 ();
			if ((len < 0) || (len > 32))
				throw new EcoException ("length out of range");
			Factor[] factors = new Factor[len];
			for (int i = 0; i < len; i++) {
				Factor f = new Factor ();
				f.multiplier = reader.ReadSingle ();
				f.name = reader.ReadString ();
			}
			CombinedBitMap8 result = new CombinedBitMap8 (progression.scene);
			result.offset = offset;
			result.factors = factors;
			result.LoadFactors(progression);
			return result;
		}

		/**
		 * Sets all values in bitmap to zero
		 */
		public override void Clear ()
		{
			throw new System.NotSupportedException("operation not supported on sparse bitmaps");
		}
		
		/**
		 * set data value val at x, y
		 */
		public override void Set (int x, int y, int val)
		{
			throw new System.NotSupportedException ("Can't set combined bitmap8 values"); 
		}

		public override int Get (int x, int y)
		{
			float result = offset;
			int index = y * width + x;
			foreach (Factor f in factors) {
				if (f.data != null) {
					result += f.multiplier * f.data [index];
				}
			}
			return (int)result;
		}
		public override int GetMin() { return 0; }
		public override int GetMax() { return 255; }
		
		public override Data CloneAndResize(Progression targetProgression, int offsetX, int offsetY) {
			CombinedBitMap8 newData = new CombinedBitMap8(targetProgression.scene);
			newData.factors = new Factor[factors.Length];
			for (int i = 0; i < factors.Length; i++) {
				Factor f = new Factor();
				f.multiplier = factors[i].multiplier;
				f.name = factors[i].name;
				newData.factors[i] = f;
			}
			newData.LoadFactors(targetProgression);
			return newData;
		}
	}
}