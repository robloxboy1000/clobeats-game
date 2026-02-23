using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enhanced input handler: shows a hit-window visual, supports strum/chords,
// hits multiple notes in a lane within the window, and supports sustained notes.
public class LaneInputManager : MonoBehaviour
{
    public float hitWindowSeconds = 0.15f; // +/- timing window in seconds

    // Optional: assign a simple transparent prefab (SpriteRenderer or UI Image) that will be
    // scaled/positioned to show the timing window at the strike line.
    public GameObject hitWindowPrefab;

    NoteSpawner spawner;
    GameObject hitWindowInstance;

    // Tracks held frets (for chord/strum behavior)
    HashSet<int> heldLanes = new HashSet<int>();

    // Active sustains that are currently being held after a successful hit
    class ActiveSustain { public GameObject note; public float endTime; public int lane; }
    List<ActiveSustain> activeSustains = new List<ActiveSustain>();
    ImprovedStrikeline strikeline;

    UIUpdater uiUpdater;
    

    public bool autoPlayEnabled = false;
    public bool showHitWindow = false;
    GameObject note = null;

    MusicPlayer mp;


    void Start()
    {
        spawner = FindAnyObjectByType<NoteSpawner>();
        mp = FindAnyObjectByType<MusicPlayer>();
        if (hitWindowPrefab != null)
        {
            hitWindowInstance = Instantiate(hitWindowPrefab, transform);
            hitWindowInstance.name = "HitWindowVisual";
        }
    }

    void Update()
    {
        if (spawner == null) spawner = FindAnyObjectByType<NoteSpawner>();
        if (strikeline == null) strikeline = FindAnyObjectByType<ImprovedStrikeline>();
        if (uiUpdater == null) uiUpdater = FindAnyObjectByType<UIUpdater>();
        UpdateHitWindowVisual();
        UpdateActiveSustains();

        if (heldLanes.Contains(0))
        {
            if (strikeline != null)
            strikeline.HoldLane(0); // zero-based xOffset
        }
        else
        {
            if (strikeline != null)
            strikeline.ReleaseLane(0);
        }
        if (heldLanes.Contains(1))
        {
            if (strikeline != null)
            strikeline.HoldLane(1); // zero-based xOffset
        }
        else
        {
            if (strikeline != null)
            strikeline.ReleaseLane(1);
        }
        if (heldLanes.Contains(2))
        {
            if (strikeline != null)
            strikeline.HoldLane(2); // zero-based xOffset
        }
        else
        {
            if (strikeline != null)
            strikeline.ReleaseLane(2);
        }
        if (heldLanes.Contains(3))
        {
            if (strikeline != null)
            strikeline.HoldLane(3); // zero-based xOffset
        }
        else
        {
            if (strikeline != null)
            strikeline.ReleaseLane(3);
        }
        if (heldLanes.Contains(4))
        {
            if (strikeline != null)
            strikeline.HoldLane(4); // zero-based xOffset
        }
        else
        {
            if (strikeline != null)
            strikeline.ReleaseLane(4);
        }

        if (autoPlayEnabled)
        {
            hitWindowSeconds = 0.05f; // center to strikeline
            for (int i = 0; i <= 7; i++)
            {
                TryHitLane(i, true);
            }
        }
    }

    // Input handlers to wire from the Input System
    public void OnFretPressed(int laneIndex)
    {
        heldLanes.Add(laneIndex);
    }

    public void OnFretReleased(int laneIndex)
    {
        heldLanes.Remove(laneIndex);
        EndSustainForLane(laneIndex);
    }

    // Called when the player strums; will attempt to hit all currently held frets (chords)
    public void OnStrum()
    {
        if (heldLanes.Count == 0) TryHitLane(7);
        // Copy so TryHitLane can modify collections safely
        var lanes = new List<int>(heldLanes);
        foreach (var lane in lanes)
        {
            TryHitLane(lane);
        }
    }

    public int GetHeldLanes()
    {
        return heldLanes.Count;
    }

    // Backwards-compatible single-fret hit (e.g., mapping a single key without a strum action)
    public void OnFretHit(int laneIndex)
    {
        TryHitLane(laneIndex);
    }

