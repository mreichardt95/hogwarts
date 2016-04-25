using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

public class SimplePlayer : NetworkBehaviour
{
	[SyncVar]
	public int hp = 100;

    public void Start () {
        Debug.Log("Ready");
    }
	void Update ()
    {
		if (!isLocalPlayer)
			return;

        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            RpcTest();
        }
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            CmdTest(10);
        }
    }

    [ClientRpc]
	public void RpcTest() {
		GameObject.Find ("Canvas/Text").GetComponent<Text> ().text += "P" + gameObject.GetComponent<NetworkIdentity>().netId + " sent ClientRPC\n";
    }

    [Command]
	public void CmdTest(int n) {
		GameObject.Find ("Canvas/Text").GetComponent<Text> ().text += "P" + gameObject.GetComponent<NetworkIdentity>().netId + " sent Command\n";
		hp -= n;
    }
}
