using UnityEngine;
using System.Collections;
using Ecosim;

public class Deer: Animal {
	
	public AudioClip[] clips;
	
	public Animation anim;
	public float walkSpeed = 1.0f;
	
	const string A_EAT = "grazen1";
	const string A_WALK = "loopje1";
	const string A_LOOK = "uitkijken1";
	
	// Use this for initialization
	void Start () {
//		anim = gameObject.GetComponent<Animation>();
	
		anim[A_EAT].wrapMode = WrapMode.Once;
		anim[A_LOOK].wrapMode = WrapMode.Once;
		anim[A_WALK].wrapMode = WrapMode.Loop;
		
		SetupPosition ();
		if (!AnimalMgr.self.editMode) {
			StartCoroutine(COControl());
		}
	}
	
	IEnumerator COControl() {
		CharacterController ctrl = gameObject.GetComponent<CharacterController>();
		while (true) {			
			float rnd = Random.value;
			if (rnd < 0.2f) {
				anim.Stop();
				yield return new WaitForSeconds(1f);
				AudioSource.PlayClipAtPoint(clips[Random.Range(0, clips.Length)], transform.position);
				yield return new WaitForSeconds(5f);
			}
			else if (rnd < 0.6f) {
				anim.Play(A_EAT);
				yield return new WaitForSeconds(anim[A_EAT].length);
			}
			else if (rnd < 0.4f) {
				anim.Play(A_LOOK);
				yield return new WaitForSeconds(anim[A_LOOK].length);
			}
			else {
				transform.localRotation = Quaternion.Euler (0f, Random.Range (0f, 360f), 0f);
				anim.Play(A_WALK);
				float dur = Random.Range(4f, 8f);
				while (dur > 0) {
					dur -= Time.deltaTime;
					ctrl.SimpleMove(walkSpeed * transform.forward - 0.005f * Vector3.up);
					yield return 0;
				}
				if (transform.position.y < -10f) {
					SetupPosition ();
				}
				else {
					CommitPosition ();
				}
			}
		}
	}	
}
