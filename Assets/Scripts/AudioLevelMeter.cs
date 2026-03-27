using UnityEngine;
using System;
using System.Linq;

public class AudioLevelMeter : MonoBehaviour
{
    public float audioLevel; // This float will hold the current audio level (0 to 1)
    public short unclampedAudioLevel;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            unclampedAudioLevel = (short)musicPlayer.GetSongAudioLevel(); // 0 to 32768
            
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to get audio level: " + ex.Message);
        }
        
        CameraEffects cameraEffects = FindAnyObjectByType<CameraEffects>();
        if (cameraEffects != null)
        {
            cameraEffects.SetChromaticAberration(audioLevel);
        }
        
    }
}
