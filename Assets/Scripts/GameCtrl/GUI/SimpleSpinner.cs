using UnityEngine;
using System.Collections;

public class SimpleSpinner : MonoBehaviour {

	float angle = 0f;
	public RotatableGuiItem guiItem;
	private static SimpleSpinner self;
	
	public static void ActivateSpinner () {
		self.gameObject.SetActive (true);
	}
	
	public static void DeactivateSpinner () {
		self.gameObject.SetActive (false);
	}
	
	public static bool SpinnerIsActive () {
		return self.gameObject.activeSelf;
	}
	
	void Awake () {
		self = this;
		gameObject.SetActive (false);
	}
	
	void OnDestroy () {
		self = null;
	}
	
	// Update is called once per frame
	void Update () {
		angle = Mathf.Repeat(angle + Time.deltaTime * 18f, 12f);
		guiItem.angle = Mathf.Round(angle) * 30;
		
	}
}
