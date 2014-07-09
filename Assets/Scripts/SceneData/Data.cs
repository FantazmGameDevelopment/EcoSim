using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using Ecosim;

namespace Ecosim.SceneData
{
	/**
	 * Abstract class for holding a 2-dimensional set of integer values. The range of valid values
	 * is dependent on implementation.
	 */
	public abstract class Data
	{
		
		/**
		 * delegate is used in several processing functions, like processing all cells with value not 0.
		 */
		public delegate void DProcess(int x, int y, int val, System.Object data);
		
		const int DATA_HEADER = 0x48000001; // some semi-random hex code, version 1 :-)
		public const int MAX_WIDTH = 4096;
		public const int MAX_HEIGHT = 4096;
		public const int MAX_SIZE = MAX_WIDTH * MAX_HEIGHT;

		public readonly int width;
		public readonly int height;
		public readonly Scene scene;
		
		public bool hasChanged = false;
		
		/**
		 * Base constructor, dimensions of data is determined by scene
		 */
		public Data(Scene scene) {
			this.scene = scene;
			width = scene.width;
			height = scene.height;
			EnforceValidSize(width, height);
		}
		
		/**
		 * Special variant where width and height can be set
		 * Normally data width and height must match the dimensions
		 * defined in scene, but sometimes it can be useful to have
		 * a temporarily data instance with different dimensions.
		 * These can not be stored to disk (as when reading in a check
		 * is done to see if dimensions match scene).
		 */
		public Data(Scene scene, int width, int height) {
			this.scene = scene;
			this.width = width;
			this.height = height;
			EnforceValidSize(width, height);
		}
		
		
		public static void EnforceValidSize(int width, int height) {
			if ((width <= 0) || (height <= 0) || (width > MAX_WIDTH) || (height > MAX_HEIGHT) || (((width | height) & 0xf) != 0)) {
				throw new System.Exception("Invalid size " + width + "x" + height);
			}
		}

		public static void EnforceValidSize(Scene scene, int width, int height) {
			if ((width <= 0) || (height <= 0) || (width > MAX_WIDTH) || (height > MAX_HEIGHT) || (((width | height) & 0xf) != 0)) {
				throw new System.Exception("Invalid size " + width + "x" + height);
			}
			if ((width != scene.width) || (height != scene.height)) {
				throw new System.Exception("Invalid size " + width + "x" + height + " expected " + scene.width + "x" + scene.height);
			}
		}
		
		/**
		 * Save the data. Save is an abstract method so it should be implemented by all different derived
		 * classes of Data.
		 * writer the datastream to write to
		 * progression the progression the data is part of
		 * implementations of Save should always first do a writer.Write of their class name as this is
		 * used to reflect the right implementation on loading.
		 */
		public abstract void Save(BinaryWriter writer, Progression progression);
		
		public static Data Load(string path, Progression progression) {
			FileStream stream = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read);
			try {
				BinaryReader reader = new BinaryReader (stream);
				if (reader.ReadInt32() != DATA_HEADER) {
					throw new EcoException("Invalid header for data at '" + path + "'");
				}
				Data data = Load (reader, progression);
				reader.Close();
				return data;
			}
			finally {
				stream.Close();
			}
		}
		
		/**
		 * Saves data to given path, data is part of given progression
		 */
		public void Save(string path, Progression progression) {
			FileStream stream = new FileStream (path, FileMode.Create);
			try {
				BinaryWriter writer = new BinaryWriter (stream);
				writer.Write(DATA_HEADER);
				Save(writer, progression);
				writer.Close ();
			} finally {
				stream.Close ();
			}
		}
		
		/**
		 * Loads in the Data type. Should never be null (exceptions are thrown on error reading in)
		 * reader is a binary data stream
		 * progression is the current progression the data is part of
		 * The Load method uses reflection to get the right implementation for loading the Data type.
		 */
		public static Data Load(BinaryReader reader, Progression progression) {
			string typeName = reader.ReadString();
			Type t = Type.GetType("Ecosim.SceneData." + typeName);

			if (t.IsSubclassOf(typeof(Data))) {
				MethodInfo method = t.GetMethod("LoadInternal", BindingFlags.Static | BindingFlags.NonPublic);
				Data result = (Data) method.Invoke(null, new Object[] { reader, progression });
				return result;
				
			}
			throw new EcoException("Unknown Data type '" + typeName + "'");
		}
		
		/**
		 * Sets all values in bitmap to zero
		 */
		public abstract void Clear();
		
		/**
		 * Set value val at position x, y.
		 * The range of val is dependent on the type of Data, for example BitMap4 has range of 0..15, ParameterData 0..255
		 */
		public abstract void Set (int x, int y, int val);

		/**
		 * Set value val at position c.
		 * The range of val is dependent on the type of Data, for example BitMap4 has range of 0..15, ParameterData 0..255
		 */
		public virtual void Set (Coordinate c, int val) {
			Set (c.x, c.y, val);
		}

		/**
		 * Get value at position c.
		 * The range of result is dependent on the type of Data.
		 */
		public virtual int Get (Coordinate c) {
			return Get (c.x, c.y);
		}
		
		
		/**
		 * Get value at position x, y.
		 * The range of result is dependent on the type of Data.
		 */
		public abstract int Get (int x, int y);	
		
		/* minimum value supported by data */
		public abstract int GetMin();
		
