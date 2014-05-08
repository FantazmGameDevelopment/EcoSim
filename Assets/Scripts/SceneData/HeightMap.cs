using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ecosim.SceneData
{
	
/**
 * Stores tile parameter data for one data type (e.g. pH or salinity)
 */
	public class HeightMap : Data
	{
		public const float HEIGHT_PER_UNIT = 0.01f; // one unit is 0.01 meter
		public const float VERTICAL_HEIGHT = 655.35f; // makes one unit == 0.01 meter
	
		public HeightMap (Scene scene) 
		: base(scene)
		{
		
			data = new ushort[width * height];
		}
	
		HeightMap (Scene scene, ushort[] data) 
		: base(scene)
		{
			this.data = data;
		}
	
		// we made it public so code can use it directly for performance reasons
		public ushort[] data;

		public override void Save(BinaryWriter writer, Progression progression) {
			writer.Write("HeightMap");
			writer.Write(width);
			writer.Write(height);
			for (int i = 0; i < data.Length; i++) {
				writer.Write(data[i]);
			}
		}

		static HeightMap LoadInternal(BinaryReader reader, Progression progression) {
			int width = reader.ReadInt32();
			int height = reader.ReadInt32();
			EnforceValidSize (progression.scene, width, height);			
			int len = width * height; // len in shorts instead of bytes
			ushort[] data = new ushort[len];
			for (int i = 0; i < len; i++) {
				data[i] = reader.ReadUInt16();
			}
			return new HeightMap(progression.scene, data);
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
		 * Gets height at x, y, height is a float between 0 and VERTICAL_HEIGHT
		 */
		public float GetHeight (int x, int y) {
			return HEIGHT_PER_UNIT * data [y * width + x];
		}
		
		/**
		 * Gets height using interpolation between tiles. It won't match the real terrain perfectly
		 * but it should be close enough to be usable. Doesn't take canals into account.
		 * x and y are real world terrain coordinates instead of tile coordinates. x and y will be clamped.
		 */
		public float GetInterpolatedHeight (float x, float y) {
			x = Mathf.Clamp(x / TerrainMgr.TERRAIN_SCALE - 0.5f, 0f, width - 1.01f);
			y = Mathf.Clamp(y / TerrainMgr.TERRAIN_SCALE - 0.5f, 0f, height - 1.01f);
			int xInt = (int) x;
			int yInt = (int) y;
			float xFraq = x - xInt;
			float yFraq = y - yInt;
			int v00 = data [yInt * width + xInt];
			int v10 = data [yInt * width + xInt + 1];
			int v01 = data [yInt * width + xInt + width];
			int v11 = data [yInt * width + xInt + width + 1];
			float result = v00 * (1f - xFraq) * (1f - yFraq) + v10 * xFraq * (1f - yFraq) + v01 * (1f - xFraq) * yFraq + v11 * xFraq * yFraq;
			return HEIGHT_PER_UNIT * result;			
		}
		
		/**
		 * Sets height at x, y, height will be clamped 0 and VERTICAL_HEIGHT and stored in its internal ushort representation
		 */
		public void SetHeight (int x, int y, float height) {
			 data [y * width + x] = (ushort) UnityEngine.Mathf.Clamp ((int) (height / HEIGHT_PER_UNIT), 0, 65535);
		}
		
		/**
		 * copy data from src to data
		 */
		public void CopyFrom (HeightMap src)
		{
			System.Array.Copy (src.data, data, data.Length);
			hasChanged = true;
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
			if (toData is HeightMap) {
				// data is of same type
				if ((toData.width != width) || (toData.height != height)) {
					throw new EcoException("size mismatch, toData needs to be same size as data");
				}
				// we can quickly copy array data....
				data.CopyTo(((HeightMap) toData).data, 0);
			}
			else if (toData is BitMap16) {
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