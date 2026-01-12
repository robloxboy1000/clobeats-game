using System.Collections.Generic;
using UnityEngine;

// Manages sustain visuals when they are instantiated separately from note objects.
public class SustainManager : MonoBehaviour
{
    public static SustainManager Instance;

    [Tooltip("Prefab for the sustain visual. Must have a SustainVisual component.")]
    public GameObject sustainPrefab;

    [Tooltip("Optional container transform to parent sustain visuals under.")]
    public Transform container;

    [Tooltip("World X positions for each lane index (set in inspector).")]
    public float[] laneXPositions;

    Queue<GameObject> pool = new Queue<GameObject>();
    Dictionary<int, SustainVisual> active = new Dictionary<int, SustainVisual>();

    NoteSpawner spawner;

    void Awake()
    {
        if (Instance == null) Instance = this; else if (Instance != this) Destroy(gameObject);
        if (container == null) container = transform;
        spawner = FindAnyObjectByType<NoteSpawner>();
    }

    GameObject GetFromPool()
    {
        while (pool.Count > 0)
        {
            var g = pool.Dequeue();
            if (g != null) { g.SetActive(true); return g; }
        }
        return Instantiate(sustainPrefab);
    }

    void ReturnToPool(GameObject g)
    {
        if (g == null) return;
        g.SetActive(false);
        pool.Enqueue(g);
    }

    // Start a sustain visual for the given lane and durationSeconds.
    public void StartSustain(int laneIndex, float durationSeconds)
    {
        if (sustainPrefab == null) return;

        // End existing on that lane
        if (active.TryGetValue(laneIndex, out var old))
        {
            old.ForceEnd();
            active.Remove(laneIndex);
        }

        var go = GetFromPool();
        go.transform.SetParent(container, false);

        var v = go.GetComponent<SustainVisual>();
        if (v == null) v = go.AddComponent<SustainVisual>();

        float baseY = spawner != null ? spawner.GetStrikeLineY() : 0f;
        float spacing = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        float x = (laneXPositions != null && laneIndex >= 0 && laneIndex < laneXPositions.Length)
            ? laneXPositions[laneIndex]
            : laneIndex;

        v.Setup(laneIndex, x, baseY, durationSeconds, spacing);
        active[laneIndex] = v;
    }

    // End a sustain visual early for a lane
    public void EndSustain(int laneIndex)
    {
        if (active.TryGetValue(laneIndex, out var v))
        {
            v.ForceEnd();
            active.Remove(laneIndex);
            ReturnToPool(v.gameObject);
        }
    }

    // Called by SustainVisual when it naturally finishes (duration elapsed)
    internal void NotifyVisualFinished(int laneIndex, SustainVisual v)
    {
        if (active.TryGetValue(laneIndex, out var current) && current == v)
        {
            active.Remove(laneIndex);
        }
        ReturnToPool(v.gameObject);
    }
}
