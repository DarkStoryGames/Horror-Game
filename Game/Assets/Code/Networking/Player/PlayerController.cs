using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour {

	public Transform FirstPerson;
	public Transform ThirdPerson;
	public GameObject deadRag;

	public CharacterController Charcont;
	public CharacterMotor CharMotor;

	public Player myPlayer;

	public Vector3 CurPos;
	public Quaternion CurRot;
	public Vector3 CurLocPos;
	public Quaternion CurLocRot;

	public WalkingState walkingstate = WalkingState.Idle;

	public float WalkSpeed;
	public float RunSpeed;
	public float VelocityMagnitude;

	public GameObject lighter;
	public GameObject fpLighter;

	public bool lighterOn;

	void Start () 
	{
		if(networkView.isMine)
		{
			myPlayer = NetworkManager.getPlayer(networkView.owner);
			myPlayer.pController = this;
		}

		FirstPerson.gameObject.SetActive(false);
		ThirdPerson.gameObject.SetActive(false);
		DontDestroyOnLoad(gameObject);

		lighterOn = true;
	}

	[RPC]
	public void RequestPlayer(string Nameee)
	{
		networkView.RPC("GiveMyPlayer", RPCMode.OthersBuffered, Nameee);
	}
	
	[RPC]
	public void GiveMyPlayer(string n)
	{
		StartCoroutine(GivePlayer(n));
	}
	
	IEnumerator GivePlayer(string nn)
	{
		while(!NetworkManager.HasPlayer(nn))
		{
			yield return new WaitForEndOfFrame();
		}
		myPlayer = NetworkManager.getPlayer(nn);
		myPlayer.pController = this;
	}

	void Update () 
	{
		if(Input.GetKey(KeyCode.Keypad9))
			Suicide();
	}

	void FixedUpdate()
	{
		SpeedController();
		//Animationcontroller();
		VelocityMagnitude = Charcont.velocity.magnitude;

		CheckForLighter();
	}

	public void SpeedController()
	{
		if((Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0) && VelocityMagnitude > 0)
		{
			if(Input.GetButton("Sprint"))
			{
				walkingstate = WalkingState.Running;
				CharMotor.movement.maxForwardSpeed = RunSpeed;
				CharMotor.movement.maxSidewaysSpeed = RunSpeed;
				CharMotor.movement.maxBackwardsSpeed = RunSpeed / 2;
			}
			else
			{
				walkingstate = WalkingState.Walking;
				CharMotor.movement.maxForwardSpeed = WalkSpeed;
				CharMotor.movement.maxSidewaysSpeed = WalkSpeed;
				CharMotor.movement.maxBackwardsSpeed = WalkSpeed / 2;
			}
		}
		else
			walkingstate = WalkingState.Idle;
	}

	public void Suicide()
	{
		networkView.RPC ("Die", RPCMode.All);
		myPlayer.deaths ++;
		myPlayer.isAlive = false;
		myPlayer.health = 0;
	}

	[RPC]
	void Server_TakeDamage(float Damage)
	{
		networkView.RPC ("Client_TakeDamage", RPCMode.Server, Damage);
	}
	
	[RPC]
	void Client_TakeDamage(float Damage)
	{
		myPlayer.health -= Damage;
		if(myPlayer.health <= 0)
		{
			Network.Instantiate(deadRag, FirstPerson.position, FirstPerson.rotation, 0);
			networkView.RPC ("Die", RPCMode.All);
			myPlayer.deaths ++;
			myPlayer.isAlive = false;
			myPlayer.health = 0;
		}
	}

	[RPC]
	void Spawn()
	{
		if(NetworkManager.instance.matchStarted == true)
		{
			myPlayer.isAlive = true;
			myPlayer.health = 100;
			if(networkView.isMine)
			{
				FirstPerson.gameObject.SetActive(true);
				ThirdPerson.gameObject.SetActive(false);
			}
			else
			{
				FirstPerson.gameObject.SetActive(false);
				ThirdPerson.gameObject.SetActive(true);
			}
		}
	}
	
	[RPC]
	void Die()
	{
		myPlayer.isAlive = false;
		FirstPerson.gameObject.SetActive (false);
		ThirdPerson.gameObject.SetActive(false);
	}
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		if(stream.isWriting)
		{
			CurPos = Vector3.Lerp(CurPos, FirstPerson.position, 0.2f);
			CurRot = Quaternion.Lerp(CurRot, FirstPerson.rotation, 0.2f);
			stream.Serialize(ref CurPos);
			stream.Serialize(ref CurRot);
			//char Ani = (char)GetComponent<NetworkAnimStates>().CurrentAnim;
			//stream.Serialize(ref Ani);
		}
		else
		{
			stream.Serialize(ref CurPos);
			stream.Serialize(ref CurRot);
			Vector3 newpostogive = new Vector3(CurPos.x, CurPos.y, CurPos.z);
			ThirdPerson.position = newpostogive;
			ThirdPerson.rotation = CurRot;
			//char Ani = (char)0;
			//stream.Serialize(ref Ani);
			//GetComponent<NetworkAnimStates>().CurrentAnim = (Animations)Ani;
		}
	}

	void CheckForLighter()
	{
		if(networkView.isMine)
		{
			if(Input.GetMouseButtonDown(0))
		   		Client_SwitchLighter();
		}
	}

	void Client_SwitchLighter()
	{
		lighterOn = !lighterOn;
		fpLighter.gameObject.SetActive(lighterOn);
		NetworkManager.instance.client_switchlight(myPlayer.PlayerName, lighterOn);
	}

	public void client_giveLighter()
	{
		LighterHolder lh = new LighterHolder();
		lh.PlayersName = myPlayer.PlayerName;
		lh.lighter = lighter;
		NetworkManager.instance.lighters.Add(lh);
		NetworkManager.instance.client_addlighter(myPlayer.PlayerName, lighter);
	}
}

public enum WalkingState
{
	Idle,
	Walking,
	Running
}