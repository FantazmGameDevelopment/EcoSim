using UnityEngine;
using System.Collections;

namespace Ecosim
{
	public struct ValueCoordinate
	{
		public ValueCoordinate (int x, int y, int v)
		{
			this.x = (short)x;
			this.y = (short)y;
			this.v = v;
		}

		public readonly short x;
		public readonly short y;
		public int v;
		
		public static implicit operator Coordinate (ValueCoordinate vc) {
			return new Coordinate (vc.x, vc.y);
		}
	}

}