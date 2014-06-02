using UnityEngine;
using System.Collections;
using Ecosim;

public class IntroScreen : MonoBehaviour
{
	private bool isStarted = false;
	public Texture2D banner;
	public Texture2D info;
	public Transform spinnerT;
	
	public GUIStyle boxStyle;
	
	void Awake ()
	{
		Debug.LogWarning ("Build version: " + GameSettings.VERSION_STR);
	}

	void OnGUI ()
	{
		int hheight = Screen.height / 2;
		int hwidth = Screen.width / 2;
		GUI.Label (new Rect (hwidth - 310, hheight + 33 * -3 - 154, 620, 153), banner, GUIStyle.none);
		GUI.Label (new Rect (hwidth - 310, hheight + 33 * -3, 620, 32), isStarted?"Loading...":"Press any key...", boxStyle);
		GUI.Label (new Rect (hwidth - info.width / 2, hheight + 33 * 0, info.width, info.height), info, GUIStyle.none);
		
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
