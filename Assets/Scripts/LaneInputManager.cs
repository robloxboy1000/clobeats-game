using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    // Prevent OnStrum being processed multiple times in the same frame or too quickly.
    int lastStrumFrame = -1;
    float lastStrumTime = -999f;
    [Tooltip("Minimum seconds between processed strums to prevent duplicate triggers")]
    public float strumCooldownSeconds = 0.05f;

    public bool autoPlayEnabled = false;
    public bool showHitWindow = false;

    void Start()
    {
        spawner = FindAnyObjectByType<NoteSpawner>();
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
        // One-shot guard: ignore additional calls in the same frame or within a short cooldown
        if (Time.frameCount == lastStrumFrame) return;
        if (Time.unscaledTime - lastStrumTime < strumCooldownSeconds) return;
        lastStrumFrame = Time.frameCount;
        lastStrumTime = Time.unscaledTime;

        if (heldLanes.Count == 0) return;
        // Copy so TryHitLane can modify collections safely
        var lanes = new List<int>(heldLanes);
        foreach (var lane in lanes)
        {
            TryHitLane(lane);
        }
    }

    // Backwards-compatible single-fret hit (e.g., mapping a single key without a strum action)
    public void OnFretHit(int laneIndex)
    {
        TryHitLane(laneIndex);
    }

    void TryHitLane(int laneIndex)
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
            var note = LaneManager.Instance.GetNextNoteInLane(laneIndex);
            if (note == null) break;

            float noteY = note.transform.position.y;
            // Convert world Y to seconds-until-strike using the same mapping NoteSpawner uses.
            // NoteSpawner places objects at: y = strikeY + startingYPosition + startingYOffset + (timeSeconds + spawnLeadSeconds) * spacingFactor
            // therefore timeSeconds = (y - baseY) / spacingFactor - spawnLeadSeconds
            float secondsUntil = (noteY - baseY) / Mathf.Max(0.0001f, spacingFactor) - spawner.spawnLeadSeconds;
            if (Mathf.Abs(secondsUntil) <= hitWindowSeconds)
            {
                Debug.Log("Hit lane " + laneIndex + " note with " + secondsUntil + " seconds until strike line.");

                var sustainComp = note.GetComponent<SustainedNote>();
                //LaneManager.Instance.UnregisterNote(note);

                if (sustainComp != null && sustainComp.durationSeconds > 0f)
                {
                    // Start sustain tracking instead of immediately returning to pool.
                    activeSustains.Add(new ActiveSustain
                    {
                        note = note,
                        endTime = Time.time + sustainComp.durationSeconds,
                        lane = laneIndex
                    });
                    // Optionally: play sustain start FX / scoring events here
                    if (strikeline != null)
                    {
                        strikeline.HitNote(laneIndex - 2); // zero-based xOffset
                        strikeline.HitSustain(laneIndex - 2); // zero-based xOffset
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
                    }
                }

                // continue loop to allow multiple notes in the same lane to be hit if present
                break;
            }
            else
            {
                // Next note is outside of timing window -> stop
                if (strikeline != null)
                {
                    strikeline.MissNote();
                }
                Debug.Log("No more hittable notes in lane " + laneIndex + "; next note in " + secondsUntil + " seconds."); 
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
            if (Time.time >= s.endTime)
            {
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
