/*******************************************************************************
File:      CollectibleLogic.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      11/11/2022
Course:    DES 214

Description:
    This component is shared among all collectible objects and determines what
    kind of collectible it is, along with the logic for colliding with the player.

*******************************************************************************/
using UnityEngine;
using static PCG;

public enum CollectibleTypes { HealthBoost, SpeedBoost, ShotBoost, SilverKey, GoldKey, Heart }

public class CollectibleLogic : MonoBehaviour
{
    public CollectibleTypes Type;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Check collision against player
        var player = collision.gameObject.GetComponent<PlayerLogic>();
        if (player == null)
            return;

        switch (Type)
        {
            //Heals the player for one health, if not at max already
            case CollectibleTypes.Heart:
                if (player.Health == player.MaxHealth)
                    return; //We didn'y use it, so don't destroy it
                ++player.Health;
                break;
            //Increases max health and heals completely
            case CollectibleTypes.HealthBoost:
                ++player.MaxHealth;
                player.Health = player.MaxHealth;
                break;
            //Permanently increase the player's speed
            case CollectibleTypes.SpeedBoost:
                ++player.Speed;
                break;
            //Permanently increases the number and range of shots the player fires
            case CollectibleTypes.ShotBoost:
                player.IncrementWeaponShots();
                player.IncrementWeaponRange();
                break;
            //Increments number of silver keys
            case CollectibleTypes.SilverKey:
                ++player.SilverKeys;
                CinematicPanTo("GoldKey(Clone)"); //Camera pans to a gold key if there is one
                DestroyDoors("SilverDoor"); //Remove this call to make the player have to collide with each door
                break;
            //Increments the number of gold keys
            case CollectibleTypes.GoldKey:
                ++player.GoldKeys;
                CinematicPanTo("Portal(Clone)"); //Camera pans to the exit portal
                DestroyDoors("GoldDoor"); //Remove this call to make the player have to collide with each door
                break;
        }

        // Spawn a death marker from the enemy
        Instantiate(PCGObject.Prefabs["deathMarker"], transform.position, Quaternion.identity);

        //Destroy the collectible
        Destroy(gameObject);
    }

    //Destroy all doors with the given name
    void DestroyDoors(string doorName)
    {
        GameObject[] doors = GameObject.FindGameObjectsWithTag(doorName);
        foreach (GameObject door in doors)
            GameObject.Destroy(door);
    }

    //Cinematic pan to the first object with the given name
    void CinematicPanTo(string name)
    {
        var panTo = GameObject.Find(name);
        if (panTo == null)
            return;
        var cinematicAnchor = Instantiate(PCGObject.Prefabs["cinematicanchor"]);
        cinematicAnchor.transform.position = panTo.transform.position;
    }
}
