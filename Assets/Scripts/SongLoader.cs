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
            textAsset = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(chartFilePath));
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
