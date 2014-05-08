using System.IO;
using System.Xml;
using UnityEngine;

namespace Ecosim
{
	public static class IOUtil
	{

		public static string MyURIFileEncode (string path)
		{
			path = WWW.EscapeURL (path).Replace ("%2f", "/");
			if (!path.StartsWith ("/"))
				path = "/" + path;
			return "file://" + path;
		}
	
		public static float[] ReadFA (BinaryReader reader, int len)
		{
			float[] result = new float[len];
			for (int i = 0; i < len; i++)
				result [i] = reader.ReadSingle ();
			return result;
		}
	
		public static void WriteFA (BinaryWriter writer, float[] data)
		{
			foreach (float f in data)
				writer.Write (f);
		}
	
		public static Vector3 ReadVector3 (BinaryReader reader)
		{
			return new Vector3 (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());
		}

		public static Quaternion ReadQuaternion (BinaryReader reader)
		{
			return new Quaternion (reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle (), reader.ReadSingle ());
		}
	
		public static void WriteVector3 (BinaryWriter writer, Vector3 v)
		{
			writer.Write (v.x);
			writer.Write (v.y);
			writer.Write (v.z);
		}

		public static void WriteQuaternion (BinaryWriter writer, Quaternion q)
		{
			writer.Write (q.x);
			writer.Write (q.y);
			writer.Write (q.z);
			writer.Write (q.w);
		}
	
		public static void ReadUntilEndElement (XmlTextReader reader, string endElement)
		{
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == endElement)) {
						break;
					}
				}
			}
		}

		public static void DirectoryCopy (string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo (sourceDirName);
			DirectoryInfo[] dirs = dir.GetDirectories ();

			if (!dir.Exists) {
				throw new DirectoryNotFoundException (
                "Source directory does not exist or could not be found: "
                + sourceDirName);
			}

			// If the destination directory doesn't exist, create it. 
			if (!Directory.Exists (destDirName)) {
				Directory.CreateDirectory (destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles ();
			foreach (FileInfo file in files) {
				string temppath = Path.Combine (destDirName, file.Name);
				file.CopyTo (temppath, true);
			}

			// If copying subdirectories, copy them and their contents to new location. 
			if (copySubDirs) {
				foreach (DirectoryInfo subdir in dirs) {
					string temppath = Path.Combine (destDirName, subdir.Name);
					DirectoryCopy (subdir.FullName, temppath, copySubDirs);
				}
			}
		}		
	}

}