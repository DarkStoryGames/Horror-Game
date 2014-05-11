using UnityEngine;
using System.Collections;

public class MenuManager : MonoBehaviour {

	public static MenuManager instance;
	private string CurMenu;
	public string Name;
	public string MatchName;
	public string ipToConnect;

	void Start () {
		instance = this;
		CurMenu = "Main";
		Name = PlayerPrefs.GetString ("username");
		if(Name == "")
		{
			Name = "User " + Random.Range(0, 9999);
		}
		MatchName = "My Server " + Random.Range(0, 9999);
		ipToConnect = "127.0.0.1";
	}

	void Update () {
	
	}

	void ToMenu(string menu)
	{
		CurMenu = menu;
	}

	void OnGUI()
	{
		if(CurMenu == "Main")
			Main();
		if(CurMenu == "Host")
			Host();
		if(CurMenu == "Lobby")
			Lobby();
		if(CurMenu == "FindServer")
			MatchList();
	}

	public void Main()
	{
		Name = GUI.TextField(new Rect(10, 35, 150, 20), Name);
		GUI.Label(new Rect(10, 10, 300, 20), "Username:");
		if(GUI.Button(new Rect(165, 35, 120, 20), "Set Username"))
		{
			PlayerPrefs.SetString("username", Name);
			NetworkManager.instance.PlayerName = PlayerPrefs.GetString("username");
		}
		if(GUI.Button(new Rect(10, 60, 120, 40), "Host Game"))
		{
			ToMenu ("Host");
		}
		if(GUI.Button(new Rect(10, 105, 120, 40), "Match List"))
		{
			ToMenu ("FindServer");
		}
		ipToConnect = GUI.TextField(new Rect(140, 73, 128, 20), ipToConnect);
		if(GUI.Button(new Rect(268, 73, 128, 20), "Connect"))
		{
			Network.Connect(ipToConnect, 5698);
			ToMenu ("Lobby");
		}
	}
	
	public void Host()
	{
		if(GUI.Button(new Rect(0, 0, 128, 32), "Start Game")){
			NetworkManager.instance.StartServer(MatchName);
			ToMenu("Lobby");
		}
		if(GUI.Button(new Rect(0, 33, 128, 32), "Main Menu")){
			ToMenu("Main");
		}
		MatchName = GUI.TextField(new Rect(130, 0, 128, 32), MatchName);
		GUI.Label (new Rect(260, 0, 128, 32), "Match Name");
	}
	
	public void Lobby()
	{
		if(Network.isServer)
		{
			if(GUI.Button(new Rect(Screen.width - 128, Screen.height - 64, 128, 32), "Start Match")){
				NetworkManager.instance.networkView.RPC ("LoadLevel", RPCMode.All, NetworkManager.instance.curLevel.LoadName);
			}
		}
		
		if(GUI.Button(new Rect(Screen.width - 128, Screen.height - 32, 128, 32), "Disconnect")){
			ToMenu("Main");
			Network.Disconnect();
		}
		
		GUILayout.BeginArea(new Rect(0, 0, 128, Screen.height), "Players");
		GUILayout.Space(20);
		foreach(Player pl in NetworkManager.instance.PlayerList)
		{
			GUILayout.BeginHorizontal();
			GUI.color = Color.green;
			GUILayout.Box (pl.PlayerName);
			GUI.color = Color.white;
			GUILayout.EndHorizontal();
		}
		GUILayout.EndArea();
	}
	
	public void MatchList()
	{
		if(GUI.Button(new Rect(0, 0, 128, 32), "Refresh")){
			MasterServer.RequestHostList("HorrorGame");
		}
		if(GUI.Button(new Rect(0, 33, 128, 32), "Main Menu")){
			ToMenu("Main");
		}
		
		GUILayout.BeginArea(new Rect(Screen.width / 2, 0, Screen.width / 2, Screen.height), "Server List", "box");
		foreach(HostData hd in MasterServer.PollHostList())
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(hd.gameName);
			if(GUILayout.Button ("Connect"))
			{
				Network.Connect(hd);
				ToMenu ("Lobby");
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndArea();
	}
}
