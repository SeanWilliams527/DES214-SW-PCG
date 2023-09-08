/*******************************************************************************
File:      CameraTarget.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      09/18/2020
Course:    DES214

Description:
    This component is added to an object that acts as the target that a weighted
	dynamic camera will attempt to follow. This object will update its position
	based on the data fed to it by objects acting as camera anchors.

*******************************************************************************/

using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Minimum zoom, regardless of other calculations
    private float MinZoom = 5.0f;
    //Maximum zoom, regarless of other calculations.
    private float MaxZoom = 20.0f; //Note that you might need to increase this if the player is really fast.
    //When calculating the zoom level, we might want it to
    //zoom out a bit more or less than our raw calculations say.
    private float ZoomDivisor = 1.0f; //Fairly arbitrary, but by default just use the calculations raw.

    //When calculating the position of the camera target, how much should it
    //be biased towards the player, regardless of weight calculations?
    //A value of 1 is 50/50, a value of 0 means just use the weights, while
    //values of 4 or 5 or more makes weights mean very little.
    private float PlayerBias = 1.0f;

    //How long should we stay in cinematic mode once a cinematic anhor is reached?
    private float CinematicDelay = 3.0f;

    //////////////////////////////////////////////////////////////////////////

    //The current desired zoom level
    [HideInInspector]
    public float Zoom = 0.0f;

    //What are the edges of the camera box we should be interpolating to?
    [HideInInspector]
    public float leftEdge = float.MaxValue;
	[HideInInspector]
    public float rightEdge = -float.MaxValue;
	[HideInInspector]
    public float bottomEdge = float.MaxValue;
	[HideInInspector]
    public float topEdge = -float.MaxValue;

    //What are total weights of any active anchors?
    [HideInInspector]
    public float xAccumulator = 0.0f;
	[HideInInspector]
    public float yAccumulator = 0.0f;
	[HideInInspector]	
    public float weightsAccumulator = 0.0f;

	//Where is the player currently?
	[HideInInspector]	
    public Vector2 playerTarget;

	//Start is called once when the object is created
	void Start()
	{
		Zoom = MinZoom; //Start zoomed in...
	}

    //Update is called once per frame
    void Update()
    {
		//Determine the desired zoom level based on the furthest "edge" from the player
		float leftOfCamera = transform.position.x - rightEdge;
        float rightOfCamera = leftEdge - transform.position.x;
        float aboveCamera = topEdge - transform.position.y;
        float belowCamera = transform.position.y - bottomEdge;
        Zoom = Mathf.Max(leftOfCamera, rightOfCamera, aboveCamera, belowCamera) / ZoomDivisor; //ZoomDivisor just adjusts the zoom by an arbirtary amount

        //Check for minimums and maximums
		Zoom = Mathf.Min(Zoom, MaxZoom);
		Zoom = Mathf.Max(Zoom, MinZoom);

		//Reset the edges for the next update
		leftEdge = float.MaxValue;
		rightEdge = -float.MaxValue;
		bottomEdge = float.MaxValue;
		topEdge = -float.MaxValue;

		//No active anchors, so just follow the player
		if (weightsAccumulator <= 0)
		{
			transform.position = (Vector3)playerTarget;
			return;
		}

		//Get the average weighted position to move the camera target to.
		Vector3 newPosition;
		newPosition.x = xAccumulator / weightsAccumulator;
		newPosition.y = yAccumulator / weightsAccumulator;
		newPosition.z = -1.0f;
		//Average with the player's target position (biased towards the player),
		//unless weights are 10000+ (i.e., a cinematic anchor is active)
		if (weightsAccumulator < 10000.0f)
			transform.position = (newPosition + (Vector3)playerTarget * PlayerBias) / (PlayerBias + 1.0f);
		else
			transform.position = newPosition;

		//Clear the accumulators for the next update
		xAccumulator = 0.0f;
		yAccumulator = 0.0f; 
		weightsAccumulator = 0.0f;
        //Note that the order objects update in will not really make a difference in how this works.
    }

    //Put all enemies and the player into cinematic mode
    public void StartCinematic()
    {
        var enemies = FindObjectsOfType<EnemyLogic>();
        foreach (EnemyLogic e in enemies)
            e.CinematicMode = true;
        var players = FindObjectsOfType<PlayerLogic>();
        foreach (PlayerLogic p in players)
            p.CinematicMode = true;
    }

    //End the cinematic mode
    public void EndCinematic()
    {
        //Keep everyone in cinematic mode for a few seconds
        Invoke("ExitCinematicMode", CinematicDelay);
    }

    //Pull all enemies and the player out of cinematic mode
    void ExitCinematicMode()
    {
        var enemies = FindObjectsOfType<EnemyLogic>();
        foreach (EnemyLogic e in enemies)
            e.CinematicMode = false;
        var players = FindObjectsOfType<PlayerLogic>();
        foreach (PlayerLogic p in players)
            p.CinematicMode = false;
    }
}
