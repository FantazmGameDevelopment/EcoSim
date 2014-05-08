using System.Collections.Generic;
using System.IO;

namespace Ecosim.SceneData
{
	
/**
 * Stores tile parameter data for one data type (e.g. pH or salinity)
 */
	public class VegetationData : Data
	{
		public const int SUCCESSION_MASK = 0x3f;
		public const int SUCCESSION_SHIFT = 9;
		public const int VEGETATION_MASK = 0x1f;
		public const int VEGETATION_SHIFT = 4;
		public const int TILE_MASK = 0x0f;
		public const int TILE_SHIFT = 0;
		
		public VegetationData (Scene scene) 
		: base(scene)
		{
		
			data = new ushort[width * height];
		}
	
		VegetationData (Scene scene, ushort[] data) 
		: base(scene)
		{
			this.data = data;
		}
	
		// we made it public so code can use it directly for performance reasons
		public ushort[] data;

		public override void Save (BinaryWriter writer, Progression progression)
		{
			writer.Write ("VegetationData");
			writer.Write (width);
			writer.Write (height);
			for (int i = 0; i < data.Length; i++) {
				writer.Write (data [i]);
			}
		}

		static VegetationData LoadInternal (BinaryReader reader, Progression progression)
		{
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			EnforceValidSize (progression.scene, width, height);
			int len = width * height; // len in shorts instead of bytes
			ushort[] data = new ushort[len];
			for (int i = 0; i < len; i++) {
				data [i] = reader.ReadUInt16 ();
			}
			return new VegetationData (progression.scene, data);
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
		 * returns tile type at x, y
		 */
		public TileType GetTileType (int x, int y)
		{
			int vegetationInt = data [y * width + x];
			int successionId = (vegetationInt >> SUCCESSION_SHIFT) & SUCCESSION_MASK;
			int vegetationId = (vegetationInt >> VEGETATION_SHIFT) & VEGETATION_MASK;
			int tileId = (vegetationInt >> TILE_SHIFT) & TILE_MASK;
			SuccessionType s = scene.successionTypes [successionId];
			VegetationType v = s.vegetations [vegetationId];
			TileType tile = v.tiles [tileId];
			return tile;
		}

		/**
		 * returns vegetation type at x, y
		 */
		public VegetationType GetVegetationType (int x, int y)
		{
			int vegetationInt = data [y * width + x];
			int successionId = (vegetationInt >> SUCCESSION_SHIFT) & SUCCESSION_MASK;
			int vegetationId = (vegetationInt >> VEGETATION_SHIFT) & VEGETATION_MASK;
			SuccessionType s = scene.successionTypes [successionId];
			VegetationType v = s.vegetations [vegetationId];
			return v;
		}

		/**
		 * returns succession type at x, y
		 */
		public SuccessionType GetSuccessionType (int x, int y)
		{
			int vegetationInt = data [y * width + x];
			int successionId = (vegetationInt >> SUCCESSION_SHIFT) & SUCCESSION_MASK;
			SuccessionType s = scene.successionTypes [successionId];
			return s;
		}
		
		public void SetTileType (int x, int y, TileType tile)
		{
			int p = (tile.vegetationType.successionType.index << SUCCESSION_SHIFT);
			p |= (tile.vegetationType.index << VEGETATION_SHIFT);
			p |= (tile.index << TILE_SHIFT);
			data [y * width + x] = (ushort)p;
		}
		
		
		/**
		 * after calling all tiles should have succession, vegetation and tile id's
		 * refering to existing data, no id's out of range. It is required that
		 * the first succession defined has at least one vegetation, and the first
		 * of these vegetations at least one tile.
		 * 
		 * if checkOnly is true, no fixes are done, the vegetation map is only checked for errors
		 * minorErrors are cases where a tileId is incorrrect but vegetation has tile types defined,
		 * tile is just set to a valid tile type for vegetation. majorErrors is where either the
		 * vegetation doesn't have tiles or vegetationId or successionId are invalid. In these cases
		 * the tile will be set to the first tile type of the first vegetation type of the first
		 * succession type.
		 * 
		 * tile Errors are fixed by changing tileId to a valid tile type. If no valid tile type exist
		 * 
		 * return null if no corrections are done, otherwise a SparseBitMap1 with
		 * values set to 1 on every location that has values fixed.
		 */
		public SparseBitMap1 FixVegetation (bool checkOnly, out int minorErrors, out int majorErrors)
		{
			minorErrors = 0;
			majorErrors = 0;
			SuccessionType[] successionTypes = scene.successionTypes;
			SparseBitMap1 result = new SparseBitMap1 (scene);
			int p = 0;
			System.Random rnd = new System.Random ();
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int vegetationInt = data [p];
					int successionId = (vegetationInt >> SUCCESSION_SHIFT) & SUCCESSION_MASK;
					if (successionId >= successionTypes.Length) {
						majorErrors++;
						result.Set (x, y, 1);
						if (!checkOnly) {
							data [p] = 0; 
						}
					} else {
						SuccessionType s = scene.successionTypes [successionId];
						int vegetationId = (vegetationInt >> VEGETATION_SHIFT) & VEGETATION_MASK;
						if (vegetationId >= s.vegetations.Length) {
							majorErrors++;
							result.Set (x, y, 1);
							if (!checkOnly) {
								data [p] = 0; 
							}
						} else {
							VegetationType v = s.vegetations [vegetationId];
							int tileId = (vegetationInt >> TILE_SHIFT) & TILE_MASK;
							int tileCount = v.tiles.Length;
							if (tileId >= tileCount) {
								result.Set (x, y, 1);
								if (tileCount == 0) {
									majorErrors++;
									if (!checkOnly) {
										data [p] = 0; 
									}
								} else {
									minorErrors++;
									if (!checkOnly) {
										if (tileCount == 1) {
											SetTileType (x, y, v.tiles [0]);
										} else {
											SetTileType (x, y, v.tiles [RndUtil.RndRange (ref rnd, 1, tileCount)]);
										}
									}
								}
							}
						}
					}
					p++;
				}
			}
			if ((minorErrors > 0) || (majorErrors > 0))
				return result;
			return null;
		}
		
