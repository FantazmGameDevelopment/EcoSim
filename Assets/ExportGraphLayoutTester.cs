using UnityEngine;
using System.Collections;

public class ExportGraphLayoutTester : MonoBehaviour {

	public int w;
	public int h;

	void Start ()
	{
		w = Ecosim.GameCtrl.ExportGraphWindow.windowWidth;
		h = Ecosim.GameCtrl.ExportGraphWindow.windowHeight;
	}

	void Reset ()
	{
		Start ();
	}
	
	void Update () 
	{
		Ecosim.GameCtrl.ExportGraphWindow.windowWidth = w;
		Ecosim.GameCtrl.ExportGraphWindow.windowHeight = h;
	}
}
