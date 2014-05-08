using UnityEngine;
using System.Collections;

namespace Ecosim
{
	public static class Layers
	{
		public const int L_TERRAIN = 8;
		public const int L_DECALS = 9;
		public const int L_ANIMALS = 10;
		public const int L_WATER = 11;
		public const int L_ROADS = 12;
		public const int L_OVERVIEW = 16;
		public const int L_EDIT1 = 29;
		public const int L_EDIT2 = 30;
		public const int L_GUI = 31;
		
		public const int M_TERRAIN = 1 << L_TERRAIN;
		public const int M_DECALS= 1 << L_DECALS;
		public const int M_ANIMALS = 1 << L_ANIMALS;
		public const int M_WATER = 1 << L_WATER;
		public const int M_ROADS = 1 << L_ROADS;
		public const int M_OVERVIEW = 1 << L_OVERVIEW;
		public const int M_EDIT1 = 1 << L_EDIT1;
		public const int M_EDIT2 = 1 << L_EDIT2;
		public const int M_GUI = 1 << L_GUI;
	}
}