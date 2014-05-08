using System.Collections.Generic;
using System.IO;

namespace Ecosim.SceneData
{
	public class BitMap2 : Data
	{
	
		public BitMap2 (Scene scene) 
		: base(scene)
		{
		
			data = new byte[width * height / 4];
		}

		BitMap2 (Scene scene, byte[] data)
		: base(scene)
		{
		
			this.data = data;
		}
		
		public byte[] data;

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
		 * set bitmap at x, y to true
		 */
		public override void Set (int x, int y, int val)
		{
			int index = y * width + x;
			int i1 = (index >> 2);
			int i2 = (index & 0x03) << 1;
			val = (val & 0x03) << i2;
			int mask = ~(0x03 << i2);
			data[i1] = (byte) ((data[i1] & mask) | val);
			hasChanged = true;
		}

		public override void Save(BinaryWriter writer, Progression progression) {
			writer.Write("BitMap2");
			writer.Write(width);
			writer.Write(height);
			writer.Write(data);
		}

		static BitMap2 LoadInternal(BinaryReader reader, Progression progression) {
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			EnforceValidSize(progression.scene, width, height);
			int len = (width * height) >> 2;
			byte[] data = reader.ReadBytes(len);
			if (data.Length != len) {
				throw new EcoException("Unexpected EOD");
			}
			return new BitMap2(progression.scene, data);
		}
		
		/**
		 * get data value at x, y
		 */
		public override int Get (int x, int y)
		{
			int index = y * width + x;
			int i1 = (index >> 2);
			int i2 = (index & 0x03) << 1;
			int mask = (0x03 << i2);
			int val = (data[i1] & mask);
			return val >> i2;
		}
	
		public bool IsSet (int x, int y)
		{
			int index = y * width + x;
			int i1 = (index >> 2);
			int i2 = (index & 0x03) << 1;
			int mask = (0x03 << i2);
			int val = (data[i1] & mask);
			return val != 0;
		}
		public override int GetMin() { return 0; }
		public override int GetMax() { return 3; }

		/**
		 * Copies this instance data to 'toData'
		 * This overridden version has a fast copy if toData is same data type, otherwise
		 * it falls back to base implementation
		 */
		public override void CopyTo(Data toData) {
			if (toData is BitMap2) {
				// data is of same type
				if ((toData.width != width) || (toData.height != height)) {
					throw new EcoException("size mismatch, toData needs to be same size as data");
				}
				// we can quickly copy array data....
				data.CopyTo(((BitMap2) toData).data, 0);
			}
			else {
				// fall back on normal simple CopyTo
				base.CopyTo(toData);
			}
		}
	
	}
	
}