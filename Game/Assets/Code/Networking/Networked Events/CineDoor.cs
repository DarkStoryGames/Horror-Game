using UnityEngine;
using System.Collections;

public class CineDoor : MonoBehaviour {

	public GameObject door;
	public bool opened;

	void Start () {
		CineDoorHolder cdh = new CineDoorHolder();
		cdh.cineDoor = this;
		cdh.door = gameObject;
		NetworkManager.instance.cineDoors.Add (cdh);
		opened = false;
	}

	void Update () {
	
	}

	public void openDoor()
	{
		if(!opened)
		{
			door.animation.Play("Open");
			opened = true;
		}
	}


}
