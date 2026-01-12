using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Globalization;
using System.Drawing;
using System.Threading.Tasks;

public class SongFolderLoader : MonoBehaviour
{
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

    public void Load()
    {
        if (string.IsNullOrEmpty(songFolderPath) || !System.IO.Directory.Exists(songFolderPath))
        {
            Debug.LogError("Invalid song folder path: " + songFolderPath);
            return;
        }
        else
        {
            try
            {
                Debug.Log("Loading song folder: " + songFolderPath);
                SongLoader songLoader = FindFirstObjectByType<SongLoader>();
                if (System.IO.File.Exists(songFolderPath + @"\song.ogg"))
                {
                    songLoader.songAudioClipPath = songFolderPath + @"\song.ogg";
                }
                if (System.IO.File.Exists(songFolderPath + @"\guitar.ogg"))
                {
                    songLoader.guitarAudioClipPath = songFolderPath + @"\guitar.ogg";
                }
                if (System.IO.File.Exists(songFolderPath + @"\notes.chart"))
                {
                    songLoader.chartFilePath = songFolderPath + @"\notes.chart";
                }
                if (System.IO.File.Exists(songFolderPath + @"\venueAnim.json"))
                {
                    VenueAnimationPlayer.Instance.cameraAnimationFile = songFolderPath + @"\venueAnim.json";
                }
                
                if (System.IO.File.Exists(songFolderPath + @"\video.webm"))
                {
                    songLoader.songVideoClipPath = songFolderPath + @"\video.webm";
                    songVideoClipPathSet = true;
                }
                else
                {
                    songLoader.songVideoClipPath = string.Empty;
                    songVideoClipPathSet = false;
                }
                songLoader.SetSongData(songLoader.chartFilePath, songLoader.songAudioClipPath, songLoader.guitarAudioClipPath, songLoader.songVideoClipPath);
                //LoadIniFile(System.IO.File.ReadAllText(songFolderPath + @"\song.ini"));
            }
            catch (Exception ex)
            {
                Debug.LogError("Error loading song folder: " + ex.Message);
                Debug.LogError(ex.StackTrace);
            }
        }
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
                    loadingPhrase = phrase.Trim();
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
