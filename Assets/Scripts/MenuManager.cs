using UnityEngine.UI;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Threading.Tasks;

public class MenuManager : MonoBehaviour
{
    public GameObject menuCanvas;
    GameObject startPanel;
    GameObject mainMenuPanel;
    GameObject quickplayPanel;
    GameObject exitgamePanel;
    GameObject logoObject;
    private IDisposable m_EventListener;
    TMPro.TextMeshProUGUI hoverHelpText;
    GameObject optionsPanel;
    GameObject onlineIndicatorPanel;
    string hoverHelpFilePath;
    public Dictionary<string, string> hoverHelpStrings;
    public Dictionary<string, GameObject> menuButtons;
    private GameObject UGUIListHelper;

    Button playSongUIButton;
    Button playSongOnlineUIButton;
    GameObject songInfoPanel;

    public GameObject cbFeedPanel;
    public GameObject loadingPanel;
    public GameObject loadingPreviewImage;

    public Color accentColor;

    public bool isOnline = false;
    public string currentPreviewingSongPath = string.Empty;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    async void Awake()
    {
        m_EventListener = InputSystem.onAnyButtonPress.Call(OnButtonPressed);
        startPanel = menuCanvas.transform.Find("StartPanel").gameObject;
        mainMenuPanel = menuCanvas.transform.Find("MainMenuPanel").gameObject;
        quickplayPanel = menuCanvas.transform.Find("QuickPlayPanel").gameObject;
        songInfoPanel = quickplayPanel.transform.Find("SongInfoPanel").gameObject;
        cbFeedPanel = menuCanvas.transform.Find("CBFeedPanel").gameObject;
        loadingPanel = menuCanvas.transform.Find("LoadingPanel").gameObject;
        playSongUIButton = songInfoPanel.transform.Find("PlaySongButton").gameObject.GetComponent<Button>();
        playSongUIButton.onClick.AddListener(async () =>
        {
            LoadingManager loadingManager = FindAnyObjectByType<LoadingManager>();
            if (loadingManager != null)
            {
                PlayerPrefs.SetString("SelectedFolderPath", currentPreviewingSongPath);
                PlayerPrefs.Save();
                SongFolderLoader songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
                if (songFolderLoader != null)
                {
                    songFolderLoader.songFolderPath = currentPreviewingSongPath;
                    await songFolderLoader.Load();
                }
                else
                {
                    Debug.LogError("SongFolderLoader not found in scene!");
                }
                MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
                if (musicPlayer != null)
                {
                    if (musicPlayer.previewAudioPlaying)
                    {
                        musicPlayer.StopPreviewAudio();
                    }
                }
                loadingManager.LoadScene("Gameplay");
            }
        });
        playSongOnlineUIButton = songInfoPanel.transform.Find("PlaySongOnlineButton").gameObject.GetComponent<Button>();
        loadingPreviewImage = songInfoPanel.transform.Find("AlbumImage").gameObject.transform.Find("LoadingImage").gameObject;
        logoObject = menuCanvas.transform.Find("Logo").gameObject;
        exitgamePanel = menuCanvas.transform.Find("ExitGamePanel").gameObject;
        hoverHelpText = mainMenuPanel.transform.Find("HoverHelpText").GetComponent<TMPro.TextMeshProUGUI>();
        optionsPanel = menuCanvas.transform.Find("OptionsPanel").gameObject;
        onlineIndicatorPanel = menuCanvas.transform.Find("OnlineIndicatorPanel").gameObject;
        UGUIListHelper = FindFirstObjectByType<UGUIMenuList>().gameObject;

        hoverHelpFilePath = Path.Combine(Application.streamingAssetsPath, "HoverHelpData.xml");
        hoverHelpStrings = await ReadXmlToDictionary(hoverHelpFilePath);
        menuButtons = new Dictionary<string, GameObject>();
        foreach (Transform child in mainMenuPanel.transform)
        {
            if (child.gameObject.GetComponent<Button>() != null)
            {
                menuButtons.Add(child.gameObject.name, child.gameObject);
            }
        }
        if (!CheckIfButtonsAreNull())
        {
            menuButtons["quickplay"].GetComponent<Button>().onClick.AddListener( () => {
                Debug.Log("Quick Play button pressed.");
                mainMenuPanel.SetActive(false);
                quickplayPanel.SetActive(true);
                logoObject.SetActive(false);
                
            });
            menuButtons["multiplayer"].GetComponent<Button>().onClick.AddListener(() => {
                Debug.Log("Multiplayer button pressed.");
            });
            menuButtons["onlinemultiplayer"].GetComponent<Button>().onClick.AddListener(() => {
                Debug.Log("Online Multiplayer button pressed.");
            });
            menuButtons["leaderboards"].GetComponent<Button>().onClick.AddListener(() => {
                Debug.Log("Leaderboards button pressed.");
            });
            menuButtons["options"].GetComponent<Button>().onClick.AddListener(() => {
                Debug.Log("Options button pressed.");
                mainMenuPanel.SetActive(false);
                optionsPanel.SetActive(true);
                logoObject.SetActive(false);
            });
        }
        if (mainMenuPanel != null)
        mainMenuPanel.SetActive(false);
        if (quickplayPanel != null)
        quickplayPanel.SetActive(false);
        if (startPanel != null)
        startPanel.SetActive(true);
        if (exitgamePanel != null)
        exitgamePanel.SetActive(false);
        if (optionsPanel != null)
        optionsPanel.SetActive(false);
        if (onlineIndicatorPanel != null)
        onlineIndicatorPanel.SetActive(false);
        if (cbFeedPanel != null)
        cbFeedPanel.SetActive(true);
        if (loadingPanel != null)
        loadingPanel.SetActive(false);
        if (loadingPreviewImage != null)
        loadingPreviewImage.SetActive(false);
    }

