using System.Collections.Generic;
using System.IO;

namespace Ecosim.SceneData
{
	
/**
 * Stores tile parameter data for one data type (e.g. pH or salinity)
 * Storage is done through a Dictionary and is only suitable for situations where
 * there are not too many non-0 values
 */
	public class SparseBitMap8 : Data
	{
		
		private const int DICT_SIZE = 127;
	
		public SparseBitMap8 (Scene scene) 
		: base(scene)
		{
			data = new Dictionary<Coordinate, int> (DICT_SIZE);
		}
	
		SparseBitMap8 (Scene scene, Dictionary<Coordinate, int> data) 
		: base(scene)
		{
			this.data = data;
		}

		public override void Save (BinaryWriter writer, Progression progression)
		{
			writer.Write ("SparseBitMap8");
			writer.Write (width);
			writer.Write (height);
			writer.Write (data.Count);
			foreach (KeyValuePair<Coordinate, int> val in data) {
				writer.Write ((ushort)(val.Key.x));
				writer.Write ((ushort)(val.Key.y));
				writer.Write (val.Value);
			}
		}

		static SparseBitMap8 LoadInternal (BinaryReader reader, Progression progression)
		{
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			EnforceValidSize (progression.scene, width, height);
			int count = reader.ReadInt32 ();
			Dictionary<Coordinate, int> data = new Dictionary<Coordinate, int> (DICT_SIZE);
			for (int i = 0; i < count; i++) {
				int x = reader.ReadUInt16 ();
				int y = reader.ReadUInt16 ();
				int val = reader.ReadInt32 ();
				data.Add (new Coordinate (x, y), val);
			}
			return new SparseBitMap8 (progression.scene, data);
		}
		
		private Dictionary<Coordinate, int> data;
		
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
			Dictionary<Coordinate, int> copy = new Dictionary<Coordinate, int> (this.data);
			foreach (KeyValuePair<Coordinate, int> item in copy) {
				Coordinate c = item.Key;
				int val = item.Value;
				if (val != 0) {
					fn (c.x, c.y, val, data);
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
			val = (val < 0) ? 0 : ((val > 255) ? 255 : val);
			Coordinate c = new Coordinate (x, y);
			if (val == 0) {
				if (data.ContainsKey (c)) {
					data.Remove (c);
					hasChanged = true;
				}
			} else {
				int currentVal;
				if (data.TryGetValue (c, out currentVal)) {
					if (currentVal != val) {
						data [c] = val;
						hasChanged = true;
					}
				} else {
					data.Add (c, val);
					hasChanged = true;
				}
			}
		}

		/**
		 * set data value val at c
		 */
		public override void Set (Coordinate c, int val)
		{
			val = (val < 0) ? 0 : ((val > 255) ? 255 : val);
			if (val == 0) {
				if (data.ContainsKey (c)) {
					data.Remove (c);
					hasChanged = true;
				}
			} else {
				int currentVal;
				if (data.TryGetValue (c, out currentVal)) {
					if (currentVal != val) {
						data [c] = val;
						hasChanged = true;
					}
				} else {
					data.Add (c, val);
					hasChanged = true;
				}
			}
		}
		
		
		/**
		 * get data value at x, y
		 */
		public override int Get (int x, int y)
		{
			int val;
			Coordinate c = new Coordinate (x, y);
			if (data.TryGetValue (c, out val)) {
				return val;
			}
			return 0;
		}

		/**
		 * get data value at c
		 */
		public override int Get (Coordinate c)
		{
			int val;
			if (data.TryGetValue (c, out val)) {
				return val;
			}
			return 0;
		}
		
		
		/**
		 * copy data from src to data
		 */
		public void CopyFrom (SparseBitMap8 src)
		{
			if (src == this)
				return;
			data.Clear ();
			foreach (KeyValuePair<Coordinate, int> val in src.data) {
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
			return 255;
		}

		/**
		 * Copies this instance data to 'toData'
		 */
		public override void CopyTo (Data toData)
		{
			if (toData == this)
				return;
			toData.Clear ();
			foreach (KeyValuePair<Coordinate, int> val in data) {
				Coordinate c = val.Key;
				toData.Set (c, val.Value);
			}
		}

		/**
		 * Enumerates ValueCoordinates of all values in data set that are not 0.
		 */
		public override IEnumerable<ValueCoordinate> EnumerateNotZero() {
			// to make enumeration mutable (we can change data during enumeration) we
			// make a copy of the current data set and use that for enumeration
			Dictionary<Coordinate, int> copy = new Dictionary<Coordinate, int> (data);
			foreach (KeyValuePair<Coordinate, int> kv in copy) {
				yield return new ValueCoordinate(kv.Key.x, kv.Key.y, kv.Value);
			}
		}

		/**
		 * Enumerates ValueCoordinates of all values in data set area that are not 0
		 * minX, minY, maxX, maxY (all inclusive) is the value range for x and y
		 */
		public override IEnumerable<ValueCoordinate> EnumerateNotZero(int minX, int minY, int maxX, int maxY) {
			// to make enumeration mutable (we can change data during enumeration) we
			// make a copy of the current data set and use that for enumeration
			Dictionary<Coordinate, int> copy = new Dictionary<Coordinate, int> (data);
			foreach (KeyValuePair<Coordinate, int> kv in copy) {
				int x = kv.Key.x;
				int y = kv.Key.y;
				if ((x >= minX) && (x <= maxX) && (y >= minY) && (y <= maxY)) {
					yield return new ValueCoordinate(x, y, kv.Value);
				}
			}
		}
		
	}
}