using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    public static LightingManager Instance { get; private set; }
    public List<GameObject> stageLights = new List<GameObject>();

    public void StageLight(int lightID, float intensity, Vector2 rotation, Color color)
    {
        try
        {
            
        
        if (stageLights[lightID].gameObject != null)
        {
            var light = stageLights[lightID].GetComponentInChildren<VLight>();
            if (light != null)
            {
                light.lightMultiplier = intensity;
                light.colorTint = color;
            }
            GameObject lightX = stageLights[lightID].transform.Find("Spotlight" + lightID + "X").gameObject;
            GameObject lightY = stageLights[lightID];

            if (lightX != null && lightY != null)
            {
                lightX.transform.rotation = Quaternion.Euler(rotation.x, 0 , 0);
                lightY.transform.rotation = Quaternion.Euler(0, rotation.y, 0);
            }

        }
        else
        {
            Debug.LogWarning("LightingManager: Invalid stage light ID: " + lightID);
        }

        }
        catch (Exception ex)
        {
            Debug.LogWarning("LightingManager: Exception Occoured: " + ex.Message);
        }
    }
}
