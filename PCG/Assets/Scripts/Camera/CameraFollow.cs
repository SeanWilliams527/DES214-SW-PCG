/*******************************************************************************
File:      CameraFollow.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      11/11/2022
Course:    DES 214

Description:
    This component is added to a camera to have it follow a specified target.
    It follows the target using an adjusted 2D linear interpolation on FixedUpdate.

*******************************************************************************/

using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Maximum acceleration rate of the camera
    private float MaxAccel = 1.0f;
    //Percentage of distance to interpolate over one second
    private float Interpolant = 1.0f;
    //Percentage to interpolate zooming out over one second
    private float ZoomOutInterpolant = 1.0f;
    //Percentage to interpolate zooming in over one second
    private float ZoomInInterpolant = 0.95f;
    private float ZoomInInterpolantDefault = 0.95f;
    //Map mode zoom size
    private float MapModeZoom = 200.0f; //This might need to be bigger if you have a large level

    //////////////////////////////////////////////////////////////////////////

    //The camera target being followed
    [HideInInspector]
    public Transform ObjectToFollow;

    //Calculated speed limit used to enforce maximum acceleration
    private float SpeedLimit = 0.0f;

    //Fixed update should always be used for smoother camera movement
    void FixedUpdate()
    {
        //Nothing to follow...
        if (ObjectToFollow == null)
            return;

        //Follow the camera target
        FollowTarget();

        //Adjust the zoom level
        AdjustZoom();
    }

    void Update()
    {
        //M to see the whole map
        if (Input.GetKeyDown(KeyCode.M))
        {
            GetComponent<Camera>().orthographicSize = MapModeZoom;
            // Flip between map mode and zoom mode
            if (ZoomInInterpolant == 0.0f) // Already in map mode
                ZoomInInterpolant = ZoomInInterpolantDefault;
            else
                ZoomInInterpolant = 0.0f;
        }
    }

    //Follow the camera target
    void FollowTarget()
    {
        //Find the offset to the target
        Vector3 targetPos = ObjectToFollow.position;
        Vector2 adjust = targetPos - transform.position;
        float distance2D = adjust.magnitude; //Use later to detect overshooting

        //Determine amount to interpolate
        adjust *= Interpolant * Time.deltaTime;

        //Adjust if it is going too fast
        if (adjust.magnitude > SpeedLimit)
            adjust = adjust.normalized * SpeedLimit;

        //Adjust if it is going too slow so it doesn't take forever at the end
        if (adjust.magnitude < 0.5f * Time.deltaTime)
            adjust = adjust.normalized * 0.5f * Time.deltaTime;

        //Save old position for speed limit calculation below
        var oldPosition = transform.position;

        //Move towards the target, but not along the Z axis
        if (adjust.magnitude < distance2D)
            transform.Translate(adjust.x, adjust.y, 0.0f);
        else //Don't overshoot the target
            transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);

        //Limit how fast the camera can accelerate so it doesn't feel too jumpy
        SpeedLimit = (transform.position - oldPosition).magnitude / Time.deltaTime + MaxAccel * Time.deltaTime;
    }

    //Adjust the zoom level over time
    void AdjustZoom()
    {
        //Find the target zoom level
        float targetZoom = ObjectToFollow.GetComponent<CameraTarget>().Zoom;
        float zoomAdjust = targetZoom - GetComponent<Camera>().orthographicSize;

        //Use later to detect overshooting
        float zoomDistance = Mathf.Abs(zoomAdjust);

        //Determine amount to interpolate
        if (zoomAdjust > 0.0f)
            zoomAdjust *= ZoomOutInterpolant * Time.deltaTime;
        else
            zoomAdjust *= ZoomInInterpolant * Time.deltaTime;

        //Adjust if it is going too slow
        if (zoomAdjust < 0.5f * Time.deltaTime && zoomAdjust > 0)
            zoomAdjust = 0.5f * Time.deltaTime;
        else if (zoomAdjust > -0.01f && zoomAdjust < 0)
            zoomAdjust = -0.5f * Time.deltaTime;

        //Move towards the target zoom level
        if (Mathf.Abs(zoomAdjust) < zoomDistance)
            GetComponent<Camera>().orthographicSize += zoomAdjust;
        else //Don't overshoot the target zoom level
            GetComponent<Camera>().orthographicSize = targetZoom;
    }
}
