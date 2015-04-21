using UnityEngine;
using System.Collections;
using Ecosim;

public class Cow: Animal {
	
	public AudioClip[] clips;
	
	public Animation anim;
	public float walkSpeed = 1.0f;

	public bool useAnimLengthForWait = false;
	
	const string A_EAT = "grazen";
	const string A_CHEW = "herkauw";
	const string A_WALK = "loopje";
		
	// Use this for initialization
	void Start () {
//		anim = gameObject.GetComponent<Animation>();
	
		anim[A_EAT].wrapMode = WrapMode.Once;
		anim[A_CHEW].wrapMode = WrapMode.Once;
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
				anim.Play(A_CHEW);
				yield return new WaitForSeconds(1f);
				AudioSource.PlayClipAtPoint(clips[Random.Range(0, clips.Length)], transform.position);
				if (this.useAnimLengthForWait) {
					yield return new WaitForSeconds(anim[A_CHEW].length - 1f);
				} else {
					yield return new WaitForSeconds(5f);
				}
			}
			else if (walkSpeed <= 0 || rnd < 0.6f) {
				anim.Play(A_EAT);
				yield return new WaitForSeconds(anim[A_EAT].length);
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
					SetupPosition();
				}
				else {
					CommitPosition();
				}
			}
		}
	}	
}
