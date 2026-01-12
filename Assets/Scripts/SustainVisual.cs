using UnityEngine;

// Attached to the sustain visual prefab; handles sizing/position and notifies the manager when done.
public class SustainVisual : MonoBehaviour
{
    int laneIndex = -1;
    float endTime;

    // Setup called by SustainManager
    public void Setup(int laneIndex, float x, float baseY, float duration, float spacing)
    {
        this.laneIndex = laneIndex;
        float height = Mathf.Max(0.001f, duration * spacing);
        transform.position = new Vector3(x, baseY + (height * 0.5f), transform.position.z);
        Vector3 s = transform.localScale;
        s.y = height;
        transform.localScale = s;
        endTime = Time.time + duration;
        gameObject.SetActive(true);
    }

    // Force end early (called by manager)
    public void ForceEnd()
    {
        NotifyFinished();
    }

    void Update()
    {
        if (Time.time >= endTime)
        {
            NotifyFinished();
        }
    }

    void NotifyFinished()
    {
        if (SustainManager.Instance != null)
        {
            SustainManager.Instance.NotifyVisualFinished(laneIndex, this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
