using System.Collections.Generic;
using UnityEngine;

// Manages lightweight lane-based tracking of notes so we can avoid per-note colliders.
// Notes are registered when spawned and unregistered when returned/destroyed.
// Player input should query the lane for the next note (by position) and perform timing checks.
public class LaneManager : MonoBehaviour
{
    static LaneManager _instance;
    public static LaneManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<LaneManager>();
                if (_instance == null)
                {
                    var go = new GameObject("_LaneManager");
                    #if UNITY_EDITOR
                    //go.hideFlags = HideFlags.HideAndDontSave;
                    #endif
                    _instance = go.AddComponent<LaneManager>();
                }
            }
            return _instance;
        }
    }

    // simple per-lane lists; lane index should match your note.fret values (0..4), open notes can use -1 or a dedicated lane
    Dictionary<int, List<GameObject>> lanes = new Dictionary<int, List<GameObject>>();

    public void RegisterNote(GameObject note, int lane)
    {
        if (note == null) return;
        if (!lanes.TryGetValue(lane, out var list))
        {
            list = new List<GameObject>();
            lanes[lane] = list;
        }
        if (!list.Contains(note)) list.Add(note);
    }

    public void UnregisterNote(GameObject note)
    {
        if (note == null) return;
        foreach (var kv in lanes)
        {
            var list = kv.Value;
            if (list != null && list.Contains(note))
            {
                list.Remove(note);
                return;
            }
        }
    }

    // Returns the next note in the lane which is closest to the strike line (lowest Y) but still ahead.
    // Caller should perform timing judgement using strike Y and spacingFactor.
    public GameObject GetNextNoteInLane(int lane)
    {
        if (!lanes.TryGetValue(lane, out var list) || list.Count == 0) return null;
        GameObject best = null;
        float bestY = float.MaxValue;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var g = list[i];
            if (g == null || g.Equals(null)) { list.RemoveAt(i); continue; }
            float y = g.transform.position.y;
            if (y < bestY)
            {
                bestY = y;
                best = g;
            }
        }
        return best;
    }

    // Convenience: returns the queue length for a lane
    public int LaneCount(int lane)
    {
        if (!lanes.TryGetValue(lane, out var list)) return 0;
        return list.Count;
    }
}
