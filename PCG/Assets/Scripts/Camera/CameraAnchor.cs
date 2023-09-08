/*******************************************************************************
File:      CameraAnchor.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      11/11/2022
Course:    DES 214

Description:
    This component is tells a different object with a CameraTarget component that its
    weighted position should influence where the camera goes and what the zoom level
    should be.

*******************************************************************************/

using UnityEngine;

public class CameraAnchor : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////
    
    //How much "pull" does this object have on the camera?
    public float Weight = 1.0f;
	//How "big" is this object for zoom purposes when the camera wants it on screen?
	public float Padding = 5.0f;
    //Is this a cinematic anchor that overrides everything else?
    public bool Cinematic = false;

    //Number of seconds for the target to lead the player by while moving
    private float PlayerMovementLead = 1.0f; //One second seems reasonable
    //Distance to for the target to lead the player by based on facing
    private float PlayerFacingLead = 1.0f; //Just depends on how strong you want this to be

    //////////////////////////////////////////////////////////////////////////

    //Track the camera target
    private CameraTarget WeightedCameraTarget;

    //Update is called once per frame
    void Update()
    {
        //Find a camera target if you don't already have one
        FindCameraTarget();
        
        //If this anchor is on an enemy that is not aggroed, ignore it
        var enemy = GetComponent<EnemyLogic>();
        if (enemy != null && enemy.IsAggroed() == false)
            return;

        //Reduce the weight proportionally by the distance for enemies
        float weight = Weight;
        if (enemy != null)
            weight /= (enemy.Player.transform.position - transform.position).magnitude + 1.0f; //Add one to not overweight close enemies;

        //For cinematic anchors, override all the other weights with a large value
        if (Cinematic)
            weight = 10000.0f;

        //Accumulate the positions and total weights on the camera target object,
        //which will be averaged together later by that object.
        WeightedCameraTarget.xAccumulator += transform.position.x * weight;
        WeightedCameraTarget.yAccumulator += transform.position.y * weight;
        WeightedCameraTarget.weightsAccumulator += weight;

        //Determine edges of the camera box that we need to zoom out to in order to see everything
        CalculateScreenEdges(transform.position, Padding);

        //Special logic if this anchor is on a player 
        if (GetComponent<PlayerLogic>() != null)
            PlayerUpdate();

        //Special logic if this anchor is cinematic
        if (Cinematic)
            CinematicUpdate();
    }

    //Handle the logic for a player anchor
    void PlayerUpdate()
    {
        //Give the camera target the player's position
        WeightedCameraTarget.playerTarget = transform.position;
        //Adjust to lead the player based on their current velocity
        WeightedCameraTarget.playerTarget += GetComponent<Rigidbody2D>().velocity * PlayerMovementLead;
        //Adjust to lead the player based on their current facing
        WeightedCameraTarget.playerTarget += (Vector2)transform.up * PlayerFacingLead;
        //We need to keep the leading point on screen as well
        CalculateScreenEdges(WeightedCameraTarget.playerTarget, Padding);
    }

    //Handle the logic for a cinematic anchor
    void CinematicUpdate()
    {
        //Set the cinematic mode every update just to be safe
        WeightedCameraTarget.StartCinematic();

        //If the camera has reached the cinematic anchor, destroy the anchor
        var cameras = FindObjectsOfType<CameraFollow>();
        if (cameras.Length == 0)
            return;

        //Ignore the Z direction
        Vector2 camera2D = cameras[0].transform.position;
        Vector2 anchor2D = transform.position;

        //Use the padding value as a buffer, so we don't have to get exactly to the anchor
        if ((camera2D - anchor2D).magnitude <= Padding)
        {
            WeightedCameraTarget.EndCinematic();
            Destroy(gameObject);
        }
    }

    //Find a camera target if you don't already have one
    void FindCameraTarget()
    {
        if (WeightedCameraTarget != null)
            return;
        //Find the camera target (there should be only one)
        GameObject[] CameraTargets = GameObject.FindGameObjectsWithTag("CameraTarget");
        if (CameraTargets.Length == 0)
            return; //No camera target shouldn't ever happen...
        //Store it for later
        WeightedCameraTarget = CameraTargets[0].GetComponent<CameraTarget>();
    }

    //Set the edges so that the given point plus any padding stays on screen
    void CalculateScreenEdges(Vector3 position, float padding)
    {
        WeightedCameraTarget.leftEdge = Mathf.Min(WeightedCameraTarget.leftEdge, position.x - padding);
        WeightedCameraTarget.rightEdge = Mathf.Max(WeightedCameraTarget.rightEdge, position.x + padding);
        WeightedCameraTarget.bottomEdge = Mathf.Min(WeightedCameraTarget.bottomEdge, position.y - padding);
        WeightedCameraTarget.topEdge = Mathf.Max(WeightedCameraTarget.topEdge, position.y + padding);
    }
}
