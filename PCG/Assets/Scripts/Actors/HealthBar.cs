/*******************************************************************************
File:      HealthBar.cs
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      11/11/2022
Course:    DES 214

Description:
    Handles the health bar display.

*******************************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////
    // DESIGNER ADJUSTABLE VALUES
    //////////////////////////////////////////////////////////////////////////

    //Prefab for individual health ticks on the bar
    public GameObject HealthBarTickPrefab;
    //The background for the bar
    public Transform HealthBarBackground;
    //Colors for the ticks
    public Color FilledTickColor = Color.red;
    public Color MissingTickColor = Color.gray;

    //////////////////////////////////////////////////////////////////////////

    //Handles the maximum health
    public int MaxHealth
    {
        get { return _MaxHealth; }

        set
        {
            //Recreate all hearts icons since the size is different now
            for (int i = 0; i < HealthBarBackground.transform.childCount; ++i)
            {
                Destroy(HealthBarBackground.transform.GetChild(i).gameObject);
            }
            Ticks.Clear();

            for (int i = 0; i < value; ++i)
            {
                //Parent new icons to the panel
                var obj = Instantiate(HealthBarTickPrefab, HealthBarBackground.transform);
                Ticks.Add(obj.GetComponent<Image>());
            }

            _MaxHealth = value;
        }
    }
    private int _MaxHealth = 0;

    //List of all the health ticks
    private List<Image> Ticks = new List<Image>();

    //Handles current health
    public int Health
    {
        get { return _Health; }

        set
        {
            //Ignore values out of bounds
            if (value > MaxHealth || value < 0)
                return;

            //Color health icons based on new health
            for (int i = Ticks.Count - 1; i >= 0; --i)
            {
                if (i < value)
                    Ticks[i].color = FilledTickColor;
                else
                    Ticks[i].color = MissingTickColor;
            }

            _Health = value;
        }

    }
    private int _Health;

    //Called once per frame
    void Update()
    {
        //Check to see if we have an enemy parent
        var parent = transform.parent;
        if (parent == null || parent.GetComponent<EnemyLogic>() == null)
            return;
        //If we do, make sure the rotation is set to zero
        transform.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
        //Then set the position (world, not local, so the parent's rotation doesn't matter)
        //to the parent's position plus an offset upwards of 120% of the scale of the enemy
        transform.position = parent.transform.position + new Vector3(0.0f, 1.2f * parent.transform.localScale.y, 0.0f);
    }
}
