using UnityEngine;
using System.Collections;

public class CineDoorTrigger : MonoBehaviour {

	public bool shouldCheck;

	void Start () {
		shouldCheck = true;
	}

	void Update () {
	
	}

	IEnumerator shouldCheckDoor()
	{
		shouldCheck = false;
		yield return new WaitForSeconds(2);
		shouldCheck = true;
	}

	void OnTriggerEnter(Collider other) 
	{
		if(shouldCheck)
		{
			int random = Random.Range(0, 20);
			Debug.Log(random);
			if(random > 15)
				NetworkManager.instance.Client_OpenDoor(transform.parent.gameObject);
			StartCoroutine(shouldCheckDoor());
		}
	}
}
