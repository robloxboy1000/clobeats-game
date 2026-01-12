using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks; // Added namespace for Task support
using UnityEngine.SceneManagement;
using Unity.VisualScripting;


public class NoteSpawner : MonoBehaviour
{
    // strum
    public GameObject greenNotePrefab;
    public GameObject redNotePrefab;
    public GameObject yellowNotePrefab;
    public GameObject blueNotePrefab;
    public GameObject orangeNotePrefab;




    // open
    public GameObject openNotePrefab; 

    public GameObject barPrefab; // Prefab for bars
    public GameObject beatPrefab; // Prefab for beats

    public List<NoteInfo> notes = new List<NoteInfo>();
    public List<SyncInfo> syncTrack = new List<SyncInfo>();
    // map tick -> concatenated event string
    public Dictionary<int, string> events = new Dictionary<int, string>();
    

    public class NoteInfo
    {
        public float spawnTime;
        public int fret;
        public int length; // For sustain notes
        public bool isHopo = false; // whether this note should be treated as a HOPO
    }

    // Prewarm pooled note and sustain prefabs based on note density in the chart.
    private void PrewarmNotePools()
    {
        Debug.Log($"Prewarming note pools...");
        try
        {
            // count notes per fret and sustains
            int green = 0, red = 0, yellow = 0, blue = 0, orange = 0, open = 0;

            foreach (var n in notes)
            {
                if (n == null) continue;
                switch (n.fret)
                {
                    case 0: green++; break;
                    case 1: red++; break;
                    case 2: yellow++; break;
                    case 3: blue++; break;
                    case 4: orange++; break;
                    case 7: open++; break;
                    default: break;
                }
            }

            // multiplier and minimum to avoid too-small pools
            const float multiplier = 1.25f;
            const int minPool = 8;
     
            if (greenNotePrefab != null)
            {
                int cnt = Mathf.Max(minPool, Mathf.CeilToInt(green * multiplier));
                NotePoolManager.Instance.Prewarm(greenNotePrefab, cnt);
            }
            if (redNotePrefab != null)
            {
                int cnt = Mathf.Max(minPool, Mathf.CeilToInt(red * multiplier));
                NotePoolManager.Instance.Prewarm(redNotePrefab, cnt);
            }
            if (yellowNotePrefab != null)
            {
                int cnt = Mathf.Max(minPool, Mathf.CeilToInt(yellow * multiplier));
                NotePoolManager.Instance.Prewarm(yellowNotePrefab, cnt);
            }
            if (blueNotePrefab != null)
            {
                int cnt = Mathf.Max(minPool, Mathf.CeilToInt(blue * multiplier));
                NotePoolManager.Instance.Prewarm(blueNotePrefab, cnt);
            }
            if (orangeNotePrefab != null)
            {
                int cnt = Mathf.Max(minPool, Mathf.CeilToInt(orange * multiplier));
                NotePoolManager.Instance.Prewarm(orangeNotePrefab, cnt);
            }
            if (openNotePrefab != null)
            {
                int cnt = Mathf.Max(minPool, Mathf.CeilToInt(open * multiplier));
                NotePoolManager.Instance.Prewarm(openNotePrefab, cnt);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("PrewarmNotePools failed: " + ex.Message);
        }
    }

    public class SyncInfo
    {
        public float time;
        public float bpm;
        public string timeSignature;
    }

    

    private int resolution = 192; // Default resolution
    float currentBpm = 120f; // Default BPM
    public int songLengthInTicks = 0;
    public int currentTick = 0;

    private UIUpdater uiUpdater;

    public float startingYPosition = 16f; // Configurable starting Y position for notes and bars/beats
    public float spawnLeadSeconds = 2f; // Extra seconds added to spacing so notes spawn further away from the strikeline
    public float preSpawnAheadSeconds = 10f; // How far ahead (in seconds) to spawn bars/beats
    // If true, pre-spawn uses an absolute-time "long plane" layout (legacy behavior):
    // y = strikeY + startingYPosition + startingYOffset + (timeSeconds + spawnLeadSeconds) * spacingFactor
    // If false, spawn positions are computed relative to currentMusicTime (timeSeconds - currentSongSeconds)
    public bool preSpawnLongPlane = true;
    
    // Optional: use the strikeline's world Z as the reference center (commonly 0)
    public Transform strikeLineTransform;
    public bool useStrikeLineY = true;
    // If you prefer an offset from the strikeline instead of absolute startingZPosition, set this
    public float startingYOffset = 0f;
    public int barPoolSize = 16;
    public int beatPoolSize = 64;

    // Pools and active lists
    private Queue<GameObject> barPool = new Queue<GameObject>();
    private Queue<GameObject> beatPool = new Queue<GameObject>();
    private List<GameObject> activeBars = new List<GameObject>();
    private List<GameObject> activeBeats = new List<GameObject>();

    public float recycleGraceSeconds = 1f; // seconds to wait after pass before recycling
    // Track scheduled song seconds for pooled objects to avoid relying on transform.z
    private Dictionary<GameObject, float> scheduledTimeByObject = new Dictionary<GameObject, float>();

    private Coroutine barBeatSpawnerCoroutine = null;
    private MusicPlayer musicPlayer;
    string videoClipPath;
    bool videoPrepared = false;
    [SerializeField]
    private SongLoader songLoader;
    [SerializeField]
    private SongFolderLoader songFolderLoader;
    private string chartFileData = "";
    private string audioClipPath = "";
    private string guitarClipPath = "";
    private string desiredDifficultySingleThreaded = "Expert";

    public int hopoThreshold = 170; // ms threshold for HOPOs

    public float noteSpawningXOffset = 0; // used for multiplayer
    public string playerType = "Single";
    private float desiredHyperspeedSingleThreaded = 5f;

    void Start()
    {
        uiUpdater = FindFirstObjectByType<UIUpdater>(); // Initialize UIUpdater
        musicPlayer = FindFirstObjectByType<MusicPlayer>();
        songLoader = FindFirstObjectByType<SongLoader>();
        songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
        
    }
    async void Awake()
    {
        desiredDifficultySingleThreaded = PlayerPrefs.GetString("SelectedDifficulty", "Expert");
        desiredHyperspeedSingleThreaded = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        CreatePools();
        PrewarmNotePools();
        if (uiUpdater != null)
        {
            uiUpdater.loadingOverlay.SetActive(true);
            uiUpdater.songInfoPanel.SetActive(false);
        }
        await System.Threading.Tasks.Task.Delay(5000);
        await Load();
    }
    
    private async Task Load()
    {
        if (songLoader != null && songLoader.songDataSet)
        {
            await songLoader.LoadSongData((txtAsset, audioClip, guitarClip, videoClip) =>
            {
                chartFileData = txtAsset;
                audioClipPath = audioClip;
                guitarClipPath = guitarClip;
                videoClipPath = videoClip;
            });
            await songFolderLoader.LoadIniFile(System.IO.File.ReadAllText(songFolderLoader.songFolderPath + @"\song.ini"));
            musicPlayer.loadAudio(audioClipPath);
            musicPlayer.loadVideo(videoClipPath);
            musicPlayer.loadGuitarAudio(guitarClipPath);
            
            
            await ParseChartFile(chartFileData);
            //preSpawnAheadSeconds = musicPlayer.CalculateSongEndTimeInMilliseconds() / 1000f + 5f; // extend ahead time to cover full song length plus buffer
            //await System.Threading.Tasks.Task.Delay(1000);
            if (!string.IsNullOrEmpty(videoClipPath))
            {
                Debug.Log("Loading video clip: " + videoClipPath);
                LoadVideoVenue();
                    
            }

            if (uiUpdater != null)
            {
                Debug.Log("Song data loaded successfully.");
                await System.Threading.Tasks.Task.Delay(500);
                GameManager gm = FindFirstObjectByType<GameManager>();
                if (gm != null)
                {
                    StartCoroutine(gm.PlaySong());
                }
            }

        }
        else if (songLoader != null && !songLoader.songDataSet)
        {
            Debug.LogError("Song data not set in SongLoader.");
        }
        else
        {
            Debug.LogError("SongLoader instance not found.");
        }
    }
    private void LoadVideoVenue()
    {
        SceneManager.LoadSceneAsync("Image_Video Venue", LoadSceneMode.Additive);
        if (SceneManager.GetSceneByName("Blank").isLoaded)
        {
            SceneManager.UnloadSceneAsync("Blank");
        }
        else if (SceneManager.GetSceneByName("3DVenue").isLoaded)
        {
            SceneManager.UnloadSceneAsync("3DVenue");
        }
    }


    public void Play()
    {
        StartMovingNotes();
        // Start managed spawning of bars/beats (pooled, ahead-window)
        if (PlayerPrefs.GetInt("EnableBarBeats", 1) == 1)
        {
            if (barBeatSpawnerCoroutine == null)
            {
                barBeatSpawnerCoroutine = StartCoroutine(ManageBarBeatSpawning());
            }
        }
        else
        {
            // PlayerPrefs requests no bars/beats; still spawn a single FirstBar to synchronize music.
            SpawnFirstBarOnly();
        }
    }
        

    // Spawn only the first bar (tagged FirstBar) so music can be synchronized while global bar/beat spawning is disabled.
    private void SpawnFirstBarOnly()
    {
        try
        {
            // Avoid spawning duplicate FirstBar
            foreach (var b in activeBars)
            {
                if (b != null && b.CompareTag("FirstBar")) return;
            }
        }
        catch (Exception) { }

        int firstBarTick = 0;
        if (syncTrack != null && syncTrack.Count > 0) firstBarTick = (int)syncTrack[0].time;
        float barTime = GetTimeInSecondsAtTick(firstBarTick);
        float currentSongSeconds = 0f;
        if (musicPlayer == null) musicPlayer = FindAnyObjectByType<MusicPlayer>();
        if (musicPlayer != null) currentSongSeconds = (float)musicPlayer.GetElapsedTime();

        float spacingFactor = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        float strikeY = GetStrikeLineY();
        float secondsUntilBar = barTime - currentSongSeconds;
        float barY = strikeY + startingYPosition + startingYOffset + (secondsUntilBar + spawnLeadSeconds) * spacingFactor;

        var bar = GetBarFromPool();
        if (bar == null) return;
        bar.transform.position = new Vector3(0f, barY, 0f);
        var bGate = bar.GetComponent<VisibilityGate>();
        if (bGate == null) bGate = bar.AddComponent<VisibilityGate>();
        bGate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
        bar.SetActive(true);
        scheduledTimeByObject[bar] = barTime;

        GlobalMoveY gm = FindAnyObjectByType<GlobalMoveY>();
        if (gm != null && !gm.objectsToMove.Contains(bar)) gm.objectsToMove.Add(bar);
        activeBars.Add(bar);
        try { bar.tag = "FirstBar"; } catch (Exception) { }
    }
        
    

    private void CreatePools()
    {
        if (barPrefab != null && barPool.Count == 0)
        {
            for (int i = 0; i < barPoolSize; i++)
            {
                var go = Instantiate(barPrefab);
                // Ensure visibility gate exists and is initialized so pooled bars are hidden until reveal Z
                var gate = go.GetComponent<VisibilityGate>();
                if (gate == null) gate = go.AddComponent<VisibilityGate>();
                gate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
                go.SetActive(false);
                barPool.Enqueue(go);
            }
        }

        if (beatPrefab != null && beatPool.Count == 0)
        {
            for (int i = 0; i < beatPoolSize; i++)
            {
                var go = Instantiate(beatPrefab);
                var gate = go.GetComponent<VisibilityGate>();
                if (gate == null) gate = go.AddComponent<VisibilityGate>();
                gate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
                go.SetActive(false);
                beatPool.Enqueue(go);
            }
        }
    }

    private GameObject GetBarFromPool()
    {
        // Skip any destroyed/null objects in the pool
        while (barPool.Count > 0)
        {
            var go = barPool.Dequeue();
            if (go != null)
            {
                // ensure no stale scheduled mapping remains
                if (scheduledTimeByObject.ContainsKey(go)) scheduledTimeByObject.Remove(go);
                // initialize visibility gate for this instance in case reveal Z changed
                var gate = go.GetComponent<VisibilityGate>();
                if (gate == null) gate = go.AddComponent<VisibilityGate>();
                gate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
                return go;
            }
        }

        var newGo = Instantiate(barPrefab);
        newGo.SetActive(false);
        return newGo;
    }

    private void ReturnBarToPool(GameObject go)
    {
        // If the object has been destroyed elsewhere, skip enqueueing
        if (go == null) return;
        if (go.Equals(null)) return;
        // remove from GlobalMoveY and clear any scheduled-time mapping for this object and return to pool
        RemoveObjectFromGlobalMoveY(go);
        if (scheduledTimeByObject.ContainsKey(go)) scheduledTimeByObject.Remove(go);
        // hide renderers before returning to pool
        var gate = go.GetComponent<VisibilityGate>();
        if (gate == null) gate = go.AddComponent<VisibilityGate>();
        gate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
        go.SetActive(false);
        barPool.Enqueue(go);
    }

    private GameObject GetBeatFromPool()
    {
        // Skip any destroyed/null objects in the pool
        while (beatPool.Count > 0)
        {
            var go = beatPool.Dequeue();
            if (go != null)
            {
                if (scheduledTimeByObject.ContainsKey(go)) scheduledTimeByObject.Remove(go);
                var gate = go.GetComponent<VisibilityGate>();
                if (gate == null) gate = go.AddComponent<VisibilityGate>();
                gate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
                return go;
            }
        }

        var newGo = Instantiate(beatPrefab);
        newGo.SetActive(false);
        return newGo;
    }

    private void ReturnBeatToPool(GameObject go)
    {
        if (go == null) return;
        if (go.Equals(null)) return;
        RemoveObjectFromGlobalMoveY(go);
        if (scheduledTimeByObject.ContainsKey(go)) scheduledTimeByObject.Remove(go);
        var gate = go.GetComponent<VisibilityGate>();
        if (gate == null) gate = go.AddComponent<VisibilityGate>();
        gate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
        go.SetActive(false);
        beatPool.Enqueue(go);
    }

    // Public helper for other systems to return objects to the appropriate pool or safely deactivate them.
    public void ReturnObjectToPool(GameObject go)
    {
        if (go == null) return;
        if (go.Equals(null)) return;

        // Ensure the lane manager doesn't retain references to this object
        try { LaneManager.Instance.UnregisterNote(go); } catch (Exception) { }

        // If this is a pooled note/sustain, return to NotePoolManager
        var pn = go.GetComponent<PooledNote>();
        if (pn != null)
        {
            // remove from movement and scheduling before pooling
            RemoveObjectFromGlobalMoveY(go);
            if (scheduledTimeByObject.ContainsKey(go)) scheduledTimeByObject.Remove(go);
            try { NotePoolManager.Instance.Return(go); } catch (Exception) { go.SetActive(false); }
            return;
        }

        // If this object is tracked as an active bar or beat, remove from active lists and route to the right pool
        if (activeBars.Contains(go))
        {
            activeBars.Remove(go);
            if (scheduledTimeByObject.ContainsKey(go)) scheduledTimeByObject.Remove(go);
            ReturnBarToPool(go);
            return;
        }

        if (activeBeats.Contains(go))
        {
            activeBeats.Remove(go);
            if (scheduledTimeByObject.ContainsKey(go)) scheduledTimeByObject.Remove(go);
            ReturnBeatToPool(go);
            return;
        }

        // Heuristics based on tag or name
        try
        {
            if (go.CompareTag("FirstBar") || go.name.ToLower().Contains("bar"))
            {
                ReturnBarToPool(go);
                return;
            }
        }
        catch (Exception) { }

        try
        {
            if (go.name.ToLower().Contains("beat"))
            {
                ReturnBeatToPool(go);
                return;
            }
        }
        catch (Exception) { }

        // If it has a Sustain component, simply deactivate it and remove from movement
        var sustainComp = go.GetComponent<Sustain>();
        if (sustainComp != null)
        {
            RemoveObjectFromGlobalMoveY(go);
            go.SetActive(false);
            return;
        }

        // Fallback: just remove from GlobalMoveY and deactivate
        RemoveObjectFromGlobalMoveY(go);
        go.SetActive(false);
    }

    private async Task ParseChartFile(string data)
    {
        Debug.Log("Parsing chart file...");
        string[] lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        // Diagnostic counters
        int parsedNotesCount = 0;
        int failedNoteLines = 0;
        List<string> failedSamples = new List<string>();

        // Determine which Single section to parse: prefer PlayerPrefs-selected difficulty, else Expert, else first Single found.
        string desiredDifficulty = desiredDifficultySingleThreaded; // PlayerPrefs can only be called on main thread.
        string chosenSingleHeader = null; // e.g. "[ExpertSingle]"
        List<string> singleHeaders = new List<string>();
        foreach (string raw in lines)
        {
            string t = raw.Trim();
            if (t.StartsWith("[") && t.EndsWith("]") && t.IndexOf("Single", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                singleHeaders.Add(t);
            }
        }

        // Pick matching header if available
        foreach (string hdr in singleHeaders)
        {
            // case-insensitive match
            if (hdr.IndexOf(desiredDifficulty, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                chosenSingleHeader = hdr;
                break;
            }
        }

        if (chosenSingleHeader == null)
        {
            // prefer Expert if present
            var expert = singleHeaders.Find(h => h.IndexOf("ExpertSingle", StringComparison.OrdinalIgnoreCase) >= 0);
            if (expert != null) chosenSingleHeader = expert; else if (singleHeaders.Count > 0) chosenSingleHeader = singleHeaders[0];
        }

        bool inSongSection = false;
        bool inEventsSection = false;
        bool inDesiredSingleSection = false;
        bool inSyncTrackSection = false;
        NoteInfo previousNote = null; // Track the previous note

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("["))
            {
                inSongSection = trimmedLine.Equals("[Song]", StringComparison.OrdinalIgnoreCase);
                inEventsSection = trimmedLine.Equals("[Events]", StringComparison.OrdinalIgnoreCase);
                inDesiredSingleSection = chosenSingleHeader != null && trimmedLine.Equals(chosenSingleHeader, StringComparison.OrdinalIgnoreCase);
                inSyncTrackSection = trimmedLine.Equals("[SyncTrack]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if ((inSongSection || inDesiredSingleSection || inSyncTrackSection || inEventsSection) && trimmedLine.StartsWith("{"))
            {
                continue; // Skip opening brace
            }

            if ((inSongSection || inDesiredSingleSection || inSyncTrackSection || inEventsSection) && trimmedLine.StartsWith("}"))
            {
                if (inSongSection) inSongSection = false;
                if (inSyncTrackSection) inSyncTrackSection = false;
                if (inEventsSection) inEventsSection = false;
                if (inDesiredSingleSection) inDesiredSingleSection = false;
                continue;
            }

            if (inSongSection)
            {
                string[] parts = trimmedLine.Split('=');
                if (parts.Length == 2 && parts[0].Trim().Equals("Resolution", StringComparison.OrdinalIgnoreCase) && int.TryParse(parts[1].Trim(), out int res))
                {
                    resolution = res;
                }
            }

            if (inSyncTrackSection)
            {
                Debug.Log("Parsing sync track...");
                string[] parts = trimmedLine.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int time))
                {
                    string[] syncParts = parts[1].Trim().Split(' ');
                    if (syncParts.Length >= 2 && syncParts[0] == "B" && float.TryParse(syncParts[1], out float bpm))
                    {
                        await Task.Run(() => syncTrack.Add(new SyncInfo
                        {
                            time = time,
                            bpm = bpm / 1000f, // converts from 120000 to 120.000 (example)
                            timeSignature = "4" // Default time signature; "4" = 4/4, "2" = 2/4, "3" = 3/4
                        }));
                    }
                    else if (syncParts.Length >= 2 && syncParts[0] == "TS")
                    {
                        if (syncTrack.Count > 0)
                        {
                            syncTrack[syncTrack.Count - 1].timeSignature = syncParts[1];
                        }
                    }
                }
            }

            if (inEventsSection)
            {
                Debug.Log("Parsing global events...");
                string[] parts = trimmedLine.Split(new[] { '=' }, 2);
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int eventTime))
                {
                    string rhs = parts[1].Trim();
                    string parsedEventString = string.Empty;

                    // Expect formats like: E "string with spaces"  or E token
                    if (rhs.StartsWith("E"))
                    {
                        string afterE = rhs.Substring(1).Trim();
                        if (afterE.Length > 0 && (afterE[0] == '"' || afterE[0] == '\''))
                        {
                            char quote = afterE[0];
                            int endIdx = afterE.IndexOf(quote, 1);
                            if (endIdx > 0)
                            {
                                parsedEventString = afterE.Substring(1, endIdx - 1);
                            }
                            else
                            {
                                // unmatched quote: take remainder without the leading quote
                                parsedEventString = afterE.TrimStart(quote).Trim();
                            }
                        }
                        else
                        {
                            // no quotes -> take first token
                            var toks = afterE.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (toks.Length > 0) parsedEventString = toks[0];
                        }
                    }
                    else
                    {
                        // fallback: if rhs itself is quoted, strip quotes; else take rhs
                        if ((rhs.StartsWith("\"") && rhs.EndsWith("\"")) || (rhs.StartsWith("'") && rhs.EndsWith("'")))
                        {
                            parsedEventString = rhs.Substring(1, rhs.Length - 2);
                        }
                        else parsedEventString = rhs;
                    }

                    // insert/append into dictionary keyed by tick
                    if (events.ContainsKey(eventTime))
                    {
                        if (!string.IsNullOrEmpty(parsedEventString))
                            events[eventTime] = string.IsNullOrEmpty(events[eventTime]) ? parsedEventString : events[eventTime] + "|" + parsedEventString;
                    }
                    else
                    {
                        events[eventTime] = parsedEventString;
                    }
                }
                
            }

            if (inDesiredSingleSection)
            {
                Debug.Log("Parsing notes...");
                string[] parts = trimmedLine.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int tickStart) && parts[1].Trim().StartsWith("N"))
                {
                    string[] noteParts = parts[1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (noteParts.Length >= 3 && int.TryParse(noteParts[1], out int fret) && int.TryParse(noteParts[2], out int lengthInTicks))
                    {
                        NoteInfo currentNote;

                        // Classify notes based on fret number
                        if (fret == 7)
                        {
                            currentNote = new OpenNoteInfo
                            {
                                spawnTime = tickStart,
                                fret = fret,
                                length = lengthInTicks
                            };
                        }
                        else
                        {
                            currentNote = new NoteInfo
                            {
                                spawnTime = tickStart,
                                fret = fret,
                                length = lengthInTicks
                            };
                        }
                        previousNote = currentNote; // Update the previous note
                        parsedNotesCount++;

                        // Optionally pre-spawn the visual note immediately if we have enough sync data
                        if (syncTrack != null && syncTrack.Count > 0)
                        {
                            
                            float spacingFactor = desiredHyperspeedSingleThreaded;
                            float timeSeconds = GetTimeInSecondsAtTick(tickStart);
                            if (musicPlayer != null)
                            {
                                musicPlayer.currentTime = Mathf.Clamp(timeSeconds, 0, songLengthInTicks - 1);
                            }
                            float strikeY = GetStrikeLineY();
                            float yPosition = preSpawnLongPlane
                                ? strikeY + startingYPosition + startingYOffset + (timeSeconds + spawnLeadSeconds) * spacingFactor
                                : strikeY + startingYPosition + startingYOffset + ((timeSeconds - ((musicPlayer != null) ? (float)musicPlayer.GetElapsedTime() : 0f)) + spawnLeadSeconds) * spacingFactor;

                            Vector2 position = new Vector2((fret - 2f) + noteSpawningXOffset, yPosition);
                            Vector2 openNotePosition = new Vector2(0 + noteSpawningXOffset, yPosition);
                            GameObject noteInstance = null;
                            
                            if (currentNote is OpenNoteInfo)
                            {
                                if (openNotePrefab != null) noteInstance = NotePoolManager.Instance.Get(openNotePrefab); noteInstance.transform.position = openNotePosition;
                            }
                            else
                            {
                                switch (fret)
                                {
                                    case 0: if (greenNotePrefab != null) noteInstance = NotePoolManager.Instance.Get(greenNotePrefab); if (noteInstance != null) noteInstance.transform.position = position; break;
                                    case 1: if (redNotePrefab != null) noteInstance = NotePoolManager.Instance.Get(redNotePrefab); if (noteInstance != null) noteInstance.transform.position = position; break;
                                    case 2: if (yellowNotePrefab != null) noteInstance = NotePoolManager.Instance.Get(yellowNotePrefab); if (noteInstance != null) noteInstance.transform.position = position; break;
                                    case 3: if (blueNotePrefab != null) noteInstance = NotePoolManager.Instance.Get(blueNotePrefab); if (noteInstance != null) noteInstance.transform.position = position; break;
                                    case 4: if (orangeNotePrefab != null) noteInstance = NotePoolManager.Instance.Get(orangeNotePrefab); if (noteInstance != null) noteInstance.transform.position = position; break;
                                    default: break;
                                }
                            }
                            

                            if (noteInstance != null)
                            {
                                var gate = noteInstance.GetComponent<VisibilityGate>();
                                if (gate == null) gate = noteInstance.AddComponent<VisibilityGate>();
                                gate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
                                try { LaneManager.Instance.RegisterNote(noteInstance, fret); } catch (Exception) { }
                                noteInstance.SetActive(true);
                                // Compute HOPO flags: if the time between consecutive notes is within the
                                // HOPO threshold (in milliseconds), mark the later note's `isHopo = true`.
                                try
                                {
                                    for (int i2 = 1; i2 < notes.Count; i2++)
                                    {
                                        var prev = notes[i2 - 1];
                                        var curr = notes[i2];
                                        if (prev == null || curr == null) continue;
                                        // time difference in milliseconds
                                        float prevSec = GetTimeInSecondsAtTick((int)prev.spawnTime);
                                        float currSec = GetTimeInSecondsAtTick((int)curr.spawnTime);
                                        float deltaMs = (currSec - prevSec) * 1000f;

                                        // Detect if a "forced" indicator (fret 5) exists on the same tick as `curr`.
                                        bool hasForcedSameTick = false;
                                        for (int j = 0; j < notes.Count; j++)
                                        {
                                            var maybe = notes[j];
                                            if (maybe == null) continue;
                                            if ((int)maybe.spawnTime == (int)curr.spawnTime && maybe.fret == 5)
                                            {
                                                hasForcedSameTick = true;
                                                break;
                                            }
                                        }

                                        if (hasForcedSameTick)
                                        {
                                            // Forced indicator toggles the default behavior:
                                            // - If the time delta is outside the HOPO threshold -> force HOPO
                                            // - If inside the HOPO threshold -> force normal note
                                            curr.isHopo = (deltaMs > hopoThreshold);
                                        }
                                        else
                                        {
                                            // Default HOPO rule: within threshold (and >0) -> HOPO
                                            curr.isHopo = (deltaMs > 0f && deltaMs <= hopoThreshold);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning("Failed computing HOPO flags: " + ex.Message);
                                }
                                // If the note was computed as a HOPO, enable its HOPO indicator child or component.
                                try
                                {
                                    
                                    bool hopo = false;
                                    // safe-guard: `note` may be a NoteInfo reference
                                    hopo = currentNote.isHopo;
                                    // Try to find a child named "HOPO" first
                                    var hopoChild = noteInstance.transform.Find("HOPO");
                                    if (hopoChild != null)
                                    {
                                        hopoChild.gameObject.SetActive(hopo);
                                    }
                                    else
                                    {
                                        // Or try a component named HopoIndicator on the prefab
                                        var hopoComp = noteInstance.GetComponentInChildren<MonoBehaviour>(true);
                                        if (hopoComp != null && hopoComp.GetType().Name == "HopoIndicator")
                                        {
                                            hopoComp.gameObject.SetActive(hopo);
                                        }
                                    }
                                }
                                catch (Exception) { }
                                AddObjectToGlobalMoveY(noteInstance);
                            }
                            else
                            {
                                
                            }

                            // sustain creation (kept simple here: instantiate sustain prefab children if available)
                            GameObject sustainInstance = null;
                            if (lengthInTicks > 0)
                            {
                                if (noteInstance != null)
                                {
                                    sustainInstance = noteInstance.GetComponentInChildren<Sustain>().gameObject;
                                }
                                
                                if (sustainInstance != null)
                                {
                                    SetupSustain(sustainInstance, tickStart, lengthInTicks, spacingFactor);
                                    var sGate = sustainInstance.GetComponent<VisibilityGate>();
                                    if (sGate == null) sGate = sustainInstance.AddComponent<VisibilityGate>();
                                    sGate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
                                    sustainInstance.SetActive(true);
                                }
                            }

                            
                        }
                        else
                        {
                            
                            
                        }
                    }
                    else
                    {
                        failedNoteLines++;
                        if (failedSamples.Count < 20) failedSamples.Add(trimmedLine);
                    }
                }
            }
            
            
            await Task.Yield(); // Yield to keep UI responsive during long parsing
            
        }

        // Diagnostics summary
        Debug.Log($"ParseChartFile: chosenSingle={chosenSingleHeader}, parsedNotes={parsedNotesCount}, failedNoteLines={failedNoteLines}, totalNotesArraySize(after)={notes.Count}");
        if (failedSamples.Count > 0)
        {
            Debug.LogWarning("ParseChartFile: sample failed lines (up to 20):\n" + string.Join("\n", failedSamples.ToArray()));
        }
        syncTrack.Sort((a, b) => a.time.CompareTo(b.time));
        if (musicPlayer != null)
        {
            musicPlayer.currentTime = 0f;
        }

        

        // If `songLengthInTicks` isn't provided, compute a sensible value from notes or the sync track.
        // This prevents spawning bars/beats beyond the end of the song.
        if (songLengthInTicks <= 0)
        {
            int maxTick = 0;

            // Use notes (including sustain length) to determine the final tick
            foreach (var n in notes)
            {
                int noteStart = (int)n.spawnTime;
                int noteEnd = noteStart + n.length;
                if (noteEnd > maxTick) maxTick = noteEnd;
                if (noteStart > maxTick) maxTick = noteStart;
            }

            // If no notes present, fall back to the last sync point
            if (maxTick == 0 && syncTrack.Count > 0)
            {
                maxTick = (int)syncTrack[syncTrack.Count - 1].time;
            }

            // Ensure we have at least 1 tick to avoid zero-length loops
            songLengthInTicks = Math.Max(1, maxTick);
        }

        
    }

    private void ParseMidiFile(string filePath)
    {
        // Implement MIDI parsing logic if needed
    }

    
    public string GetEventInfoStringOnTick(int tick)
    {
        try
        {
            if (events != null && events.TryGetValue(tick, out string val)) return val ?? string.Empty;
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    // Configure sustain prefab scale and position so its visual length matches the song duration
    private void SetupSustain(GameObject sustainInstance, float startTick, int lengthTicks, float spacingFactor)
    {
        if (sustainInstance == null) return;
        float startSeconds = GetTimeInSecondsAtTick((int)startTick);
        float endSeconds = GetTimeInSecondsAtTick((int)(startTick + lengthTicks));
        Vector2 sustainPosition = new Vector2(sustainInstance.transform.position.x, startSeconds);
        sustainInstance.transform.position = sustainPosition;
        // Ensure there is a Sustain component to manage visual updates
        var sustainComp = sustainInstance.GetComponent<Sustain>();
        if (sustainComp == null) sustainComp = sustainInstance.AddComponent<Sustain>();
        sustainComp.Initialize(startSeconds, endSeconds, spacingFactor);
    }

    private void AddObjectToGlobalMoveY(GameObject go)
    {
        if (go == null) return;
        GlobalMoveY globalMoveY = FindAnyObjectByType<GlobalMoveY>();
        if (globalMoveY != null && !globalMoveY.objectsToMove.Contains(go))
        {
            globalMoveY.objectsToMove.Add(go);
        }
    }

    private void RemoveObjectFromGlobalMoveY(GameObject go)
    {
        if (go == null) return;
        GlobalMoveY globalMoveY = FindAnyObjectByType<GlobalMoveY>();
        if (globalMoveY != null && globalMoveY.objectsToMove.Contains(go))
        {
            globalMoveY.objectsToMove.Remove(go);
        }
    }

    private void StartMovingNotes()
    {
        GlobalMoveY globalMoveY = FindAnyObjectByType<GlobalMoveY>();
        if (globalMoveY != null)
        {
            globalMoveY.isMoving = true;
        }
    }

    private class OpenNoteInfo : NoteInfo
    {
        // Class to represent open notes
    }

    // Returns cumulative time in seconds from tick 0 up to the given tick, honoring tempo changes in syncTrack
    public float GetTimeInSecondsAtTick(float tick)
    {
        if (syncTrack == null || syncTrack.Count == 0)
        {
            float defaultSecondsPerTick = 60f / (currentBpm * resolution);
            return tick * defaultSecondsPerTick;
        }

        float totalSeconds = 0f;
        float prevTick = 0;
        float prevBpm = syncTrack[0].bpm;
        for (int i = 0; i < syncTrack.Count; i++)
        {
            var sync = syncTrack[i];
            float segEnd = Math.Min(sync.time, tick);
            float delta = segEnd - prevTick;
            if (delta > 0)
            {
                totalSeconds += delta * (60f / (prevBpm * resolution));
            }

            prevTick = segEnd;
            prevBpm = sync.bpm;

            if (segEnd >= tick) break;
        }

        if (prevTick < tick)
        {
            totalSeconds += (tick - prevTick) * (60f / (prevBpm * resolution));
        }

        return totalSeconds;
    }

    // Convert elapsed song time (seconds) to nearest chart tick, honoring tempo changes in syncTrack.
    public float GetTickAtTimeSeconds(float seconds, bool roundToNearest = false)
    {
        if (seconds <= 0f) return 0;
        if (syncTrack == null || syncTrack.Count == 0)
        {
            float sPerTick = 60f / (currentBpm * resolution);
            float ticksF = seconds / sPerTick;
            return roundToNearest ? Mathf.RoundToInt(ticksF) : Mathf.FloorToInt(ticksF);
        }

        float accumulatedSeconds = 0f;
        int prevTick = 0;
        float prevBpm = syncTrack[0].bpm;

        for (int i = 0; i < syncTrack.Count; i++)
        {
            var entry = syncTrack[i];
            int segEndTick = (int)entry.time;
            int segTicks = Math.Max(0, segEndTick - prevTick);
            float secPerTick = 60f / (prevBpm * resolution);
            float segSeconds = segTicks * secPerTick;

            if (seconds <= accumulatedSeconds + segSeconds)
            {
                float remaining = seconds - accumulatedSeconds;
                float ticksIntoSeg = remaining / secPerTick;
                float tickF = prevTick + ticksIntoSeg;
                return roundToNearest ? Mathf.Clamp(Mathf.RoundToInt(tickF), 0, songLengthInTicks) : Mathf.Clamp(Mathf.FloorToInt(tickF), 0, songLengthInTicks);
            }

            accumulatedSeconds += segSeconds;
            prevTick = segEndTick;
            prevBpm = entry.bpm;
        }

        // after last sync entry
        float lastSecPerTick = 60f / (prevBpm * resolution);
        float ticksAfter = (seconds - accumulatedSeconds) / lastSecPerTick;
        float finalTickF = prevTick + ticksAfter;
        return roundToNearest ? Mathf.Clamp(Mathf.RoundToInt(finalTickF), 0, songLengthInTicks) : Mathf.Clamp(Mathf.FloorToInt(finalTickF), 0, songLengthInTicks);
    }

    // Returns seconds per tick at a specific tick (based on the most recent sync entry at or before that tick)
    private float GetSecondsPerTickAtTick(int tick)
    {
        if (syncTrack == null || syncTrack.Count == 0)
        {
            return 60f / (currentBpm * resolution);
        }

        float bpm = currentBpm;
        for (int i = 0; i < syncTrack.Count; i++)
        {
            if (syncTrack[i].time <= tick)
            {
                bpm = syncTrack[i].bpm;
            }
            else
            {
                break;
            }
        }

        return 60f / (bpm * resolution);
    }

    // Coroutine: manages spawning bars and beats ahead of current playback time using pooling
    private IEnumerator ManageBarBeatSpawning()
    {
        if (musicPlayer == null) musicPlayer = FindAnyObjectByType<MusicPlayer>();
        float spacingFactor = PlayerPrefs.GetFloat("Hyperspeed", 5f);

        // Start spawning from the first sync tick or 0
        int nextBarTick = 0;
        if (syncTrack != null && syncTrack.Count > 0) nextBarTick = (int)syncTrack[0].time;

        bool firstBarTagged = false;

        while (true)
        {
            if (musicPlayer == null)
            {
                yield return null;
                continue;
            }

            float currentSongSeconds = (float)musicPlayer.GetElapsedTime();

            // spawn bars while their time is within the ahead window
            while (nextBarTick < songLengthInTicks)
            {
                float barTime = GetTimeInSecondsAtTick(nextBarTick);
                if (barTime > currentSongSeconds + preSpawnAheadSeconds) break;

                // Spawn bar and record its scheduled time
                var bar = GetBarFromPool();
                    float strikeY = GetStrikeLineY();
                    float secondsUntilBar = barTime - currentSongSeconds;
                    float barY = strikeY + startingYPosition + startingYOffset + (secondsUntilBar + spawnLeadSeconds) * spacingFactor;
                bar.transform.position = new Vector3(0f, barY, 0f);
                // ensure visibility gate is initialized with current reveal Z in case values changed since pooling
                var bGate = bar.GetComponent<VisibilityGate>();
                if (bGate == null) bGate = bar.AddComponent<VisibilityGate>();
                bGate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
                bar.SetActive(true);
                scheduledTimeByObject[bar] = barTime;
                // add to GlobalMoveY so pooled bars are moved along with notes
                AddObjectToGlobalMoveY(bar);
                // insert into activeBars keeping ascending scheduled time order; fall back to transform.z if mapping missing
                int insertIndex = activeBars.FindIndex(bobj => {
                    float btime;
                    if (scheduledTimeByObject.TryGetValue(bobj, out btime)) return btime > barTime;
                    // fallback: infer scheduled time from z position (use strike-line + starting offsets as base)
                    btime = (bobj.transform.position.y - (GetStrikeLineY() + startingYPosition + startingYOffset)) / Math.Max(0.0001f, spacingFactor);
                    return btime > barTime;
                });
                if (insertIndex >= 0) activeBars.Insert(insertIndex, bar); else activeBars.Add(bar);

                if (!firstBarTagged)
                {
                    bar.tag = "FirstBar";
                    firstBarTagged = true;
                }

                // spawn beats within this bar
                SyncInfo sync = FindSyncForTick(nextBarTick);
                int ticksPerBeat = resolution;
                int beatsPerBar = sync != null && sync.timeSignature == "2" ? 2 : 4;
                for (int beatIndex = 1; beatIndex < beatsPerBar; beatIndex++)
                {
                    int beatTick = nextBarTick + (beatIndex * ticksPerBeat);
                    if (beatTick >= songLengthInTicks) break;
                    var beatGO = GetBeatFromPool();
                    float beatTime = GetTimeInSecondsAtTick(beatTick);
                    float strikeY2 = GetStrikeLineY();
                    float secondsUntilBeat = beatTime - currentSongSeconds;
                    float beatY = strikeY2 + startingYPosition + startingYOffset + (secondsUntilBeat + spawnLeadSeconds) * spacingFactor;
                    beatGO.transform.position = new Vector3(0f, beatY, 0f);
                    var gate = beatGO.GetComponent<VisibilityGate>();
                    if (gate == null) gate = beatGO.AddComponent<VisibilityGate>();
                    gate.Initialize(GetStrikeLineY() + startingYPosition + startingYOffset);
                    beatGO.SetActive(true);
                    scheduledTimeByObject[beatGO] = beatTime;
                    // add to GlobalMoveY so pooled beats are moved along with notes
                    AddObjectToGlobalMoveY(beatGO);
                    int bInsert = activeBeats.FindIndex(bobj => {
                        float btime;
                        if (scheduledTimeByObject.TryGetValue(bobj, out btime)) return btime > beatTime;
                        btime = (bobj.transform.position.y - (GetStrikeLineY() + startingYPosition + startingYOffset)) / Math.Max(0.0001f, spacingFactor);
                        return btime > beatTime;
                    });
                    if (bInsert >= 0) activeBeats.Insert(bInsert, beatGO); else activeBeats.Add(beatGO);
                }

                // advance to next bar tick based on current sync's bar size
                int barAdvance = resolution * (sync != null && sync.timeSignature == "2" ? 2 : 4);
                nextBarTick += barAdvance;
            }

            // Recycle bars/beats that are behind the current playback (passed)
            for (int i = activeBars.Count - 1; i >= 0; i--)
            {
                var b = activeBars[i];
                // If the GameObject was destroyed elsewhere, just remove the reference and mapping
                if (b == null || b.Equals(null))
                {
                    if (scheduledTimeByObject.ContainsKey(b)) scheduledTimeByObject.Remove(b);
                    activeBars.RemoveAt(i);
                    continue;
                }

                // get scheduled time from mapping if available; fallback to transform (using strike-line + starting offsets as base)
                float scheduledSeconds = scheduledTimeByObject.TryGetValue(b, out float sTime) ? sTime : (b.transform.position.y - (GetStrikeLineY() + startingYPosition + startingYOffset)) / Math.Max(0.0001f, spacingFactor);
                if (scheduledSeconds < currentSongSeconds - recycleGraceSeconds) // grace before recycling
                {
                    activeBars.RemoveAt(i);
                    scheduledTimeByObject.Remove(b);
                    ReturnBarToPool(b);
                }
            }

            for (int i = activeBeats.Count - 1; i >= 0; i--)
            {
                var bt = activeBeats[i];
                if (bt == null || bt.Equals(null))
                {
                    if (scheduledTimeByObject.ContainsKey(bt)) scheduledTimeByObject.Remove(bt);
                    activeBeats.RemoveAt(i);
                    continue;
                }

                float scheduledSeconds = scheduledTimeByObject.TryGetValue(bt, out float sTimeBt) ? sTimeBt : (bt.transform.position.y - (GetStrikeLineY() + startingYPosition + startingYOffset)) / Math.Max(0.0001f, spacingFactor);
                if (scheduledSeconds < currentSongSeconds - recycleGraceSeconds)
                {
                    activeBeats.RemoveAt(i);
                    scheduledTimeByObject.Remove(bt);
                    ReturnBeatToPool(bt);
                }
            }

            // stop if we've spawned everything and recycled all
            if (nextBarTick >= songLengthInTicks && activeBars.Count == 0 && activeBeats.Count == 0)
            {
                barBeatSpawnerCoroutine = null;
                yield break;
            }

            yield return null;
        }
    }

    // Find the most recent SyncInfo at or before tick
    public SyncInfo FindSyncForTick(int tick)
    {
        SyncInfo last = null;
        for (int i = 0; i < syncTrack.Count; i++)
        {
            if (syncTrack[i].time <= tick) last = syncTrack[i]; else break;
        }
        return last ?? (syncTrack.Count > 0 ? syncTrack[0] : null);
    }

    // Ensure there is only one currentTick variable
    // Remove duplicate declaration if it exists

    // Update the UpdateCurrentTick method to reference the existing currentTick variable
    // `songTime` is provided in seconds. Convert to chart ticks using the tempo map
    // so event lookups (which are keyed by tick) work correctly.
    public void UpdateCurrentTick(float songTimeSeconds)
    {
        try
        {
            // Use the tempo-aware conversion helper to map seconds -> ticks
            float tickF = GetTickAtTimeSeconds(songTimeSeconds, false);
            currentTick = Mathf.Clamp((int)tickF, 0, songLengthInTicks);
        }
        catch (Exception)
        {
            currentTick = 0;
        }
    }

    // Helper: return the Y value of the strikeline used as the center. Falls back to 0 if not available.
    public float GetStrikeLineY()
    {
        if (useStrikeLineY)
        {
            if (strikeLineTransform != null) return strikeLineTransform.position.y;
            // Try to find a common strikeline component in the scene
            var sl = FindAnyObjectByType<ImprovedStrikeline>();
            if (sl != null) return sl.transform.position.y;
            // Optionally try GameObject tag "Strikeline" if you use that
            var tagged = GameObject.FindWithTag("Strikeline");
            if (tagged != null) return tagged.transform.position.y;
        }

        // Fallback: use startingYPosition if you prefer absolute coordinates; otherwise 0.
        return 0f;
    }
    void Update()
    {
        if (!videoPrepared)
        {
            if (!string.IsNullOrEmpty(videoClipPath) && musicPlayer != null && musicPlayer.videoPlayer != null)
            {
                if (!musicPlayer.videoPlayer.isPrepared)
                {
                    musicPlayer.videoPlayer.url = videoClipPath;
                    musicPlayer.videoPlayer.Prepare();
                    videoPrepared = true;
                }
            }
        }

        
    }
}
