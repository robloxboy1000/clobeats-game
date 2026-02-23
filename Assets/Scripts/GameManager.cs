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
    public List<string> songFolders;

    public Dictionary<int, SongInfo> cachedSongs = new Dictionary<int, SongInfo>();
    public Dictionary<int, SongEntryInfo> cachedEntries = new Dictionary<int, SongEntryInfo>();

    public class SongInfo
    {
        public int resolution = 192;
        public Queue<NoteSpawner.SyncInfo> syncInfos = new Queue<NoteSpawner.SyncInfo>();
        public Queue<NoteSpawner.NoteInfo> noteInfos = new Queue<NoteSpawner.NoteInfo>();
        public Dictionary<int, string> globalEvents = new Dictionary<int, string>();
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
    }
    public string ddst;
    public int songChartCount = 0;
    public int songEntryCount = 0;

    public int currentSongResolution = 192;
    public Queue<NoteSpawner.NoteInfo> currentSongNotes = new Queue<NoteSpawner.NoteInfo>();
    public Queue<NoteSpawner.SyncInfo> currentSongSyncTrack = new Queue<NoteSpawner.SyncInfo>();
    public Dictionary<int, string> currentSongEvents = new Dictionary<int, string>();
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

    public async Task CacheSongs(bool songEntryMode)
    {
        Debug.Log("Caching songs");
        foreach (string folder in songFolders)
        {
            Debug.Log("Caching folder " + songChartCount + ": " + folder);
            // caches chart file and add to cached song dictionary
            if (!File.Exists(folder + "/notes.chart")) continue;
            var songFolderFiles = await Task.Run(() => Directory.GetFiles(folder));
            List<string> supportedFormats = new List<string> { "wav", "ogg", "mp3" };
            var songMatch = songFolderFiles
                .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                .FirstOrDefault(x => x.name == "song" && supportedFormats.Contains(x.ext));
            if (songMatch != null)
            {
                if (!File.Exists(songMatch.path)) continue;
            }
            else
            {
                continue;
            }
            
            if (!File.Exists(folder + "/song.ini")) continue;
            if (!songEntryMode)
            {
                await CacheChartFile(await File.ReadAllTextAsync(folder + "/notes.chart"), songChartCount);
                cachedSongs.Add(songChartCount, new SongInfo
                {
                    resolution = currentSongResolution,
                    syncInfos = currentSongSyncTrack,
                    noteInfos = currentSongNotes,
                    globalEvents = currentSongEvents,
                    songLengthInTicks = currentSongLengthInTicks
                });
                // clear current parsing values
                currentSongResolution = 192;
                currentSongSyncTrack.Clear();
                currentSongNotes.Clear();
                currentSongEvents.Clear();
                currentSongLengthInTicks = 0;
                // increase song count
                songChartCount++;
            }
            else if (songEntryMode)
            {
                await CacheIniFile(await File.ReadAllTextAsync(folder + "/song.ini"), songEntryCount);
                currentSongPath = folder;
                cachedEntries.Add(songEntryCount, new SongEntryInfo
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
                    cachedSongID = songChartCount,
                    songPath = currentSongPath
                });
                currentSongTitle = string.Empty;
                currentSongArtist = string.Empty;
                currentSongAlbum = string.Empty;
                currentSongYear = 0;
                currentSongLoadingPhrase = string.Empty;
                currentSongAuthor = string.Empty;
                currentSongLength = 0;
                currentSongAccentColor = string.Empty;
                currentSongPreviewStartTime = 0;
                currentSongPath = string.Empty;
                songEntryCount++;
            }
            
            await Task.Yield(); // keep main thread from freezing
        }
    }

    public async Task CacheSingleSong(string folder, int songID, bool songEntryMode)
    {
        Debug.Log("Caching song ID " + songID);
        if (!songEntryMode)
            {
                if (!File.Exists(folder + "/notes.chart")) return;
                await CacheChartFile(await File.ReadAllTextAsync(folder + "/notes.chart"), songID);
                cachedSongs.Add(songID, new SongInfo
                {
                    resolution = currentSongResolution,
                    syncInfos = currentSongSyncTrack,
                    noteInfos = currentSongNotes,
                    globalEvents = currentSongEvents,
                    songLengthInTicks = currentSongLengthInTicks
                });
                
            }
            else if (songEntryMode)
            {
                if (!File.Exists(folder + "/song.ini")) return;
                await CacheIniFile(await File.ReadAllTextAsync(folder + "/song.ini"), songID);
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
                    cachedSongID = songChartCount,
                    songPath = currentSongPath
                });
            }
    }

    private async Task CacheChartFile(string data, int songID)
    {
        Debug.Log("Caching chart file for song ID " + songID);
        string[] lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        // Diagnostic counters
        int parsedNotesCount = 0;
        int failedNoteLines = 0;
        List<string> failedSamples = new List<string>();

        // Determine which Single section to parse: prefer PlayerPrefs-selected difficulty, else Expert, else first Single found.
        string desiredDifficulty = ddst; // PlayerPrefs can only be called on main thread.
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
        NoteSpawner.NoteInfo previousNote = null; // Track the previous note

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
                    currentSongResolution = res;
                }
            }

            if (inSyncTrackSection)
            {
                //Debug.Log("Parsing sync track...");
                string[] parts = trimmedLine.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int time))
                {
                    string[] syncParts = parts[1].Trim().Split(' ');
                    if (syncParts.Length >= 2 && syncParts[0] == "B" && float.TryParse(syncParts[1], out float bpm))
                    {
                        await Task.Run(() => currentSongSyncTrack.Enqueue(new NoteSpawner.SyncInfo
                        {
                            time = time,
                            bpm = bpm / 1000f, // converts from 120000 to 120.000 (example)
                            timeSignature = "4" // Default time signature; "4" = 4/4, "2" = 2/4, "3" = 3/4
                        }));
                    }
                    else if (syncParts.Length >= 2 && syncParts[0] == "TS")
                    {
                        if (currentSongSyncTrack.Count > 0)
                        {
                            currentSongSyncTrack.ElementAt(currentSongSyncTrack.Count - 1).timeSignature = syncParts[1];
                        }
                    }
                }
            }

            if (inEventsSection)
            {
                //Debug.Log("Parsing global events...");
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
                    if (currentSongEvents.ContainsKey(eventTime))
                    {
                        if (!string.IsNullOrEmpty(parsedEventString))
                            currentSongEvents[eventTime] = string.IsNullOrEmpty(currentSongEvents[eventTime]) ? parsedEventString : currentSongEvents[eventTime] + "|" + parsedEventString;
                    }
                    else
                    {
                        currentSongEvents[eventTime] = parsedEventString;
                    }
                }
                
            }

            if (inDesiredSingleSection)
            {
                //Debug.Log("Parsing notes...");
                string[] parts = trimmedLine.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int tickStart) && parts[1].Trim().StartsWith("N"))
                {
                    string[] noteParts = parts[1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (noteParts.Length >= 3 && int.TryParse(noteParts[1], out int fret) && int.TryParse(noteParts[2], out int lengthInTicks))
                    {
                        NoteSpawner.NoteInfo currentNote;

                        // Classify notes based on fret number
                        if (fret == 7)
                        {
                            currentNote = new NoteSpawner.OpenNoteInfo
                            {
                                spawnTime = tickStart,
                                fret = fret,
                                length = 0
                            };
                        }
                        else
                        {
                            currentNote = new NoteSpawner.NoteInfo
                            {
                                spawnTime = tickStart,
                                fret = fret,
                                length = 0
                            };
                        }
                        currentSongNotes.Enqueue(currentNote);
                        previousNote = currentNote; // Update the previous note
                        parsedNotesCount++;
                    }
                    else
                    {
                        failedNoteLines++;
                        if (failedSamples.Count < 20) failedSamples.Add(trimmedLine);
                    }
                }
            }
            if (!inDesiredSingleSection && parsedNotesCount > 0)
            {
                return;
            }
            
            
            await Task.Yield(); // Yield to keep UI responsive during long parsing
            
        }

        // Diagnostics summary
        Debug.Log($"ParseChartFile: chosenSingle={chosenSingleHeader}, parsedNotes={parsedNotesCount}, failedNoteLines={failedNoteLines}, totalNotesArraySize(after)={currentSongNotes.Count}");
        if (failedSamples.Count > 0)
        {
            Debug.LogWarning("ParseChartFile: sample failed lines (up to 20):\n" + string.Join("\n", failedSamples.ToArray()));
        }
        
        // If `songLengthInTicks` isn't provided, compute a sensible value from notes or the sync track.
        // This prevents spawning bars/beats beyond the end of the song.
        if (currentSongLengthInTicks <= 0)
        {
            int maxTick = 0;

            // Use notes (including sustain length) to determine the final tick
            foreach (var n in currentSongNotes)
            {
                int noteStart = (int)n.spawnTime;
                int noteEnd = noteStart + n.length;
                if (noteEnd > maxTick) maxTick = noteEnd;
                if (noteStart > maxTick) maxTick = noteStart;
            }

            // If no notes present, fall back to the last sync point
            if (maxTick == 0 && currentSongSyncTrack.Count > 0)
            {
                maxTick = (int)currentSongSyncTrack.ElementAt(currentSongSyncTrack.Count - 1).time;
            }

            // Ensure we have at least 1 tick to avoid zero-length loops
            currentSongLengthInTicks = Math.Max(1, maxTick);
        }
    }
    public async Task ReadMidiFile(string path)
    {
        var midiFile = MidiFile.Read(path);
        WriteTimedObjects(midiFile.GetTimedEvents());
        await Task.Yield();
    }
    private static void WriteTimedObjects<TObject>(ICollection<TObject> timedObjects)
            where TObject : ITimedObject
    {
        foreach (var timedObject in timedObjects)
        {
            Debug.Log($"[{timedObject.GetType().Name}] {timedObject} (time = {timedObject.Time})");
        }
    } 

    public async Task CacheIniFile(string data, int ID)
    {
        Debug.Log("caching INI file for song ID " + ID);
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

    public IEnumerator PlaySong()
    {
        GameObject venue = GameObject.Find("3DVenue_Camera");
        UIUpdater uiUpdater = FindAnyObjectByType<UIUpdater>();
        GameObject gp = GameObject.Find("GuitarPlayer");
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
            yield return new WaitForSecondsRealtime(1f);
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
        }
        venue.SetActive(false);
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

    public static async Task GetStringFromAddr(string addr)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("CloBeats/0.0.1");
            try
            {
                await client.GetStringAsync(addr);
            }
            catch (Exception ex)
            {
                Debug.LogError("Server error occoured: " + ex.Message);
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
        
    }
    
}