    private bool CheckIfButtonsAreNull()
    {
        if (menuButtons["quickplay"] != null)
        {
            return false;
        }
        else if (menuButtons["multiplayer"] != null)
        {
            return false;
        }
        else if (menuButtons["onlinemultiplayer"] != null)
        {
            return false;
        }
        else if (menuButtons["leaderboards"] != null)
        {
            return false;
        }
        else if (menuButtons["options"] != null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (menuButtons == null) return;
        if (playSongOnlineUIButton != null)
        {
            playSongOnlineUIButton.interactable = isOnline;
        }
        
        if (menuButtons.Count == 0)
        {
            
        }
        if (menuButtons.Count > 0)
        {
            if (!CheckIfButtonsAreNull())
            {
                GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
                if (selectedObj != null && menuButtons.ContainsKey(selectedObj.name))
                {
                    HoverEventSender hoverSender = selectedObj.GetComponent<HoverEventSender>();
                    if (hoverSender != null && hoverSender.isHovering)
                    {
                        //Debug.Log("Showing help for " + selectedObj.name);
                        ShowHelpText(selectedObj.name);
                    }
                    else if (hoverSender != null && !hoverSender.isHovering)
                    {
                        hoverHelpText.text = "Hover over an option to see more info.";
                    }
                }
            }
        }
        if (accentColor != null)
        {
            Camera camera = Camera.main;
            camera.backgroundColor = accentColor;
        }
    }

    void OnEnable()
    {
         
    }

    public async Task ConnectionSuccessful()
    {
        string username = FindAnyObjectByType<GeneralSettingsObject>().username;
        string serverAddress = FindAnyObjectByType<GeneralSettingsObject>().serverAddress;
        if (onlineIndicatorPanel != null)
        {
            onlineIndicatorPanel.SetActive(true);
            TMPro.TextMeshProUGUI textObject = onlineIndicatorPanel.transform.Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            textObject.text = $"Connected as {username} to {serverAddress}";
            await Task.Delay(3000);
            onlineIndicatorPanel.SetActive(false);
        }
    }

    void OnButtonPressed(InputControl button)
    {
        var device = button.device;
        if (device is Keyboard || device is Gamepad)
        {
            Debug.Log(button.name + " pressed from MenuManager");
            
            if (button.name == "escape")
            {
                if (quickplayPanel.activeSelf)
                {
                    quickplayPanel.SetActive(false);
                    mainMenuPanel.SetActive(true);
                    logoObject.SetActive(true);
                    accentColor = Color.blue;
                    MusicPlayer musicPlayer = FindFirstObjectByType<MusicPlayer>();
                    if (musicPlayer != null)
                    {
                        if (musicPlayer.previewAudioPlaying)
                        {
                            musicPlayer.StopPreviewAudio();
                        }
                    }
                }
                else if (optionsPanel.activeSelf)
                {
                    optionsPanel.SetActive(false);
                    mainMenuPanel.SetActive(true);
                    logoObject.SetActive(true);
                }
                else if (mainMenuPanel.activeSelf)
                {
                    mainMenuPanel.SetActive(false);
                    exitgamePanel.SetActive(true);
                }
                else if (exitgamePanel.activeSelf)
                {
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    UnityEngine.Application.Quit();
                    #endif
                }
            }
            else
            {
                if (exitgamePanel.activeSelf)
                {
                    exitgamePanel.SetActive(false);
                    mainMenuPanel.SetActive(true);
                }
                else if (startPanel.activeSelf)
                {
                    startPanel.SetActive(false);
                    mainMenuPanel.SetActive(true);
                }
            }
            
        }
    }
    

    void OnDisable()
    {
        if (m_EventListener != null)
        m_EventListener.Dispose();
    }

    public async Task<Dictionary<string, string>> ReadXmlToDictionary(string filePath)
    {
        try
        {
            // Load the XML document from the file path
            XDocument doc = await Task.Run ( () => XDocument.Load(filePath) );

            // Use LINQ to select elements and convert to a dictionary
            Dictionary<string, string> settingsDict = doc.Root
                .Elements("Entry") // Select all 'Setting' elements under the root
                .ToDictionary(
                    el => (string)el.Attribute("key"),   // Key: the value of the 'key' attribute
                    el => (string)el.Attribute("value")  // Value: the value of the 'value' attribute
                );

            return settingsDict;
        }
        catch (System.IO.FileNotFoundException)
        {
            Debug.LogError($"Error: The file '{filePath}' was not found.");
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"An error occurred: {ex.Message}");
            return null;
        }
    }

    public void ShowHelpText(string key)
    {
        if (hoverHelpStrings.ContainsKey(key))
        {
            hoverHelpText.text = hoverHelpStrings[key];
        }
        else
        {
            hoverHelpText.text = "Hover over an option to see more info.";
        }
    }

}