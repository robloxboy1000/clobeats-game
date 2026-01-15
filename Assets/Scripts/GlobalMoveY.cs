using System.Collections;
using System.Collections.Generic;
using Octokit;
using UnityEngine;

public class GlobalMoveY : MonoBehaviour
{
    public List<GameObject> objectsToMove;
    public float speed = 5f;
    public bool isMoving = false;
    bool musicStarted = false;
    const float EPS = 0.01f;
    // cached references to avoid per-object/find overhead
    ImprovedStrikeline sl;
    MusicPlayer mp;
    NoteDeletor nd;
    NoteSpawner ns;
    float cachedSpeed;
    float prefsPollInterval = 0.5f; // seconds between PlayerPrefs polls
    float lastPrefsCheckTime = 0f;
    // Start is called before the first frame update
    void Start()
    {
        cachedSpeed = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        speed = cachedSpeed;
        sl = FindAnyObjectByType<ImprovedStrikeline>();
        mp = FindAnyObjectByType<MusicPlayer>();
        nd = FindAnyObjectByType<NoteDeletor>();
        ns = FindAnyObjectByType<NoteSpawner>();
    }

    void Awake()
    {
        cachedSpeed = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        speed = cachedSpeed;
    }
    void OnEnable()
    {
        cachedSpeed = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        speed = cachedSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        // Poll PlayerPrefs occasionally rather than every frame
        if (Time.time - lastPrefsCheckTime > prefsPollInterval)
        {
            lastPrefsCheckTime = Time.time;
            float newSpeed = PlayerPrefs.GetFloat("Hyperspeed", cachedSpeed);
            if (Mathf.Abs(newSpeed - cachedSpeed) > 0.0001f)
            {
                cachedSpeed = newSpeed;
                speed = cachedSpeed;
            }
        }

        if (!isMoving) return;

        // Ensure cached global refs are present
        if (sl == null) sl = FindAnyObjectByType<ImprovedStrikeline>();
        if (mp == null) mp = FindAnyObjectByType<MusicPlayer>();
        if (nd == null) nd = FindAnyObjectByType<NoteDeletor>();
        if (ns == null) ns = FindAnyObjectByType<NoteSpawner>();

        // Iterate backwards so we can remove destroyed/null entries safely
        // Prefer DSP-derived elapsed time (allows negative lead before audio starts).
        float currentSongSeconds = 0f;
        if (mp != null)
        {
            currentSongSeconds = (float)mp.GetElapsedTimeDsp();
        }
        else if (ns != null)
        {
            currentSongSeconds = ns.GetTimeInSecondsAtTick(ns.currentTick);
        }

        for (int i = objectsToMove.Count - 1; i >= 0; --i)
        {
            var obj = objectsToMove[i];
            if (obj == null)
            {
                objectsToMove.RemoveAt(i);
                continue;
            }

            var t = obj.transform;
            // If the object has a ScheduledTime component, compute its world Y directly
            // from its scheduled song time and the currentSongSeconds using NoteSpawner's
            // layout parameters so tempo changes are respected.
            var sched = obj.GetComponent<ScheduledTime>();
            if (sched != null && ns != null)
            {
                float spacingFactor = PlayerPrefs.GetFloat("Hyperspeed", ns != null ? ns.desiredHyperspeedSingleThreaded : speed);
                float strikeY = ns != null ? ns.GetStrikeLineY() : 0f;
                float targetY = strikeY + ns.startingYPosition + ns.startingYOffset + ((sched.scheduledSeconds - currentSongSeconds) + ns.spawnLeadSeconds) * spacingFactor;
                // preserve x,z
                t.position = new Vector3(t.position.x, targetY, t.position.z);
            }
            else
            {
                t.Translate(0f, -speed * Time.deltaTime, 0f, Space.World);
            }

            if (t.position.y < -100f)
            {
                NotePoolManager.Instance.Return(obj);
                objectsToMove.RemoveAt(i);
                continue;
            }

            if (!musicStarted && obj.CompareTag("FirstBar") && sl != null)
            {
                float strikeY = sl.transform.position.y;
                if (t.position.y <= strikeY + EPS)
                {
                    //mp.dspSongStart = AudioSettings.dspTime + 0.25;
                    //mp?.PlayScheduled(mp.dspSongStart);
                    //if (nd != null) nd.isPlaying = true;
                    musicStarted = true;
                }
            }
        }
    }
}
