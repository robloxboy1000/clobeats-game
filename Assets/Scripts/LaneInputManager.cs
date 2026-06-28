using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
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
    int _laneCount;

    int laneCount
    {
        get => _laneCount;
        set
        {
            if (_laneCount != value)
            {
                _laneCount = value;
                OnLaneHeldCountChanged();
            }
        }
    }

    // Active sustains that are currently being held after a successful hit
    class ActiveSustain { public GameObject note; public float endTime; public int lane; }
    List<ActiveSustain> activeSustains = new List<ActiveSustain>();
    ImprovedStrikeline strikeline;

    UIUpdater uiUpdater;
    

    public bool autoPlayEnabled = false;
    public bool showHitWindow = false;
    GameObject note = null;

    MusicPlayer mp;
    public float secondsUntil;


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

    public void ToggleAutoplay(bool toggle)
    {
        autoPlayEnabled = toggle;
    }

    void Update()
    {
        if (spawner == null) spawner = FindAnyObjectByType<NoteSpawner>();
        if (strikeline == null) strikeline = FindAnyObjectByType<ImprovedStrikeline>();
        if (uiUpdater == null) uiUpdater = FindAnyObjectByType<UIUpdater>();
        UpdateHitWindowVisual();
        UpdateActiveSustains();
        laneCount = heldLanes.Count;

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
            TryHitLane(0, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(1, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(2, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(3, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(4, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(7, true, NoteVisualChanger.NoteType.Forced, true);
            TryHitLane(8, true, NoteVisualChanger.NoteType.Forced, true);
        }
    }

    // Input handlers to wire from the Input System
    public void OnFretPressed(int laneIndex)
    {
        heldLanes.Add(laneIndex);
        if (uiUpdater.savednotesHit > 0)
        {
            TryHitLane(laneIndex, false, NoteVisualChanger.NoteType.HOPO, true);
        }
        else
        {
            TryHitLane(laneIndex, false, NoteVisualChanger.NoteType.Tap, true);
        }
    }

    public void OnFretReleased(int laneIndex)
    {
        heldLanes.Remove(laneIndex);
        EndSustainForLane(laneIndex);
    }

    // Called when the player strums; will attempt to hit all currently held frets (chords)
    public bool OnStrum()
    {
        if (heldLanes.Count == 0)
        {
            if (TryHitLane(7))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            // Copy so TryHitLane can modify collections safely
            var lanes = new List<int>(heldLanes);
            Dictionary<int, bool> laneBools = new Dictionary<int, bool>();
            foreach (var lane in lanes)
            {
                laneBools.Add(lane, TryHitLane(lane, false, NoteVisualChanger.NoteType.Forced | NoteVisualChanger.NoteType.HOPO | NoteVisualChanger.NoteType.Tap, true));
            }
            if (laneBools.ContainsValue(false))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    // Backwards-compatible single-fret hit (e.g., mapping a single key without a strum action)
    public void OnFretHit(int laneIndex)
    {
        TryHitLane(laneIndex, false, NoteVisualChanger.NoteType.Forced | NoteVisualChanger.NoteType.HOPO | NoteVisualChanger.NoteType.Tap, true);
    }
    public void OnLaneHeldCountChanged()
    {
        OnStrum();
    }

    bool TryHitLane(int laneIndex, bool autoHit = false, NoteVisualChanger.NoteType noteType = NoteVisualChanger.NoteType.Forced, bool isChord = false)
    {
        if (spawner == null) spawner = FindAnyObjectByType<NoteSpawner>();
        if (spawner == null) return false;
        float spacingFactor = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        float strikeY = spawner.GetStrikeLineY();
        // Use the same base Y as NoteSpawner so world Y <-> seconds mapping matches:
        float baseY = strikeY + spawner.startingYPosition + spawner.startingYOffset;
        note = LaneManager.Instance.GetNextNoteInLane(laneIndex);
        if (note == null) return false;
        float noteY = note.transform.position.y;
        // Prefer scheduled song time if available (more accurate and synced to audio DSP clock)
        var sched = note.GetComponent<ScheduledTime>();
        mp = FindAnyObjectByType<MusicPlayer>();
        float currentSongSeconds = mp != null ? (float)mp.GetElapsedTime() : Time.time;
        var visual = note.GetComponent<NoteVisualChanger>();
            
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
        float autoHitWindowSeconds = Mathf.Min(0, hitWindowSeconds);
        if (Mathf.Abs(secondsUntil) <= hitWindowSeconds && !autoHit)
        {
            //Debug.Log("PlayerHit lane " + laneIndex + " note with " + secondsUntil + " seconds until strike line.");
            var sustainComp = note.GetComponent<SustainedNote>();
            if (visual != null)
            {
                if (visual.currentNoteType == noteType)
                {
                    LaneManager.Instance.UnregisterNote(note);
                }
                else
                {
                    return false;
                }
            }
            

            Color sustainColor = Color.white;

            if (sustainComp != null && sustainComp.durationSeconds > 0f)
            {
                spawner.ReturnObjectToPool(note);
                // Start sustain tracking instead of immediately returning to pool.
                activeSustains.Add(new ActiveSustain
                {
                    note = note,
                    endTime = spawner.GetTimeInSecondsAtTick(spawner.currentTick) + sustainComp.durationSeconds,
                    lane = laneIndex
                });
                // Optionally: play sustain start FX / scoring events here
                if (strikeline != null)
                {
                    strikeline.HitNote(laneIndex - 2); // zero-based xOffset
                    strikeline.HitSustain(laneIndex - 2); // zero-based xOffset
                    strikeline.SLTopHit(laneIndex);
                }
                int visualLaneInd = 0;
                switch (laneIndex)
                {
                    case 0: sustainColor = Color.green; visualLaneInd = 0; break;
                    case 1: sustainColor = Color.red; visualLaneInd = 1; break;
                    case 2: sustainColor = Color.yellow; visualLaneInd = 2; break;
                    case 3: sustainColor = Color.blue; visualLaneInd = 3; break;
                    case 4: sustainColor = new Color(1f, 0.5f, 0f); visualLaneInd = 4; break;
                    case 7: sustainColor = Color.magenta; visualLaneInd = 2; break;
                    case 8: sustainColor = Color.green; visualLaneInd = 2; break;
                }
                // Spawn / show separate sustain visual managed by SustainManager
                if (SustainManager.Instance != null)
                {
                    SustainManager.Instance.StartSustain(visualLaneInd, sustainComp.durationSeconds, sustainColor, isChord);
                }
            }
            else
            {
                spawner.ReturnObjectToPool(note);
                // Optionally: play tap FX / scoring events here
                if (strikeline != null)
                {
                    if (laneIndex == 7)
                    {
                        strikeline.HitNote(7);
                        strikeline.SLTopHit(laneIndex);
                    }
                    else
                    {
                        strikeline.HitNote(laneIndex - 2); // zero-based xOffset
                        strikeline.SLTopHit(laneIndex);
                    }
                }
            }
            return true;
        }
        else if (secondsUntil <= autoHitWindowSeconds && autoHit)
        {
            //Debug.Log("AutoHit lane " + laneIndex + " note with " + secondsUntil + " seconds until strike line.");

            var sustainComp = note.GetComponent<SustainedNote>();
            if (visual != null)
            {
                if (!autoHit)
                {
                    if (visual.currentNoteType == noteType)
                    {
                        LaneManager.Instance.UnregisterNote(note);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    LaneManager.Instance.UnregisterNote(note);
                }
                
            }

            Color sustainColor = Color.white;

            if (sustainComp != null && sustainComp.durationSeconds > 0f)
            {
                spawner.ReturnObjectToPool(note);
                // Start sustain tracking instead of immediately returning to pool.
                activeSustains.Add(new ActiveSustain
                {
                    note = note,
                    endTime = spawner.GetTimeInSecondsAtTick(spawner.currentTick) + sustainComp.durationSeconds,
                    lane = laneIndex
                });
                // Optionally: play sustain start FX / scoring events here
                if (strikeline != null)
                {
                    strikeline.HitNote(laneIndex - 2); // zero-based xOffset
                    strikeline.HitSustain(laneIndex - 2); // zero-based xOffset
                    strikeline.SLTopHit(laneIndex);
                }
                int visualLaneInd = 0;
                switch (laneIndex)
                {
                    case 0: sustainColor = Color.green; visualLaneInd = 0; break;
                    case 1: sustainColor = Color.red; visualLaneInd = 1; break;
                    case 2: sustainColor = Color.yellow; visualLaneInd = 2; break;
                    case 3: sustainColor = Color.blue; visualLaneInd = 3; break;
                    case 4: sustainColor = new Color(1f, 0.5f, 0f); visualLaneInd = 4; break;
                    case 7: sustainColor = Color.magenta; visualLaneInd = 2; break;
                    case 8: sustainColor = Color.green; visualLaneInd = 2; break;
                }
                // Spawn / show separate sustain visual managed by SustainManager
                if (SustainManager.Instance != null)
                {
                    SustainManager.Instance.StartSustain(visualLaneInd, sustainComp.durationSeconds, sustainColor, isChord);
                }
            }
            else
            {
                spawner.ReturnObjectToPool(note);
                // Optionally: play tap FX / scoring events here
                if (strikeline != null)
                {
                    if (laneIndex == 7)
                    {
                        strikeline.HitNote(7);
                        strikeline.SLTopHit(laneIndex);
                    }
                    else
                    {
                        strikeline.HitNote(laneIndex - 2); // zero-based xOffset
                        strikeline.SLTopHit(laneIndex);
                    }
                }
            }
            return true;
        }
        else
        {
            return false;
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
            if (spawner.GetTimeInSecondsAtTick(spawner.currentTick) >= s.endTime)
            {
                //Debug.Log(s.lane + " ended");
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
                    uiUpdater.UpdateForSustainHold(uiUpdater.inStar ? Time.deltaTime * 40f : Time.deltaTime * 20f); // e.g., score for holding sustain
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
