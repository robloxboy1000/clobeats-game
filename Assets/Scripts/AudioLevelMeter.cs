using UnityEngine;
using System;
using System.Linq;

public class AudioLevelMeter : MonoBehaviour
{
    public float audioLevel; // This float will hold the current audio level (0 to 1)
    public float updateStep = 0.1f;
	public int sampleDataLength = 1024;

    public int intensityDivide = 5;

	private float currentUpdateTime = 0f;

	private float clipLoudness;
	private float[] clipSampleData;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        clipSampleData = new float[sampleDataLength];
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (musicPlayer != null)
            {
                currentUpdateTime += Time.deltaTime;
		        if (currentUpdateTime >= updateStep)
                {
		        	currentUpdateTime = 0f;
                    if (musicPlayer.previewAudioStream.clip != null)
		        	musicPlayer.previewAudioStream.clip.GetData(clipSampleData, musicPlayer.previewAudioStream.timeSamples);
		        	clipLoudness = 0f;
		        	foreach (var sample in clipSampleData)
                    {
		        		clipLoudness += Mathf.Abs(sample);
		        	}
		        	clipLoudness /= sampleDataLength; //clipLoudness is what you are looking for
		        }
                audioLevel = Mathf.Clamp(clipLoudness / intensityDivide, 0, 1f);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to get audio level: " + ex);
        }
        
        CameraEffects cameraEffects = FindAnyObjectByType<CameraEffects>();
        if (cameraEffects != null)
        {
            cameraEffects.SetChromaticAberration(audioLevel);
        }
        
    }
}
