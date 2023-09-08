/*******************************************************************************
File:      BulletLogic.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      11/11/2022
Course:    DES 214

Description:
    Handles bullet collisions and range.

*******************************************************************************/
using UnityEngine;

public enum Teams { Player, Enemy }

public class BulletLogic : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Enemy bullet or player bullet?
    public Teams Team = Teams.Player;

    //////////////////////////////////////////////////////////////////////////

    [HideInInspector]
    public float BulletRangeLeft; //Range left before self-destruction

    void Update()
    {
        //Destroy the bullet after it has travelled far enough
        BulletRangeLeft -= (Time.deltaTime * GetComponent<Rigidbody2D>().velocity.magnitude);
		if (BulletRangeLeft < 0)
			Destroy(gameObject);
	}

    //Destroy the bullet if it hits a player or wall
    private void OnTriggerEnter2D(Collider2D col)
    {
		//No friendly fire
        if (col.isTrigger || col.tag == Team.ToString())
            return;
        Destroy(gameObject);
    }
}