    void TryHitLane(int laneIndex, bool autoHit = false)
    {
        if (spawner == null) spawner = FindAnyObjectByType<NoteSpawner>();
        if (spawner == null) return;

        
        float spacingFactor = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        float strikeY = spawner.GetStrikeLineY();
        // Use the same base Y as NoteSpawner so world Y <-> seconds mapping matches:
        float baseY = strikeY + spawner.startingYPosition + spawner.startingYOffset;

        // Try to hit as many consecutive notes in this lane as fall within the timing window.
        while (true)
        {
            note = LaneManager.Instance.GetNextNoteInLane(laneIndex);
            if (note == null) break;

            float noteY = note.transform.position.y;
            // Prefer scheduled song time if available (more accurate and synced to audio DSP clock)
            var sched = note.GetComponent<ScheduledTime>();
            mp = FindAnyObjectByType<MusicPlayer>();
            float currentSongSeconds = mp != null ? (float)mp.GetElapsedTimeDsp() : Time.time;
            float secondsUntil;
            if (sched != null)
            {
                // scheduledSeconds is the song time when the note should be at the strike line
                secondsUntil = sched.scheduledSeconds - currentSongSeconds;
            }
            else
            {
                // Fallback to world-Y -> time mapping used by NoteSpawner
                // NoteSpawner: y = strikeY + startingYPosition + startingYOffset + (timeSeconds + spawnLeadSeconds) * spacingFactor
                // therefore timeSeconds = (y - baseY) / spacingFactor - spawnLeadSeconds
                secondsUntil = (noteY - baseY) / Mathf.Max(0.0001f, spacingFactor) - spawner.spawnLeadSeconds;
            }
            if (Mathf.Abs(secondsUntil) <= hitWindowSeconds)
            {
                //Debug.Log("Hit lane " + laneIndex + " note with " + secondsUntil + " seconds until strike line.");

                var sustainComp = note.GetComponent<SustainedNote>();
                LaneManager.Instance.UnregisterNote(note);

                if (sustainComp != null && sustainComp.durationSeconds > 0f)
                {
                    // Start sustain tracking instead of immediately returning to pool.
                    activeSustains.Add(new ActiveSustain
                    {
                        note = note,
                        endTime = (float)mp.GetElapsedTimeDsp() + sustainComp.durationSeconds,
                        lane = laneIndex
                    });
                    // Optionally: play sustain start FX / scoring events here
                    if (strikeline != null)
                    {
                        strikeline.HitNote(laneIndex - 2); // zero-based xOffset
                        strikeline.HitSustain(laneIndex - 2); // zero-based xOffset
                        strikeline.SLTopHit(laneIndex);
                    }
                    // Spawn / show separate sustain visual managed by SustainManager
                    if (SustainManager.Instance != null)
                    {
                        SustainManager.Instance.StartSustain(laneIndex, sustainComp.durationSeconds);
                    }
                }
                else
                {
                    spawner.ReturnObjectToPool(note);
                    // Optionally: play tap FX / scoring events here
                    if (strikeline != null)
                    {
                        strikeline.HitNote(laneIndex - 2); // zero-based xOffset
                        strikeline.SLTopHit(laneIndex);
                    }
                }

                // continue loop to allow multiple notes in the same lane to be hit if present
                break;
            }
            else
            {
                if (!autoHit)
                {
                    // Next note is outside of timing window -> stop
                    if (strikeline != null)
                    {
                        strikeline.MissNote();
                    }
                }
                //Debug.Log("No more hittable notes in lane " + laneIndex + "; next note in " + secondsUntil + " seconds."); 
                break;
            }
        }
    }

    void UpdateActiveSustains()
    {
        if (activeSustains.Count == 0) return;

        for (int i = activeSustains.Count - 1; i >= 0; --i)
        {
            var s = activeSustains[i];
            if (s.note == null)
            {
                activeSustains.RemoveAt(i);
                continue;
            }

            // If sustain time elapsed, end it
            if ((float)mp.GetElapsedTimeDsp() >= s.endTime)
            {
                Debug.Log(s.lane + " ended");
                if (spawner != null)
                {
                    spawner.ReturnObjectToPool(s.note);
                }
                activeSustains.RemoveAt(i);
                if (strikeline != null)
                {
                    strikeline.DisableSustainSparks(s.lane - 2);
                }
                if (SustainManager.Instance != null)
                {
                    SustainManager.Instance.EndSustain(s.lane);
                }
            }
            else
            {
                // sustain is ongoing; scoring / FX per-frame can be handled here
                if (uiUpdater != null)
                {
                    uiUpdater.UpdateForSustainHold(Time.deltaTime * 20f); // e.g., score for holding sustain
                }
            }
        }
    }

    void EndSustainForLane(int laneIndex)
    {
        // Ends any active sustain that belongs to the released lane (player released fret early)
        for (int i = activeSustains.Count - 1; i >= 0; --i)
        {
            if (activeSustains[i].lane == laneIndex)
            {
                var note = activeSustains[i].note;
                if (note != null && spawner != null) spawner.ReturnObjectToPool(note);
                activeSustains.RemoveAt(i);
                if (strikeline != null)
                {
                    strikeline.DisableSustainSparks(laneIndex - 2);
                }
                if (SustainManager.Instance != null)
                {
                    SustainManager.Instance.EndSustain(laneIndex);
                }
            }
        }
    }

    void UpdateHitWindowVisual()
    {
        if (hitWindowInstance == null || spawner == null) return;

        float strikeY = spawner.GetStrikeLineY();
        float spacingFactor = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        
        // visual height in world units approximated by seconds * spacingFactor
        float height = hitWindowSeconds * spacingFactor;
        // position at strike line
        Vector3 position = hitWindowInstance.transform.position;
        position.y = strikeY;
        hitWindowInstance.transform.position = position;

        // Scale: assume the prefab's localScale.y == 1 corresponds to height == 1 world unit.
        Vector3 scale = hitWindowInstance.transform.localScale;
        scale.y = height;
        hitWindowInstance.transform.localScale = scale;

        if (!showHitWindow)
        {
            hitWindowInstance.SetActive(false);
        }
        else
        {
            hitWindowInstance.SetActive(true);
        }
    }
}
