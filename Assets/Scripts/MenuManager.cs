using UnityEngine.UI;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Threading.Tasks;

public class MenuManager : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject startPanel;
    public GameObject mainMenuPanel;
    public GameObject quickplayPanel;
    public GameObject exitgamePanel;
    public GameObject logoObject;
    public TMPro.TextMeshProUGUI hoverHelpText;
    public GameObject optionsPanel;
    public GameObject onlineIndicatorPanel;
    string hoverHelpFilePath;
    public Dictionary<string, string> hoverHelpStrings;
    public Dictionary<string, GameObject> menuButtons;
    private GameObject UGUIListHelper;

    public Button playSongUIButton;
    public Button playSongOnlineUIButton;
    public GameObject songInfoPanel;

    public GameObject cbFeedPanel;
    public GameObject loadingPanel;
    public GameObject loadingPreviewImage;

    public Color accentColor;

    public bool isOnline = false;
    public string currentPreviewingSongPath = string.Empty;
    public int currentPreviewingID = 0;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    void Awake()
    {
        startPanel = menuCanvas.transform.Find("StartPanel").gameObject;
        mainMenuPanel = menuCanvas.transform.Find("MainMenuPanel").gameObject;
        quickplayPanel = menuCanvas.transform.Find("QuickPlayPanel").gameObject;
        songInfoPanel = quickplayPanel.transform.Find("SongInfoPanel").gameObject;
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
                GameManager gameManager = FindAnyObjectByType<GameManager>();
                if (songFolderLoader != null)
                {
                    gameManager.currentSongID = currentPreviewingID - 1;
                    songFolderLoader.songFolderPath = currentPreviewingSongPath;
                    await songFolderLoader.Load();
                }
                else
                {
                    Debug.LogError("SongFolderLoader not found in scene!");
                }
                if (gameManager != null)
                {
                    gameManager.EnableLoadSongVisual(gameManager.unDestructibleLoadingPhraseScreen, Path.Combine(songFolderLoader.songFolderPath, "song.ini"));
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
        optionsPanel = menuCanvas.transform.Find("OptionsPanel").gameObject;
        onlineIndicatorPanel = menuCanvas.transform.Find("OnlineIndicatorPanel").gameObject;
        UGUIListHelper = FindFirstObjectByType<UGUIMenuList>().gameObject;

        menuButtons = new Dictionary<string, GameObject>();
        foreach (Transform child in mainMenuPanel.transform)
        {
            if (child.gameObject.GetComponent<Selectable>() != null)
            {
                menuButtons.Add(child.gameObject.name, child.gameObject);
            }
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

    private void OpenQPPanel()
    {
        mainMenuPanel.SetActive(false);
        quickplayPanel.SetActive(true);
        logoObject.SetActive(false);
    }

    private void OpenOptionsPanel()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
        logoObject.SetActive(false);
    }

    private bool CheckIfButtonsAreNull()
    {
        if (menuButtons["quickplay"] != null)
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
                    Debug.Log("Currnetly selected: " + selectedObj.name);
                }
            }
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

    public async void Submit(int menuIndex = 0)
    {
        if (menuIndex == 1)
        {
            if (playSongUIButton.interactable)
            {
                LoadingManager loadingManager = FindAnyObjectByType<LoadingManager>();
                if (loadingManager != null)
                {
                    PlayerPrefs.SetString("SelectedFolderPath", currentPreviewingSongPath);
                    PlayerPrefs.Save();
                    SongFolderLoader songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
                    GameManager gameManager = FindAnyObjectByType<GameManager>();
                    if (songFolderLoader != null)
                    {
                        gameManager.currentSongID = currentPreviewingID - 1;
                        songFolderLoader.songFolderPath = currentPreviewingSongPath;
                        await songFolderLoader.Load();
                    }
                    else
                    {
                        Debug.LogError("SongFolderLoader not found in scene!");
                    }
                    
                    if (gameManager != null)
                    {
                        gameManager.EnableLoadSongVisual(gameManager.unDestructibleLoadingPhraseScreen, Path.Combine(songFolderLoader.songFolderPath, "song.ini"));
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
    public void Exit()
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

    public void ScrollSongListDown()
    {
        UGUIMenuList menuList = FindAnyObjectByType<UGUIMenuList>();
        {
            
        }
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