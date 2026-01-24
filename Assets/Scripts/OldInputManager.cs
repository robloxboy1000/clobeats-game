using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.UI;
using System;
using UnityEngine.InputSystem.LowLevel;
using XInputDotNetPure;
using System.Threading.Tasks;


public class OldInputManager : MonoBehaviour
{
    public List<Key> fretKeys = new List<Key>(5);
    public Key pauseKey = Key.Escape;
    public Key strumUpKey = Key.UpArrow;
    public Key strumDownKey = Key.DownArrow;
    public GameObject pauseMenu;

    private LaneInputManager laneInputManager;

    
    public bool isPaused = false;

    public bool denyInput = false;
    public bool gamePadMode = false;

    public float currentTimeScale = 1;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        GameObject eventSystem = GameObject.Find("EventSystem");
        if (eventSystem != null)
        {
            DontDestroyOnLoad(eventSystem);
        }
        
        SceneManager.sceneLoaded += OnLevelLoaded; 
        pauseMenu = Instantiate(pauseMenu);
        DontDestroyOnLoad(pauseMenu);
        pauseMenu.SetActive(false);

        laneInputManager = FindFirstObjectByType<LaneInputManager>();
    }

    private async Task InputNoteDown(int value)
    {
        if (laneInputManager != null)
        {
            laneInputManager.OnFretPressed(value);
            await Task.Yield();
        }
    }
    private async Task InputNoteHit(int value)
    {
        if (laneInputManager != null)
        {
            laneInputManager.OnFretHit(value);
            await Task.Yield();
        }
    }
    private async Task InputNoteUp(int value)
    {
        if (laneInputManager != null)
        {
            laneInputManager.OnFretReleased(value);
            await Task.Yield();
        }
    }
    private async Task InputStrum()
    {
        if (laneInputManager != null)
        {
            laneInputManager.OnStrum();
            await Task.Yield();
        }
    }

    
    // Update is called once per frame
    async void Update()
    {   
        Time.timeScale = currentTimeScale;
        laneInputManager = FindFirstObjectByType<LaneInputManager>();
        if (laneInputManager == null) return;
        if (Keyboard.current == null) return;
        if (denyInput) return;



        if (Keyboard.current[pauseKey].wasPressedThisFrame)
        {
            Debug.Log("Pause Key Pressed");
            
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (isPaused)
            {
                if (musicPlayer != null)
                {
                    musicPlayer.resumeAudio();
                }
                currentTimeScale = 1.0f; // Resume game
                if (pauseMenu != null)
                {
                    pauseMenu.SetActive(false);
                }
                isPaused = false;
            }
            else
            {
                if (musicPlayer != null)
                {
                    musicPlayer.pauseAudio();
                }
                currentTimeScale = 0.0f; // Pause game
                if (pauseMenu != null)
                {
                    pauseMenu.SetActive(true);
                }
                isPaused = true;
            }
        }
        if (!gamePadMode)
        {
            if (Keyboard.current[fretKeys[0]].wasPressedThisFrame) await InputNoteDown(0);
            else if (Keyboard.current[fretKeys[0]].wasReleasedThisFrame) await InputNoteUp(0);
            else if (Keyboard.current[fretKeys[1]].wasPressedThisFrame) await InputNoteDown(1);
            else if (Keyboard.current[fretKeys[1]].wasReleasedThisFrame) await InputNoteUp(1);
            else if (Keyboard.current[fretKeys[2]].wasPressedThisFrame) await InputNoteDown(2);
            else if (Keyboard.current[fretKeys[2]].wasReleasedThisFrame) await InputNoteUp(2);
            else if (Keyboard.current[fretKeys[3]].wasPressedThisFrame) await InputNoteDown(3);
            else if (Keyboard.current[fretKeys[3]].wasReleasedThisFrame) await InputNoteUp(3);
            else if (Keyboard.current[fretKeys[4]].wasPressedThisFrame) await InputNoteDown(4);
            else if (Keyboard.current[fretKeys[4]].wasReleasedThisFrame) await InputNoteUp(4);

            if (Keyboard.current[strumDownKey].wasPressedThisFrame) await InputStrum();
            else if (Keyboard.current[strumUpKey].wasPressedThisFrame) await InputStrum();
        }
        else if (gamePadMode)
        {
            if (Keyboard.current[fretKeys[0]].wasPressedThisFrame) await InputNoteHit(0);
            else if (Keyboard.current[fretKeys[1]].wasPressedThisFrame) await InputNoteHit(1);
            else if (Keyboard.current[fretKeys[2]].wasPressedThisFrame) await InputNoteHit(2);
            else if (Keyboard.current[fretKeys[3]].wasPressedThisFrame) await InputNoteHit(3);
            else if (Keyboard.current[fretKeys[4]].wasPressedThisFrame) await InputNoteHit(4);
        }

    
    }
    void OnLevelLoaded(Scene scene, LoadSceneMode mode)
    {
        // Optionally handle level load events here
        if (scene.name != "Gameplay")
        {
            denyInput = false;
        }
        else if (scene.name == "Gameplay")
        {
            denyInput = false;
        }
        else
        {
            denyInput = false;
        }
    }
}
