﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour {

	public string PlayerName;
	public string MatchName;
	public int maxplayers;
	public string servername;

	public static NetworkManager instance;

	public List<Player> PlayerList = new List<Player>();
	public Player myPlayer;

	public GameObject SpawnPlayer;

	public bool matchStarted;
	public bool MatchLoaded;

	public Level curLevel;
	public List<Level> listOfLevels = new List<Level>();

	public List<CineDoorHolder> cineDoors = new List<CineDoorHolder>();

	public List<LighterHolder> lighters = new List<LighterHolder>();

	void Awake()
	{
		instance = this;
		PlayerName = PlayerPrefs.GetString("username");
	}

	void Start () {
		DontDestroyOnLoad(gameObject);
	}

	void Update () {
	
	}

	public void StartServer(string serverName)
	{
		Network.InitializeSecurity();
		Network.InitializeServer(4, 5698, false);
		MasterServer.RegisterHost("HorrorGame", serverName, "");
		Debug.Log("Started Server");
		servername = serverName;
		maxplayers = 4;
	}

	void OnPlayerConnected(NetworkPlayer id)
	{
		foreach(Player pl in PlayerList)
		{
			networkView.RPC ("Client_PlayerJoined", id, pl.PlayerName, pl.OnlinePlayer);
		}
	}

	void OnConnectedToServer()
	{
		networkView.RPC("Server_PlayerJoined", RPCMode.Server, PlayerName, Network.player);
	}

	void OnServerInitialized()
	{
		Server_PlayerJoined (PlayerName, Network.player);
	}

	void OnPlayerDisconnected(NetworkPlayer id)
	{
		networkView.RPC("RemovePlayer", RPCMode.All, id);
		Network.Destroy (getPlayer(id).pController.gameObject);
		Network.RemoveRPCs(id);
	}

	void OnDisconnectedFromServer(NetworkDisconnection Info)
	{
		foreach(Player pl in PlayerList)
		{
			Network.Destroy(pl.pController.gameObject);
		}
		PlayerList.Clear ();
		Application.LoadLevel(0);
	}

	[RPC]
	public void Server_PlayerJoined(string Username, NetworkPlayer id)
	{
		networkView.RPC("Client_PlayerJoined", RPCMode.All, Username, id);
	}

	[RPC]
	public void Client_PlayerJoined(string Username, NetworkPlayer id)
	{
		Player pTemp = new Player();
		pTemp.PlayerName = Username;
		pTemp.OnlinePlayer = id;
		PlayerList.Add(pTemp);

		if(Network.player == id)
		{
			myPlayer = pTemp;
			GameObject LastPlayer = Network.Instantiate(SpawnPlayer, Vector3.zero, Quaternion.identity, 0) as GameObject;
			LastPlayer.networkView.RPC("RequestPlayer", RPCMode.AllBuffered, Username);
		}
	}

	[RPC]
	public void RemovePlayer(NetworkPlayer id)
	{
		Player temp = new Player();
		foreach(Player pl in PlayerList)
		{
			if(pl.OnlinePlayer == id)
			{
				temp = pl;
			}
		}
		if(temp != null)
		{
			PlayerList.Remove (temp);
		}
	}

	[RPC]
	public void RemoveAllPlayers()
	{
		Player temp = new Player();
		foreach(Player pl in PlayerList)
		{
			PlayerList.Remove (pl);
			print ("Player: " + pl.PlayerName + " Was Removed");
			Network.Destroy(pl.pController.gameObject);
			MasterServer.UnregisterHost();
			Application.LoadLevel(1);
		}
	}

	[RPC]
	public void LoadLevel(string loadName)
	{
		matchStarted = true;
		foreach(Player pl in PlayerList)
		{
			pl.pController.client_giveLighter();
		}
		Application.LoadLevel(1);
	}

	[RPC]
	public Level getLevel(string LoadName, bool isStarted)
	{
		foreach(Level lvl in listOfLevels)
		{
			if(LoadName == lvl.LoadName)
			{
				curLevel = lvl;
				return lvl;
			}
		}
		if(isStarted)
		{
			LoadLevel(LoadName);
		}

		return null;
	}

	public static Player getPlayer(NetworkPlayer id)
	{
		foreach(Player pl in instance.PlayerList)
		{
			if(pl.OnlinePlayer == id)
			{
				return pl;
			}
		}
		return null;
	}

	public static bool HasPlayer(string n)
	{
		foreach(Player pl in instance.PlayerList)
		{
			if(pl.PlayerName == n)
			{
				return true;
			}
		}
		return false;
	}
	
	public static Player getPlayer(string id)
	{
		foreach(Player pl in instance.PlayerList)
		{
			if(pl.PlayerName == id)
			{
				return pl;
			}
		}
		return null;
	}

	void OnGUI()
	{
		if(matchStarted == true && !myPlayer.isAlive)
		{
			if(GUI.Button(new Rect(Screen.width - 50, 0, 50, 20), "Spawn"))
			{
				int SpawnIndex = Random.Range(0, LevelManager.instance.SpawnPoints.Length - 1);
				myPlayer.pController.FirstPerson.localPosition = LevelManager.instance.SpawnPoints[SpawnIndex].transform.position;
				myPlayer.pController.FirstPerson.localRotation = LevelManager.instance.SpawnPoints[SpawnIndex].transform.rotation;
				myPlayer.pController.networkView.RPC ("Spawn", RPCMode.All);
			}
		}
	}

	public void Client_OpenDoor(GameObject go)
	{
		int id = -1;
		Debug.Log (cineDoors.Count);
		for(int i = 0; i < cineDoors.Count - 1; i++)
		{
			Debug.Log (i);
			if(cineDoors[i].door == go)
				id = i;
		}
		if(id != -1)
			networkView.RPC("Server_OpenDoor", RPCMode.All, id);
	}

	[RPC]
	void Server_OpenDoor(int id)
	{
		//if(networkView.isMine)
		//{
			cineDoors[id].cineDoor.openDoor();
		//}
	}

	public void client_switchlight(string playerName, bool on)
	{
		int id = -1;
		for(int i = 0; i < lighters.Count; i++)
		{
			if(lighters[i].PlayersName == playerName)
			{
				id = i;
				print (playerName);
			}
		}
		if(id!=-1)
			networkView.RPC("Server_SwitchLighter", RPCMode.All, id, on, playerName);

	}

	[RPC]
	public void Server_SwitchLighter(int id, bool on, string pname)
	{
		print (id);
		if(pname == lighters[id].PlayersName)
			lighters[id].lighter.gameObject.SetActive(on);

	}

	public void client_addlighter(string pn, GameObject go)
	{
		int id = -1;
		for(int i = 0; i < lighters.Count - 1; i++)
		{
			if(lighters[i].lighter == go)
				id = i;
		}

		if(id != -1)
			networkView.RPC("server_addlighter", RPCMode.All, pn, id);
	}

	[RPC]
	void server_addlighter(string pn, int i)
	{
		lighters[i].PlayersName = pn;
	}
}

[System.Serializable]
public class Player
{
	public string PlayerName;
	public NetworkPlayer OnlinePlayer;
	public float health = 100;
	public PlayerController pController;
	public bool isAlive;
	public int deaths;
}

[System.Serializable]
public class Level
{
	public string LoadName;
	public string PlayName;
}

[System.Serializable]
public class CineDoorHolder
{
	public GameObject door;
	public CineDoor cineDoor;

	//void Start()
	//{
		//cineDoor = door.GetComponent<CineDoor>();
	//}
}

[System.Serializable]
public class LighterHolder
{
	public string PlayersName;
	public GameObject lighter;
}