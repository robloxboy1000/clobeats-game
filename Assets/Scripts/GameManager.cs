using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using TMPro;
using System.Net.WebSockets;
using UnityEngine.Networking;


public class GameManager : MonoBehaviour
{
    public float audioOffsetSeconds = 0.04f;
    public bool enableSustains = true;
    public bool initialized = false;
    public bool allowFail = true;
    [Tooltip("Change this to the actual server you want to connect to.")]
    public string serverIPAddr = "pixlplaya5.xyz";
    public List<string> songFolders;

    public Dictionary<int, SongInfo> cachedSongs = new Dictionary<int, SongInfo>();
    public List<SongEntryInfo> cachedEntries = new List<SongEntryInfo>();
    public Dictionary<int, AudioClip> cachedAudioClips = new Dictionary<int, AudioClip>();

    public class SongInfo
    {
        public int resolution = 480;
        public Queue<NoteSpawner.SyncInfo> syncInfos = new Queue<NoteSpawner.SyncInfo>();
        public Queue<NoteSpawner.NoteInfo> noteInfos = new Queue<NoteSpawner.NoteInfo>();
        public Dictionary<int, NoteSpawner.GlobalEventInfo> globalEvents = new Dictionary<int, NoteSpawner.GlobalEventInfo>();
        public List<NoteSpawner.GlobalEventInfo> beatEvents = new List<NoteSpawner.GlobalEventInfo>();
        public int songLengthInTicks = 0;
        public List<NoteSpawner.GlobalEventInfo> forcedNoteEvents = new List<NoteSpawner.GlobalEventInfo>();
        public List<NoteSpawner.GlobalEventInfo> venueAnimCueEvents = new List<NoteSpawner.GlobalEventInfo>();
        public List<NoteSpawner.LyricEventInfo> lyricEvents = new List<NoteSpawner.LyricEventInfo>();
    }
    [Serializable]
    public class SongEntryInfo
    {
        public string songTitle;
        public string songArtist;
        public string songAlbum;
        public int songYear;
        public string songLoadingPhrase;
        public string songAuthor;
        public int songLength;
        public string songAccentColor;
        public int songPreviewStartTime;
        public int cachedSongID;
        public string songPath;
        public int songNumber;
    }
    public string ddst;
    public string currentPart = "Guitar";

    public int currentSongResolution = 480;
    public Queue<NoteSpawner.NoteInfo> currentSongNotes = new Queue<NoteSpawner.NoteInfo>();
    public Queue<NoteSpawner.SyncInfo> currentSongSyncTrack = new Queue<NoteSpawner.SyncInfo>();
    public Dictionary<int, NoteSpawner.GlobalEventInfo> currentSongEvents = new Dictionary<int, NoteSpawner.GlobalEventInfo>();
    public List<NoteSpawner.GlobalEventInfo> currentSongBeatEvents = new List<NoteSpawner.GlobalEventInfo>();
    public List<NoteSpawner.GlobalEventInfo> currentSongForcedNoteEvents = new List<NoteSpawner.GlobalEventInfo>();
    public List<NoteSpawner.GlobalEventInfo> currentSongVenueCueEvents = new List<NoteSpawner.GlobalEventInfo>();
    public List<NoteSpawner.LyricEventInfo> currentSongLyrics = new List<NoteSpawner.LyricEventInfo>();
    public int currentSongLengthInTicks = 0;

    public string currentSongTitle;
    public string currentSongArtist;
    public string currentSongAlbum;
    public int currentSongYear;
    public string currentSongLoadingPhrase;
    public string currentSongAuthor;
    public int currentSongLength;
    public string currentSongAccentColor;
    public int currentSongPreviewStartTime;
    public string currentSongPath;

    public int currentSongID = 0;

    public bool inSong = false;
    public string savePath = null;

    public GameObject unDestructibleLoadingPhraseScreen;

    // note mappings (RBN2)
    Dictionary<string, Dictionary<int, int>> partGuitarDifficultyMappings = new Dictionary<string, Dictionary<int,int>>
    {
        ["EasyRhythm"] = new Dictionary<int,int> { {94,8} },
        ["Easy"] = new Dictionary<int,int> { {60,0}, {61,1}, {62,2}, {63,3}, {64,4}, {59,7} },
        ["Medium"] = new Dictionary<int,int> { {72,0}, {73,1}, {74,2}, {75,3}, {76,4}, {71,7} },
        ["Hard"] = new Dictionary<int,int> { {84,0}, {85,1}, {86,2}, {87,3}, {88,4}, {83,7} },
        ["Expert"] = new Dictionary<int,int> { {96,0}, {97,1}, {98,2}, {99,3}, {100,4}, {95,7}, {94,8} },
    };

    Dictionary<string, Dictionary<int, int>> partDrumsDifficultyMappings = new Dictionary<string, Dictionary<int,int>>
    {
        ["Easy"] = new Dictionary<int,int> { {65,0}, {61,1}, {62,2}, {63,3}, {64,4}, {60,7} },
        ["Medium"] = new Dictionary<int,int> { {77,0}, {73,1}, {74,2}, {75,3}, {76,4}, {72,7} },
        ["Hard"] = new Dictionary<int,int> { {89,0}, {85,1}, {86,2}, {87,3}, {88,4}, {84,7} },
        ["Expert"] = new Dictionary<int,int> { {97,0}, {98,1}, {99,2}, {100,3}, {101,4}, {96,7}, {95,7} },
    };

    public async Task PlaySongGlobal(string path)
    {
        await EnableLoadUnCachedSongVisual(unDestructibleLoadingPhraseScreen, Path.Combine(path, "song.ini"));
        SongFolderLoader songFolderLoader = FindAnyObjectByType<SongFolderLoader>();
        if (songFolderLoader != null)
        {
            songFolderLoader.songFolderPath = path;
            await songFolderLoader.Load();
        }
        else
        {
            Debug.LogError("SongFolderLoader not found in scene!");
        }
            
        SceneManager.LoadScene("Gameplay", LoadSceneMode.Additive); // load synchronously
            
        NoteSpawner noteSpawner = FindAnyObjectByType<NoteSpawner>();
        if (noteSpawner)
        {
            await noteSpawner.Load();
            await noteSpawner.InitGameplay();
            
            
        }
    }

