using UnityEngine;
using System.Collections;

public class LevelManager : MonoBehaviour {

	public static LevelManager instance;

	public GameObject[] SpawnPoints;

	void Start () {
		instance = this;
		SpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
	}

	void Update () {
	
	}
}
