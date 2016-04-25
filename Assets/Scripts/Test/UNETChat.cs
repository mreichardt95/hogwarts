using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UNETChat : NetworkBehaviour{

	//Chat test

	//This is the message that will be sent to the server
	[Command]
	public void CmdChat(string msg) {
		RpcMsgToAll (msg);
	}

	//The server received the command then run rpc on all clients
	[ClientRpc]
	public void RpcMsgToAll(string msg){
		GameObject.Find ("Canvas/Text").GetComponent<Text> ().text += "P" + gameObject.GetComponent<NetworkIdentity> ().netId + ": " + msg + "\n";
	}


	public void SendMsg(){
		CmdChat (GameObject.Find ("Canvas/InputField/Text").GetComponent<Text> ().text);
	}

}
