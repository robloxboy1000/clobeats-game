using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioLevelMeter : MonoBehaviour
{
    public float audioLevel; // This float will hold the current audio level (0 to 1)
    private float[] spectrumData; // Array to hold the raw audio data
    // Start is called before the first frame update
    void Start()
    {
        // Define the size of the array (must be a power of 2: 64, 128, 512, etc.)
        spectrumData = new float[512];
    }

    // Update is called once per frame
    void Update()
    {
        // Fill the array with the current audio data
        // Use AudioListener.GetOutputData to get the master output, 
        // or AudioSource.GetOutputData for a specific source.
        AudioListener.GetOutputData(spectrumData, 0); // Channel 0

        // Process the data to find the average/peak level
        float sum = 0f;
        foreach (float sample in spectrumData)
        {
            // Sum the absolute values of the samples
            sum += Mathf.Abs(sample);
        }

        // Calculate the average (RMS-ish) level and clamp it between 0 and 1
        audioLevel = Mathf.Clamp(sum / spectrumData.Length, 0f, 1f);

        // You can now use the 'audioLevel' float to drive UI elements, visual effects, etc.
        // E.g., Debug.Log("Current Level: " + audioLevel);
        if (FindAnyObjectByType<CameraEffects>() != null)
        {
            CameraEffects cameraEffects = FindAnyObjectByType<CameraEffects>();
            cameraEffects.SetChromaticAberration(audioLevel);
        }
    }
}
