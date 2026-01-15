using UnityEngine;
using System;

public class NoteDeletor : MonoBehaviour
{
    // set in inspector
    public bool firstBarHit = false;
    public bool isMissed = false;
    public bool isDeLagger = false;
    public bool isBot = false;
    private MusicPlayer musicPlayer;
    private UIUpdater uiUpdater;
    private ImprovedStrikeline strikeline;
    private NoteSpawner noteSpawner;
    public bool isPlaying = false;

    GameObject currentlyCollidingObject;
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        musicPlayer = FindAnyObjectByType<MusicPlayer>();
        uiUpdater = FindAnyObjectByType<UIUpdater>();
        strikeline = FindAnyObjectByType<ImprovedStrikeline>();
        noteSpawner = FindAnyObjectByType<NoteSpawner>();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        currentlyCollidingObject = other.gameObject;
        Debug.Log("2d trigger entered");
        if (firstBarHit && other.gameObject.CompareTag("FirstBar"))
        {
            //musicPlayer.NoDelayPlayAudio();
            //isPlaying = true;
        }
        if (!isMissed)
        {
            if (isBot)
            {

            }
            else
            {
                
            }
        }
        else if (isDeLagger)
        {
            if (noteSpawner != null) noteSpawner.ReturnObjectToPool(other.gameObject); else Destroy(other.gameObject);
        }
        else
        {
            //Debug.Log("Note missed!");
            if (noteSpawner != null) noteSpawner.ReturnObjectToPool(other.gameObject); else Destroy(other.gameObject);
            //uiUpdater.DecreaseRockMeter();
            //uiUpdater.ResetCombo();
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("2d trigger exited");
        currentlyCollidingObject = null;
    }
    private void OnTriggerEnter(Collider other)
    {
        currentlyCollidingObject = other.gameObject;
        //Debug.Log("3d trigger entered");
        if (firstBarHit && other.gameObject.CompareTag("FirstBar"))
        {
            //musicPlayer.NoDelayPlayAudio();
            //isPlaying = true;
        }
        if (!isMissed)
        {
            if (isBot)
            {

            }
            else
            {

            }
        }
        else if (isDeLagger)
        {
            if (noteSpawner != null) noteSpawner.ReturnObjectToPool(other.gameObject); else Destroy(other.gameObject);
        }
        else
        {
            //Debug.Log("Note missed!");
            if (noteSpawner != null) noteSpawner.ReturnObjectToPool(other.gameObject); else Destroy(other.gameObject);
            //uiUpdater.DecreaseRockMeter();
            //uiUpdater.ResetCombo();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        //Debug.Log("3d trigger exited");
        currentlyCollidingObject = null;
    }
    // Update is called once per frame
    void Update()
    {
        
        
        
    }
}
