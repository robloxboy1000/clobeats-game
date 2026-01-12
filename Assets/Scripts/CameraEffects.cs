using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraEffects : MonoBehaviour
{
    public Camera cameraToTweak;
    bool mainCameraFound = false;
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        mainCameraFound = false;
    }

    // Update is called once per frame
    async void Update()
    {
        while (!mainCameraFound)
        {
            if (Camera.main != null)
            {
                cameraToTweak = Camera.main;
                mainCameraFound = true;
            }
            await System.Threading.Tasks.Task.Delay(100);
        }
    }
    public void SetChromaticAberration(float intensity)
    {
        if (cameraToTweak != null)
        {
            if (cameraToTweak.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>() != null)
            {
                cameraToTweak.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>().profile.GetSetting<UnityEngine.Rendering.PostProcessing.ChromaticAberration>().intensity.value = intensity;
            }
            else
            {
                return;
            }
        }
    }
}
