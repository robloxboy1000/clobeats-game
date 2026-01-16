using System.Collections.Generic;
using Octokit;
using UnityEngine;

// Generic pool manager keyed by prefab GameObject. Intended for notes and sustains.
public class NotePoolManager : MonoBehaviour
{
    static NotePoolManager _instance;
    public static NotePoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<NotePoolManager>();
                if (_instance == null)
                {
                    var go = new GameObject("_NotePoolManager");
                    #if UNITY_EDITOR
                    go.hideFlags = HideFlags.HideAndDontSave;
                    #endif
                    _instance = go.AddComponent<NotePoolManager>();
                }
            }
            return _instance;
        }
    }

    // Pool per prefab
    Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    NoteSpawner ns;

    void Update()
    {
        if (ns == null) ns = FindAnyObjectByType<NoteSpawner>();
    }

    // Prewarm a pool for a prefab
    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        if (!pools.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pools[prefab] = q;
        }

        for (int i = 0; i < count; i++)
        {
            var inst = Instantiate(prefab, ns.highwayTransform);
            var pn = inst.GetComponent<PooledNote>();
            if (pn == null) pn = inst.AddComponent<PooledNote>();
            pn.prefab = prefab;
            inst.SetActive(false);
            q.Enqueue(inst);
        }
    }

    // Get an instance for a prefab. Will create new if pool empty.
    public GameObject Get(GameObject prefab)
    {
        if (prefab == null) return null;
        if (!pools.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>();
            pools[prefab] = q;
        }

        while (q.Count > 0)
        {
            var g = q.Dequeue();
            if (g == null) continue;
            return g;
        }

        var newGo = Instantiate(prefab, ns.highwayTransform);
        var pnNew = newGo.GetComponent<PooledNote>();
        if (pnNew == null) pnNew = newGo.AddComponent<PooledNote>();
        pnNew.prefab = prefab;
        newGo.SetActive(false);
        return newGo;
    }

    // Return an object to its pool. Requires PooledNote.prefab to be set.
    public void Return(GameObject go)
    {
        if (go == null) return;
        var pn = go.GetComponent<PooledNote>();
        if (pn == null || pn.prefab == null)
        {
            // Not a pooled object; destroy fallback
            Destroy(go);
            return;
        }

        // Reset state and enqueue
        go.SetActive(false);
        if (!pools.TryGetValue(pn.prefab, out var q))
        {
            q = new Queue<GameObject>();
            pools[pn.prefab] = q;
        }
        q.Enqueue(go);
    }
}
