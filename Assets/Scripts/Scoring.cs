using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Scoring : MonoBehaviour
{
    public int playerID;
    public int currentSongID;
    public int currentSongPath;
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

    public void SetPlayerID(int id)
    {
        playerID = id;
    }

    public void Save(string savePath)
    {
        using (StreamWriter writer = new StreamWriter(savePath + @"\save" + currentSongPath.GetHashCode() + ".txt"))
        {
            writer.WriteLine("[save]");
            writer.WriteLine($"songid = {currentSongID}");
            writer.WriteLine($"songpath = {currentSongPath}");
            writer.WriteLine($"playerid = {playerID}");
            writer.WriteLine($"score = {currentScore}");
            writer.WriteLine($"nhit = {currentNotesHit}");
            writer.WriteLine($"nmiss = {currentNotesMissed}");
            writer.WriteLine($"lstsecsuntil = {lastNoteSecondsUntil}");
            writer.WriteLine($"lstfret = {lastFret}");
        }
    }
}
