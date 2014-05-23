using UnityEngine;
using System.Collections;

namespace Ecosim
{
	/**
	 * Utility class with static helper functions for string encoding/processing
	 */
	public static class StringUtil
	{
		/**
		 * Decodes string into unity Vector3
		 */
		public static Vector3 StringToVector3 (string s)
		{
			s = s.Trim ();
			s = s.TrimStart ('(').TrimEnd (')');
			string[] nrs = s.Split (',');
			return new Vector3 (float.Parse (nrs [0]), float.Parse (nrs [1]), float.Parse (nrs [2]));
		}
	
		/**
		 * Encodes unity Vector3 into string
		 */
		public static string Vector3ToString (Vector3 v)
		{
			return "(" + v.x + ',' + v.y + ',' + v.z + ')';
		}
	
		/**
		 * Decodes string into unity Color
		 */
		public static Color StringToColor (string s)
		{
			s = s.Trim ();
			s = s.TrimStart ('(').TrimEnd (')');
			string[] nrs = s.Split (',');
			return new Color (float.Parse (nrs [0]) / 255f, float.Parse (nrs [1]) / 255f, float.Parse (nrs [2]) / 255f);
		}
	
		/**
		 * Encodes unity Color into string
		 */
		public static string ColorToString (Color c)
		{
			return "(" + ((int)(c.r * 255)) + ',' + ((int)(c.g * 255)) + ',' + ((int)(c.b * 255)) + ')';
		}
	
		/**
		 * Encodes array of byte into string
		 */
		public static string BAToString (byte[] data)
		{
			if (data == null)
				return "<null>";
			string s = null;
			foreach (byte b in data) {
				if (s != null) {
					s += ", " + (int)b;
				} else {
					s = "" + (int)b;
				}
			}
			return s;
		}
	
		/**
		 * Encodes array of int into a string
		 */
		public static string IAToString (int[] ia)
		{
			string result = "";
			foreach (int i in ia) {
				if (result == "")
					result = i.ToString ();
				else
					result += "," + i.ToString ();
			}
			return result;
		}
	
		/**
		 * Decodes string into an array of int
		 */
		public static int[] StringToIA (string str)
		{
			string[] sa = str.Split (',');
			int[] result = new int[sa.Length];
			for (int i = 0; i < sa.Length; i++)
				result [i] = int.Parse (sa [i]);
			return result;
		}

		/**
		 * Encodes an array of Coordinate into a string
		 */
		public static string CoordAToString (Coordinate[] coorda)
		{
			string result = "";
			foreach (Coordinate coord in coorda) {
				if (result != "")
					result += '|';
				result += coord.x.ToString () + ',' + coord.y.ToString ();
			}
			return result;
		}
		
		/**
		 * Encodes an array of ValueCoordinate into a string
		 */
		public static string ValCoordAToString (ValueCoordinate[] coorda)
		{
			string result = "";
			foreach (ValueCoordinate coord in coorda) {
				if (result != "")
					result += '|';
				result += coord.x.ToString () + ',' + coord.y.ToString () + ',' + coord.v.ToString ();
			}
			return result;
		}
	
		/**
		 * returns array of Coordinate read from string str
		 */
		public static Coordinate[] StringToCoordA (string str)
		{
			str = str.Trim ();
			if (str == "")
				return new Coordinate[0];
			string[] sa = str.Split ('|');
			Coordinate[] result = new Coordinate[sa.Length];
			for (int i = 0; i < sa.Length; i++) {
				string[] ca = sa [i].Split (',');
				result [i] = new Coordinate (int.Parse (ca [0]), int.Parse (ca [1]));
			}
			return result;
		}
		
		/**
		 * returns array of ValueCoordinate read from string str
		 */
		public static ValueCoordinate[] StringToValCoordA (string str)
		{
			str = str.Trim ();
			if (str == "")
				return new ValueCoordinate[0];
			string[] sa = str.Split ('|');
			ValueCoordinate[] result = new ValueCoordinate[sa.Length];
			for (int i = 0; i < sa.Length; i++) {
				string[] ca = sa [i].Split (',');
				result [i] = new ValueCoordinate (int.Parse (ca [0]), int.Parse (ca [1]), int.Parse (ca [2]));
			}
			return result;
		}
		
		/**
		 * We define id as starting with letter, followed by 0 or more letters, digits or '_' chars.
		 * return true if id is valid
		 */
		public static bool IsValidID(string id) {
			if (id.Length == 0) return false;
			if (!char.IsLetter(id[0])) return false;
			foreach (char c in id) {
				if (!char.IsLetterOrDigit(c) && !(c == '_')) return false;
			}
			return true;
		}

		public static string MakeValidID (string id) {
			return MakeValidID (id, false);
		}

		public static string MakeValidID (string id, bool isInternal) {
			string newId = (isInternal) ? "_" : "";
			foreach (char c in id) {
				if (char.IsLetterOrDigit(c)) 
					newId += c.ToString();
			}
			return newId.Replace(" ", "");
		}
	}
	
}