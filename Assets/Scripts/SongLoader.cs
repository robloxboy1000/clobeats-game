using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class SongLoader : MonoBehaviour
{
    public static SongLoader Instance { get; private set; }

    public string chartFilePath;

    public string songAudioClipPath;

    public string songVideoClipPath;

    public bool songDataSet = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        
    }

    public async Task SetSongData(string chartPath, 
    string audioPath = "",  
    string videoPath = "")
    {
        chartFilePath = chartPath;
        songAudioClipPath = audioPath;
        songVideoClipPath = videoPath;
        songDataSet = true;
        await Task.Yield();
    }


    public async Task LoadSongData(System.Action<string, 
    string, 
    string> onLoaded)
    {
        string textAsset;
        string audioClip;
        string videoClip;

        try
        {
            string path = System.IO.Path.GetFullPath(chartFilePath ?? string.Empty);
            // If chartFilePath is a MIDI, delegate to GameManager MIDI reader
            string ext = string.IsNullOrEmpty(path) ? string.Empty : System.IO.Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            if (ext == "mid" || ext == "midi")
            {
                // Ensure GameManager exists and parse the MIDI into the game's caches
                var gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    await gm.ReadMidiFile(path);
                }
                // leave textAsset empty (system will use GameManager's caches)
                textAsset = string.Empty;
            }
            else
            {
                var gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    await gm.ReadChartFile(path);
                }
                textAsset = string.Empty;
            }
        }
        catch
        {
            textAsset = string.Empty;
        }
        try
        {
            audioClip = System.IO.Path.GetFullPath(songAudioClipPath);
            
        }
        catch
        {
            audioClip = string.Empty;
        }
        try
        {
            videoClip = System.IO.File.Exists(System.IO.Path.GetFullPath(songVideoClipPath)) ? System.IO.Path.GetFullPath(songVideoClipPath) : string.Empty;
        }
        catch
        {
            videoClip = string.Empty;
        }
        onLoaded?.Invoke(textAsset, audioClip, videoClip);
        await Task.CompletedTask;    
    }
}
