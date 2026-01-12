using UnityEngine;

// Simple component placed on a note prefab to indicate it is a sustained note
// and how long the sustain lasts (in seconds).
public class SustainedNote : MonoBehaviour
{
    [Tooltip("Duration of the sustain in seconds (from the moment the note is hit)")]
    public float durationSeconds = 1.0f;
    // Set the duration of the sustain
    public void SetDuration(float duration)
    {
        durationSeconds = duration;
    }
}
