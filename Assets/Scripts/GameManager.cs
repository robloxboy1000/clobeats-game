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
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;


public class GameManager : MonoBehaviour
{
    public bool enableSustains = true;
    public List<string> songFolders;

    public Dictionary<int, SongInfo> cachedSongs = new Dictionary<int, SongInfo>();
    public Dictionary<int, SongEntryInfo> cachedEntries = new Dictionary<int, SongEntryInfo>();

    public class SongInfo
    {
        public int resolution = 480;
        public Queue<NoteSpawner.SyncInfo> syncInfos = new Queue<NoteSpawner.SyncInfo>();
        public Queue<NoteSpawner.NoteInfo> noteInfos = new Queue<NoteSpawner.NoteInfo>();
        public Queue<NoteSpawner.GlobalEventInfo> globalEvents = new Queue<NoteSpawner.GlobalEventInfo>();
        public int songLengthInTicks = 0;
    }

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
    public int cachedSongChartCount = 0;
    public int cachedSongEntryCount = 0;

    public int currentSongResolution = 480;
    public Queue<NoteSpawner.NoteInfo> currentSongNotes = new Queue<NoteSpawner.NoteInfo>();
    public Queue<NoteSpawner.SyncInfo> currentSongSyncTrack = new Queue<NoteSpawner.SyncInfo>();
    public Queue<NoteSpawner.GlobalEventInfo> currentSongEvents = new Queue<NoteSpawner.GlobalEventInfo>();
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

    // note mappings
    Dictionary<string, Dictionary<int, int>> difficultyMappings = new Dictionary<string, Dictionary<int,int>>
    {
        ["Easy"] = new Dictionary<int,int> { {60,0}, {61,1}, {62,2}, {63,3}, {64,4}, {59,7} },
        ["Medium"] = new Dictionary<int,int> { {72,0}, {73,1}, {74,2}, {75,3}, {76,4}, {71,7} },
        ["Hard"] = new Dictionary<int,int> { {84,0}, {85,1}, {86,2}, {87,3}, {88,4}, {83,7} },
        ["Expert"] = new Dictionary<int,int> { {96,0}, {97,1}, {98,2}, {99,3}, {100,4}, {95,7} },
    };

