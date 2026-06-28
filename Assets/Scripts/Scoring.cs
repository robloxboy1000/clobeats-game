using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Scoring : MonoBehaviour
{
    public string playerTag;
    public int currentSongID;
    public string currentSongPath;
    public float lastNoteSecondsUntil;
    public int lastFret;
    public int currentScore;
    public int currentNotesHit;
    public int currentNotesMissed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetSongID(int id)
    {
        currentSongID = id;
    }

    public void SetPlayerTag(string tag)
    {
        playerTag = tag;
    }

    public void Save(string savePath)
    {
        using (StreamWriter writer = new StreamWriter(savePath + @"\save" + currentSongID + ".txt"))
        {
            writer.WriteLine("[save]");
            writer.WriteLine($"songid = {currentSongID}");
            writer.WriteLine($"songpath = {currentSongPath}");
            writer.WriteLine($"playertag = {playerTag}");
            writer.WriteLine($"score = {currentScore}");
            writer.WriteLine($"nhit = {currentNotesHit}");
            writer.WriteLine($"nmiss = {currentNotesMissed}");
            writer.WriteLine($"lstsecsuntil = {lastNoteSecondsUntil}");
            writer.WriteLine($"lstfret = {lastFret}");
        }
    }
}
