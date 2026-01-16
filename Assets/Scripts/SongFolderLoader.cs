using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

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
    public bool songVideoClipPathSet = false;
    public UnityEngine.Color songAccentColor;
    public List<string> supportedFormats = new List<string> { "wav", "ogg", "mp3" };
    public string[] songFolderFiles;

    void Awake()
    {
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
                songFolderFiles = await Task.Run (() => Directory.GetFiles(songFolderPath));
                

                SongLoader songLoader = FindFirstObjectByType<SongLoader>();

                // Find a file named "song" with a supported extension and set the audio path
                var songMatch = songFolderFiles
                    .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                    .FirstOrDefault(x => x.name == "song" && supportedFormats.Contains(x.ext));

                if (songMatch != null)
                {
                    songLoader.songAudioClipPath = songMatch.path;
                }

                // Find a file named "guitar" with a supported extension and set the guitar path
                var guitarMatch = songFolderFiles
                    .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                    .FirstOrDefault(x => x.name == "guitar" && supportedFormats.Contains(x.ext));

                if (guitarMatch != null)
                {
                    songLoader.guitarAudioClipPath = guitarMatch.path;
                }


                if (File.Exists(songFolderPath + @"\notes.chart"))
                {
                    songLoader.chartFilePath = songFolderPath + @"\notes.chart";
                }
                if (File.Exists(songFolderPath + @"\venueAnim.json"))
                {
                    VenueAnimationPlayer.Instance.cameraAnimationFile = songFolderPath + @"\venueAnim.json";
                }
                
                if (File.Exists(songFolderPath + @"\video.webm"))
                {
                    songLoader.songVideoClipPath = songFolderPath + @"\video.webm";
                    songVideoClipPathSet = true;
                }
                else
                {
                    songLoader.songVideoClipPath = string.Empty;
                    songVideoClipPathSet = false;
                }
                await songLoader.SetSongData(songLoader.chartFilePath, songLoader.songAudioClipPath, songLoader.guitarAudioClipPath, songLoader.songVideoClipPath);
                await Task.Yield();
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading song folder: " + ex.Message);
                Debug.LogError(ex.StackTrace);
            }
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
    }
    public async Task LoadIniFile(string data)
    {
        string[] lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        bool inSongSection = false;

        foreach (string line in lines)
        {
            //Debug.Log("INI Line: " + line);
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
                    songName = name.Trim();
                }
                else if (parts.Length == 2 && parts[0].Trim() == "artist" && parts[1].Trim() is string artist)
                {
                    songArtist = artist.Trim();
                }
                else if (parts.Length == 2 && parts[0].Trim() == "album" && parts[1].Trim() is string album)
                {
                    songAlbum = album.Trim();
                }
                else if (parts.Length == 2 && parts[0].Trim() == "year" && int.TryParse(parts[1].Trim(), out int year))
                {
                    songYear = year;
                }
                else if (parts.Length == 2 && parts[0].Trim() == "loading_phrase" && parts[1].Trim() is string phrase)
                {
                    if (phrase == string.Empty || phrase == null)
                    {
                        loadingPhrase = string.Empty;
                    }
                    else
                    {
                        loadingPhrase = phrase.Trim();
                    }
                }
                else if (parts.Length == 2 && parts[0].Trim() == "charter" && parts[1].Trim() is string author)
                {
                    authorName = author.Trim();
                }
                else if (parts.Length == 2 && parts[0].Trim() == "song_length" && int.TryParse(parts[1].Trim(), out int length))
                {
                    NoteSpawner noteSpawner = FindAnyObjectByType<NoteSpawner>();
                    if (noteSpawner != null)
                    noteSpawner.songLengthInTicks = length;
                }
                else if (parts.Length == 2 && parts[0].Trim() == "back_color" && parts[1].Trim() is string hex)
                {
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
                }
                else if (parts.Length == 2 && parts[0].Trim() == "preview_start_time" && int.TryParse(parts[1].Trim(), out int startTime))
                {
                    previewStartTime = startTime;
                }

                UIUpdater uiUpdater = FindAnyObjectByType<UIUpdater>();
                if (uiUpdater != null)
                uiUpdater.UpdateSongInfo(songName, songArtist);
            }
            await Task.Yield();
        }
    }
}
