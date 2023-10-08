/*******************************************************************************
File:      EnemyLogic.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      11/11/2022
Course:    DES 214

Description:
    Handles enemy stats, movement, and aggro behavior.

*******************************************************************************/
using System;
using UnityEngine;
using static PCG;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyLogic : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Movement speed
    public float Speed = 1.0f;
    //Starting health
    public int StartingHealth = 1;
    //Distance at which the enemy will attack
    public float AggroRange = 1.0f;
    //Time between checks for wandering a random direction
    public float WanderInterval = 4.0f;
    //Time between checks for wandering a random direction
    public float WanderChance = 0.3f; //30% chance
    //Chance of dropping a heart on death
    public float DropChance = 0.35f; //35% chance
    //Chance of dropping a random boost on death
    public float BoostDropChance = 0.1f;

    //////////////////////////////////////////////////////////////////////////

    //Current health
    [HideInInspector]
    public int Health
    {
        get { return _Health; }
        set { EnemyHealthBar.Health = value; _Health = value;}
    }
    private int _Health;
    //Reference to the health bar
    private HealthBar EnemyHealthBar;

    //Current aggro state
    [HideInInspector]
    public bool Aggroed = false;
    //Minimum deaggro range, which is calculated based on
    //enemy aggro range and player range
    private float MinDeaggroRange = 0.0f; //Should always be more than the aggro range

    //Current wander state
    [HideInInspector]
    public bool Wander = false;

    //Timers
    private float Timer = 0.0f;
    private float MoveVerticalTimer = 0.0f; //Keeps enemies from jittering against walls
    private float MoveHorizontalTimer = 0.0f; //Keeps enemies from jittering against walls

    //Track the player for aggro and targeting purposes
    [HideInInspector]
    public Transform Player = null;

    //Don't do anything because a cinematic occuring
    [HideInInspector]
    public bool CinematicMode = false;

    // Start is called before the first frame update
    void Start()
    {
        //Set the minimum range at which aggro will be dropped to 150% of the aggro range
        MinDeaggroRange = AggroRange * 1.5f;

        //Initialize enemy health and health bar
        EnemyHealthBar = transform.Find("EnemyHealthBar").GetComponent<HealthBar>();
        EnemyHealthBar.MaxHealth = StartingHealth;
        EnemyHealthBar.Health = StartingHealth;
        Health = StartingHealth;
    }

    // Update is called once per frame
    void Update()
    {
        //Don't do anything if in cinematic mode
        if (CinematicMode)
        {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            return;
        }

        //Check to see if we are already tracking the player
        TrackThePlayer();

        //Increment the timers
        Timer += Time.deltaTime;
		MoveVerticalTimer -= Time.deltaTime;
		MoveHorizontalTimer -= Time.deltaTime;

        //Should we wander in a random direction?
        WanderingUpdate();

        //No reference to an active player, nothing to chase
        if (Player == null || !Player.gameObject.activeInHierarchy)
        {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            SetAggroState(false);
            return;
        }

        //If player is within aggro range, chase it!
        var dir = (Player.position - transform.position);
        if (dir.magnitude <= AggroRange)
		{
			if (Aggroed == false)
			{
				Wander = false;
				Timer = 0;
			}
            SetAggroState(true);
        }
        else if (dir.magnitude > MinDeaggroRange) //Too far away, so drop aggro
            SetAggroState(false);
		
        //Rotate to face the player (unless we are wandering)
		if (Aggroed == true && Wander == false)
			transform.up = SnapVectorToGrid(dir, MoveVerticalTimer > 0, MoveHorizontalTimer > 0);
        //Note that we account for whether the enemy is up against a wall so we don't get stuck

        //Move at designated velocity
        if (Aggroed == true || Wander == true)
            GetComponent<Rigidbody2D>().velocity = transform.up * Speed;
		else //Stop is we are not aggroed or wandering
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    //Not using a normal getter and setter so that these calls are more explicit
    public bool IsAggroed()
    {
        return Aggroed;
    }

    //May need to update the deaggro range...
    public void SetAggroState(bool active)
    {
        //If we are just detecting the player, recalculate deaggro ranges
        if (active == true && Aggroed == false)
            RecalculateDeaggroRange();

        //Set the aggro state
        Aggroed = active;
    }

    //Track the player
    void TrackThePlayer()
    {
        //Already tracking the player
        if (Player != null)
            return;

        //Find the player
        var player = GameObject.Find("Player(Clone)");
        if (player == null)
            return;
        Player = player.transform;
    }

    //Increase the deaggro range as the player's weapons get longer ranges
    void RecalculateDeaggroRange()
    {
        if (Player == null)
            return;

        //Find the maximum range of all player weapons
        var maxBulletRange = 0.0f;
        for (int i = 0; i < Player.childCount; i++)
        {
            Transform child = Player.GetChild(i);
            WeaponLogic weapon = child.GetComponent<WeaponLogic>();
            if (weapon != null && weapon.BulletRange > maxBulletRange)
                maxBulletRange = weapon.BulletRange;
        }

        //If this range is less than 150% of the max weapon range, use that instead
        if (MinDeaggroRange < maxBulletRange * 1.5f)
            MinDeaggroRange = maxBulletRange * 1.5f;
    }

    //Update the wandering state
    void WanderingUpdate()
    {
        //Check to see if we should wander
        if (Wander == false && Timer >= WanderInterval)
        {
            if (UnityEngine.Random.Range(0.0f, 1.0f) <= WanderChance)
            {
                Wander = true;
                //Pick a random direction, but account for whether the enemy is up against a wall
                transform.up = SnapVectorToGrid(UnityEngine.Random.insideUnitCircle, MoveVerticalTimer > 0, MoveHorizontalTimer > 0);
            }
            Timer = 0.0f;
        }

        //Check to see if it is time to stop wandering
        if (Wander == true && Timer >= WanderInterval / 4.0f)
        {
            //Stop wandering at one quarter the wander interval if aggroed, half if not
            if (Aggroed == true || Timer >= WanderInterval / 2.0f)
            {
                Wander = false;
                Timer = 0.0f;
            }
        }
    }

    //Snap this vector to only going vertical and/or horizontal
    //This allows and enemy to move along a wall instead of getting stuck
    private Vector3 SnapVectorToGrid(Vector3 v, bool vert, bool horiz)
    {
        var snappedVector = v;
        if (vert == true && horiz != true)
            snappedVector.x = 0;
        if (horiz == true && vert != true)
            snappedVector.y = 0;
        if (snappedVector.magnitude <= 0.05f)
            return v.normalized;
        return snappedVector.normalized;
    }

    //Check to see if the enemy has hit another enemy, the player, or is up against a wall
    private void OnCollisionStay2D(Collision2D col)
    {
        //Aggro on friendly collision if the other enemy is already aggroed
        var enemy = col.gameObject.GetComponent<EnemyLogic>();
        if (enemy != null && enemy.IsAggroed() == true)
        {
            GetComponent<EnemyLogic>().SetAggroState(true);
            return;
        }

        //Aggro on collision with player
        var player = col.gameObject.GetComponent<PlayerLogic>();
        if (player != null)
        {
            GetComponent<EnemyLogic>().SetAggroState(true);
            return;
        }

        if (col.gameObject.ToString().StartsWith("Wall") == false)
            return;
        //This a wall, so figure out whether it is horizontal or vertical
        var wallTransform = col.collider.transform;
        var xdist = Math.Abs(transform.position.x - wallTransform.position.x);
        var ydist = Math.Abs(transform.position.y - wallTransform.position.y);
        //If it is horizontal, reset the horizontal move timer so we only move horizontal for a bit
        if (xdist < ydist &&
            xdist <= wallTransform.localScale.x / 2.0f + transform.localScale.x / 2.0f &&
            MoveHorizontalTimer < -0.25f)
            MoveHorizontalTimer = 0.5f;
        //If it is vertical, reset the horizontal move timer so we only move vertical for a bit
        if (ydist < xdist &&
            ydist <= wallTransform.localScale.y / 2.0f + transform.localScale.x / 2.0f &&
            MoveVerticalTimer < -0.25f)
            MoveVerticalTimer = 0.5f;
        //These delays on checking prevent the enemy from jittering back and forth on a wall
    }

    //Check to see if we are hit by a bullet
    private void OnTriggerEnter2D(Collider2D col)
    {
        var bullet = col.GetComponent<BulletLogic>();
        //Check for an enemy bullet
        if (bullet != null && bullet.Team == Teams.Player)
        {
            Health -= 1;
            GetComponent<EnemyLogic>().SetAggroState(true); //Aggro when hit
            if (Health <= 0) //We're dead, so destroy ourself
            {
                float roll = UnityEngine.Random.Range(0.0f, 1.0f);
                if (roll <= DropChance)
                    Instantiate(PCGObject.Prefabs["heart"], transform.position, Quaternion.identity);
                else if (roll <= DropChance + BoostDropChance)
                    DropRandomBoost();
                Destroy(gameObject);
            }
        }
        //Aggro but no damage on friendly fire
        if (bullet != null && bullet.Team != Teams.Player)
            GetComponent<EnemyLogic>().SetAggroState(true);
    }

    // Drop a random boost
    void DropRandomBoost()
    {
        float roll = UnityEngine.Random.Range(0.0f, 0.99f);
        if (roll <= 0.33f)
            Instantiate(PCGObject.Prefabs["healthboost"], transform.position, Quaternion.identity);
        else if (roll <= 0.66f)
            Instantiate(PCGObject.Prefabs["shotboost"], transform.position, Quaternion.identity);
        else
            Instantiate(PCGObject.Prefabs["speedboost"], transform.position, Quaternion.identity);
    }
}
