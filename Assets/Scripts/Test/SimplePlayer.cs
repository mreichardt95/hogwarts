using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SimplePlayer : NetworkBehaviour
{
    public void Start () {
        Debug.Log("Ready");
    }
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            RpcTest();
        }
        if (Input.GetKeyDown(KeyCode.Mouse1)) {
            CmdTest();
        }
    }

    [ClientRpc]
    public void RpcTest() {
        Debug.Log("ClientRpc received");
    }

    [Command]
    public void CmdTest() {
        Debug.Log("Command received");
    }
}
