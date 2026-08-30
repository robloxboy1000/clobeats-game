using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Net.Http;

public class SongFolderLoader : MonoBehaviour
{
    public static SongFolderLoader Instance { get; private set; }
    public string songFolderPath;
    public string songName = "Unset";
    public string songArtist = "Unset";
    public string songAlbum = "Unset";
    public int songYear = 0;
    public string loadingPhrase = "Unset";
    public string authorName = "Unset";
    public int previewStartTime = 0;
    public int songLength = 0;
    public string songAudioClipPath;
    public string chartFilePath;
    public string songVideoClipPath;
    public bool songVideoClipPathSet = false;
    public UnityEngine.Color songAccentColor;
    public List<string> supportedFormats = new List<string> { "wav", "ogg", "mp3", "opus", "mid", "midi" };
    public List<string> supportedVideos = new List<string> { "webm", "mp4", "avi", "ogv", "mpeg" };
    public string[] songFolderFiles;

    void Awake()
    {
        if (Instance == null) Instance = this; else if (Instance != this) Destroy(gameObject); // Instance support
        string savedPath = PlayerPrefs.GetString("SelectedFolderPath", null);
        if (!string.IsNullOrEmpty(savedPath))
        {
            songFolderPath = savedPath;
            //Load();
        }
        else
        {
            Debug.Log("No saved song folder path found, using path set in editor.");
            if (string.IsNullOrEmpty(songFolderPath))
            {
                Debug.LogWarning("Song folder path is not set.");
            }
            else
            {
                //Load();
            }
        }
    }

    

    public async Task Load()
    {
        if (string.IsNullOrEmpty(songFolderPath) || !Directory.Exists(songFolderPath))
        {
            Debug.LogError("Invalid song folder path: " + songFolderPath);
            return;
        }
        else
        {
            try
            {
                Debug.Log("Loading song folder: " + songFolderPath);
                songFolderFiles = await Task.Run(() => Directory.GetFiles(songFolderPath));
                

                SongLoader songLoader = FindFirstObjectByType<SongLoader>();

                // Find a file named "song" with a supported extension and set the audio path
                var songMatch = songFolderFiles
                    .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                    .FirstOrDefault(x => x.name == "song" && supportedFormats.Contains(x.ext));

                if (songMatch != null)
                {
                    songAudioClipPath = songMatch.path;
                }


                List<string> supportedMidis = new List<string> { "mid", "midi" };
                var midiMatch = songFolderFiles
                    .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                    .FirstOrDefault(x => supportedMidis.Contains(x.ext));

                if (File.Exists(songFolderPath + Path.DirectorySeparatorChar + "notes.chart"))
                {
                    chartFilePath = songFolderPath + Path.DirectorySeparatorChar + "notes.chart";
                }
                else if (midiMatch != null)
                {
                    // use MIDI as chart source
                    chartFilePath = midiMatch.path;
                }
                if (File.Exists(songFolderPath + Path.DirectorySeparatorChar + "scripts.json"))
                {
                    VenueAnimationPlayer.Instance.cameraAnimationFile = songFolderPath + Path.DirectorySeparatorChar + "scripts.json";
                }

                var videoMatch = songFolderFiles
                    .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                    .FirstOrDefault(x => x.name == "video" && supportedVideos.Contains(x.ext));
                
                if (videoMatch != null)
                {
                    songVideoClipPath = videoMatch.path;
                    songVideoClipPathSet = true;
                }
                else
                {
                    songVideoClipPath = string.Empty;
                    songVideoClipPathSet = false;
                }
                await songLoader.SetSongData(chartFilePath);
                await Task.Yield();
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading song folder: " + ex.Message);
                Debug.LogError(ex.StackTrace);
            }
        }
    }

