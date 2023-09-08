/*******************************************************************************
File:      WeaponLogic.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      11/11/2022
Course:    DES 214

Description:
    This script turns a game object into a weapon. It assumes the weapon object
    is parented to an enemy or player object and may not function if it is not.
    To give a game object multiple weapons, just parent multiple weapon objects
    to that game object (say, for a boss with multiple attacks).

*******************************************************************************/
using UnityEngine;

public class WeaponLogic : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //The prefab used whenever a shot is fired
    public GameObject BulletPrefab;
    //Cooldown between shots in seconds
    public float ShotCooldown = 1.0f;
    //A multiplier of the parent object's speed (should always be at least 1.5 or greater)
    public float BulletSpeed = 3.0f;
    //Distance the bullet will travel before self-destructing
    public float BulletRange = 1.0f;
    //Number of bullets fired simultaneously with a single shot
    public int BulletsPerShot = 1;
    //If there is more than one bullet per shot, how spread out are they? (in degrees)
    public float BulletSpreadAngle = 0f;
    //Weapon will only fire when it is active
    public bool WeaponActive = true;
    //////////////////////////////////////////////////////////////////////////

    //Bullets that are too fast can tunnel, so cap the speed
    private float MaximumBulletSpeed = 20.0f; //This should be at least double the player's maximum speed

    //Tracks how long it has been since the last shot
    private float ShotTimer = 0.0f;

    // Update is called once per frame
    void Update()
    {
        //Don't fire in cinematic mode
        var player = transform.parent.GetComponent<PlayerLogic>();
        if (player != null && player.CinematicMode)
            return;
        var enemy = transform.parent.GetComponent<EnemyLogic>();
        if (enemy != null && enemy.CinematicMode)
            return;

        //Shot timer keeps going even when inactive
        ShotTimer += Time.deltaTime;

        //No shooting unless active
        if (!WeaponActive)
            return;

        //No shooting until cooldown is finished
        if (ShotTimer < ShotCooldown)
            return;

        //If the parent is the player, don't shoot if the left mouse is not pressed
        if (player != null && !Input.GetMouseButton(0))
            return;

        //If the parent is an enemy and it is not aggroed, don't shoot
        if (enemy != null && enemy.Aggroed == false)
            return;

        //If the parent is an enemy and it is wandering, don't shoot
        if (enemy != null && enemy.Wander)
            return;

        int bulletsLeft = BulletsPerShot;
        float angleAdjust = 0.0f;
        //Odd number of bullets means fire the first one straight ahead
        if (bulletsLeft % 2 == 1)
        {
            FireBullet(0.0f);
            bulletsLeft--;
        }
        else //Even number of bullets means we need to adjust the angle slightly
        {
            angleAdjust = 0.5f;
        }
        //The rest of the bullets are spread out evenly
        while (bulletsLeft > 0)
        {
            FireBullet(BulletSpreadAngle * (bulletsLeft / 2) - (BulletSpreadAngle * angleAdjust));
            FireBullet(-BulletSpreadAngle * (bulletsLeft / 2) + (BulletSpreadAngle * angleAdjust));
            bulletsLeft -= 2; //Must do this afterwards, otherwise the angle will be wrong
        }
        //Weapon is now on cooldown
        ShotTimer = 0;
    }

    void FireBullet(float rotate)
    {
        //Instantiate a bullet and rotate it the given amount
        var bullet = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
        var fwd = RotateVector(transform.up, Mathf.PI * rotate / 180.0f);
        bullet.transform.up = fwd.normalized;

        //Multiply the bullet speed by the player or enemy speed, so the bullets aren't too slow
        float bulletSpeed = BulletSpeed;
        if (transform.parent.GetComponent<PlayerLogic>() != null)
            bulletSpeed *= transform.parent.GetComponent<PlayerLogic>().Speed;
        else if (transform.parent.GetComponent<EnemyLogic>() != null)
            bulletSpeed *= transform.parent.GetComponent<EnemyLogic>().Speed;

        //Cap bullet speeds at a reasonable value to prevent tunnelling
        bulletSpeed = Mathf.Min(bulletSpeed, MaximumBulletSpeed);

        //Set the bullet speed and range
        bullet.GetComponent<Rigidbody2D>().velocity = fwd * bulletSpeed;
        bullet.GetComponent<BulletLogic>().BulletRangeLeft = BulletRange;
    }

    Vector2 RotateVector(Vector2 vec, float Angle)
    {
        //x2 = cos(A) * x1 - sin(A) * y1
        var newX = Mathf.Cos(Angle) * vec.x - Mathf.Sin(Angle) * vec.y;
        //y2 = sin(A) * x1 + cos(B) * y1;
        var newY = Mathf.Sin(Angle) * vec.x + Mathf.Cos(Angle) * vec.y;
        //Return a rotated vector
        return new Vector2(newX, newY);
    }
}
