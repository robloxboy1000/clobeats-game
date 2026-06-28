using System.Collections.Generic;
using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] overstrumClips;
    public AudioClip comboLostClip;
    public AudioClip highwayRiseClip;
    public AudioClip fretRippleUpClip;
    public AudioClip songFailClip;
    public AudioClip scoreShowClip;
    public Dictionary<string, AudioClip> loadedAudioClips = new Dictionary<string, AudioClip>();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("AudioSource not found on this GameObject or assigned in Inspector.");
                enabled = false; // Disable script if no AudioSource
                return;
            }
        }
    }
    public void PlayOverstrumClip()
    {
        if (overstrumClips != null && overstrumClips.Length > 0)
        {
            int randomIndex = Random.Range(0, overstrumClips.Length);
            audioSource.PlayOneShot(overstrumClips[randomIndex]); // Use PlayOneShot to avoid cutting off current sounds
        }
        else
        {
            Debug.LogWarning("No overstrum audio clips available to play.");
        }
    }
    public void PlayComboLostClip()
    {
        if (comboLostClip != null)
        {
            audioSource.PlayOneShot(comboLostClip);
        }
    }
    public void PlayHighwayRiseClip()
    {
        if (highwayRiseClip != null)
        {
            audioSource.PlayOneShot(highwayRiseClip);
        }
    }
    public void PlayFretRippleUpClip()
    {
        if (fretRippleUpClip != null)
        {
            audioSource.PlayOneShot(fretRippleUpClip);
        }
    }
    public void PlaySongFailedClip()
    {
        if (songFailClip != null)
        {
            audioSource.PlayOneShot(songFailClip);
        }
    }
    public void PlayScoreShowClip()
    {
        if (scoreShowClip != null)
        {
            audioSource.PlayOneShot(scoreShowClip);
        }
    }
    public void PlayClip(string clipName)
    {
        if (loadedAudioClips.TryGetValue(clipName, out var clip))
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogError("[SFXPlayer] Audio clip '" + clipName + "' is not loaded.");
        }
    }
    public void LoadClip(string clipName)
    {
        AudioClip clip = Resources.Load<AudioClip>(clipName);
        if (clip != null)
        {
            loadedAudioClips.Add(clipName, clip);
        }
        else
        {
            Debug.LogError("[SFXPlayer] Audio clip '" + clipName + "' does not exist in resources.");
        }
    }
    public void LoadActualClip(AudioClip clip1)
    {
        if (clip1 != null)
        {
            loadedAudioClips.Add(clip1.name, clip1);
        }
        else
        {
            Debug.LogError("[SFXPlayer] Audio clip '" + clip1.name + "' does not exist.");
        }
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
