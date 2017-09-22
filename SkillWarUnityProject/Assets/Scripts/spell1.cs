using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spell1 : MonoBehaviour {

	Transform enemyTransform;
	Transform meTransform;
	public bool isMine = false;
	// Use this for initialization
	void Start () {
		enemyTransform = GameObject.Find("Enemy").GetComponent<Transform> ();
		meTransform = GameObject.Find("Me").GetComponent<Transform> ();
		StartCoroutine (spellMovement ());
	}

	IEnumerator spellMovement() {
		if (this.transform.rotation.eulerAngles.y == 0) {
			Debug.Log ("aaaaa");
			this.transform.position = new Vector3 (this.transform.position.x, this.transform.position.y, this.transform.position.z + 1);
		} else if (this.transform.rotation.eulerAngles.y == 90) {
			Debug.Log ("bbbbb");
			this.transform.position = new Vector3 (this.transform.position.x + 1, this.transform.position.y, this.transform.position.z);
		} else if (this.transform.rotation.eulerAngles.y == 180) {
			Debug.Log ("ccccc");
			this.transform.position = new Vector3 (this.transform.position.x, this.transform.position.y, this.transform.position.z - 1);
		} else if (this.transform.rotation.eulerAngles.y == 270) {
			Debug.Log ("ddddd");
			this.transform.position = new Vector3 (this.transform.position.x - 1, this.transform.position.y, this.transform.position.z);
		}
		if (this.transform.position.z > 19 || this.transform.position.x > 33 || this.transform.position.x < 0 || this.transform.position.z < 0) {
			Destroy (this.gameObject);
		} else if (isMine) {
			if (this.transform.position.x - 0.5f == enemyTransform.position.x && this.transform.position.z - 0.5f == enemyTransform.position.z) {
				enemyTransform.Find("Player").GetComponent<Renderer> ().material.color = Color.red;
				Destroy (this.gameObject);
			}
		} else if (this.transform.position.x - 0.5f == meTransform.position.x && this.transform.position.z - 0.5f == meTransform.position.z) {
			meTransform.Find("Player").GetComponent<Renderer> ().material.color = Color.red;
			Destroy (this.gameObject);
		}
		yield return new WaitForSeconds(0.2f);
		StartCoroutine (spellMovement ());
	}
}
