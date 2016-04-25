﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/*
This NPC brain is based on one-to-many way, which means that players set themself as target.
*/

public class NPC : Photon.MonoBehaviour
{
	public int Id;

	public int health
	{
		get {return data.health;}
		set {
			//prevent negative health values
			if (value <  0) {
				value = 0;
			}
			
			data.health = value;

			// @ToDo: Update UI bar
		}
	}
	public int maxHealth;
	public new string name
	{
		get {return data.name;}
	}

	public List<WaypointData> waypoints
	{
		get {return data.waypoints;}
	}

	public bool isFriendly {
		get {
			return !data.isAggresive;
		}
	}

	public GameObject projectilePrefab;
	public AnimationClip idleAnimation;
	public AnimationClip runAnimation;
	public AnimationClip attackAnimation;
	public AnimationClip deathAnimation;
	public Animation anim;
	public bool check = false;
	public bool resetDps = false;
	public bool stunned = false;
	public float SmoothingDelay = 5;

	private bool isRanged;
	private float timeSinceLastAttack;
	private bool inCombat;
	private bool isDead {
        get {
            if (health < 1) {
                return true;
            }
            return false;
        }
    }
	private bool EnableCombat;
	private bool isUseless = false;
	private bool isStunned = false;
	private float amountSlowedBy;
	private bool isKnockedBack = false;
	private bool isAttacking = false;
	private NPCData data;
	private GameObject target;
	private Vector3 initialPos;
	private float distanceFromIPos = 0;
	private float OriginalAttacksPerSecond;
	private bool backToIPos = false;
	private int currentWaypoint = 0;
	private float curTime = 0;
	private float pauseDuration = 2;
	private Vector3 correctPlayerPos = Vector3.zero;
	private Quaternion correctPlayerRot = Quaternion.identity;
	private bool gotFirstUpdate = false;
    private bool killNotiSent = false;
    private bool isLooted = false;
    private Dictionary<Player, int> attackers = new Dictionary<Player, int>();
    private Vector3 originalPosition;

    public NamePlate namePlate;
	
	public void Start()
	{
		if (Id == 0) {
			throw new Exception ("Id not assigned");
		}
		data = NPC.get (Id);
		Color color;

		try {
			anim = transform.FindChild("Model").GetComponent<Animation> ();
		} catch (Exception) {}

		this.OriginalAttacksPerSecond = data.attacksPerSecond;
		this.initialPos = this.transform.position;
		this.maxHealth = this.health;

		if (data.isAggresive) {
			color = NamePlate.COLOR_ENEMY;
		} else {
			color = NamePlate.COLOR_NORMAL;
		}
		namePlate.setName (data.name, color);
		namePlate.setLevel (data.level);
        originalPosition = transform.position;
    }
	
