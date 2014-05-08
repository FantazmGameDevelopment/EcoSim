using UnityEngine;
using System.Collections;

public class Goose : MonoBehaviour {
	
	public AudioSource sound;
	public Renderer myRenderer;
	
	const string A_FLY = "vlucht_2";
	// Use this for initialization
	void Start () {
		StartCoroutine(COSound());
		Animation anim = gameObject.GetComponentInChildren<Animation>();
		anim[A_FLY].wrapMode = WrapMode.Loop;
		anim.Play(A_FLY);
		anim[A_FLY].speed = Random.Range(0.9f, 1.1f);
		anim[A_FLY].time = Random.Range(0f, 2f);
	}
	
	IEnumerator COSound() {
		while (gameObject) {
			sound.Play();
			yield return new WaitForSeconds(Random.Range(4.0f, 8.0f));
		}
	}
}
