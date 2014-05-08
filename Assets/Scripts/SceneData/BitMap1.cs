using System.Collections;
using System.IO;

namespace Ecosim.SceneData
{
	public class BitMap1 : Data
	{
	
		public BitMap1 (Scene scene)
		: base(scene)
		{
		
			data = new byte[width * height / 8];
		}

		public BitMap1 (Scene scene, int width, int height)
		: base(scene, width, height)
		{
		
			data = new byte[width * height / 8];
		}
		
		
		BitMap1 (Scene scene, byte[] data)
		: base(scene)
		{
		
			this.data = data;
		}
		
		public byte[] data;
	
		public override void Save (BinaryWriter writer, Progression progression)
		{
			writer.Write ("BitMap1");
			writer.Write (width);
			writer.Write (height);
			writer.Write (data);
		}
		
		static BitMap1 LoadInternal (BinaryReader reader, Progression progression)
		{
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			EnforceValidSize (progression.scene, width, height);
			int len = (width * height) >> 3;
			byte[] data = reader.ReadBytes (len);
			if (data.Length != len) {
				throw new EcoException ("Unexpected EOD");
			}
			return new BitMap1 (progression.scene, data);
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
	 	 * set bitmap at x, y to true
	 	 */
		public override void Set (int x, int y, int val)
		{
			int index = y * width + x;
			int i1 = (index >> 3);
			int i2 = 1 << (index & 0x07);
			if (val != 0) {
				int v = data [i1];
				v |= i2;
				data [i1] = (byte)v;
			} else {
				int v = data [i1];
				v &= ~i2;
				data [i1] = (byte)v;
			}
			hasChanged = true;
		}

	
		/**
		 * get data value at x, y
	 	 */
		public override int Get (int x, int y)
		{
			int index = y * width + x;
			int i1 = (index >> 3);
			int i2 = 1 << (index & 0x07);
			return ((data [i1] & i2) != 0) ? 1 : 0;
		}
	
		public void SetToTrue (int x, int y)
		{
			int index = y * width + x;
			int i1 = (index >> 3);
			int i2 = 1 << (index & 0x07);
			int v = data [i1];
			v |= i2;
			data [i1] = (byte)v;
			hasChanged = true;
		}
	
		public void SetToFalse (int x, int y)
		{
			int index = y * width + x;
			int i1 = (index >> 3);
			int i2 = 1 << (index & 0x07);
			int v = data [i1];
			v &= ~i2;
			data [i1] = (byte)v;
			hasChanged = true;
		}
	
		public bool IsSet (int x, int y)
		{
			int index = y * width + x;
			int i1 = (index >> 3);
			int i2 = 1 << (index & 0x07);
			return ((data [i1] & i2) != 0);
		}
		
		public override int GetMin ()
		{
			return 0;
		}

		public override int GetMax ()
		{
			return 1;
		}

		/**
		 * Copies this instance data to 'toData'
		 * This overridden version has a fast copy if toData is same data type, otherwise
		 * it falls back to base implementation
		 */
		public override void CopyTo (Data toData)
		{
			if (toData is BitMap1) {
				// data is of same type
				if ((toData.width != width) || (toData.height != height)) {
					throw new EcoException ("size mismatch, toData needs to be same size as data");
				}
				// we can quickly copy array data....
				data.CopyTo (((BitMap1)toData).data, 0);
			} else {
				// fall back on normal simple CopyTo
				base.CopyTo (toData);
			}
		}
	
	}
}