	private void Update()
	{
        if (isDead)
        {
            if (!killNotiSent && photonView.isMine)
            {
                namePlate.health.fillAmount = 0;

                foreach (KeyValuePair<Player, int> entry in attackers) {
                    entry.Key.photonView.RPC("addKill", entry.Key.photonView.owner, data.id, Task.ActorType.NPC, data.level, entry.Value, data.health, data.expValue, data.template);
                }
                anim.Play(this.deathAnimation.name);
                NPCManager.Instance.prepareRespawn(this);
                killNotiSent = true;
            }
            return;
        }

		// static NPCs like vendors, dont have photonView as they dont perfom any action
        if (!photonView) {
            return;
        }

        if (!photonView.isMine && gotFirstUpdate) {
			transform.position = Vector3.Lerp(transform.position, correctPlayerPos, Time.deltaTime * this.SmoothingDelay);
			transform.rotation = Quaternion.Lerp(transform.rotation, correctPlayerRot, Time.deltaTime * this.SmoothingDelay);
		}

        if (Application.isEditor) {
			if (data.subRace != NPCData.creatureSubRace.Normal) {return;} // enable in debug to not verifiy if you are the master
		} else {
			if (data.subRace != NPCData.creatureSubRace.Normal || !photonView.isMine) {return;}
		}

        // check if target has died
        if (target && target.GetComponent<Player>().isDead) {
            EnableCombat = false;
            target = null;
        }

        if (this.EnableCombat) {
			this.timeSinceLastAttack += Time.deltaTime;
		}
		
		float num = 0;
		distanceFromIPos = Vector3.Distance(this.transform.position, this.initialPos);

		if (isTooFar()) {
			backToIPos = true;
		} else if (this.transform.position == initialPos) {
			backToIPos = false;
		}

		if (target != null) {
			num = Vector3.Distance(this.transform.position, target.transform.position);

			if (this.inCombat && !isTooFar() || num < data.distanceToLoseAggro && data.isAggresive && !isTooFar()) {
				this.EnableCombat = true;
			} else {
				this.EnableCombat = false;
				this.inCombat = false;
				this.target = null;
			}
		}

		if (this.isKnockedBack)
		{
			Transform transform = this.transform;
			Vector3 vector3 = transform.position + (this.target.transform.forward * 15f * Time.deltaTime);
			transform.position = vector3;
		}
		else
		{
			if (this.isStunned) {
				this.EnableCombat = false;
			}
					
			if (this.isUseless) {
				return;
			}
					
			if (this.EnableCombat)
			{
				this.transform.eulerAngles = new Vector3(0.0f, Mathf.Atan2((this.target.transform.position.x - this.transform.position.x), (this.target.transform.position.z - this.transform.position.z)) * 57.29578f, 0.0f);
				if (Vector3.Distance(this.transform.position, this.target.transform.position) > data.attackRange)
				{
					anim.Play(this.runAnimation.name);
					transform.position = Vector3.MoveTowards(this.transform.position, this.target.transform.position, data.runSpeed * Time.deltaTime);
				}
				if (this.timeSinceLastAttack > 1.0 / data.attacksPerSecond && !this.isAttacking && num < data.attackRange)
				{
					this.timeSinceLastAttack = 0.0f;
					anim.Play(this.attackAnimation.name);
					this.target.GetComponent<Player>().photonView.RPC("getDamage", this.target.GetComponent<Player>().photonView.owner, data.damage, photonView.viewID);
				}
				else
				{
					if (anim.isPlaying) {
						return;
					}
					anim.Play(this.idleAnimation.name);
				}
			}
			else {
				if (backToIPos) {
					gotoInitialPoint();
				} else {

					if (waypoints.Count == 0) {
						anim.Play(this.idleAnimation.name);
					} else {
						// run points in loop
						if (currentWaypoint < waypoints.Count) {
							followPoint ();
						} else {
							currentWaypoint = 0;
						}
					}
				}
			}
					
		}
		
        if (health != maxHealth) {
            namePlate.health.fillAmount = Mathf.Lerp(namePlate.health.fillAmount, health / (float)maxHealth, 4f * Time.deltaTime);
        }
	}
	
	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (photonView.isMine)
		{
			//We own this player: send the others our data
			stream.SendNext(transform.position);
			stream.SendNext(transform.rotation); 
		}
		else
		{
			//Network player, receive data
			correctPlayerPos = (Vector3)stream.ReceiveNext();
			correctPlayerRot = (Quaternion)stream.ReceiveNext();

			if (!gotFirstUpdate) {
				transform.position = correctPlayerPos;
				transform.rotation = correctPlayerRot;
				gotFirstUpdate = true;
			}
		}
	}

	public bool isTooFar () {
		if (distanceFromIPos >= data.distanceToLoseAggro) {
			return true;
		}
		return false;
	}

    public void reset ()
    {
        health = maxHealth;
        namePlate.health.fillAmount = health;
        target = null;
        EnableCombat = false;

        transform.position = originalPosition;
        currentWaypoint = 0;
        attackers = new Dictionary<Player, int>();
        isLooted = false;
		killNotiSent = false;
    }

	/**
	 * Points NPC back to initial position
	 *
	 */
	public void gotoInitialPoint ()
    {
		anim.Play(this.runAnimation.name);
		this.transform.position = Vector3.MoveTowards(this.transform.position, this.initialPos, data.runSpeed * Time.deltaTime);
		this.transform.eulerAngles = new Vector3(0.0f, Mathf.Atan2((this.initialPos.x - this.transform.position.x), (this.initialPos.z - this.transform.position.z)) * 57.29578f, 0.0f);

		if (this.health < this.maxHealth) {
			StartCoroutine (restoreHealth());
		}
	}

	public IEnumerator TakeDamageByFlagType(Spell spell, GameObject player)
	{
		if(spell.spellFlag == Spell.SpellFlag.Slow)
		{
			/*
			 * @ToDo: lower NPC speed
			originalRunSpeed = data.runSpeed;
			originalWalkSpeed = data.walkSpeed;
			data.runSpeed = 1.5f;
			data.walkSpeed = 0.5f;

			yield return new WaitForSeconds(spell.slowDuration);
			cont.runSpeed = originalRunSpeed;
			cont.walkSpeed = originalWalkSpeed;
			yield break;
			*/
			Debug.Log("Slowed");
			
		}
		
		else if(spell.spellFlag == Spell.SpellFlag.DamagePerSecond)
		{
			if(resetDps && check){
				check = false;
				resetDps = false;
				StopAllCoroutines();
			}
			
			if(!check)
				StartCoroutine(DOT(spell.dotDamage, spell.dotTick, spell.dotSeconds, spell.dotEffect, player));
			
		}
		
		else
		{
			Debug.Log("don't have spell flag.");
			yield break;
		}
		
	}
	
	public IEnumerator DOT (int damage, int over, int time, GameObject dotEffect, GameObject player)
	{
		int count = 0;
		check = true;
		
		
		while (count < over) {
			yield return new WaitForSeconds(time);
			getHit(damage, player, true);
			PhotonNetwork.Instantiate("Particles/" + dotEffect.name, transform.position, Quaternion.identity, 0);
			count ++;
		}
		
		check = false;
	}

	public void getHit (int damage, GameObject attacker, bool isDPS = false)
    {
		// @ToDo: start a hit animation
		health-= damage;

        Player player = attacker.GetComponent<Player>();
        if (!attackers.ContainsKey(player)) {
            attackers.Add(player, 0);
        }

        attackers[player] += damage;

        if (!target) {
            target = attacker;
        }
		// show our numeric damage in UI
        if (player.isMine) {
            namePlate.setDamage(damage, isDPS);
        }
    }

	public IEnumerator restoreHealth () {
		while (this.health < this.maxHealth) {
			this.health += 10;

			if (this.health > this.maxHealth) {
				this.health = this.maxHealth;
			}

			yield return new WaitForSeconds(1);
		}
	}

	/**
	 * Set NPC target
	 */
	public void setTarget (GameObject gameObject) {

		// trying to set an invalid target?
		if (gameObject.tag != "Player" && gameObject.tag != "NPC" || target != null) {
			return;
		}
		// @ToDo: raycast on target direction to see if we can see it (is behind a wall?, etc..)

		float num = Vector3.Distance(this.transform.position, gameObject.transform.position);

		if ((double) num < data.distanceToLoseAggro && data.isAggresive && !isTooFar()) {
			target = gameObject;
		}
	}

	// Sets this NPC as player's target
	public void setSelected (bool force = false) {

		if (!force && Player.Instance.target != null) {
			return;
		}

		PlayerHotkeys.isClickingATarget = true;
		if (data.isAggresive) {
			Player.Instance.target = this;
		} else {
			namePlate.Name.color = NamePlate.COLOR_SELECTED;
		}
	}

	public void OnMouseDown() {
		setSelected (true);

        if (!data.isAggresive) {
            QuestManager.Instance.sendAction(data.id, Task.ActorType.NPC, Task.ActionType.Talk, 0, data.template);
        }
    }

	public void OnMouseOver () {
		Texture2D texture = null;

        if (isDead && !isLooted) {
            texture = GameCursor.Buy;
        } else if (data.isAggresive) {
			texture = GameCursor.Attack;
		} else {
            switch (data.subRace)
            {
                case NPCData.creatureSubRace.Seller:
                    texture = GameCursor.Buy;
                    break;
                case NPCData.creatureSubRace.Quest:
                    texture = GameCursor.QuestAvailable;
                    break;
            }
        }

		if (texture != null) {
			Cursor.SetCursor(texture, Vector2.zero, CursorMode.Auto);
		}
	}
	public void OnMouseEnter () {
		OnMouseOver();
	}
	public void OnMouseExit () {
		Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
	}

	public static NPCData get (int id) {
		return Service.db.SelectKey<NPCData> ("npc", id);
	}
	
	public void KnockBackSelf()
	{
		if (this == null)
			return;
		this.StartCoroutine("KnockBack");
	}

	private IEnumerator KnockBack()
	{
		yield return new WaitForSeconds(1);
	}
	
	public void StunSelf(float timeToStunSelf)
	{
		this.StartCoroutine("Stun", (object) timeToStunSelf);
	}

	private IEnumerator Stun(float timeToStun)
	{
		yield return new WaitForSeconds(1);
	}
	
	public void SlowAttackSpeed(float amountToSlow)
	{
		data.attacksPerSecond = this.OriginalAttacksPerSecond;
		this.StartCoroutine("Slow", (object) amountToSlow);
	}

	private IEnumerator Slow(float amountToReduceBy)
	{
		yield return new WaitForSeconds(1);
	}

	void followPoint ()
    {	
		Vector3 target = waypoints[currentWaypoint].position;
		Vector3 moveDirection = initialPos + target; // sum the relative pos

		if (moveDirection.magnitude < 0.5f || moveDirection == this.transform.position) {
			if (curTime == 0) {
				curTime = Time.time; // Pause over the Waypoint
			}
			if ((Time.time - curTime) >= pauseDuration) {
				currentWaypoint++;
				curTime = 0;
			}
			anim.Play(this.idleAnimation.name);
		} else {
			anim.Play(this.runAnimation.name);

            this.transform.position = Vector3.MoveTowards(this.transform.position, moveDirection, data.runSpeed * Time.deltaTime);
			this.transform.eulerAngles = new Vector3(0.0f, Mathf.Atan2((moveDirection.x - this.transform.position.x), (moveDirection.z - this.transform.position.z)) * 57.29578f, 0.0f);

		}    
	}

}
public class ItemSpawner {}