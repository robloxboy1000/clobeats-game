using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] overstrumClips;
    public AudioClip comboLostClip;
    public AudioClip highwayRiseClip;
    public AudioClip fretRippleUpClip;
    public AudioClip songFailClip;


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


    // Update is called once per frame
    void Update()
    {
        
    }
}