    public void ExitGame(bool ask)
    {
        if (ask)
        {
            MessageBox.Instance.Show("Are you sure you want to exit the game?<br>All unsaved data will be lost.", "Message", ForceExitGame);
        }
        else
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
            UnityEngine.Application.Quit();
        }
        
    }
    public void ForceExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
            UnityEngine.Application.Quit();
    }

    public IEnumerator PlayerRocksAnim()
    {
        TMPInstanceMaker.Instance.CreateTextObject("Song Cleared",
            SceneManager.GetSceneByName("Gameplay").isLoaded ?
            GameObject.Find("UI").transform :
            transform,
            new Vector2(960,540),
            new Vector3(0,0,0),
            24);
        yield return new WaitForSeconds(6);
        SceneManager.LoadScene("MainMenu");
    }

    // Start is called before the first frame update
    void Start()
    {
        ddst = PlayerPrefs.GetString("SelectedDifficulty");
    }
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        if (unDestructibleLoadingPhraseScreen == null)
        {
            Instantiate(unDestructibleLoadingPhraseScreen);
            DontDestroyOnLoad(unDestructibleLoadingPhraseScreen);
            unDestructibleLoadingPhraseScreen.SetActive(false);
        }
    }

    public SongEntryInfo GetCachedSongEntry(int id)
    {
        return cachedEntries.FirstOrDefault(entry =>
            entry.songNumber == id ||
            entry.cachedSongID == id);
    }

    
    public void CacheSingleSong(string folder, int songID, int count)
    {
        if (!File.Exists(folder + Path.DirectorySeparatorChar + "song.ini")) return;
        INIParser parser = new INIParser();
        parser.Open(folder + Path.DirectorySeparatorChar + "song.ini");
        cachedEntries.Add(new SongEntryInfo
        {
            songTitle = parser.ReadValue("song", "name", string.Empty),
            songArtist = parser.ReadValue("song", "artist", string.Empty),
            songAlbum = parser.ReadValue("song", "album", string.Empty),
            songYear = parser.ReadValue("song", "year", 0),
            songLoadingPhrase = parser.ReadValue("song", "loading_phrase", string.Empty),
            songAuthor = parser.ReadValue("song", "charter", string.Empty),
            songLength = parser.ReadValue("song", "song_length", 0),
            songAccentColor = parser.ReadValue("song", "back_color", "#0000ff"),
            songPreviewStartTime = parser.ReadValue("song", "preview_start_time", 0),
            cachedSongID = songID,
            songPath = folder,
            songNumber = count
        });
        parser.Close();
    }
    public IEnumerator CacheAudioFile(string filePath, int songID)
    {
        string uriPath = new System.Uri(filePath).AbsoluteUri;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.UNKNOWN))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error loading audio clip: " + uwr.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
                if (clip != null)
                {
                    cachedAudioClips.Add(songID, clip);
                }
            }
        }
    }
    public string FindSongInPath(string path)
    {
        SongFolderLoader songFolderLoader = FindAnyObjectByType<SongFolderLoader>();
        // Find a file named "song" with a supported extension and set the audio path
        string[] songFiles = Directory.GetFiles(path);
        var songMatch = songFiles
            .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
            .FirstOrDefault(x => x.name == "song" && songFolderLoader.supportedFormats.Contains(x.ext));

        if (songMatch != null)
        {
            return songMatch.path;
        }
        else
        {
            return string.Empty;
        }
    }
    public async Task ReadMidiFile(string path)
    {
        // Read MIDI, cache it by a stable hash of the full path, and copy into current song queues
        string fullPath = Path.GetFullPath(path ?? string.Empty);
        int songID = Mathf.Abs(fullPath.GetHashCode());
        //Debug.Log(songID);

        try
        {
            if (cachedSongs.TryGetValue(songID, out SongInfo si))
            {
                // Copy cached info into current song state
                currentSongResolution = si.resolution;
                currentSongSyncTrack = new Queue<NoteSpawner.SyncInfo>(si.syncInfos);
                currentSongNotes = new Queue<NoteSpawner.NoteInfo>(si.noteInfos);
                currentSongEvents = new Dictionary<int, NoteSpawner.GlobalEventInfo>(si.globalEvents);
                currentSongBeatEvents = new List<NoteSpawner.GlobalEventInfo>(si.beatEvents);
                currentSongVenueCueEvents = new List<NoteSpawner.GlobalEventInfo>(si.venueAnimCueEvents);
                currentSongLyrics = new List<NoteSpawner.LyricEventInfo>(si.lyricEvents);
                currentSongLengthInTicks = si.songLengthInTicks;
            }
            else
            {
                await CacheMidiFile(fullPath, songID);
                if (cachedSongs.TryGetValue(songID, out SongInfo ucsi))
                {
                    // Copy cached info into current song state
                    currentSongResolution = ucsi.resolution;
                    currentSongSyncTrack = new Queue<NoteSpawner.SyncInfo>(ucsi.syncInfos);
                    currentSongNotes = new Queue<NoteSpawner.NoteInfo>(ucsi.noteInfos);
                    currentSongEvents = new Dictionary<int, NoteSpawner.GlobalEventInfo>(ucsi.globalEvents);
                    currentSongBeatEvents = new List<NoteSpawner.GlobalEventInfo>(ucsi.beatEvents);
                    currentSongVenueCueEvents = new List<NoteSpawner.GlobalEventInfo>(ucsi.venueAnimCueEvents);
                    currentSongLyrics = new List<NoteSpawner.LyricEventInfo>(ucsi.lyricEvents);
                    currentSongLengthInTicks = ucsi.songLengthInTicks;
                }
                else
                {
                    Debug.LogError("[ReadMidiFile] Failed to read MIDI file.");
                    MessageBox.Instance.Show("Failed to read MIDI file.", "Error", null);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("ReadMidiFile failed: " + ex.Message);
        }
        await Task.Yield();
    }
    public async Task ReadChartFile(string path)
    {
        // Read MIDI, cache it by a stable hash of the full path, and copy into current song queues
        string fullPath = Path.GetFullPath(path ?? string.Empty);
        int songID = Mathf.Abs(fullPath.GetHashCode());
        //Debug.Log(songID);

        try
        {
            

            if (cachedSongs.TryGetValue(songID, out SongInfo si))
            {
                // Copy cached info into current song state
                currentSongResolution = si.resolution;
                currentSongSyncTrack = new Queue<NoteSpawner.SyncInfo>(si.syncInfos);
                currentSongNotes = new Queue<NoteSpawner.NoteInfo>(si.noteInfos);
                currentSongEvents = new Dictionary<int, NoteSpawner.GlobalEventInfo>(si.globalEvents);
                currentSongBeatEvents = new List<NoteSpawner.GlobalEventInfo>(si.beatEvents);
                currentSongForcedNoteEvents = new List<NoteSpawner.GlobalEventInfo>(si.forcedNoteEvents);
                currentSongLengthInTicks = si.songLengthInTicks;
            }
            else
            {
                await CacheChartFile(fullPath, songID);
                if (cachedSongs.TryGetValue(songID, out SongInfo ucsi))
                {
                    // Copy cached info into current song state
                    currentSongResolution = ucsi.resolution;
                    currentSongSyncTrack = new Queue<NoteSpawner.SyncInfo>(ucsi.syncInfos);
                    currentSongNotes = new Queue<NoteSpawner.NoteInfo>(ucsi.noteInfos);
                    currentSongEvents = new Dictionary<int, NoteSpawner.GlobalEventInfo>(ucsi.globalEvents);
                    currentSongBeatEvents = new List<NoteSpawner.GlobalEventInfo>(ucsi.beatEvents);
                    currentSongVenueCueEvents = new List<NoteSpawner.GlobalEventInfo>(ucsi.venueAnimCueEvents);
                    currentSongLyrics = new List<NoteSpawner.LyricEventInfo>(ucsi.lyricEvents);
                    currentSongLengthInTicks = ucsi.songLengthInTicks;
                }
                else
                {
                    Debug.LogError("[ReadChartFile] Failed to read chart file.");
                    MessageBox.Instance.Show("Failed to read Chart file.", "Error", null);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("ReadChartFile failed: " + ex.Message);
        }
        await Task.Yield();
    }

    /// <summary>
    /// Cache a MIDI file by path into the song cache under the supplied songID.
    /// Builds a SongInfo containing sync (tempo/time-signature) events and note infos (tick times/lengths).
    /// </summary>
    public async Task CacheMidiFile(string path, int songID)
    {
        Debug.Log("Parsing MIDI file");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new FileNotFoundException("MIDI file not found", path);

        MidiFile midi = MidiFile.Read(path);

        // Build SongInfo
        SongInfo info = new SongInfo();

        // Determine MIDI resolution and compute scale to game's resolution (use currentSongResolution as target)
        int midiResolution = 480; // default fallback
        try
        {
            if (midi.TimeDivision is Melanchall.DryWetMidi.Core.TicksPerQuarterNoteTimeDivision tpq)
            {
                midiResolution = tpq.TicksPerQuarterNote;
            }
        }
        catch
        {
            // keep default
        }

        int targetResolution = currentSongResolution > 0 ? currentSongResolution : 480;
        double scale = (double)targetResolution / Math.Max(1, midiResolution);

        var globalEvents = GetTextEventsFromTrackByName(midi, "EVENTS", scale);
        foreach (var evt in globalEvents)
        {
            int key = (int)evt.spawnTime;
            if (!info.globalEvents.TryGetValue(key, out var existing))
            {
                info.globalEvents.Add(key, evt);
            }
            else
            {
                // Merge event values for identical spawn times using '|' as separator
                string sep = "|";
                if (string.IsNullOrEmpty(existing.value))
                {
                    existing.value = evt.value;
                }
                else if (!string.IsNullOrEmpty(evt.value))
                {
                    existing.value = existing.value + sep + evt.value;
                }
                // keep existing.spawnTime and existing.spawnTimeMs
            }
        }
        
        

        // Parse BEAT track (optional). Use MIDI note 12 = measure/bar marker, 13 = regular beat.
        try
        {
            var beatEvents = GetBeatEventsFromTrackByName(midi, "BEAT", scale);
            foreach (var be in beatEvents) info.beatEvents.Add(be);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CacheMidiFile: failed to parse BEAT track: " + ex.Message);
        }
        try
        {
            // choose difficulty key (string) from player settings or UI
            string chosenDiff = ddst;
            string chosenPart = currentPart.ToUpper();
        
            if (string.IsNullOrEmpty(chosenDiff)) chosenDiff = "Expert";
            /*if (chosenPart == "DRUMS")
            {
                if (partDrumsDifficultyMappings.TryGetValue(chosenDiff, out var map))
                {
                    var trackNotes = GetNotesFromTrackByName(midi, "PART " + chosenPart, map, scale);
                    foreach (var ni in trackNotes) info.noteInfos.Enqueue(ni);
                }
            }
            else
            {
                if (partGuitarDifficultyMappings.TryGetValue(chosenDiff, out var map))
                {
                    var trackNotes = GetNotesFromTrackByName(midi, "PART " + chosenPart, map, scale);
                    foreach (var ni in trackNotes) info.noteInfos.Enqueue(ni);
                }
            }*/
            if (partGuitarDifficultyMappings.TryGetValue(chosenDiff, out var map))
            {
                var trackNotes = GetNotesFromTrackByName(midi, "PART " + chosenPart, map, scale);
                foreach (var ni in trackNotes) info.noteInfos.Enqueue(ni);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameManager.CacheMidiFile] Failed to parse notes: " + ex.Message);
        }

        try
        {
            var vocalEvents = GetNotesAndTextFromVocalTrack(midi, "PART VOCALS", scale); // use solo vocal track for now
            foreach (var eve in vocalEvents) info.lyricEvents.Add(eve);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameManager.CacheMidiFile] Failed to parse lyrics: " + ex.Message);
        }
        
        

        // Set cached resolution to the target resolution we converted into
        info.resolution = targetResolution;

        // Collect timed events (tempo and time signature)
        try
        {
            var timedEvents = midi.GetTimedEvents();
            foreach (var te in timedEvents)
            {
                if (te.Event is Melanchall.DryWetMidi.Core.SetTempoEvent ste)
                {
                    int tick = (int)Math.Round(te.Time * scale);
                    // microseconds per quarter note -> BPM
                    float bpm = 60000000f / (float)ste.MicrosecondsPerQuarterNote;
                    info.syncInfos.Enqueue(new NoteSpawner.SyncInfo
                    {
                        time = tick,
                        bpm = bpm,
                        timeSignature = "4"
                    });
                    Debug.Log("[GameManager.CacheMidiFile] parsed Tempo entry: \"" + bpm + "\" at tick " + tick);
                }
                else if (te.Event is Melanchall.DryWetMidi.Core.TimeSignatureEvent tse)
                {
                    int tick = (int)Math.Round(te.Time * scale);
                    string ts = tse.Numerator.ToString();
                    // Denominator in Midi TimeSignatureEvent is given as power of two exponent (e.g. 2 -> 4)
                    try { ts = tse.Numerator + "/" + (1 << tse.Denominator); } catch { ts = tse.Numerator.ToString(); }
                    // Ensure there's at least one sync entry to attach the TS to; create one if necessary
                    if (info.syncInfos.Count > 0)
                    {
                        var last = info.syncInfos.ElementAt(info.syncInfos.Count - 1);
                        last.timeSignature = ts;
                    }
                    else
                    {
                        info.syncInfos.Enqueue(new NoteSpawner.SyncInfo { time = tick, bpm = 120f, timeSignature = ts });
                    }
                    Debug.Log("[GameManager.CacheMidiFile] parsed Time Signature entry: \"" + ts + "\" at tick " + tick);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameManager.CacheMidiFile] failed to parse tempo map: " + ex.Message);
        }

        try
        {
            var vAnimEvents = GetVenueTextEventsFromTrackByName(midi, "VENUE", scale);
            foreach (var vae in vAnimEvents) info.venueAnimCueEvents.Add(vae);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameManager.CacheMidiFile] failed to parse Venue/Scripting track: " + ex.Message);
        }

        // Compute max tick from collected noteInfos (already scaled)
        int maxTick = 0;
        try
        {
            foreach (var n in info.noteInfos)
            {
                int noteStart = (int)n.spawnTime;
                int noteEnd = noteStart + n.length;
                if (noteEnd > maxTick) maxTick = noteEnd;
                if (noteStart > maxTick) maxTick = noteStart;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameManager.CacheMidiFile] failed to compute maxTick: " + ex.Message);
        }

        // If we didn't get any explicit length from notes, fall back to last sync time
        if (maxTick == 0 && info.syncInfos.Count > 0)
        {
            maxTick = (int)info.syncInfos.ElementAt(info.syncInfos.Count - 1).time;
        }

        info.songLengthInTicks = Math.Max(1, maxTick);

        // Auto-generate BEAT track events from the sync track if none were provided in the MIDI
        try
        {
            if (info.beatEvents == null || info.beatEvents.Count == 0)
            {
                if (info.beatEvents == null) info.beatEvents = new List<NoteSpawner.GlobalEventInfo>();

                var generated = GenerateBeatEventsFromSync(info, includeEighthNotes: false);
                if (generated != null && generated.Count > 0)
                {
                    foreach (var be in generated)
                    {
                        bool alreadyExists = info.beatEvents.Any(existing => existing.spawnTime == be.spawnTime && existing.value == be.value);
                        if (!alreadyExists) info.beatEvents.Add(be);
                    }
                }
            }

            PruneFastTempoBeatSubdivisions(info);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameManager.CacheMidiFile] failed to generate beat events: " + ex.Message);
        }

        // Store in cache (pool)
        if (cachedSongs.ContainsKey(songID)) cachedSongs[songID] = info; else cachedSongs.Add(songID, info);

        await Task.Yield();
    }

    public async Task CacheChartFile(string path, int songID)
    {
        Debug.LogWarning("[CacheChartFile] Please convert your chart to a Clone Hero/Phase Shift MIDI file for best experience.");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new FileNotFoundException("Chart file not found", path);

        string chartText = File.ReadAllText(path);
        SongInfo info = new SongInfo();

        // Extract all sections like [SectionName] { ... }
        var sectionRe = new Regex("\\[\\s*(.+?)\\s*\\]\\s*\\{(.*?)\\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var sections = sectionRe.Matches(chartText);

        // Helper: find the body text for a section name
        string GetSectionBody(string name)
        {
            foreach (Match m in sections)
            {
                var secName = m.Groups[1].Value.Trim();
                if (string.Equals(secName, name, StringComparison.OrdinalIgnoreCase)) return m.Groups[2].Value;
            }
            return null;
        }

        // 1) Parse [Song] for Resolution and basic metadata
        var songBody = GetSectionBody("Song");
        if (!string.IsNullOrEmpty(songBody))
        {
            var kvRe = new Regex("^\\s*(\\w+)\\s*=\\s*(?:\"([^\"]*)\"|([^\\r\\n]+))", RegexOptions.Multiline);
            foreach (Match kv in kvRe.Matches(songBody))
            {
                string k = kv.Groups[1].Value.Trim();
                string v = kv.Groups[2].Success ? kv.Groups[2].Value : kv.Groups[3].Value;
                if (string.Equals(k, "Resolution", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(v, out int res)) info.resolution = res;
                    Debug.Log("[GameManager.CacheChartFile] parsed resolution: " + res);
                }
            }
        }

        // 2) Parse [SyncTrack] for tempo/time-signature entries
        var syncBody = GetSectionBody("SyncTrack");
        if (!string.IsNullOrEmpty(syncBody))
        {
            var syncLineRe = new Regex("^\\s*(\\d+)\\s*=\\s*([A-Za-z]+)\\s*(.*)$", RegexOptions.Multiline);
            foreach (Match ln in syncLineRe.Matches(syncBody))
            {
                int tick = int.Parse(ln.Groups[1].Value);
                string typ = ln.Groups[2].Value.Trim().ToUpperInvariant();
                string rest = ln.Groups[3].Value.Trim();
                if (typ == "B")
                {
                    // B <tempo in thousandth>
                    var tok = rest.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tok.Length > 0 && float.TryParse(tok[0], out float bpmUnSolved))
                    {
                        float bpm = bpmUnSolved / 1000;
                        info.syncInfos.Enqueue(new NoteSpawner.SyncInfo { time = tick, bpm = bpm, timeSignature = "4" });
                        Debug.Log("[GameManager.CacheChartFile] parsed Tempo entry: \"" + bpm + "\" at tick " + tick);
                    }
                }
                else if (typ == "TS")
                {
                    // TS <numerator> [<denominator>]
                    var tok = rest.Split(new[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    string ts = tok.Length > 0 ? tok[0] : "4";
                    if (tok.Length > 1) ts = tok[0] + "/" + tok[1]; else ts = ts + "/4";
                    if (info.syncInfos.Count > 0)
                    {
                        var last = info.syncInfos.ElementAt(info.syncInfos.Count - 1);
                        last.timeSignature = ts;
                    }
                    else
                    {
                        info.syncInfos.Enqueue(new NoteSpawner.SyncInfo { time = tick, bpm = 120f, timeSignature = ts });
                    }
                    Debug.Log("[GameManager.CacheChartFile] parsed Time Signature entry: \"" + ts + "\" at tick " + tick);
                }
            }
        }

        // 3) Parse [Events]
        var eventsBody = GetSectionBody("Events");
        if (!string.IsNullOrEmpty(eventsBody))
        {
            var evRe = new Regex("^\\s*(\\d+)\\s*=\\s*E\\s+\"([^\"]*)\"", RegexOptions.Multiline);
            foreach (Match em in evRe.Matches(eventsBody))
            {
                int tick = int.Parse(em.Groups[1].Value);
                string txt = em.Groups[2].Value;
                if (!info.globalEvents.ContainsKey(tick))
                {
                    info.globalEvents.Add(tick, new NoteSpawner.GlobalEventInfo { spawnTime = tick, spawnTimeMs = 0f, value = txt });
                }
                else
                {
                    var ex = info.globalEvents[tick];
                    if (!string.IsNullOrEmpty(ex.value)) ex.value += "|" + txt; else ex.value = txt;
                }
                Debug.Log("[GameManager.CacheChartFile] parsed Event entry: \"" + txt + "\" at tick " + tick);
            }
        }

        // 4) Parse difficulty instrument track (e.g. ExpertSingle) and map notes
        string chosenDiff = ddst; if (string.IsNullOrEmpty(chosenDiff)) chosenDiff = "Expert";
        string chosenPart = currentPart.ToUpper();
        string desiredTrack = chosenDiff + "Single";

        Match chosenTrackMatch = null;
        foreach (Match m in sections)
        {
            string secName = m.Groups[1].Value.Trim();
            if (string.Equals(secName, desiredTrack, StringComparison.OrdinalIgnoreCase)) { chosenTrackMatch = m; break; }
        }
        // fallback: find any section that contains both the diff and "Single"
        if (chosenTrackMatch == null)
        {
            foreach (Match m in sections)
            {
                string secName = m.Groups[1].Value.Trim();
                if (secName.IndexOf("Single", StringComparison.OrdinalIgnoreCase) >= 0 && secName.IndexOf(chosenDiff, StringComparison.OrdinalIgnoreCase) >= 0)
                { chosenTrackMatch = m; break; }
            }
        }

        if (chosenTrackMatch != null)
        {
            var trackBody = chosenTrackMatch.Groups[2].Value;
            var noteRe = new Regex("^\\s*(\\d+)\\s*=\\s*N\\s+(\\d+)\\s+(\\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (Match nm in noteRe.Matches(trackBody))
            {
                int tick = int.Parse(nm.Groups[1].Value);
                int noteNum = int.Parse(nm.Groups[2].Value);
                int length = int.Parse(nm.Groups[3].Value);

                int fret = -1;
                if (partGuitarDifficultyMappings.TryGetValue(chosenDiff, out var map) && map.TryGetValue(noteNum, out var mapped)) fret = mapped;
                else if (noteNum >= 0 && noteNum <= 8) fret = noteNum; // chart-style fret numbers
                else if (partGuitarDifficultyMappings.TryGetValue("Expert", out var emap) && emap.TryGetValue(noteNum, out var m2)) fret = m2; // fallback

                if (fret < 0) { /* unknown mapping, skip */ continue; }

                info.noteInfos.Enqueue(new NoteSpawner.NoteInfo
                {
                    spawnTime = tick,
                    spawnTimeMs = 0f,
                    length = enableSustains ? length : 0,
                    lengthMs = 0f,
                    fret = fret,
                    belongingPart = chosenPart.ToLower()
                });
                Debug.Log("[GameManager.CacheChartFile] parsed Note entry: \"fret=" + fret + ", length=" + length + "\" at tick " + tick);
            }
        }

        // Ensure at least one sync entry.
        if (info.syncInfos.Count == 0) info.syncInfos.Enqueue(new NoteSpawner.SyncInfo { time = 0, bpm = 120f, timeSignature = "4" });

        // Compute max tick from collected noteInfos (already in chart ticks)
        int maxTick = 0;
        try
        {
            foreach (var n in info.noteInfos)
            {
                int noteStart = (int)n.spawnTime;
                int noteEnd = noteStart + n.length;
                if (noteEnd > maxTick) maxTick = noteEnd;
                if (noteStart > maxTick) maxTick = noteStart;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameManager.CacheChartFile] failed to compute maxTick: " + ex.Message);
        }

        if (maxTick == 0 && info.syncInfos.Count > 0)
        {
            maxTick = (int)info.syncInfos.ElementAt(info.syncInfos.Count - 1).time;
        }

        info.songLengthInTicks = Math.Max(1, maxTick);

        // Auto-generate BEAT track events from the sync track if none were provided in the chart
        try
        {
            if (info.beatEvents == null || info.beatEvents.Count == 0)
            {
                if (info.beatEvents == null) info.beatEvents = new List<NoteSpawner.GlobalEventInfo>();

                var generated = GenerateBeatEventsFromSync(info, includeEighthNotes: false);
                if (generated != null && generated.Count > 0)
                {
                    foreach (var be in generated)
                    {
                        bool alreadyExists = info.beatEvents.Any(existing => existing.spawnTime == be.spawnTime && existing.value == be.value);
                        if (!alreadyExists) info.beatEvents.Add(be);
                    }
                }
            }

            PruneFastTempoBeatSubdivisions(info);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GameManager.CacheChartFile] failed to generate beat events: " + ex.Message);
        }

        // Store in cache (pool)
        if (cachedSongs.ContainsKey(songID)) cachedSongs[songID] = info; else cachedSongs.Add(songID, info);
        await Task.Yield();
    }

    // Convert a chart tick to seconds using the parsed sync entries in SongInfo
    private double GetSecondsAtTickFromSyncs(SongInfo info, int tick)
    {
        try
        {
            var syncList = info.syncInfos.ToList();
            if (syncList == null || syncList.Count == 0)
            {
                double sPerTick = 60.0 / (120.0 * Math.Max(1, info.resolution));
                return tick * sPerTick;
            }

            double totalSeconds = 0.0;
            int prevTick = 0;
            double prevBpm = syncList[0].bpm;

            for (int i = 0; i < syncList.Count; i++)
            {
                var entry = syncList[i];
                int segTick = (int)entry.time;
                int segEnd = Math.Min(segTick, tick);
                int delta = Math.Max(0, segEnd - prevTick);
                if (delta > 0)
                {
                    totalSeconds += delta * (60.0 / (prevBpm * Math.Max(1, info.resolution)));
                }
                prevTick = segEnd;
                prevBpm = entry.bpm;
                if (segEnd >= tick) break;
            }

            if (prevTick < tick)
            {
                totalSeconds += (tick - prevTick) * (60.0 / (prevBpm * Math.Max(1, info.resolution)));
            }

            return totalSeconds;
        }
        catch
        {
            double sPerTick = 60.0 / (120.0 * Math.Max(1, info.resolution));
            return tick * sPerTick;
        }
    }

    private void PruneFastTempoBeatSubdivisions(SongInfo info)
    {
        if (info == null || info.beatEvents == null || info.beatEvents.Count == 0) return;
        if (info.syncInfos == null || info.syncInfos.Count == 0) return;

        var pruned = new List<NoteSpawner.GlobalEventInfo>();
        foreach (var ev in info.beatEvents)
        {
            if (!int.TryParse(ev.value, out int noteNum) || noteNum <= 13)
            {
                pruned.Add(ev);
                continue;
            }

            var sync = info.syncInfos.Where(s => s.time <= ev.spawnTime).OrderByDescending(s => s.time).FirstOrDefault();
            if (sync != null && sync.bpm > 180f)
            {
                continue;
            }

            pruned.Add(ev);
        }

        info.beatEvents = pruned;
    }

    // Generate beat and bar events (value "12"=bar, "13"=beat). When requested, also add 8th-note subdivisions.
    private List<NoteSpawner.GlobalEventInfo> GenerateBeatEventsFromSync(SongInfo info, bool includeEighthNotes = false)
    {
        var outList = new List<NoteSpawner.GlobalEventInfo>();
        try
        {
            var syncList = info.syncInfos.ToList();
            if (syncList == null || syncList.Count == 0) return outList;

            int resolution = Math.Max(1, info.resolution);

            for (int i = 0; i < syncList.Count; i++)
            {
                var sync = syncList[i];
                int segStart = (int)sync.time;
                int segEnd = (i < syncList.Count - 1) ? (int)syncList[i + 1].time : info.songLengthInTicks;

                // parse time signature (format: "N/D" or just "N")
                int numerator = 4;
                int denominator = 4;
                if (!string.IsNullOrEmpty(sync.timeSignature))
                {
                    var parts = sync.timeSignature.Split('/');
                    if (parts.Length >= 1) int.TryParse(parts[0], out numerator);
                    if (parts.Length >= 2) int.TryParse(parts[1], out denominator);
                }

                if (denominator <= 0) denominator = 4;
                if (numerator <= 0) numerator = 4;

                int ticksPerBeat = Math.Max(1, resolution * 4 / denominator);
                int beatsPerBar = Math.Max(1, numerator);
                int ticksPerBar = ticksPerBeat * beatsPerBar;
                int eighthNoteStep = Math.Max(1, ticksPerBeat / 2);
                bool allowEighthsInSection = includeEighthNotes && sync.bpm <= 180f;

                // start bars at the segment start; if you want alignment to zero-based bars, adjust here
                for (int barTick = segStart; barTick < segEnd; barTick += ticksPerBar)
                {
                    // bar marker (12)
                    double barSeconds = GetSecondsAtTickFromSyncs(info, barTick);
                    outList.Add(new NoteSpawner.GlobalEventInfo
                    {
                        spawnTime = barTick,
                        spawnTimeMs = (float)(barSeconds * 1000.0),
                        value = "12"
                    });

                    // quarter-note beats inside the bar (13)
                    for (int b = 1; b < beatsPerBar; b++)
                    {
                        int beatTick = barTick + b * ticksPerBeat;
                        if (beatTick >= segEnd) break;
                        double beatSeconds = GetSecondsAtTickFromSyncs(info, beatTick);
                        outList.Add(new NoteSpawner.GlobalEventInfo
                        {
                            spawnTime = beatTick,
                            spawnTimeMs = (float)(beatSeconds * 1000.0),
                            value = "13"
                        });
                    }

                    if (allowEighthsInSection)
                    {
                        for (int step = 1; step < beatsPerBar * 2; step++)
                        {
                            int eighthTick = barTick + step * eighthNoteStep;
                            if (eighthTick >= segEnd) break;
                            if (eighthTick % ticksPerBeat == 0) continue;
                            double eighthSeconds = GetSecondsAtTickFromSyncs(info, eighthTick);
                            outList.Add(new NoteSpawner.GlobalEventInfo
                            {
                                spawnTime = eighthTick,
                                spawnTimeMs = (float)(eighthSeconds * 1000.0),
                                value = "14"
                            });
                        }
                    }
                }
            }

            // Deduplicate events (spawnTime + value)
            var seen = new HashSet<string>();
            var uniq = new List<NoteSpawner.GlobalEventInfo>();
            foreach (var ev in outList.OrderBy(e => e.spawnTime).ThenBy(e => e.value))
            {
                string key = ev.spawnTime + "_" + ev.value;
                if (seen.Add(key)) uniq.Add(ev);
            }
            return uniq;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GenerateBeatEventsFromSync failed: " + ex.Message);
            return outList;
        }
    }


    private List<NoteSpawner.NoteInfo> GetNotesFromTrackByName(MidiFile midi, string trackName, Dictionary<int,int> noteToFretMap, double scale)
    {
        var result = new List<NoteSpawner.NoteInfo>();
        foreach (var trackChunk in midi.GetTrackChunks())
        {
            // Try sequence/track name or text events
            var nameEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.SequenceTrackNameEvent>().FirstOrDefault();
            string name = nameEvt?.Text;
            if (string.IsNullOrEmpty(name))
            {
                var textEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.TextEvent>().FirstOrDefault();
                name = textEvt?.Text;
            }
            if (string.IsNullOrEmpty(name)) continue;
            if (!name.Equals(trackName, StringComparison.OrdinalIgnoreCase)) continue;

            NoteDetectionSettings detectionSettings = new NoteDetectionSettings
            {
                NoteStartDetectionPolicy = NoteStartDetectionPolicy.FirstNoteOn
            };

            // Collect notes from this chunk
            var notes = trackChunk.GetNotes(detectionSettings).ToList();
            var tempoMap = midi.GetTempoMap();

            // Identify freestyle trigger notes (MIDI 120-124)
            var freestyleTriggers = notes.Where(n => n.NoteNumber >= 120 && n.NoteNumber <= 124).ToList();
            var regions = new List<(long startTick, long endTick, int startTickScaled, int lengthScaled, float startMs, float lengthMs)>();
            foreach (var ft in freestyleTriggers)
            {
                long s = ft.Time;
                long e = ft.Time + ft.Length;
                int sScaled = (int)Math.Round(s * scale);
                int lenScaled = (int)Math.Round(ft.Length * scale);
                var metricStart = TimeConverter.ConvertTo<MetricTimeSpan>(s, tempoMap);
                var metricLen = TimeConverter.ConvertTo<MetricTimeSpan>(ft.Length, tempoMap);
                float startMs = (float)(metricStart.TotalMicroseconds / 1000.0);
                float lengthMs = (float)(metricLen.TotalMicroseconds / 1000.0);
                regions.Add((s, e, sScaled, lenScaled, startMs, lengthMs));
            }

            // Add regular notes that are NOT inside any freestyle region and are not trigger notes
            foreach (var note in notes)
            {
                if (!noteToFretMap.TryGetValue(note.NoteNumber, out int fret)) continue;
                // Skip trigger notes themselves
                if (note.NoteNumber >= 120 && note.NoteNumber <= 124) continue;

                bool insideFreestyle = regions.Any(r => note.Time >= r.startTick && note.Time < r.endTick);
                if (insideFreestyle) continue; // don't add regular notes under freestyle

                var metricStart = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                var metricLength = TimeConverter.ConvertTo<MetricTimeSpan>(note.Length, tempoMap);
                double startSeconds = metricStart.TotalMicroseconds / 1_000_000.0;
                double lengthSeconds = metricLength.TotalMicroseconds / 1_000_000.0;

                result.Add(new NoteSpawner.NoteInfo
                {
                    spawnTime = (int)Math.Round(note.Time * scale), // legacy tick value
                    spawnTimeMs = (float)(startSeconds * 1000.0),
                    length = enableSustains ? (int)Math.Round(note.Length * scale) : 0,
                    lengthMs = enableSustains ? (float)(lengthSeconds * 1000.0) : 0,
                    fret = fret,
                    belongingPart = trackName.Replace("PART ", "").ToLower()
                });
                Debug.Log("[GameManager.CacheMidiFile.GetNotesFromTrackByName] parsed Note entry: \"fret=" + fret + ", length=" + (enableSustains ? (int)Math.Round(note.Length * scale) : 0) + "\" at tick " + (int)Math.Round(note.Time * scale));
            }

            // For each freestyle region create a parent sustain entry only (single visual)
            foreach (var r in regions)
            {
                // Parent freestyle sustain marker
                result.Add(new NoteSpawner.NoteInfo
                {
                    spawnTime = r.startTickScaled,
                    spawnTimeMs = r.startMs,
                    length = r.lengthScaled,
                    lengthMs = r.lengthMs,
                    fret = -1,
                    belongingPart = trackName.Replace("PART ", "").ToLower(),
                    // mark as parent so spawner can create sustain visuals
                    isFreestyleParent = true
                });
            }

            // Sort by absolute milliseconds (fallback to tick value if ms not present)
            result = result.OrderBy(n => n.spawnTimeMs > 0f ? n.spawnTimeMs : n.spawnTime * 1f).ToList();

            // If you expect only one matching track, break here.
            break;
        }
        return result;
    }

    private List<NoteSpawner.GlobalEventInfo> GetTextEventsFromTrackByName(MidiFile midi, string trackName, double scale)
    {
        var result = new List<NoteSpawner.GlobalEventInfo>();
        foreach (var trackChunk in midi.GetTrackChunks())
        {
            // Try sequence/track name or text events
            var nameEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.SequenceTrackNameEvent>().FirstOrDefault();
            string name = nameEvt?.Text;
            if (string.IsNullOrEmpty(name))
            {
                var textEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.TextEvent>().FirstOrDefault();
                name = textEvt?.Text;
            }
            if (string.IsNullOrEmpty(name)) continue;
            if (!name.Equals(trackName, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var eve in trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.TextEvent>())
            {
                // Convert MIDI tick times to absolute seconds using tempo map, then to milliseconds
                var tempoMap = midi.GetTempoMap();
                var metricStart = TimeConverter.ConvertTo<MetricTimeSpan>(eve.DeltaTime, tempoMap);
                double startSeconds = metricStart.TotalMicroseconds / 1_000_000.0;
                result.Add(new NoteSpawner.GlobalEventInfo
                {
                    spawnTime = (int)Math.Round(eve.DeltaTime * scale), // legacy tick value
                    spawnTimeMs = (float)(startSeconds * 1000.0),
                    value = eve.Text
                });
                //Debug.Log("Text event Added: " + eve.Text);
            }

            // If you expect only one matching track, break here.
            break;
        }
        return result;
    }

    private List<NoteSpawner.GlobalEventInfo> GetBeatEventsFromTrackByName(MidiFile midi, string trackName, double scale)
    {
        var result = new List<NoteSpawner.GlobalEventInfo>();
        foreach (var trackChunk in midi.GetTrackChunks())
        {
            var nameEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.SequenceTrackNameEvent>().FirstOrDefault();
            string name = nameEvt?.Text;
            if (string.IsNullOrEmpty(name))
            {
                var textEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.TextEvent>().FirstOrDefault();
                name = textEvt?.Text;
            }
            if (string.IsNullOrEmpty(name)) continue;
            if (!name.Equals(trackName, StringComparison.OrdinalIgnoreCase)) continue;

            var notes = trackChunk.GetNotes().ToList();
            var tempoMap = midi.GetTempoMap();
            foreach (var note in notes)
            {
                var metricStart = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                double startSeconds = metricStart.TotalMicroseconds / 1_000_000.0;
                result.Add(new NoteSpawner.GlobalEventInfo
                {
                    spawnTime = (int)Math.Round(note.Time * scale),
                    spawnTimeMs = (float)(startSeconds * 1000.0),
                    value = note.NoteNumber.ToString()
                });
            }

            break; // assume only one BEAT track
        }
        return result;
    }
    private List<NoteSpawner.GlobalEventInfo> GetVenueTextEventsFromTrackByName(MidiFile midi, string trackName, double scale)
    {
        var result = new List<NoteSpawner.GlobalEventInfo>();
        foreach (var trackChunk in midi.GetTrackChunks())
        {
            var nameEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.SequenceTrackNameEvent>().FirstOrDefault();
            string name = nameEvt?.Text;
            if (string.IsNullOrEmpty(name))
            {
                var textEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.TextEvent>().FirstOrDefault();
                name = textEvt?.Text;
            }
            if (string.IsNullOrEmpty(name)) continue;
            if (!name.Equals(trackName, StringComparison.OrdinalIgnoreCase)) continue;

            var notes = trackChunk.GetTimedEvents().Where(e => e.Event is BaseTextEvent).ToList();
            var tempoMap = midi.GetTempoMap();
            foreach (var note in notes)
            {
                var textEvent = (BaseTextEvent)note.Event;
                var metricStart = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                double startSeconds = metricStart.TotalMicroseconds / 1_000_000.0;
                if (textEvent.Text == name) continue;
                result.Add(new NoteSpawner.GlobalEventInfo
                {
                    spawnTime = (int)Math.Round(note.Time * scale),
                    spawnTimeMs = (float)(startSeconds * 1000.0),
                    value = textEvent.Text
                });
                //Debug.Log("added \"" + textEvent.Text + "\" at tick (" + (int)Math.Round(note.Time * scale) + ")");
            }

            break; // assume only one BEAT track
        }
        return result;
    }
    private List<NoteSpawner.LyricEventInfo> GetNotesAndTextFromVocalTrack(MidiFile midi, string trackName, double scale)
    {
        var result = new List<NoteSpawner.LyricEventInfo>();
        foreach (var trackChunk in midi.GetTrackChunks())
        {
            var nameEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.SequenceTrackNameEvent>().FirstOrDefault();
            string name = nameEvt?.Text;
            if (string.IsNullOrEmpty(name))
            {
                var textEvt = trackChunk.Events.OfType<Melanchall.DryWetMidi.Core.TextEvent>().FirstOrDefault();
                name = textEvt?.Text;
            }
            if (string.IsNullOrEmpty(name)) continue;
            if (!name.Equals(trackName, StringComparison.OrdinalIgnoreCase)) continue;

            var notes = trackChunk.GetNotes().ToList();
            var texts = trackChunk.GetTimedEvents().Where(e => e.Event is BaseTextEvent).ToList();

            // For each text event, find notes that occur at the same MIDI tick and create lyric entries.
            foreach (var timed in texts)
            {
                var baseText = timed.Event as BaseTextEvent;
                if (baseText == null) continue;
                long textTick = timed.Time;
                string lyricValue = baseText.Text;

                var matchingNotes = notes.Where(n => n.Time == textTick).ToList();
                if (matchingNotes.Count == 0) continue; // only add entries when a note matches the text tick

                foreach (var note in matchingNotes)
                {
                    double freq = 440.0 * Math.Pow(2.0, (note.NoteNumber - 69) / 12.0);
                    
                    var tempoMap = midi.GetTempoMap();
                    var metricStart = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                    var metricLength = TimeConverter.ConvertTo<MetricTimeSpan>(note.Length, tempoMap);
                    double startSeconds = metricStart.TotalMicroseconds / 1_000_000.0;
                    double lengthSeconds = metricLength.TotalMicroseconds / 1_000_000.0;

                    result.Add(new NoteSpawner.LyricEventInfo
                    {
                        spawnTick = (float)Math.Round(note.Time * scale),
                        spawnTickMs = (float)(startSeconds * 1000.0),
                        sungNote = freq,
                        length = (float)Math.Round(note.Length * scale),
                        lengthMs = (int)(lengthSeconds * 1000.0),
                        value = lyricValue
                    });
                }
            }
            break;
        }
        return result;
    }
    public string GetCachedSongTitle(int cachedEntry)
    {
        var song = GetCachedSongEntry(cachedEntry);
        return song != null ? song.songTitle : null;
    }

    public string GetCachedSongLoadingPhrase(int cachedEntry)
    {
        var song = GetCachedSongEntry(cachedEntry);
        return song != null ? song.songLoadingPhrase : null;
    }
    public void EnableLoadSongVisual(GameObject loadingCanvas, int cachedID)
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
            var song = GetCachedSongEntry(cachedID);
            if (song != null)
            {
                TextMeshProUGUI textObj = loadingCanvas.transform.Find("SongLoadingOverlay/LoadingPhraseText").GetComponent<TextMeshProUGUI>();
                textObj.text = song.songLoadingPhrase;
            }
        }
    }
    public async Task EnableLoadUnCachedSongVisual(GameObject loadingCanvas, string IniPath)
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(true);
            TextMeshProUGUI textObj = loadingCanvas.transform.Find("SongLoadingOverlay/LoadingPhraseText").GetComponent<TextMeshProUGUI>();
            string ldgphr = GetUnCachedSongLoadingPhrase(IniPath);
            if (ldgphr != string.Empty)
            {
                textObj.text = ldgphr;
            }
            else
            {
                string serverLoad = await GetStringFromAddr($"http://{serverIPAddr}/clobeats/getRandLdgPhr.php");
                string serverLoadReplaced = serverLoad.Replace("%SERVERNAME%", serverIPAddr, StringComparison.OrdinalIgnoreCase);
                textObj.text = serverLoadReplaced;
            }
        }
    }
    public string GetUnCachedSongLoadingPhrase(string iniPath)
    {
        INIParser ini = new INIParser();
        ini.Open(iniPath);
        return ini.ReadValue("song", "loading_phrase", string.Empty);
    }
    public void DisableLoadSongVisual(GameObject loadingCanvas)
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.SetActive(false);
        }
    }

    public IEnumerator PlaySong()
    {
        UIUpdater uiUpdater = FindAnyObjectByType<UIUpdater>();
        GameObject gp = GameObject.Find("GuitarPlayer");
        SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
        VenueAnimationPlayer venueAnimationPlayer = FindAnyObjectByType<VenueAnimationPlayer>();
        if (uiUpdater != null)
        {
            StartCoroutine(uiUpdater.SongInfoAnim());
            uiUpdater.InitializeUI();
            
        }
        
        if (venueAnimationPlayer != null)
        {
            venueAnimationPlayer.TryToggleHighwayCam(false);
            venueAnimationPlayer.Load();
            uiUpdater.ScoreVisibility(false);

        }
        if (unDestructibleLoadingPhraseScreen != null && unDestructibleLoadingPhraseScreen.activeSelf)
        {
            DisableLoadSongVisual(unDestructibleLoadingPhraseScreen);
        }
        //sFXPlayer.PlayClip("StartCheer1");
        
        if (SceneManager.GetSceneByBuildIndex(2).isLoaded)
        {
            
            AnimationClip vEntry = Resources.Load<AnimationClip>("VenueEntry");
            venueAnimationPlayer.TryCueCamAnim(venueAnimationPlayer.mainCamera, vEntry);
            yield return new WaitForSeconds(vEntry.length);
            venueAnimationPlayer.ReturnCameraToDefaultPosition();
            
        }

        if (gp != null)
        {
            NoteSpawner noteSpawner = FindAnyObjectByType<NoteSpawner>();
            gp.transform.position = new Vector3(0, 0, 0);
            
            if (noteSpawner != null)
            {
                venueAnimationPlayer.TryToggleHighwayCam(true);
                //yield return noteSpawner.InitGameplay();
                noteSpawner.Play();
                inSong = true;
            }
            
        }
        if (uiUpdater != null)
        {
            uiUpdater.ScoreVisibility(true);
            sFXPlayer.PlayScoreShowClip();
        }
        yield return null;
        
        
    }

    public IEnumerator FailSong()
    {
        MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
        StartCoroutine(musicPlayer.EndSong(false));
        SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
        sFXPlayer.PlaySongFailedClip();
        yield return new WaitForSecondsRealtime(3);
        SceneManager.LoadScene("MainMenu");
    }

    public void ResetAllValues()
    {
        GlobalMoveY globalMoveY = FindAnyObjectByType<GlobalMoveY>();
        if (globalMoveY != null)
        {
            if (globalMoveY.objectsToMove.Count > 0)
            {
                foreach (GameObject go in globalMoveY.objectsToMove)
                {
                    if (go != null)
                    Destroy(go);
                }
                globalMoveY.objectsToMove.Clear();
            }
        }
        currentSongTitle = string.Empty;
        currentSongArtist = string.Empty;
        currentSongAlbum = string.Empty;
        currentSongYear = 0;
        currentSongLoadingPhrase = string.Empty;
        currentSongAuthor = string.Empty;
        currentSongLength = 0;
        currentSongAccentColor = string.Empty;
        currentSongPreviewStartTime = 0;
        currentSongResolution = 192;
        currentSongSyncTrack.Clear();
        currentSongNotes.Clear();
        currentSongEvents.Clear();
        currentSongLengthInTicks = 0;
    }

    public static async Task<string> GetStringFromAddr(string addr)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("CloBeats/0.0.1");
            try
            {
                return await client.GetStringAsync(addr);
            }
            catch (Exception ex)
            {
                return "Server error occoured: " + ex.Message;
            }
        }
    }

    public static async Task PostStringToAddr(string addr, string value)
    {
        HttpContent content = new StringContent(value, Encoding.UTF8);
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("CloBeats/0.0.1");
            try
            {
                await client.PostAsync(addr, content);
            }
            catch (Exception ex)
            {
                Debug.LogError("Server error occoured: " + ex.Message);
                
            }
        }
    }

    public static async Task PostJSONToAddr(string addr, string value)
    {
        HttpContent content = new StringContent(value, Encoding.UTF8, "application/json");
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("CloBeats/0.0.1");
            
            var response = await client.PostAsync(addr, content);

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                Debug.Log("JSON POST Successful. Response: " + responseString);
            }
            else
            {
                Debug.LogError("JSON POST failed with status code " + response.StatusCode);
            }
        }
    }

    // Used for loading DDS images.
    public static Texture2D FlipTextureVerticallyGPU(Texture2D original)
    {
        RenderTexture rt = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        // Vector2(1, -1) scales the Y axis by -1 (flips vertically)
        Graphics.Blit(original, rt, new Vector2(1, -1), new Vector2(0, 1)); 

        Texture2D flipped = new Texture2D(original.width, original.height);
        RenderTexture.active = rt;
        flipped.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        flipped.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return flipped;
    }



    // Update is called once per frame
    void Update()
    {
        ddst = PlayerPrefs.GetString("SelectedDifficulty");
        currentPart = PlayerPrefs.GetString("SelectedPart");
    }
    
}
