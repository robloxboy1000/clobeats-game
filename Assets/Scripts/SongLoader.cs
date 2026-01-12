using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class SongLoader : MonoBehaviour
{
    public static SongLoader Instance { get; private set; }

    public string chartFilePath;
    public string songAudioClipPath;
    public string guitarAudioClipPath;
    public string songVideoClipPath;

    public bool songDataSet = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (chartFilePath != "" && songAudioClipPath != "" && guitarAudioClipPath != "" && songVideoClipPath != "")
        {
            SetSongData($@"{chartFilePath}", $@"{songAudioClipPath}", $@"{guitarAudioClipPath}", $@"{songVideoClipPath}");
        }
    }
    void Start()
    {
        
    }

    public void SetSongData(string chartPath, string audioPath, string guitarPath, string videoPath)
    {
        chartFilePath = chartPath;
        songAudioClipPath = audioPath;
        guitarAudioClipPath = guitarPath;
        songVideoClipPath = videoPath;
        songDataSet = true;
    }

    public async Task LoadSongData(System.Action<string, string, string, string> onLoaded)
    {
        string textAsset;
        string audioClip;
        string guitarClip;
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
            guitarClip = System.IO.Path.GetFullPath(guitarAudioClipPath);
        }
        catch
        {
            guitarClip = string.Empty;
        }
        try
        {
            videoClip = System.IO.File.Exists(System.IO.Path.GetFullPath(songVideoClipPath)) ? System.IO.Path.GetFullPath(songVideoClipPath) : string.Empty;
        }
        catch
        {
            videoClip = string.Empty;
        }
        onLoaded?.Invoke(textAsset, audioClip, guitarClip, videoClip);
        await Task.CompletedTask;    
    }
}
