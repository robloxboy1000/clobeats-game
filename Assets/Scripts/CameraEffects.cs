using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraEffects : MonoBehaviour
{
    public Camera cameraToTweak;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetChromaticAberration(float intensity)
    {
        if (cameraToTweak != null)
        {
            if (cameraToTweak.gameObject.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>() != null)
            {
                cameraToTweak.gameObject.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>().profile.GetSetting<UnityEngine.Rendering.PostProcessing.ChromaticAberration>().intensity.value = intensity;
            }
            else
            {
                cameraToTweak.gameObject.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
                var chromaticAberration = cameraToTweak.gameObject.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>().profile.AddSettings<UnityEngine.Rendering.PostProcessing.ChromaticAberration>();
                if (chromaticAberration != null)
                chromaticAberration.intensity.value = intensity;
            }
        }
    }
}
