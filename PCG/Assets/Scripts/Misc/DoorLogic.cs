/*******************************************************************************
File:      DoorLogic.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      11/11/2022
Course:    DES 214

Description:
    Destroys door objects when the player gets the corresponding key.
    Note that the default framework destroys all doors when the key is collected,
    so this code will not currently be used, but will work if the default
    destruction of all doors (in CollectibleLogic) is removed.

*******************************************************************************/
using UnityEngine;

public enum DoorType { Silver, Gold }

public class DoorLogic : MonoBehaviour
{
    public DoorType Type = DoorType.Silver;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Check for collisions with player
        var player = collision.gameObject.GetComponent<PlayerLogic>();
        if (player == null)
            return;
        //Check for silver doors/keys
        if (Type == DoorType.Silver && player.SilverKeys > 0)
        {
            player.SilverKeys--;
            Destroy(gameObject);
        }
        //Check for gold doors/keys
        if (Type == DoorType.Gold && player.GoldKeys > 0)
        {
            player.GoldKeys--;
            Destroy(gameObject);
        }
        //Additional types of keys could easily be added
    }
}
