using System.Collections.Generic;
using System.IO;

namespace Ecosim.SceneData
{
	
/**
 * Stores tile parameter data for one data type (e.g. pH or salinity)
 * Storage is done through a Dictionary and is only suitable for situations where
 * there are not too many non-0 values
 */
	public class SparseBitMap1 : Data
	{
		
		private const int DICT_SIZE = 127;
	
		public SparseBitMap1 (Scene scene) 
		: base(scene)
		{
			data = new Dictionary<Coordinate, ulong> (DICT_SIZE);
		}
	
		SparseBitMap1 (Scene scene, Dictionary<Coordinate, ulong> data) 
		: base(scene)
		{
			this.data = data;
		}

		public override void Save (BinaryWriter writer, Progression progression)
		{
			writer.Write ("SparseBitMap1");
			writer.Write (width);
			writer.Write (height);
			writer.Write (data.Count);
			foreach (KeyValuePair<Coordinate, ulong> val in data) {
				writer.Write ((ushort)(val.Key.x));
				writer.Write ((ushort)(val.Key.y));
				writer.Write (val.Value);
			}
		}

		static SparseBitMap1 LoadInternal (BinaryReader reader, Progression progression)
		{
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			EnforceValidSize (progression.scene, width, height);
			int count = reader.ReadInt32 ();
			Dictionary<Coordinate, ulong> data = new Dictionary<Coordinate, ulong> (DICT_SIZE);
			for (int i = 0; i < count; i++) {
				int x = reader.ReadUInt16 ();
				int y = reader.ReadUInt16 ();
				ulong val = reader.ReadUInt64 ();
				data.Add (new Coordinate (x, y), val);
			}
			return new SparseBitMap1 (progression.scene, data);
		}
		
		private Dictionary<Coordinate, ulong> data;
		
		/**
		 * Sets all values in bitmap to zero
		 */
		public override void Clear ()
		{
			data.Clear ();
		}

		/**
		 * Calles function fn for every element not zero, passing x, y, value of element and data to fn.
		 */
		public override void ProcessNotZero (DProcess fn, System.Object data)
		{
			Dictionary<Coordinate, ulong> copy = new Dictionary<Coordinate, ulong> (this.data);
			foreach (KeyValuePair<Coordinate, ulong> item in copy) {
				Coordinate c = item.Key;
				ulong val = item.Value;
				if (val != 0) {
					int xbase = c.x << 6;
					int y = c.y;
					for (int i = 0; i < 64; i++) {
						if ((val & (1UL << i)) != 0) {
							fn (xbase | i, y, 1, data);
						}
					}
				}
			}
		}
		
		/**
		 * Adds value val to every element
		 * The value will be clamped to the minimum and maximum values for the datatype
		 */
		public override void AddAll (int val)
		{
			throw new System.NotSupportedException ("operation not supported on sparse bitmaps");
		}
		
		/**
		 * set data value val at x, y
		 */
		public override void Set (int x, int y, int val)
		{
			Coordinate c = new Coordinate (x >> 6, y);
			ulong mask = 1UL << (x & 0x3f);
			
			ulong currentVal;
			if (data.TryGetValue (c, out currentVal)) {
				hasChanged = true;
				if (val != 0) {
					currentVal |= mask;
				} else {
					currentVal &= ~mask;
				}
				if (currentVal == 0) {
					data.Remove (c);
				} else {
					data [c] = currentVal;
				}
			} else {
				if (val != 0) {
					hasChanged = true;
					data.Add (c, mask);
				}
			}
		}

		
		/**
		 * get data value at x, y
		 */
		public override int Get (int x, int y)
		{
			Coordinate c = new Coordinate (x >> 6, y);
			ulong val;
			if (data.TryGetValue (c, out val)) {
				ulong mask = 1UL << (x & 0x3f);
				return ((val & mask) != 0) ? 1 : 0;
			}
			return 0;
		}

		
		/**
		 * copy data from src to data
		 */
		public void CopyFrom (SparseBitMap1 src)
		{
			if (src == this)
				return;
			data.Clear ();
			foreach (KeyValuePair<Coordinate, ulong> val in src.data) {
				data.Add (val.Key, val.Value);
			}
			hasChanged = true;
		}
	
		public void ClearData ()
		{
			data.Clear ();
			hasChanged = true;
		}
	
		/**
		 * increase value of all data by 1, limit it to 100;
		 */
		public void IncreaseBy1 ()
		{
			throw new System.NotSupportedException ("operation not supported on sparse bitmaps");
		}

		/**
		 * increase value of all data by 1, limit it to 0;
		 */
		public void DecreaseBy1 ()
		{
			throw new System.NotSupportedException ("operation not supported on sparse bitmaps");
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
		 */
		public override void CopyTo (Data toData)
		{
			if (toData == this)
				return;
			toData.Clear ();
			foreach (KeyValuePair<Coordinate, ulong> val in data) {
				int xbase = val.Key.x << 6;
				int y = val.Key.y;
				ulong bits = val.Value;
				for (int i = 0; i < 64; i++) {
					if ((bits & (1UL << i)) != 0) {
						toData.Set (xbase | i, y, 1);
					}
				}
			}
		}

		/**
		 * Enumerates ValueCoordinates of all values in data set that are not 0.
		 */
		public override IEnumerable<ValueCoordinate> EnumerateNotZero ()
		{
			// to make enumeration mutable (we can change data during enumeration) we
			// make a copy of the current data set and use that for enumeration
			Dictionary<Coordinate, ulong> copy = new Dictionary<Coordinate, ulong> (data);
			foreach (KeyValuePair<Coordinate, ulong> kv in copy) {
				int y = kv.Key.y;
				int xbase = kv.Key.x << 6;
				ulong bits = kv.Value;
				for (int i = 0; i < 64; i++) {
					if ((bits & (1UL << i)) != 0) {
						yield return new ValueCoordinate(xbase | i, y, 1);
					}
				}
			}
		}

		/**
		 * Enumerates ValueCoordinates of all values in data set area that are not 0
		 * minX, minY, maxX, maxY (all inclusive) is the value range for x and y
		 */
		public override IEnumerable<ValueCoordinate> EnumerateNotZero (int minX, int minY, int maxX, int maxY)
		{
			// to make enumeration mutable (we can change data during enumeration) we
			// make a copy of the current data set and use that for enumeration
			Dictionary<Coordinate, ulong> copy = new Dictionary<Coordinate, ulong> (data);
			foreach (KeyValuePair<Coordinate, ulong> kv in copy) {
				int y = kv.Key.y;
				if ((y >= minY) && (y <= maxY)) {
					int xbase = kv.Key.x << 6;
					ulong bits = kv.Value;
					int minI = (xbase >= minX) ? 0 : (minX - xbase);
					int maxI = (xbase + 64 < maxX) ? 64 : (maxX - xbase);
					for (int i = minI; i < maxI; i++) {
						if ((bits & (1UL << i)) != 0) {
							yield return new ValueCoordinate(xbase | i, y, 1);
						}
					}
				}
			}
		}
	
	}
}