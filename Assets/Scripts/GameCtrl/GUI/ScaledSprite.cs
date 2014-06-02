using UnityEngine;
using System.Collections;

public class ScaledSprite : MonoBehaviour 
{
	public Camera mainCam;

	private Transform myTransform;
	private Texture myTexture;
	private int width = -1;
	private int height = -1;

	// Use this for initialization
	void Start () {
		myTransform = transform;
		myTexture = gameObject.GetComponent<MeshRenderer> ().material.mainTexture;
	}
	
	void Update ()
	{
		int newWidth = Screen.width;
		int newHeight = Screen.height;

		// Should we update
		if ((width != newWidth) || (height != newHeight)) 
		{
			mainCam.orthographicSize = newHeight / 2;
			float aspect = mainCam.aspect;
			float texAspect = myTexture.width / myTexture.height;
			if (texAspect < aspect) {
				myTransform.localScale = new Vector3 (newWidth, 0f, newWidth / texAspect);
			} else {
				myTransform.localScale = new Vector3 (newHeight * texAspect, 0f, newHeight);
			}
			height = newHeight;
			width = newWidth;
		}
	}
}
