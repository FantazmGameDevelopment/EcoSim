using System.Collections.Generic;
using System.IO;

namespace Ecosim.SceneData
{
	/**
	 * Sparse bitmap that stores text instead of numbers. As the normal number methods
	 * of data are present, value 1 means it has a text entry at that point, value 0 means
	 * no entry at that point (so value range is 0..1). Setting and getting text values
	 * must be done through TextBitMap specific methods
	 */
	public class TextBitMap : Data
	{
		
		private const int DICT_SIZE = 127;
	
		public TextBitMap (Scene scene) 
		: base(scene)
		{
			data = new Dictionary<Coordinate, string> (DICT_SIZE);
		}
	
		TextBitMap (Scene scene, Dictionary<Coordinate, string> data) 
		: base(scene)
		{
			this.data = data;
		}

		public override void Save (BinaryWriter writer, Progression progression)
		{
			writer.Write ("TextBitMap");
			writer.Write (width);
			writer.Write (height);
			writer.Write (data.Count);
			foreach (KeyValuePair<Coordinate, string> val in data) {
				writer.Write ((ushort)(val.Key.x));
				writer.Write ((ushort)(val.Key.y));
				writer.Write (val.Value);
			}
		}

		static TextBitMap LoadInternal (BinaryReader reader, Progression progression)
		{
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			EnforceValidSize (progression.scene, width, height);
			int count = reader.ReadInt32 ();
			Dictionary<Coordinate, string> data = new Dictionary<Coordinate, string> (DICT_SIZE);
			for (int i = 0; i < count; i++) {
				int x = reader.ReadUInt16 ();
				int y = reader.ReadUInt16 ();
				string val = reader.ReadString ();
				data.Add (new Coordinate (x, y), val);
			}
			return new TextBitMap (progression.scene, data);
		}
		
		private Dictionary<Coordinate, string> data;
		
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
			foreach (Coordinate c in this.data.Keys) {
				fn (c.x, c.y, 1, data);
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
		 * set data value val at x, y, in TextBitMap this means setting value 0 results in clearing
		 * a possible text entry, val 1 results in no change if text entry already exists, otherwise
		 * an empty (string == "") entry will be generated
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
			} else if (!data.ContainsKey (c)) {
				data.Add (c, "");
				hasChanged = true;
			}
			
		}
		
		/**
		 * Sets string val to coordinate c, if a string was already set to coordinate
		 * c then it will be overwritten.
		 */
		public void SetString (Coordinate c, string val) {
			if (data.ContainsKey (c)) {
				data [c] = val;
			}
			else {
				data.Add (c, val);
			}
			hasChanged = true;
		}

		/**
		 * Sets string val to coordinate (x, y), if a string was already set to coordinate
		 * c then it will be overwritten.
		 */
		public void SetString (int x, int y, string val) {
			SetString (new Coordinate (x, y), val);
		}
		
		/**
		 * if a string val was set to coordinate c it will be removed, otherwise does nothing
		 */
		public void Clear (Coordinate c) {
			if (data.ContainsKey (c)) {
				data.Remove (c);
				hasChanged = true;
			}
		}

		/**
		 * if a string val was set to coordinate (x, y) it will be removed, otherwise does nothing
		 */
		public void Clear (int x, int y) {
			Clear (new Coordinate (x, y));
		}
		
		/**
		 * If a string was set to coordinate c,  val is concatenated otherwise val is set
		 * to coordinate c.
		 */
		public void AddString (Coordinate c, string val) {
			if (data.ContainsKey (c)) {
				data[c] += val;
			}
			else {
				data.Add (c, val);
			}
			hasChanged = true;
		}

		/**
		 * If a string was set to coordinate (x, y),  val is concatenated otherwise val is set
		 * to coordinate c.
		 */
		public void AddString (int x, int y, string val) {
			AddString (new Coordinate (x, y), val);
		}
		
		/**
		 * returns string set to coordinate c, or null if not set
		 */
		public string GetString (Coordinate c) {
			string result;
			if (data.TryGetValue (c, out result)) {
				return result;
			}
			return null;
		}

		/**
		 * returns string set to coordinate (x, y), or null if not set
		 */
		public string GetString (int x, int y) {
			return GetString (new Coordinate (x, y));
		}
		
		/**
		 * set data value val at c
		 */
		public override void Set (Coordinate c, int val)
		{
			if (val == 0) {
				if (data.ContainsKey (c)) {
					data.Remove (c);
					hasChanged = true;
				}
			} else if (!data.ContainsKey (c)) {
				data.Add (c, "");
				hasChanged = true;
			}
		}
		
		
		/**
		 * get data value at x, y (1 means has text entry at x, y, otherwise 0)
		 */
		public override int Get (int x, int y)
		{
			Coordinate c = new Coordinate (x, y);
			if (data.ContainsKey (c)) {
				return 1;
			}
			return 0;
		}

		/**
		 * get data value at c(1 means has text entry at x, y, otherwise 0)
		 */
		public override int Get (Coordinate c)
		{
			if (data.ContainsKey (c)) {
				return 1;
			}
			return 0;
		}
		
		
		/**
		 * copy data from src to data. This copy function actually copies text!
		 */
		public void CopyFrom (TextBitMap src)
		{
			if (src == this)
				return;
			data.Clear ();
			foreach (KeyValuePair<Coordinate, string> val in src.data) {
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
			foreach (Coordinate c in data.Keys) {
				toData.Set (c, 1);
			}
		}

		/**
		 * Enumerates ValueCoordinates of all values in data set that are not 0.
		 */
		public override IEnumerable<ValueCoordinate> EnumerateNotZero ()
		{
			List<Coordinate> keysList = new List<Coordinate> (data.Keys);
			foreach (Coordinate c in keysList) {
				yield return new ValueCoordinate(c.x, c.y, 1);
			}
		}

		/**
		 * Enumerates ValueCoordinates of all values in data set area that are not 0
		 * minX, minY, maxX, maxY (all inclusive) is the value range for x and y
		 */
		public override IEnumerable<ValueCoordinate> EnumerateNotZero (int minX, int minY, int maxX, int maxY)
		{
			List<Coordinate> keysList = new List<Coordinate> (data.Keys);
			foreach (Coordinate c in keysList) {
				int x = c.x;
				int y = c.y;
				if ((x >= minX) && (x <= maxX) && (y >= minY) && (y <= maxY)) {
					yield return new ValueCoordinate(x, y, 1);
				}
			}
		}
	}
}