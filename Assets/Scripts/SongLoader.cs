using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class SongLoader : MonoBehaviour
{
    public static SongLoader Instance { get; private set; }

    public string chartFolderPath;

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

    public async Task SetSongData(string chartFolder)
    {
        chartFolderPath = chartFolder;
        songDataSet = true;
        await Task.Yield();
    }


    public async Task LoadSongData()
    {
        try
        {
            string path = System.IO.Path.GetFullPath(chartFolderPath ?? string.Empty);
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
            }
            else
            {
                var gm = FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    await gm.ReadChartFile(path);
                }
            }
        }
        catch
        {

        }
        await Task.Yield();    
    }
}
