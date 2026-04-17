using System.Collections.Generic;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    public List<Light> envLights = new List<Light>();
    public List<Light> stageLights = new List<Light>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void BrightenEnvLights(float amount)
    {
        foreach (Light light in envLights)
        {
            light.intensity = amount;
        }
    }
    public void BrightenStageLights(float amount)
    {
        foreach (Light light in stageLights)
        {
            light.intensity = amount;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
