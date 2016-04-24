using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NPCManager : MonoBehaviour {

    public Dictionary<int, GameObject> npcList;
    private Dictionary<int, int> assignedList;

    public static NPCManager Instance;

	public void Start () {
        Instance = this;
    }

    public void assignNPC (int npc, int player)
    {
        if (assignedList.ContainsKey(npc)) {
            assignedList[npc] = player;
        } else {
            assignedList.Add(npc, player);
        }
    }

    public void prepareRespawn (NPC npc) {
        StartCoroutine(respawn(npc));
    }

    public IEnumerator respawn (NPC npc)
    {
        // wait before disappear so players can loot it.
        yield return new WaitForSeconds(10f);

        npc.namePlate.gameObject.SetActive(false);
        npc.gameObject.SetActive(false);

        yield return new WaitForSeconds(3f);

        npc.namePlate.gameObject.SetActive(true);
        npc.gameObject.SetActive(true);
        npc.reset();
    }
}
