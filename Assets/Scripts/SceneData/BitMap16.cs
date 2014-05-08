using System.Collections.Generic;
using System.IO;

namespace Ecosim.SceneData
{
	
/**
 * Stores tile parameter data for one data type (e.g. pH or salinity)
 */
	public class BitMap16 : Data
	{
	
		public BitMap16 (Scene scene) 
		: base(scene)
		{
		
			data = new ushort[width * height];
		}
	
		BitMap16 (Scene scene, ushort[] data) 
		: base(scene)
		{
			this.data = data;
		}
	
		// we made it public so code can use it directly for performance reasons
		public ushort[] data;

		public override void Save(BinaryWriter writer, Progression progression) {
			writer.Write("BitMap16");
			writer.Write(width);
			writer.Write(height);
			for (int i = 0; i < data.Length; i++) {
				writer.Write(data[i]);
			}
		}

		static BitMap16 LoadInternal(BinaryReader reader, Progression progression) {
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			EnforceValidSize (progression.scene, width, height);			
			int len = width * height; // len in shorts instead of bytes
			ushort[] data = new ushort[len];
			for (int i = 0; i < len; i++) {
				data[i] = reader.ReadUInt16();
			}
			return new BitMap16(progression.scene, data);
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
			data [y * width + x] = (ushort)val;
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
		public void CopyFrom (BitMap16 src)
		{
			System.Array.Copy (src.data, data, data.Length);
			hasChanged = true;
		}
	
		public void ClearData ()
		{
			System.Array.Clear (data, 0, data.Length);
			hasChanged = true;
		}
	
		public override int GetMin() { return 0; }
		public override int GetMax() { return 65535; }

	
		/**
		 * Copies this instance data to 'toData'
		 * This overridden version has a fast copy if toData is same data type, otherwise
		 * it falls back to base implementation
		 */
		public override void CopyTo(Data toData) {
			if (toData is BitMap16) {
				// data is of same type
				if ((toData.width != width) || (toData.height != height)) {
					throw new EcoException("size mismatch, toData needs to be same size as data");
				}
				// we can quickly copy array data....
				data.CopyTo(((BitMap16) toData).data, 0);
			}
			else if (toData is VegetationData) {
				// data is of same type
				if ((toData.width != width) || (toData.height != height)) {
					throw new EcoException("size mismatch, toData needs to be same size as data");
				}
				// we can quickly copy array data....
				data.CopyTo(((VegetationData) toData).data, 0);
			}
			else {
				// fall back on normal simple CopyTo
				base.CopyTo(toData);
			}
		}
	}	
}