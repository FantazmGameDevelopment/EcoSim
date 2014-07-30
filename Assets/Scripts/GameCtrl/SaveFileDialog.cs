using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;

public class SaveFileDialog
{
	[System.Runtime.InteropServices.DllImport ("user32.dll")]
	private static extern void _saveFileDialog ();

	public static bool Show (string fileName, out string url)
	{
		return Show (fileName, out url, "");
	}

	public static bool Show (string fileName, out string url, string filters)
	{
		// Save to .txt
		System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog ();
		sfd.Filter = filters;
		sfd.FileName = fileName;

		// If we're in full screen we switch back to non-fullscreen
		bool fullscreen = Screen.fullScreen;
		Resolution currentResolution = Screen.currentResolution;
		if (fullscreen) 
		{
			Resolution maxRes = Screen.resolutions [Screen.resolutions.Length - 1];
			Screen.SetResolution (maxRes.width, maxRes.height, false);
		}

		// Check result
		bool result = (sfd.ShowDialog () == System.Windows.Forms.DialogResult.OK);
		url = sfd.FileName;

		// Check if we we're fullscreen and reset it
		if (fullscreen) 
		{
			Screen.SetResolution (currentResolution.width, currentResolution.height, true);
		}
		return result;
	}
}
