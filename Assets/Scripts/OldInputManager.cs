using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Rewired;


public class OldInputManager : MonoBehaviour
{
    public enum InputUpdateType
    {
        PerFrame,
        Fixed,
        Late
    }

    public InputUpdateType updateType = InputUpdateType.PerFrame;
    public int rewiredPlayerId = 1;
    private Player player;
    public GameObject pauseMenu;
    private LaneInputManager laneInputManager;
    public bool isPaused = false;
    public bool denyInput = false;
    public bool gamepadMode = false;
    public float currentTimeScale = 1;
    public float whammyAmount = -1;
    public float tiltAmount = -1;
    public float scrollAxis = 0f;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        rewiredPlayerId = ReInput.players.GetPlayerId("Player0");
        player = ReInput.players.GetPlayer(rewiredPlayerId);
        DontDestroyOnLoad(this.gameObject);
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
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager)
            if (!laneInputManager.OnStrum() && gameManager.inSong)
            {
                var sfx = FindAnyObjectByType<SFXPlayer>();
                sfx.PlayClip("Miss");
                var ui = FindAnyObjectByType<UIUpdater>();
                ui.UpdateForNoteMiss();
            }
            await Task.Yield();
        }
    }

    
    // Update is called once per frame
    async void Update()
    {   
        laneInputManager = FindFirstObjectByType<LaneInputManager>();
        if (laneInputManager == null) return;
        if (player == null)
        {
            player = ReInput.players.GetPlayer(rewiredPlayerId);
        }
        Time.timeScale = currentTimeScale;
        if (denyInput) return;
        if (updateType == InputUpdateType.PerFrame)
        {
            await GetInput();
        }
    }

    async void FixedUpdate()
    {
        if (updateType == InputUpdateType.Fixed)
        {
            await GetInput();
        }
    }
    async void LateUpdate()
    {
        if (updateType == InputUpdateType.Late)
        {
            await GetInput();
        }
    }
    public void TriggerVibration(float leftMotor, float rightMotor, float duration)
    {   
        // Pass motor index, motor level (0.0 to 1.0), and duration in seconds
        // Motor 0 is typically the Left/Low-Frequency motor
        player.SetVibration(0, leftMotor, duration);
        
        // Motor 1 is typically the Right/High-Frequency motor
        player.SetVibration(1, rightMotor, duration);
    }
    public void MobileVibrate()
    {
        //Handheld.Vibrate(); // untested, may vibrate continuously
        Solo.MOST_IN_ONE.MOST_HapticFeedback.Generate(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.MediumImpact);
    }
    private async Task GetInput()
    {
        scrollAxis = player.GetAxis("ScrollWheel");
        whammyAmount = player.GetAxis("Whammy");
        tiltAmount = player.GetAxis("Tilt");
        if (SceneManager.GetSceneByName("HS_Screen").isLoaded)
        {
            return;
        }
        if (SceneManager.GetSceneByName("MainMenu").isLoaded && !SceneManager.GetSceneByName("Gameplay").isLoaded)
        {
            //Debug.Log("In MainMenu");
            MenuManager menuManager = FindAnyObjectByType<MenuManager>();
            if (menuManager != null)
            {
                
                if (player.GetButtonDown("Green"))
                {
                    Debug.Log("Green button pressed");
                    SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
                    sFXPlayer.PlayClip("MenuOpenLong");
                    if (menuManager.IsMenuOpen(MenuManager.StartMenuId))
                    {
                        menuManager.Submit(2);
                    }
                    else if (menuManager.IsMenuOpen(MenuManager.QuickPlayMenuId))
                    {
                        menuManager.Submit(1);
                    }
                    else
                    {
                        menuManager.SubmitCurrentSelection();
                    }
                    
                }
                if (player.GetButtonDown("Red"))
                {
                    Debug.Log("Red button pressed");
                    menuManager.Exit();
                }
                if (menuManager.IsMenuOpen(MenuManager.QuickPlayMenuId))
                {
                    if (player.GetButtonDown("StrumDown") || scrollAxis == 1)
                    {
                        UGUIMenuList menuList = FindAnyObjectByType<UGUIMenuList>();
                        if (menuList != null) menuList.SelectNext();
                        SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
                        sFXPlayer.PlayClip("MenuOpen");
                    }
                    else if (player.GetButtonDown("StrumUp") || scrollAxis == -1)
                    {
                        UGUIMenuList menuList = FindAnyObjectByType<UGUIMenuList>();
                        if (menuList != null) menuList.SelectPrevious();
                        SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
                        sFXPlayer.PlayClip("MenuOpen");
                    }
                }
                else
                {
                    if (player.GetButtonDown("StrumDown"))
                    {
                        menuManager.SelectNextControl();
                        SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
                        sFXPlayer.PlayClip("MenuOpen");
                    }
                    else if (player.GetButtonDown("StrumUp"))
                    {
                        menuManager.SelectPreviousControl();
                        SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
                        sFXPlayer.PlayClip("MenuOpen");
                    }
                }
                
            }
        }
        else if (SceneManager.GetSceneByName("Gameplay").isLoaded)
        {
            if (!gamepadMode)
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

                
                if (tiltAmount == 1)
                {
                    ReleaseSP();
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
                if (player.GetButtonUp("Start")) PauseGame();
                if (player.GetButtonUp("Select")) ReleaseSP();
            }
            
        }
        
    }
    public void ReleaseSP()
    {
        // automatically handled by just releasing whenever star power hits 100% just like in regular roBeats
        if (SceneManager.GetSceneByBuildIndex(1).isLoaded)
        {
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            StarMeter spmeter = FindAnyObjectByType<StarMeter>();
            UIUpdater uIUpdater = FindAnyObjectByType<UIUpdater>();
            if (spmeter != null && uIUpdater != null)
            {
                if (spmeter.value >= 50 && !uIUpdater.inStar)
                {
                    uIUpdater.StarPowerToggle(gameManager.currentPart.ToLower(), true);
                }
                else
                {
                    return;
                }
            }
        }
    }
    public void PauseGame()
    {
        if (SceneManager.GetSceneByBuildIndex(1).isLoaded)
        {
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (isPaused)
            {
                if (musicPlayer != null)
                {
                    //musicPlayer.resumeAudio();
                    musicPlayer.PlayAllStems();
                }
                //currentTimeScale = 1.0f; // Resume game
                MenuManager menu = FindAnyObjectByType<MenuManager>();
                if (menu != null)
                {
                    menu.ShowMenu("null");
                }
                ImprovedStrikeline strikeline = FindAnyObjectByType<ImprovedStrikeline>();
                if (strikeline != null)
                {
                    strikeline.ResetAnims();
                }
                VenueAnimationPlayer venue = FindAnyObjectByType<VenueAnimationPlayer>();
                if (venue)
                {
                    venue.TryToggleCamera(true);
                }
                isPaused = false;
            }
            else
            {
                if (musicPlayer != null)
                {
                    //musicPlayer.pauseAudio();
                    musicPlayer.PauseAllStems();
                }
                //currentTimeScale = 1.0f; // Pause game
                MenuManager menu = FindAnyObjectByType<MenuManager>();
                if (menu != null)
                {
                    menu.ShowMenu("pause");
                }
                ImprovedStrikeline strikeline = FindAnyObjectByType<ImprovedStrikeline>();
                if (strikeline != null)
                {
                    strikeline.ResetAnims();
                }
                VenueAnimationPlayer venue = FindAnyObjectByType<VenueAnimationPlayer>();
                if (venue)
                {
                    venue.TryToggleCamera(false);
                }
                isPaused = true;
            }
        }
        
    }
    
}