    // Start is called before the first frame update
    void Start()
    {
        ddst = PlayerPrefs.GetString("SelectedDifficulty");
    }
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public SongEntryInfo GetCachedSongEntry(int id)
    {
        try
        {
            return cachedEntries[id];
        }
        catch
        {
            return null;
        }
    }

    
    public async Task CacheSingleSong(string folder, int songID, int count)
    {
        if (!File.Exists(folder + "/song.ini")) return;
        await SetIniFileData(await File.ReadAllTextAsync(folder + "/song.ini"));
        cachedEntries.Add(songID, new SongEntryInfo
        {
            songTitle = currentSongTitle,
            songArtist = currentSongArtist,
            songAlbum = currentSongAlbum,
            songYear = currentSongYear,
            songLoadingPhrase = currentSongLoadingPhrase,
            songAuthor = currentSongAuthor,
            songLength = currentSongLength,
            songAccentColor = currentSongAccentColor,
            songPreviewStartTime = currentSongPreviewStartTime,
            cachedSongID = songID,
            songPath = folder,
            songNumber = count
        });
    }
    public async Task ReadMidiFile(string path)
    {
        // Read MIDI, cache it by a stable hash of the full path, and copy into current song queues
        string fullPath = Path.GetFullPath(path ?? string.Empty);
        int songID = fullPath.GetHashCode();
        //Debug.Log(songID);

        try
        {
            await CacheMidiFile(fullPath, songID);

            if (cachedSongs.TryGetValue(songID, out SongInfo si))
            {
                // Copy cached info into current song state
                currentSongResolution = si.resolution;
                currentSongSyncTrack = new Queue<NoteSpawner.SyncInfo>(si.syncInfos);
                currentSongNotes = new Queue<NoteSpawner.NoteInfo>(si.noteInfos);
                currentSongEvents = new Queue<NoteSpawner.GlobalEventInfo>(si.globalEvents);
                currentSongLengthInTicks = si.songLengthInTicks;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("ReadMidiFile failed: " + ex.Message);
        }
        await Task.Yield();
    }

    /// <summary>
    /// Cache a MIDI file by path into the song cache under the supplied songID.
    /// Builds a SongInfo containing sync (tempo/time-signature) events and note infos (tick times/lengths).
    /// </summary>
    public async Task CacheMidiFile(string path, int songID)
    {
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

        int targetResolution = currentSongResolution > 0 ? currentSongResolution : 192;
        double scale = (double)targetResolution / Math.Max(1, midiResolution);

        var globalEvents = GetTextEventsFromTrackByName(midi, "EVENTS", scale);
        foreach (var evt in globalEvents) info.globalEvents.Enqueue(evt);

        // choose difficulty key (string) from player settings or UI
        string chosenDiff = ddst;
        if (string.IsNullOrEmpty(chosenDiff)) chosenDiff = "Expert";
        if (difficultyMappings.TryGetValue(chosenDiff, out var map))
        {
            var trackNotes = GetNotesFromTrackByName(midi, "PART GUITAR", map, scale);
            foreach (var ni in trackNotes) info.noteInfos.Enqueue(ni);
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
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("CacheMidiFile: failed to parse timed events: " + ex.Message);
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
            Debug.LogWarning("CacheMidiFile: failed to compute maxTick: " + ex.Message);
        }

        // If we didn't get any explicit length from notes, fall back to last sync time
        if (maxTick == 0 && info.syncInfos.Count > 0)
        {
            maxTick = (int)info.syncInfos.ElementAt(info.syncInfos.Count - 1).time;
        }

        info.songLengthInTicks = Math.Max(1, maxTick);

        // Store in cache (pool)
        if (cachedSongs.ContainsKey(songID)) cachedSongs[songID] = info; else cachedSongs.Add(songID, info);

        await Task.Yield();
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
                NoteStartDetectionPolicy = NoteStartDetectionPolicy.FirstNoteOn,
                NoteSearchContext = NoteSearchContext.AllEventsCollections
            };
            // Get notes from this chunk only (scale ticks to target resolution)
            var notes = trackChunk.GetNotes(detectionSettings);
            foreach (var note in notes)
            {
                
                if (noteToFretMap.TryGetValue(note.NoteNumber, out int fret))
                {
                    // Convert MIDI tick times to absolute seconds using tempo map, then to milliseconds
                    var tempoMap = midi.GetTempoMap();
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
                        fret = fret
                    });
                }
            }
            
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
                Debug.Log("Text event Added: " + eve.Text);
            }

            // If you expect only one matching track, break here.
            break;
        }
        return result;
    }

    /// <summary>
    /// Pre-pool a list of MIDI files into the cache. Uses the full-path hash as songID.
    /// </summary>
    public async Task PrepoolMidiFiles(List<string> paths, int poolCount)
    {
        if (paths == null || paths.Count == 0) return;
        int count = Math.Min(poolCount, paths.Count);
        for (int i = 0; i < count; i++)
        {
            string p = paths[i];
            if (string.IsNullOrEmpty(p) || !File.Exists(p)) continue;
            int id = Path.GetFullPath(p).GetHashCode();
            if (!cachedSongs.ContainsKey(id))
            {
                try { await CacheMidiFile(p, id); } catch (Exception ex) { Debug.LogWarning("PrepoolMidiFiles: " + ex.Message); }
            }
        }
    }

    public async Task SetIniFileData(string data)
    {
        string[] lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        bool inSongSection = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("["))
            {
                inSongSection = trimmedLine == "[song]" || trimmedLine == "[Song]";
                continue;
            }

            if (inSongSection)
            {
                string[] parts = trimmedLine.Split('=');
                    
                if (parts.Length == 2 && parts[0].Trim() == "name" && parts[1].Trim() is string name)
                {
                    currentSongTitle = name.Trim();
                }
                else if (parts.Length == 2 && parts[0].Trim() == "artist" && parts[1].Trim() is string artist)
                {
                    currentSongArtist = artist.Trim();
                }
                else if (parts.Length == 2 && parts[0].Trim() == "album" && parts[1].Trim() is string album)
                {
                    currentSongAlbum = album.Trim();
                }
                else if (parts.Length == 2 && parts[0].Trim() == "year" && int.TryParse(parts[1].Trim(), out int year))
                {
                    currentSongYear = year;
                }
                else if (parts.Length == 2 && parts[0].Trim() == "loading_phrase" && parts[1].Trim() is string phrase)
                {
                    currentSongLoadingPhrase = phrase.Trim();
                }
                else if (parts.Length == 2 && parts[0].Trim() == "charter" && parts[1].Trim() is string author)
                {
                    currentSongAuthor = author.Trim();
                }
                else if (parts.Length == 2 && parts[0].Trim() == "song_length" && int.TryParse(parts[1].Trim(), out int length))
                {
                    currentSongLength = length;
                }
                else if (parts.Length == 2 && parts[0].Trim() == "back_color" && parts[1].Trim() is string hex)
                {
                    currentSongAccentColor = hex;
                }
                else if (parts.Length == 2 && parts[0].Trim() == "preview_start_time" && int.TryParse(parts[1].Trim(), out int startTime))
                {
                    currentSongPreviewStartTime = startTime;
                }
            }
            await Task.Yield();
        }
    }
    public async Task<string> GetSongTitle(string iniPath)
    {
        string data = await File.ReadAllTextAsync(iniPath);
        string[] lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        bool inSongSection = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("["))
            {
                inSongSection = trimmedLine == "[song]" || trimmedLine == "[Song]";
                continue;
            }

            if (inSongSection)
            {
                string[] parts = trimmedLine.Split('=');
                    
                if (parts.Length == 2 && parts[0].Trim() == "name" && parts[1].Trim() is string name)
                {
                    return name.Trim();
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        return null; 
    }

    public async Task<string> GetSongLoadingPhrase(string iniPath)
    {
        string data = await File.ReadAllTextAsync(iniPath);
        string[] lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        bool inSongSection = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("["))
            {
                inSongSection = trimmedLine == "[song]" || trimmedLine == "[Song]";
                continue;
            }

            if (inSongSection)
            {
                string[] parts = trimmedLine.Split('=');
                    
                if (parts.Length == 2 && parts[0].Trim() == "loading_phrase" && parts[1].Trim() is string name)
                {
                    return name.Trim();
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        return null; 
    }

    public IEnumerator PlaySong()
    {
        
        UIUpdater uiUpdater = FindAnyObjectByType<UIUpdater>();
        GameObject gp = GameObject.Find("GuitarPlayer");
        SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
        if (uiUpdater != null)
        {
            uiUpdater.songInfoPanel.SetActive(true);
            uiUpdater.loadingOverlay.SetActive(false);
        }
        if (gp != null)
        {
            gp.transform.position = new Vector3(0, -6, 6);
        }
        
        yield return new WaitForSecondsRealtime(0.1f);

        if (SceneManager.GetSceneByBuildIndex(2).isLoaded)
        {
            GameObject venue = GameObject.Find("3DVenue_Camera");
            if (venue != null)
            {
                Animation venueAnim = venue.GetComponent<Animation>();
                venueAnim.Play("VenueEntry");
                yield return new WaitForSecondsRealtime(10f);
            }
        }
        if (uiUpdater != null)
        {
            uiUpdater.InitializeUI();
            uiUpdater.songInfoPanel.SetActive(false);
        }
        
        if (gp != null)
        {
            Animation highwayAnim = gp.GetComponent<Animation>();
            highwayAnim.Play("ShowHighway");
            sFXPlayer.PlayHighwayRiseClip();
            yield return new WaitForSecondsRealtime(1f);
            var sl = gp.transform.Find("Strikeline").GetComponent<ImprovedStrikeline>();
            if (sl != null)
            {
                sl.RippleAnim();
                sFXPlayer.PlayFretRippleUpClip();
                yield return new WaitForSecondsRealtime(1f);
            }
            highwayAnim.Stop();
        }
        VenueAnimationPlayer venueAnimationPlayer = FindAnyObjectByType<VenueAnimationPlayer>();
        if (venueAnimationPlayer != null)
        {
            venueAnimationPlayer.Load();
        }
        NoteSpawner noteSpawner = FindAnyObjectByType<NoteSpawner>();
        if (noteSpawner != null)
        {
            noteSpawner.Play();
            inSong = true;
        }
        
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
                Debug.LogError("Server error occoured: " + ex.Message);
                return null;
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
    }
    
}
