using System.Collections.Generic;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    public List<GameObject> envLights = new List<GameObject>();
    public List<GameObject> stageLights = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void EnvLight(int lightID, float intensity, Color color)
    {
        if (envLights[lightID].gameObject != null)
        {
            var light = envLights[lightID].GetComponent<Light>();
            if (light != null)
            {
                light.color = color;
                light.intensity = intensity;
            }
        }
        else
        {
            Debug.LogWarning("LightingManager: Invalid environment light ID: " + lightID);
        }
    }
    public void StageLight(int lightID, float intensity, Vector2 rotation, Color color)
    {
        if (stageLights[lightID].gameObject != null)
        {
            var light = stageLights[lightID].GetComponentInChildren<Light>();
            if (light != null)
            {
                light.intensity = intensity;
                light.color = color;
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
