using UnityEngine;
using System.Collections;

/*
Collides with NPCs (with a FOV width sphere) to enable/disable them
*/

public class NPCActivator : MonoBehaviour {

	void OnTriggerStay(Collider col)
    {
        if (col.tag != "NPC" || col.isTrigger) {
            return;
        }
        
        // get the ownership so NPC can move
		if (col.gameObject.GetComponent<PhotonView> ().owner == null) {
			col.gameObject.GetComponent<PhotonView> ().TransferOwnership (PhotonNetwork.player);
		}

        if (col.gameObject.GetComponent<Animation>().enabled) {
            return;
        }

        col.gameObject.GetComponent<Animation> ().enabled = true;
		col.gameObject.GetComponent<NPC> ().enabled = true;
		col.gameObject.transform.FindChild ("Model").gameObject.SetActive (true);
	}

	void OnTriggerExit(Collider col)
    {
        if (col.tag != "NPC" || col.isTrigger) {
            return;
        }

        if (col.gameObject.GetComponent<PhotonView>().isMine) {
            col.gameObject.GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.masterClient);
        }
		
		col.gameObject.GetComponent<Animation> ().enabled = false;
		col.gameObject.GetComponent<NPC> ().enabled = false;
		col.gameObject.transform.FindChild ("Model").gameObject.SetActive (false);
		
	}
}
