using UnityEngine;
using System.Collections;

namespace Ecosim
{
	/**
	 * Simple coordinate class. Currently it uses shorts for internal storage of the x and y
	 * values. Possibly this could be changed to int to improve performance (for slightly
	 * increased memory usage).
	 */
	public struct Coordinate
	{
		public static Coordinate INVALID = new Coordinate (-1, -1);
		
		public Coordinate (int x, int y)
		{
			this.x = (short)x;
			this.y = (short)y;
		}

		public readonly short x;
		public readonly short y;
		
		public static bool operator == (Coordinate c1, Coordinate c2)
		{
			return (c1.x == c2.x) && (c1.y == c2.y);
		}

		public static bool operator != (Coordinate c1, Coordinate c2)
		{
			return (c1.x != c2.x) || (c1.y != c2.y);
		}
		
		public override bool Equals (object o)
		{
			if (o is Coordinate) {
				return this.Equals ((Coordinate)o);
			}
			return false;
		}

		public bool Equals (Coordinate other)
		{
			return (other.x == x) && (other.y == y);
		}
		
		public override int GetHashCode ()
		{
			return x ^ (y >> 2) ^ (y << 16);
		}		
	}
}