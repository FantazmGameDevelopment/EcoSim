using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;

public class SaveFileDialog
{
	[System.Runtime.InteropServices.DllImport ("user32.dll")]
	private static extern void _saveFileDialog ();

	public delegate void ResultDelegate (bool ok, string url);

	public static IEnumerator Show (string fileName, string filters, ResultDelegate onResult)
	{
		// If we're in full screen we switch back to non-fullscreen
		bool fullscreen = Screen.fullScreen;
		Resolution currentResolution = Screen.currentResolution;
		if (fullscreen) 
		{
			Resolution maxRes = Screen.resolutions [Screen.resolutions.Length - 1];
			Screen.SetResolution (maxRes.width, maxRes.height, false);
			//Screen.fullScreen = false;
			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();
		}

		// Save to .txt
		System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog ();
		sfd.Filter = filters;
		sfd.FileName = fileName;

		// Check result
		bool result = (sfd.ShowDialog () == System.Windows.Forms.DialogResult.OK);
		string url = sfd.FileName;

		// Check if we we're fullscreen and reset it
		if (fullscreen) 
		{
			Screen.SetResolution (currentResolution.width, currentResolution.height, true);
			//Screen.fullScreen = true;
			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();
		}

		if (onResult != null)
			onResult (result, url);
	}
}
