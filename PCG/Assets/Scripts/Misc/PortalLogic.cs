/*******************************************************************************
File:      PortalLogic.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      11/11/2022
Course:    DES 214

Description:
    This component handles the collision logic for portals to reset the level
    when colliding with the player.

*******************************************************************************/
using UnityEngine;
using static PCG;

public class PortalLogic : MonoBehaviour
{	
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Is this the player we colliding with?
        if (collision.GetComponent<PlayerLogic>() == null)
            return;

        //Reset the level in a tenth of a second
        PCGObject.ResetLevel(0.1f);
    }
}
