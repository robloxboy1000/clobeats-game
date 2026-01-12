using System.Collections.Generic;
using UnityEngine;

// Central manager that batches visibility checks for many VisibilityGate instances.
// This removes the per-object Update() cost when hundreds or thousands of gates exist.
public class VisibilityGateManager : MonoBehaviour
{
    static VisibilityGateManager _instance;
    public static VisibilityGateManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in scene
                _instance = FindAnyObjectByType<VisibilityGateManager>();
                if (_instance == null)
                {
                    var go = new GameObject("_VisibilityGateManager");
                    // hide from scene hierarchy during edit/play to avoid clutter
                    #if UNITY_EDITOR
                    go.hideFlags = HideFlags.HideAndDontSave;
                    #endif
                    _instance = go.AddComponent<VisibilityGateManager>();
                }
            }
            return _instance;
        }
    }

    readonly List<VisibilityGate> gates = new List<VisibilityGate>();

    // Register a gate. Duplicate registration is ignored.
    public void Register(VisibilityGate gate)
    {
        if (gate == null) return;
        if (!gates.Contains(gate)) gates.Add(gate);
    }

    // Unregister when revealed/destroyed
    public void Unregister(VisibilityGate gate)
    {
        if (gate == null) return;
        gates.Remove(gate);
    }

    void Update()
    {
        if (gates.Count == 0) return;

        // Iterate backwards to allow removals while iterating
        for (int i = gates.Count - 1; i >= 0; i--)
        {
            var g = gates[i];
            if (g == null || g.Equals(null))
            {
                gates.RemoveAt(i);
                continue;
            }

            g.CheckAndReveal();

            // If gate revealed itself it will unregister; but ensure cleanup in case it didn't
            if (g.IsRevealed)
            {
                if (i >= gates.Count) continue;
                gates.RemoveAt(i);
            }
        }
    }
}
