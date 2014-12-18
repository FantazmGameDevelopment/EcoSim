using UnityEngine;
using System.Collections;
using Ecosim;

public class IntroScreen : MonoBehaviour
{
	private bool isStarted = false;
	public Texture2D banner;
	public Texture2D info;
	public Transform spinnerT;

	public int bannerOffset = 50;
	public int bannerTextOffset = 5;
	public int bannerTextXOffset = 0;
	public int bannerTextWidthOffset = 0;
	public int infoOffset = 50;

	public GUIStyle boxStyle;
	
	void Awake ()
	{
		Debug.LogWarning ("Build version: " + GameSettings.VERSION_STR);
	}

	void OnGUI ()
	{
		int hheight = Screen.height / 2;
		int hwidth = Screen.width / 2;
		GUI.Label (new Rect (hwidth - (banner.width*0.5f), bannerOffset, banner.width, banner.height), banner, GUIStyle.none);
		GUI.Label (new Rect (hwidth - (banner.width*0.5f) + bannerTextXOffset + bannerTextWidthOffset, bannerOffset + banner.height + bannerTextOffset, banner.width - (bannerTextWidthOffset*2f), 32), isStarted?"Loading...":"Press any key...", boxStyle);
		GUI.Label (new Rect (hwidth - info.width / 2, Screen.height - info.height - infoOffset, info.width, info.height), info, GUIStyle.none);
		
		if (Event.current.type == EventType.KeyDown)
		{
			StartLoad ();
		}
		
	}
	
	float angle = 0f;
	
	// Update is called once per frame
	void Update ()
	{
		if (isStarted) {
			angle = Mathf.Repeat(angle + Time.deltaTime * 18f, 12f);
			spinnerT.localRotation = Quaternion.Euler (0f, 0f, -Mathf.Round(angle) * 30);
		}
		else if (Input.GetMouseButtonDown (0))
		{
			StartLoad ();
		}
	}
	
	void StartLoad() {
		if (!isStarted) {
			StartCoroutine (COLoadGame ());
		}
	}
	
	IEnumerator COLoadGame ()
	{
		isStarted = true;
		spinnerT.gameObject.SetActive (true);
		yield return 0;
		Application.LoadLevelAsync ("Game");
	}
}
