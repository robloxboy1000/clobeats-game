using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public string OverstrumsFolderPath = "Audio/Overstrums";
    private AudioClip[] availableClips;
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

        // Load audio clips from the specified path within a Resources folder
        // If your audio clips are not in a Resources folder, you'll need a different loading method (e.g., drag and drop)
        availableClips = Resources.LoadAll<AudioClip>(OverstrumsFolderPath);

        if (availableClips.Length == 0)
        {
            Debug.LogWarning($"No audio clips found in Resources folder at path: {OverstrumsFolderPath}");
        }
    }
    public void PlayOverstrumClip()
        {
            if (availableClips != null && availableClips.Length > 0)
            {
                int randomIndex = Random.Range(0, availableClips.Length);
                audioSource.PlayOneShot(availableClips[randomIndex]); // Use PlayOneShot to avoid cutting off current sounds
            }
            else
            {
                Debug.LogWarning("No audio clips available to play.");
            }
        }

    // Update is called once per frame
    void Update()
    {
        
    }
}
