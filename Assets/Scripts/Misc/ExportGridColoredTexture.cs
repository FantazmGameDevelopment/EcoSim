using UnityEngine;
using System.Collections;
using System.IO;

public class ExportGridColoredTexture : MonoBehaviour 
{
	public string path = "newTexture.png";

	public int width = 256;
	public int height = 256;

	public Color[] colors = new Color[2] { Color.red, Color.green };

	public bool skipFirst;

	[ContextMenu ("Export now")]
	public void Export ()
	{
		int i = 0; // Index
		Texture2D t = new Texture2D (width, height, TextureFormat.RGBA32, true);
		for (int y = 0; y < height; y++) 
		{
			for (int x = 0; x < width; x++) 
			{
				float p = (float)i / (float)(width * height); // Total percentage
				float np = 1f / (float)(colors.Length - 1); // Next color percentage
				int ci = Mathf.FloorToInt (p / np); // Color index
				Color c1 = colors[ci]; // Color1
				Color c2 = colors[ci + 1]; // Color2
				float lp = ((p - (np * ci)) / np); // Lerp percentage (t)
				t.SetPixel (x, y, Color.Lerp (c1, c2, lp));
				i++;
			}
		}

		if (skipFirst)
			t.SetPixel (0, 0, new Color (0f, 0f, 0f, 0f));

		byte[] bs = t.EncodeToPNG ();
		FileStream fs = File.Open (Application.dataPath + "/" + path, FileMode.Create);
		BinaryWriter bw = new BinaryWriter (fs);
		bw.Write (bs);
		fs.Close ();
	}
}
