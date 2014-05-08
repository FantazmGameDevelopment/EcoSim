using System.Collections.Generic;
using System.IO;

namespace Ecosim.SceneData
{
	public class BitMap4 : Data
	{
	
		public BitMap4 (Scene scene) 
		: base(scene)
		{
		
			data = new byte[width * height / 2];
		}

		BitMap4 (Scene scene, byte[] data)
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
			int i1 = (index >> 1);
			if ((index & 0x01) == 0) {
				int v = data [i1] & 0xf0 | (val & 0x0f);
				data [i1] = (byte)v;
			} else {
				int v = data [i1] & 0x0f | ((val & 0x0f) << 4);
				data [i1] = (byte)v;
			}
			hasChanged = true;
		}

		public override void Save(BinaryWriter writer, Progression progression) {
			writer.Write("BitMap4");
			writer.Write(width);
			writer.Write(height);
			writer.Write(data);
		}

		static BitMap4 LoadInternal(BinaryReader reader, Progression progression) {
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			EnforceValidSize(progression.scene, width, height);
			int len = (width * height) >> 1;
			byte[] data = reader.ReadBytes(len);
			if (data.Length != len) {
				throw new EcoException("Unexpected EOD");
			}
			return new BitMap4(progression.scene, data);
		}
		
		/**
		 * get data value at x, y
		 */
		public override int Get (int x, int y)
		{
			int index = y * width + x;
			int i1 = (index >> 1);
			if ((index & 0x01) == 0) {
				return data [i1] & 0x0f;
			} else {
				return (data [i1] & 0xf0) >> 4;
			}
		}
	
		public bool IsSet (int x, int y)
		{
			int index = y * width + x;
			int i1 = (index >> 1);
			if ((index & 0x01) == 0) {
				return (data [i1] & 0x0f) != 0;
			} else {
				return ((data [i1] & 0xf0) != 0);
			}
		}
		public override int GetMin() { return 0; }
		public override int GetMax() { return 15; }

		/**
		 * Copies this instance data to 'toData'
		 * This overridden version has a fast copy if toData is same data type, otherwise
		 * it falls back to base implementation
		 */
		public override void CopyTo(Data toData) {
			if (toData is BitMap4) {
				// data is of same type
				if ((toData.width != width) || (toData.height != height)) {
					throw new EcoException("size mismatch, toData needs to be same size as data");
				}
				// we can quickly copy array data....
				data.CopyTo(((BitMap4) toData).data, 0);
			}
			else {
				// fall back on normal simple CopyTo
				base.CopyTo(toData);
			}
		}
		
	}	
}