    public async Task DownloadFolder(string url, string outputFolder)
    {
        try
        {
            Debug.Log("Download started: " + url);
            await DownloadFileAsync(Path.Combine(url, "song.ogg"), outputFolder);
            await DownloadFileAsync(Path.Combine(url, "notes.mid"), outputFolder);
            await DownloadFileAsync(Path.Combine(url, "album.jpg"), outputFolder);
            await DownloadFileAsync(Path.Combine(url, "song.ini"), outputFolder);
            await DownloadFileAsync(Path.Combine(url, "scripts.json"), outputFolder);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[SongFolderLoader.DownloadFolder] Download failed: " + ex.Message);
        }
    }
    // Reuse HttpClient instance to prevent socket exhaustion
    private static readonly HttpClient client = new HttpClient();
    public async Task<int> DownloadFileAsync(string url, string outputPath)
    {
        // Fetch the file stream from the remote server
        using Stream downloadStream = await client.GetStreamAsync(url);
        using HttpResponseMessage downloadRespCode = await client.GetAsync(url);
        
        // Create a local file stream to write the data
        using FileStream fileStream = new FileStream(outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);
        
        // Stream the data directly to the disk
        if ((int)downloadRespCode.StatusCode == 200)
        {
            await downloadStream.CopyToAsync(fileStream);
            return (int)downloadRespCode.StatusCode;
        }
        else
        {
            return (int)downloadRespCode.StatusCode;
        }
       
    }
    public void ClearValues()
    {
        songFolderPath = string.Empty;
        songName = string.Empty;
        songArtist = string.Empty;
        songAlbum = string.Empty;
        songYear = 0;
        loadingPhrase = string.Empty;
        authorName = string.Empty;
        previewStartTime = 0;
        songLength = 0;
    }
    public async Task LoadIniFile(string filePath)
    {
        INIParser parser = new INIParser();
        parser.Open(filePath);
        bool checkSectionCase = parser.IsSectionExists("song");
        songName = checkSectionCase ? parser.ReadValue("song", "name", string.Empty) : parser.ReadValue("Song", "name", string.Empty);
        songArtist = checkSectionCase ? parser.ReadValue("song", "artist", string.Empty) : parser.ReadValue("Song", "artist", string.Empty);
        songAlbum = checkSectionCase ? parser.ReadValue("song", "album", string.Empty) : parser.ReadValue("Song", "album", string.Empty);
        songYear = checkSectionCase ? parser.ReadValue("song", "year", 0) : parser.ReadValue("Song", "year", 0);
        loadingPhrase = checkSectionCase ? parser.ReadValue("song", "loading_phrase", string.Empty) : parser.ReadValue("Song", "loading_phrase", string.Empty);
        authorName = checkSectionCase ? parser.ReadValue("song", "charter", string.Empty) : parser.ReadValue("Song", "charter", string.Empty);
        songLength = checkSectionCase ? parser.ReadValue("song", "song_length", 0) : parser.ReadValue("Song", "song_length", 0);
        NoteSpawner noteSpawner = FindAnyObjectByType<NoteSpawner>();
        noteSpawner.songLengthInTicks = songLength;
        previewStartTime = checkSectionCase ? parser.ReadValue("song", "preview_start_time", 0) : parser.ReadValue("Song", "preview_start_time", 0);
        string hex = checkSectionCase ? parser.ReadValue("song", "back_color", "#000000") : parser.ReadValue("Song", "back_color", "#000000");
        // Remove the '#' if it exists
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }
        int r = 0, g = 0, b = 0;
        if (hex.Length == 6)
        {
            // Parse the two-character substrings for R, G, and B
            r = int.Parse(hex.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            g = int.Parse(hex.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            b = int.Parse(hex.Substring(4, 2), NumberStyles.AllowHexSpecifier);
        }
        else if (hex.Length == 3)
        {
            // Handle shorthand hex codes (e.g., #F00)
            r = int.Parse(hex[0].ToString() + hex[0].ToString(), NumberStyles.AllowHexSpecifier);
            g = int.Parse(hex[1].ToString() + hex[1].ToString(), NumberStyles.AllowHexSpecifier);
            b = int.Parse(hex[2].ToString() + hex[2].ToString(), NumberStyles.AllowHexSpecifier);
        }
        else
        {
            Debug.LogError("Invalid hex color format. Color: " + hex);
        }
        songAccentColor = new UnityEngine.Color(r, g, b);
        parser.Close();

        UIUpdater uiUpdater = FindAnyObjectByType<UIUpdater>();
        if (uiUpdater != null)
        uiUpdater.UpdateSongInfo(songName, songArtist, songYear);
        await Task.Yield();
    }
}
