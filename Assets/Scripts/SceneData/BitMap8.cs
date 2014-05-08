using System.Collections.Generic;
using System.IO;

namespace Ecosim.SceneData
{
	
/**
 * Stores tile parameter data for one data type (e.g. pH or salinity)
 */
	public class BitMap8 : Data
	{
	
		public BitMap8 (Scene scene) 
		: base(scene)
		{
		
			data = new byte[width * height];
		}
	
		BitMap8 (Scene scene, byte[] data) 
		: base(scene)
		{
			this.data = data;
		}
	
		// we made it public so code can use it directly for performance reasons
		public byte[] data;

		public override void Save(BinaryWriter writer, Progression progression) {
			writer.Write("BitMap8");
			writer.Write(width);
			writer.Write(height);
			writer.Write(data);
		}

		static BitMap8 LoadInternal(BinaryReader reader, Progression progression) {
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			EnforceValidSize(progression.scene, width, height);
			int len = (width * height);
			byte[] data = reader.ReadBytes(len);
			if (data.Length != len) {
				throw new EcoException("Unexpected EOD");
			}
			return new BitMap8(progression.scene, data);
		}

		/**
		 * Sets all values in bitmap to zero
		 */
		public override void Clear ()
		{
			for (int i = data.Length - 1; i >= 0; i--) {
				data [i] = 0;
			}
		}
		
		/**
		 * set data value val at x, y
		 */
		public override void Set (int x, int y, int val)
		{
			data [y * width + x] = (byte)val;
			hasChanged = true;
		}
	
		/**
		 * get data value at x, y
		 */
		public override int Get (int x, int y)
		{
			return data [y * width + x];
		}
	
		/**
		 * copy data from src to data
		 */
		public void CopyFrom (BitMap8 src)
		{
			System.Array.Copy (src.data, data, data.Length);
			hasChanged = true;
		}
	
		public void ClearData ()
		{
			System.Array.Clear (data, 0, data.Length);
			hasChanged = true;
		}
	
		/**
		 * increase value of all data by 1, limit it to 100;
		 */
		public void IncreaseBy1 ()
		{
			for (int i = width * height - 1; i >= 0; i--) {
				byte val = data [i];
				if (val < 100) {
					val++;
					data [i] = val;
				}
			}
			hasChanged = true;
		}

		/**
		 * increase value of all data by 1, limit it to 0;
		 */
		public void DecreaseBy1 ()
		{
			for (int i = width * height - 1; i >= 0; i--) {
				byte val = data [i];
				if (val > 0) {
					val--;
					data [i] = val;
				}
			}
			hasChanged = true;
		}
		
		/**
		 * Calculates value propagation. new value of cell will be max (orig. value of cell, orig. value of neighbour - 1).
		 * For performance reasons the border of 1 cell width isn't calculated.
		 * returns true if data has propagated (is changed) during call.
		 */
		public bool Propagate ()
		{
			bool hasPropagated = false;
			byte[] orig = (byte[]) data.Clone ();
			for (int y = 1; y < height - 1; y++) {
				int p = y * width + 1;
				for (int x = 1; x < width - 1; x++) {
					int v = (int) orig[p] - 1;
					if (v > 0) {
						if (v > data[p - 1]) {
							data[p - 1] = (byte) v;
							hasPropagated = true;
						}
						if (v > data[p + 1]) {
							data[p + 1] = (byte) v;
							hasPropagated = true;
						}
						if (v > data[p - width]) {
							data[p - width] = (byte) v;
							hasPropagated = true;
						}
						if (v > data[p + width]) {
							data[p + width] = (byte) v;
							hasPropagated = true;
						}
					}
					p++;
				}
			}
			hasChanged |= hasPropagated;
			return hasPropagated;
		}

		/**
		 * Calculates value propagation. new value of cell will be max (orig. value of cell, orig. value of neighbour - weight).
		 * For performance reasons the border of 1 cell width isn't calculated.
		 * returns true if data has propagated (is changed) during call.
		 */
		public bool WeightedPropagate (BitMap8 weight)
		{
			bool hasPropagated = false;
			byte[] orig = (byte[]) data.Clone ();
			byte[] weightData = weight.data;
			for (int y = 1; y < height - 1; y++) {
				int p = y * width + 1;
				for (int x = 1; x < width - 1; x++) {
					int v = (int) orig[p] - (int) weightData[p];
					if (v > 0) {
						if (v > data[p - 1]) {
							data[p - 1] = (byte) v;
							hasPropagated = true;
						}
						if (v > data[p + 1]) {
							data[p + 1] = (byte) v;
							hasPropagated = true;
						}
						if (v > data[p - width]) {
							data[p - width] = (byte) v;
							hasPropagated = true;
						}
						if (v > data[p + width]) {
							data[p + width] = (byte) v;
							hasPropagated = true;
						}
					}
					p++;
				}
			}
			hasChanged |= hasPropagated;
			return hasPropagated;
		}
		
		public override int GetMin() { return 0; }
		public override int GetMax() { return 255; }

		/**
		 * Copies this instance data to 'toData'
		 * This overridden version has a fast copy if toData is same data type, otherwise
		 * it falls back to base implementation
		 */
		public override void CopyTo(Data toData) {
			if (toData is BitMap8) {
				// data is of same type
				if ((toData.width != width) || (toData.height != height)) {
					throw new EcoException("size mismatch, toData needs to be same size as data");
				}
				// we can quickly copy array data....
				data.CopyTo(((BitMap8) toData).data, 0);
			}
			else {
				// fall back on normal simple CopyTo
				base.CopyTo(toData);
			}
		}
	}
}