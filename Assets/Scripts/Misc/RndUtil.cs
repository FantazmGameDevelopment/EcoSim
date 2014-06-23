using UnityEngine;
using System.Collections;
using System.Text;

namespace Ecosim
{
	public static class RndUtil
	{
		/**
		 * Returns value between min (inclusive) and max (exclusive)
		 * If max <= min, returns min
		 */
		public static int RndRange (ref System.Random rnd, int min, int max)
		{
			if (max <= min + 1)
				return min;
			return (rnd.Next () % (max - min)) + min;
		}

		/**
		 * Returns value between min (inclusive) and max (inclusive)
		 * If max <= min, returns min
		 */
		public static float RndRange (ref System.Random rnd, float min, float max)
		{
			if (max <= min)
				return min;
			return min + ((max - min) * (float)rnd.NextDouble ());
		}
		
		/**
		 * Returns colour from (inclusive) transition ranges of min and max
		 */
		public static Color RndRange (ref System.Random rnd, Color min, Color max)
		{
			return Color.Lerp (min, max, (float)rnd.NextDouble ());
		}
		
		const string validChars = "abcdefghijklmnopqrstuvwxyz";
		
		/**
		 * Returns random character string usable as unique id of length size
		 * note that System.Random is not secure and seen as a good random
		 * generator, so not suitable for security strings, but good enough
		 * for getting something kinda unique.
		 */
		public static string RandomString (ref System.Random rnd, int size)
		{
			StringBuilder builder = new StringBuilder ();
			for (int i=0; i<size; i++) {
				int validIndex = (rnd.Next() % validChars.Length);
				builder.Append (validChars[validIndex]);
			}
			return builder.ToString ();
		}		
	}

}