using UnityEngine;

// Attached to the sustain visual prefab; handles sizing/position and notifies the manager when done.
public class SustainVisual : MonoBehaviour
{
    int laneIndex = -1;
    float endTime;
    public Color sustColor;

    // Setup called by SustainManager
    public void Setup(int laneIndex, float x, float baseY, float duration, float spacing, Color color)
    {
        this.laneIndex = laneIndex;
        float height = Mathf.Max(0.001f, duration * spacing);
        transform.position = new Vector3(x, baseY + (height * 0.5f), -0.05f);
        Vector3 s = transform.localScale;
        s.y = height;
        transform.localScale = s;
        var ns = FindAnyObjectByType<NoteSpawner>();
        endTime = ns.GetTimeInSecondsAtTick(ns.currentTick) + duration;
        sustColor = color;
        gameObject.SetActive(true);
        ns.AddObjectToGlobalMoveY(gameObject);
    }

    // Force end early (called by manager)
    public void ForceEnd()
    {
        NotifyFinished();
    }

    void Update()
    {
        var mp = FindAnyObjectByType<MusicPlayer>();
        if ((float)mp.currentTimeDSP >= endTime)
        {
            NotifyFinished();
        }
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = sustColor;
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
