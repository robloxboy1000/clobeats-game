using UnityEngine;

public class Sustain : MonoBehaviour
{
    public float startSeconds;
    public float endSeconds;
    public float spacingFactor;

    // Root Y where sustain begins (world space)
    private float startY;

    // The visual fill that will be scaled and moved locally. If null we fallback to this GameObject's renderer.
    private Transform fillTransform;
    private float prefabUnitLength = 1f; // world units per localScale.z
    private bool fillIsRoot = false;

    // Initialize sustain timings and compute unit length
    public void Initialize(float startSec, float endSec, float spacing)
    {
        startSeconds = startSec;
        endSeconds = endSec;
        spacingFactor = spacing;

        // Find a child named "Fill" first (recommended prefab structure). Otherwise use this object's renderer.
        var child = transform.Find("Fill");
        if (child != null)
        {
            fillTransform = child;
            fillIsRoot = false;
        }
        else
        {
            fillTransform = transform;
            fillIsRoot = true;
        }

        // Compute a prefab unit length from renderer bounds if available
        var rend = fillTransform.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            float worldY = rend.bounds.size.y;
            float localY = fillTransform.localScale.y;
            prefabUnitLength = localY > 0f ? worldY / localY : 1f;
            if (prefabUnitLength <= 0f) prefabUnitLength = 1f;
        }

        var indicator = GetComponent<SustainedNote>();
        if (indicator != null)
        {
            indicator.SetDuration(endSeconds - startSeconds);
        }

        // Set initial visual according to startSeconds (no consumption yet)
        UpdateVisual(startSeconds);
    }

    // Update the visual fill based on current song time. If currentTime >= endSeconds the sustain will be deactivated.
    public void UpdateVisual(float currentSongSeconds)
    {
        // remaining seconds from now until sustain end
        float remaining = Mathf.Clamp(endSeconds - currentSongSeconds, 0f, Mathf.Max(0f, endSeconds - startSeconds));

        float desiredWorldLength = remaining * spacingFactor;

        float newLocalY = prefabUnitLength > 0f ? desiredWorldLength / prefabUnitLength : desiredWorldLength;
        newLocalY = Mathf.Max(0.001f, newLocalY); // avoid zero scale

        // Apply scale and position depending on whether we're using a dedicated child fill or the root transform
        if (fillIsRoot)
        {
            // When the prefab has no Fill child, operate in world space so we don't overwrite the root's localPosition accidentally.
            var ls = fillTransform.localScale;
            fillTransform.localScale = new Vector2(ls.x, newLocalY);

            // Place the root so its back edge aligns with startY: center = startY + desiredWorldLength/2
            transform.position = new Vector2(transform.position.x, startY + (desiredWorldLength / 2f));
        }
        else
        {
            // Apply scale on the fill transform (local scale)
            var ls = fillTransform.localScale;
            fillTransform.localScale = new Vector2(ls.x, newLocalY);

            // Position the fill so its back aligns with the root (which should be at startY)
            // Local Y center = desiredWorldLength / 2
            fillTransform.localPosition = new Vector2(fillTransform.localPosition.x, startY + desiredWorldLength / 2f);
        }

        // Deactivate when finished
        if (remaining <= 0f)
        {
            gameObject.SetActive(false);
        }
    }
}