		/**
		 * Modifies data values to remove the given succession type (sets it to type 0, vegetation 0, tile 0)
		 * and moves all succession indexes higher than the removed one to one lower (to fill the gap)
		 */
		public int RemoveSuccessionType (SuccessionType succession)
		{
			int p = 0;
			int count = 0;
			int removeSuccessionId = succession.index;
			int mask = (VEGETATION_MASK << VEGETATION_SHIFT) | (TILE_MASK << TILE_SHIFT);
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int val = data [p];
					int successionId = (val >> SUCCESSION_SHIFT) & SUCCESSION_MASK;
					if (successionId == removeSuccessionId) {
						data [p] = 0;
						count++;
					} else if (successionId > removeSuccessionId) {
						data [p] = (ushort)(((successionId - 1) << SUCCESSION_SHIFT) | (val & mask));
					}
					p++;
				}
			}
			return count;
		}
		
		/**
		 * Modifies data values to remove the given vegetation type (sets it to type 0, vegetation 0, tile 0)
		 * and moves for same succession type the vegetation indexes higher than the removed
		 * one to one lower (to fill the gap)
		 */
		public int RemoveVegetationType (VegetationType veg)
		{
			int p = 0;
			int count = 0;
			int filter = (veg.successionType.index << SUCCESSION_SHIFT);
			int mask = (SUCCESSION_MASK << SUCCESSION_SHIFT);
			int removedVegId = veg.index;
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int val = data [p];
					if ((val & mask) == filter) {
						int vegId = (val >> VEGETATION_SHIFT) & VEGETATION_MASK;
						if (vegId == removedVegId) {
							data [p] = (ushort)0;
							count++;
						} else if (vegId > removedVegId) {
							int tilePart = val & (TILE_MASK << TILE_SHIFT);
							int newVal = filter | tilePart | ((vegId - 1) << VEGETATION_SHIFT);
							data [p] = (ushort)newVal;
						}
					}
					p++;
				}
			}
			return count;
		}
		
		/**
		 * Modifies data values to remove the given tile type (actually keeps number if next tile is defined, otherwise lower
		 * it by one. Moves all the other tiles in same vegetation with higher index to index one lower.
		 */
		public int RemoveTileType (TileType tile)
		{
			int p = 0;
			int count = 0;
			VegetationType vegT = tile.vegetationType;
			int filter = (tile.vegetationType.index << VEGETATION_SHIFT) | (tile.vegetationType.successionType.index << SUCCESSION_SHIFT);
			int mask = (VEGETATION_MASK << VEGETATION_SHIFT) | (SUCCESSION_MASK << SUCCESSION_SHIFT);
			int removedTileId = tile.index;
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					int val = data [p];
					if ((val & mask) == filter) {
						int tileId = (val >> TILE_SHIFT) & TILE_MASK;
						if (tileId == removedTileId) {
							if ((tileId > 0) && (tileId >= vegT.tiles.Length - 1)) {
								data [p] = (ushort)(filter | ((tileId - 1) << TILE_SHIFT));
								count++;
							}
						} else if (tileId > removedTileId) {
							data [p] = (ushort)(filter | ((tileId - 1) << TILE_SHIFT));
						}
					}
					p++;
				}
			}
			return count;
		}
		
		/**
		 * Returns number of tiles with vegetation type vegetation in given area (looks at all tiles where area.Get(x, y) != 0).
		 * If area == null it looks at the whole dataset.
		 */
		public int CountVegetation (VegetationType vegetation, Data area)
		{
			int count = 0;
			int mask = SUCCESSION_MASK | VEGETATION_MASK;
			int maskValue = (vegetation.index << VEGETATION_SHIFT) | (vegetation.successionType.index << SUCCESSION_SHIFT);
			if (area != null) {
				area.ProcessNotZero (delegate (int x, int y, int val, object ignore) {
					int v = data [x + width * y] & mask;
					if (v == maskValue)
						count++;
				}, null);
			} else {
				for (int p = width * height - 1; p >= 0; p--) {
					int val = data [p] & mask;
					if (val == maskValue)
						count++;
				}
			}
			return count;
		}

		/**
		 * Returns array of coordinates of tiles with vegetation type vegetation
		 * in given area (looks at all tiles where area.Get(x, y) != 0).
		 * If area == null it looks at the whole dataset.
		 * NOTE this can be expensive if the vegetation is quite common!
		 */
		public Coordinate[] GetVegetationCoordinates (VegetationType vegetation, Data area)
		{
			List<Coordinate> coordList = new List<Coordinate> (512);
			int mask = SUCCESSION_MASK | VEGETATION_MASK;
			int maskValue = (vegetation.index << VEGETATION_SHIFT) | (vegetation.successionType.index << SUCCESSION_SHIFT);
			if (area != null) {
				area.ProcessNotZero (delegate (int x, int y, int val, object ignore) {
					int v = data [x + width * y] & mask;
					if (v == maskValue)
						coordList.Add (new Coordinate (x, y));
				}, null);
			} else {
				int p = 0;
				for (int y = 0; y < height; y++) {
					for (int x = 0; x < width; x++) {
						int val = data [p] & mask;
						if (val == maskValue)
							coordList.Add (new Coordinate (x, y));
						p++;
					}
				}
			}
			return coordList.ToArray ();
		}
		
		/**
		 * copy data from src to data
		 */
		public void CopyFrom (BitMap16 src)
		{
			System.Array.Copy (src.data, data, data.Length);
			hasChanged = true;
		}

		/**
		 * copy data from src to data
		 */
		public void CopyFrom (VegetationData src)
		{
			System.Array.Copy (src.data, data, data.Length);
			hasChanged = true;
		}
		
		public void ClearData ()
		{
			System.Array.Clear (data, 0, data.Length);
			hasChanged = true;
		}
	
		public override int GetMin ()
		{
			return 0;
		}

		public override int GetMax ()
		{
			return 65535;
		}

	
		/**
		 * Copies this instance data to 'toData'
		 * This overridden version has a fast copy if toData is same data type, otherwise
		 * it falls back to base implementation
		 */
		public override void CopyTo (Data toData)
		{
			if (toData is BitMap16) {
				// data is of same type
				if ((toData.width != width) || (toData.height != height)) {
					throw new EcoException ("size mismatch, toData needs to be same size as data");
				}
				// we can quickly copy array data....
				data.CopyTo (((BitMap16)toData).data, 0);
			} else if (toData is VegetationData) {
				// data is of same type
				if ((toData.width != width) || (toData.height != height)) {
					throw new EcoException ("size mismatch, toData needs to be same size as data");
				}
				// we can quickly copy array data....
				data.CopyTo (((VegetationData)toData).data, 0);
			} else {
				// fall back on normal simple CopyTo
				base.CopyTo (toData);
			}
		}
	}	
}