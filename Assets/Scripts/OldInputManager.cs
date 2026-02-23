using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Rewired;
using UnityEngine.UI;


public class OldInputManager : MonoBehaviour
{
    public int rewiredPlayerId = 1;
    private Player player;
    public GameObject pauseMenu;
    private LaneInputManager laneInputManager;
    public bool isPaused = false;
    public bool denyInput = false;
    public float currentTimeScale = 1;
    public float whammyAmount = -1;
    public float tiltAmount = -1;
    public bool inHSScreen = false;
    public bool inMainMenu = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        rewiredPlayerId = ReInput.players.GetPlayerId("Player0");
        player = ReInput.players.GetPlayer(rewiredPlayerId);
        DontDestroyOnLoad(this.gameObject);
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
        if (player == null)
        {
            player = ReInput.players.GetPlayer(rewiredPlayerId);
        }
        Time.timeScale = currentTimeScale;
        if (denyInput) return;
        await GetInput();
        laneInputManager = FindFirstObjectByType<LaneInputManager>();
        if (laneInputManager == null) return;
    }
    private async Task GetInput()
    {
        if (inHSScreen)
        {
            
        }
        else if (inMainMenu)
        {
            Debug.Log("In MainMenu");
            MenuManager menuManager = FindAnyObjectByType<MenuManager>();
            if (menuManager != null)
            {
                if (player.GetButtonDown("Green"))
                {
                    if (menuManager.quickplayPanel.activeSelf)
                    {
                        menuManager.Submit(1);
                    }
                    else
                    {
                        menuManager.Submit(0);
                    }
                }
                if (player.GetButtonDown("Red"))
                {
                    menuManager.Exit();
                }
            }
        }
        else
        {
            if (player.GetButtonDown("Green")) await InputNoteDown(0);
            if (player.GetButtonUp("Green")) await InputNoteUp(0);
            if (player.GetButtonDown("Red")) await InputNoteDown(1);
            if (player.GetButtonUp("Red")) await InputNoteUp(1);
            if (player.GetButtonDown("Yellow")) await InputNoteDown(2);
            if (player.GetButtonUp("Yellow")) await InputNoteUp(2);
            if (player.GetButtonDown("Blue")) await InputNoteDown(3);
            if (player.GetButtonUp("Blue")) await InputNoteUp(3);
            if (player.GetButtonDown("Orange")) await InputNoteDown(4);
            if (player.GetButtonUp("Orange")) await InputNoteUp(4);

            if (player.GetButtonDown("StrumUp")) await InputStrum();
            if (player.GetButtonDown("StrumDown")) await InputStrum();

            if (player.GetButtonUp("Start")) PauseGame();
            if (player.GetButtonUp("Select")) ReleaseSP();

            whammyAmount = player.GetAxis("Whammy");
            tiltAmount = player.GetAxis("Tilt");
        }
        
    }
    public void ReleaseSP()
    {
        Debug.Log("add special phrase release code here");
    }
    public void PauseGame()
    {
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
    void OnLevelLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene loaded: " + scene.name);
        if (scene.name == "Gameplay")
        {
            denyInput = false;
            inHSScreen = false;
            inMainMenu = false;
        }
        else if (scene.name == "HS_Screen")
        {
            inHSScreen = true;
            inMainMenu = false;
        }
        else if (scene.name == "MainMenu")
        {
            inMainMenu = true;
            denyInput = false;
            inHSScreen = false;
        }
        else
        {
            denyInput = false;
            inHSScreen = false;
            inMainMenu = false;
        }
    }
}