		/* maximum value supported by data */
		public abstract int GetMax();

		/**
		 * Gets the total sum of all values.
		 */ 
		public virtual int GetSum () {
			int sum = 0;
			foreach (ValueCoordinate vc in EnumerateNotZero ()) {
				sum += vc.v;
			}
			return sum;
		}

		/**
		 * Gets the average value of all coordinates. We include all tiles in the calculation.
		 */ 
		public virtual float GetAverage () {
			return GetAverage (true);
		}

		/**
		 * Gets the average value of all coordinates. The includeZeroes determines whether we count tiles with a value of 0.
		 */ 
		public virtual float GetAverage (bool includeZeroes) {
			int sum = 0;
			int notZeroes = 0;
			foreach (ValueCoordinate vc in EnumerateNotZero ()) {
				sum += vc.v;
				notZeroes ++;
			}
			int total = (includeZeroes) ? (width * height) : notZeroes;
			return (float)sum / (float)total;
		}
		
		/**
		 * Calles function fn for every element not zero, passing x, y, value of element and data to fn.
		 */
		public virtual void ProcessNotZero(DProcess fn, System.Object data) {
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int val = Get (x, y);
					if (val != 0) fn(x, y, val, data);
				}
			}
		}
		
		/**
		 * Adds value val to every element
		 * The value will be clamped to the minimum and maximum values for the datatype
		 */
		public virtual void AddAll(int val) {
			if (val == 0) return;
			if (val > 0) {
				int max = GetMax();
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						int v = Get (x, y) + val;
						if (v > max) {
							Set (x, y, max);
						}
						else {
							Set (x, y, v);
						}
					}
				}
			}
			else {
				int min = GetMin();
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						int v = Get (x, y) + val;
						if (v < min) {
							Set (x, y, min);
						}
						else {
							Set (x, y, v);
						}
					}
				}
			}
		}
		
		/**
		 * Copies this instance data to 'toData'
		 * Values will be clamped to minimum and maximum values allowed by toData 
		 */
		public virtual void CopyTo(Data toData) {
			if ((toData.width != width) || (toData.height != height)) {
				throw new EcoException("size mismatch, toData needs to be same size as data");
			}
			if ((toData.GetMin() > GetMin()) || (toData.GetMax() < GetMax())) {
				// very slow copy as we have to clamp values
				int min = toData.GetMin();
				int max = toData.GetMax();
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						int val = Get(x, y);
						if (val < min) val = min;
						if (val > max) val = max;
						toData.Set(x, y, val);
					}
				}
			}
			else {
				// quicker copy, we know we don't have to clamp data....
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						toData.Set (x, y, Get(x, y));
					}
				}
			}
		}

		public virtual Data Clone ()
		{
			Data clone = (Data)System.Activator.CreateInstance (this.GetType(), scene);
			this.CopyTo (clone);
			return clone;
		}
		
		/**
		 * Creates a copy of instance with new sizes, copying content from offsetX, offsetY
		 * new size can be smaller or larger and offsetX can be out of bounds of original
		 * instance. Out of bound values will be set to 0. The size of the new data will
		 * be determined by the scene targetProgression is connected to.
		 * targetProgression is the progression the new data will be part of (can be
		 * the same as original data)
		 */
		public virtual Data CloneAndResize(Progression targetProgression, int offsetX, int offsetY) {
			System.Reflection.ConstructorInfo cinfo = GetType().GetConstructor(new Type[] { typeof(int), typeof(int) });
//			UnityEngine.Debug.LogWarning ("cinfo = " + cinfo + " my type = " + GetType());
			Data newData = (Data) cinfo.Invoke(new object[] { scene });
			
			int newWidth = targetProgression.scene.width;
			int newHeight = targetProgression.scene.height;
			for (int y = 0; y < newHeight; y++) {
				int srcY = y + offsetY;
				if ((srcY >= 0) && (srcY < height)) {
					for (int x = 0; x < newWidth; x++) {
						int srcX = x + offsetX;
						if ((srcX >= 0) && (srcX < width)) {
							newData.Set (x, y, Get (srcX, srcY));
						}
					}
				}
			}
			return newData;
		}
		
		/**
		 * Enumerates ValueCoordinates of all values in data set that are not 0.
		 */
		public virtual IEnumerable<ValueCoordinate> EnumerateNotZero() {
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int v = Get (x, y);
					if (v != 0) {
						yield return new ValueCoordinate (x, y, v);
					}
				}
			}
		}

		/**
		 * Enumerates ValueCoordinates of all values in data set area that are not 0
		 * minX, minY, maxX, maxY (all inclusive) is the value range for x and y
		 */
		public virtual IEnumerable<ValueCoordinate> EnumerateNotZero(int minX, int minY, int maxX, int maxY) {
			minX = UnityEngine.Mathf.Clamp (minX, 0, width - 1);
			maxX = UnityEngine.Mathf.Clamp (maxX, 0, width - 1);
			minY = UnityEngine.Mathf.Clamp (minY, 0, height - 1);
			maxY = UnityEngine.Mathf.Clamp (maxY, 0, height - 1);
			for (int y = minY; y <= maxY; y++) {
				for (int x = minX; x <= maxX; x++) {
					int v = Get (x, y);
					if (v != 0) {
						yield return new ValueCoordinate (x, y, v);
					}
				}
			}
		}
		
	}	
}
