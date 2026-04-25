using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading;
using System.Linq;
using System.ComponentModel;

public class PlayerPrefsLoader : MonoBehaviour
{
    private TMPro.TMP_InputField pathInputField;
    private TMPro.TMP_InputField songsFolderPathInputField;
    private Slider speedInputField;
    private Button playButton;
    private Toggle venueToggle;
    private Button clearSettingsButton;
    private TMPro.TMP_Dropdown difficultyDropdown;
    private TMPro.TMP_Dropdown partDropdown;
    private Button mainMenuButton;
    private Toggle enableBarBeatsToggle;
    private Toggle autoplayToggle;
    private Toggle enableThirtyFPSCapToggle;
    private TMPro.TMP_Dropdown qualityDropdown;
    private TMPro.TextMeshProUGUI resolutionText;
    public bool autoLoad = false;
    public bool serverMode = false;

    public GameObject blankImage;
    public GameObject indefiniteLoadingScreen;
    public List<string> songItemNames = new List<string>();
    public string configFilePath = Application.dataPath + "/config.ini";
    // Start is called before the first frame update
    void Start()
    {
        

    }
    async void Awake()
    {
        
        pathInputField = gameObject.transform.Find("SongFolderPathField").GetComponent<TMPro.TMP_InputField>();
        if (pathInputField != null)
        {
            pathInputField.text = PlayerPrefs.GetString("SelectedFolderPath", string.Empty);
        }
        songsFolderPathInputField = gameObject.transform.Find("SongsFolderPathField").GetComponent<TMPro.TMP_InputField>();
        if (pathInputField != null)
        {
            songsFolderPathInputField.text = PlayerPrefs.GetString("SongsFolderPath", string.Empty);
        }

        speedInputField = gameObject.transform.Find("HyperspeedSlider").GetComponent<Slider>();
        if (speedInputField != null)
        {
            speedInputField.value = PlayerPrefs.GetFloat("Hyperspeed", 5f);
        }

        venueToggle = gameObject.transform.Find("VenueToggle").GetComponent<Toggle>();
        if (venueToggle != null)
        {
            venueToggle.isOn = PlayerPrefs.GetInt("EnableVenue", 1) == 1;
        }

        enableThirtyFPSCapToggle = gameObject.transform.Find("enableThirtyFPSCapToggle").GetComponent<Toggle>();
        if (enableThirtyFPSCapToggle != null)
        {
            enableThirtyFPSCapToggle.isOn = PlayerPrefs.GetInt("ThirtyFPSCap", 0) == 1;
        }

        difficultyDropdown = gameObject.transform.Find("DifficultyDropdown").GetComponent<TMPro.TMP_Dropdown>();
        if (difficultyDropdown != null)
        {
            int savedDifficulty = PlayerPrefs.GetString("SelectedDifficulty", "Easy") switch
            {
                "Easy" => 0,
                "Medium" => 1,
                "Hard" => 2,
                "Expert" => 3,
                _ => 0,
            };
            if (savedDifficulty >= 0 && savedDifficulty < difficultyDropdown.options.Count)
            {
                difficultyDropdown.value = savedDifficulty;
            }
        }
        partDropdown = gameObject.transform.Find("PartDropdown").GetComponent<TMPro.TMP_Dropdown>();
        if (partDropdown != null)
        {
            int savedPart = PlayerPrefs.GetString("SelectedPart", "Guitar") switch
            {
                "Guitar" => 0,
                "Bass" => 1,
                "Drums" => 2,
                "Keys" => 3,
                "Guitar CoOp" => 4,
                _ => 0,
            };
            if (savedPart >= 0 && savedPart < partDropdown.options.Count)
            {
                partDropdown.value = savedPart;
            }
        }

        qualityDropdown = gameObject.transform.Find("QualityDropdown").GetComponent<TMPro.TMP_Dropdown>();
        if (qualityDropdown != null)
        {
            int savedQuality = PlayerPrefs.GetInt("GraphicsQuality", 2);
            if (savedQuality >= 0 && savedQuality < qualityDropdown.options.Count)
            {
                qualityDropdown.value = savedQuality;
                //QualitySettings.SetQualityLevel(savedQuality);
            }
            qualityDropdown.onValueChanged.AddListener((index) =>
            {
                //QualitySettings.SetQualityLevel(index);
            });
        }

        resolutionText = gameObject.transform.Find("ResolutionText").GetComponent<TMPro.TextMeshProUGUI>();
        if (resolutionText != null)
        {
            resolutionText.text = Display.displays[0].renderingWidth + " x " + Display.displays[0].renderingHeight + " @ " + Screen.currentResolution.refreshRateRatio + "Hz";
        }

        enableBarBeatsToggle = gameObject.transform.Find("EnableBarBeatsToggle").GetComponent<Toggle>();
        if (enableBarBeatsToggle != null)
        {
            enableBarBeatsToggle.isOn = PlayerPrefs.GetInt("EnableBarBeats", 1) == 1;
        }
        autoplayToggle = gameObject.transform.Find("AutoplayToggle").GetComponent<Toggle>();
        if (autoplayToggle != null)
        {
            autoplayToggle.isOn = PlayerPrefs.GetInt("EnableAutoplay", 0) == 1;
            LaneInputManager laneInputManager = FindAnyObjectByType<LaneInputManager>();
            laneInputManager.autoPlayEnabled = autoplayToggle.isOn;
            
        }

        clearSettingsButton = gameObject.transform.Find("ClearSettingsButton").GetComponent<Button>();
        if (clearSettingsButton != null)
        {
            clearSettingsButton.onClick.AddListener(() =>
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                pathInputField.text = "";
                speedInputField.value = 5f;
                venueToggle.isOn = true;
                difficultyDropdown.value = 0;
                enableBarBeatsToggle.isOn = true;
                autoplayToggle.isOn = false;
            });
        }

        playButton = gameObject.transform.Find("PlayButton").GetComponent<Button>();
        playButton.onClick.AddListener(async () =>
        {
            PlayerPrefs.SetString("SelectedFolderPath", pathInputField.text);
            PlayerPrefs.SetFloat("Hyperspeed", speedInputField.value);
            PlayerPrefs.SetInt("EnableVenue", venueToggle.isOn ? 1 : 0);
            string selectedDifficulty = difficultyDropdown.options[difficultyDropdown.value].text;
            PlayerPrefs.SetString("SelectedDifficulty", selectedDifficulty);
            string selectedpart = partDropdown.options[partDropdown.value].text;
            PlayerPrefs.SetString("SelectedPart", selectedpart);
            PlayerPrefs.SetInt("EnableBarBeats", enableBarBeatsToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("EnableAutoplay", autoplayToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("ThirtyFPSCap", enableThirtyFPSCapToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("GraphicsQuality", qualityDropdown.value);
            PlayerPrefs.Save();

            SongFolderLoader songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
            if (songFolderLoader != null)
            {
                songFolderLoader.songFolderPath = pathInputField.text;
                await songFolderLoader.Load();
            }
            else
            {
                Debug.LogError("SongFolderLoader not found in scene!");
            }

            GameManager gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.EnableLoadSongVisual(gameManager.unDestructibleLoadingPhraseScreen, Path.Combine(songFolderLoader.songFolderPath, "song.ini"));
            }

            LoadingManager loader = FindFirstObjectByType<LoadingManager>();
            if (loader != null)
            {
                loader.LoadScene("Gameplay");
            }
            else
            {
                Debug.LogError("LoadingManager not found in scene!");
            }
        });
        
        mainMenuButton = gameObject.transform.Find("LoadMainMenuButton").GetComponent<Button>();
        mainMenuButton.onClick.AddListener(async () =>
        {
            PlayerPrefs.SetString("SelectedFolderPath", pathInputField.text);
            PlayerPrefs.SetFloat("Hyperspeed", speedInputField.value);
            PlayerPrefs.SetInt("EnableVenue", venueToggle.isOn ? 1 : 0);
            string selectedDifficulty = difficultyDropdown.options[difficultyDropdown.value].text;
            PlayerPrefs.SetString("SelectedDifficulty", selectedDifficulty);
            string selectedpart = partDropdown.options[partDropdown.value].text;
            PlayerPrefs.SetString("SelectedPart", selectedpart);
            PlayerPrefs.SetInt("EnableBarBeats", enableBarBeatsToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("EnableAutoplay", autoplayToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("ThirtyFPSCap", enableThirtyFPSCapToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("GraphicsQuality", qualityDropdown.value);
            PlayerPrefs.Save();

            LoadingManager loader = FindFirstObjectByType<LoadingManager>();
            if (loader != null)
            {
                if (await LoadGame(false))
                {
                    loader.LoadScene("MainMenu");
                }
                
            }
            else
            {
                Debug.LogError("LoadingManager not found in scene!");
            }
        });

        GameManager gameManager = FindAnyObjectByType<GameManager>();
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        gameManager.savePath = documentsPath + @"\CloBeats\save";
        if (!File.Exists(gameManager.savePath))
        {
            Directory.CreateDirectory(gameManager.savePath);
        }

        if (autoLoad)
        {
            Debug.Log("Auto Load enabled.");
            if (blankImage != null)
            {
                blankImage.SetActive(true);
            }
            if (indefiniteLoadingScreen != null)
            {
                indefiniteLoadingScreen.SetActive(false);
            }
            await LoadGame();
        }
        else
        {
            Debug.Log("Auto Load disabled.");
            if (blankImage != null)
            {
                blankImage.SetActive(false);
            }
            if (indefiniteLoadingScreen != null)
            {
                indefiniteLoadingScreen.SetActive(false);
            }
        }
    }
    public async Task<bool> LoadGame(bool loadMainMenu = true)
    {
        if (indefiniteLoadingScreen != null)
        {
            indefiniteLoadingScreen.SetActive(true);
        }
        if (blankImage != null)
        {
            blankImage.SetActive(true);
        }
        if (await LoadWholeGame(loadMainMenu))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public void RegenerateCacheFile()
    {
        Debug.Log("Regenerating cache");
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string songFoldersPath = documentsPath + @"\CloBeats\songs\local";
        string[] directories = Directory.GetDirectories(songFoldersPath);
        int songCount = 0;
        using (StreamWriter sw = new StreamWriter(documentsPath + @"\CloBeats\cbfoldercache"))
        {
            foreach (string dir in directories)
            {
                sw.WriteLine($"{songCount}={dir.GetHashCode()}={dir.Replace("/", @"\")}");
                songCount++;
            }
        }
        Debug.Log("Restart the game.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        UnityEngine.Application.Quit();
    }
    public void ListDirs()
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string songFoldersPath = documentsPath + @"\CloBeats\songs\local";
        string[] directories = Directory.GetDirectories(songFoldersPath);
        string[] cachedSongsFile = File.ReadAllLines(documentsPath + @"\CloBeats\cbfoldercache");
        foreach (string dir in directories)
        {
            Debug.Log("Directories: " + dir);
        }
        foreach (string dir in cachedSongsFile)
        {
            Debug.Log("Directories (cached): " + dir);
        }
    }
    public async Task<bool> LoadWholeGame(bool loadMainMenu = true)
    {
        Debug.Log("Loading game...");
        ListDirs();
        if (songItemNames.Count == 0)
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string songFoldersPath = documentsPath + @"\CloBeats\songs\local";
                if (songFoldersPath != string.Empty)
                {
                    GameManager gameManager = FindAnyObjectByType<GameManager>();
                    if (!File.Exists(songFoldersPath))
                    {
                        Directory.CreateDirectory(songFoldersPath);
                    }
                    
                    string[] directories = Directory.GetDirectories(songFoldersPath);
                    string[] cachedSongsFile = File.ReadAllLines(documentsPath + @"\CloBeats\cbfoldercache");
                    if (cachedSongsFile != null)
                    {
                        foreach (string line in cachedSongsFile)
                        {
                            string[] hashandpath = line.Split('=', StringSplitOptions.None);
                            int number = int.Parse(hashandpath[0]);
                            int hash = int.Parse(hashandpath[1]);
                            string path = hashandpath[2].Replace("/", @"\");
                            string dir = directories[number];
                            if (path == dir)
                            {
                                Debug.Log(number + " '" + path + "' equals '" + dir + "'");
                                gameManager.songFolders.Add(dir);
                                await gameManager.CacheSingleSong(dir, hash, number);
                                await Task.Yield();
                            }
                            else
                            {
                                throw new Exception($"{number} '{path}' does not equal '{dir}'");
                            }
                            
                        }
                    }
                }
                else
                {
                    
                }
                
            }
            catch (Exception ex)
            {
                Debug.LogError("Song listing failed: " + ex);
                RegenerateCacheFile();
                return false;
            }
        }
        else
        {
            return false;
        }
        

        Shader.WarmupAllShaders();
        ShaderOven shaderOven = FindFirstObjectByType<ShaderOven>();
        shaderOven.shaders.WarmUp();
        if (indefiniteLoadingScreen != null)
        {
            indefiniteLoadingScreen.SetActive(false);
        }
        if (loadMainMenu)
        {
            LoadingManager loader = FindFirstObjectByType<LoadingManager>();
            if (loader != null)
            {
                loader.LoadScene("HS_Screen", LoadSceneMode.Single, true);
                await Task.Delay(6000);
                loader.LoadScene("MainMenu", LoadSceneMode.Single, true);
                return true;
            }
            else
            {
                Debug.LogError("LoadingManager not found in scene!");
                return false;
            }
        }
        else
        {
            return true;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (playButton != null)
        {
            if (!string.IsNullOrEmpty(pathInputField.text) && System.IO.Directory.Exists(pathInputField.text))
                playButton.interactable = true;
            else
            playButton.interactable = false;
        }
        if (pathInputField != null)
        {
            SongFolderLoader songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
            if (songFolderLoader != null)
            {
                if (songFolderLoader.songFolderPath != pathInputField.text)
                {
                    if (System.IO.Directory.Exists(pathInputField.text))
                    {
                        songFolderLoader.songFolderPath = pathInputField.text;
                        //songFolderLoader.Load();
                    }
                }
                
            }
        }
        if (songsFolderPathInputField != null)
        {
            if (System.IO.Directory.Exists(songsFolderPathInputField.text))
            {
                PlayerPrefs.SetString("SongsFolderPath", songsFolderPathInputField.text);
            }
        }

        if (resolutionText != null)
        {
            resolutionText.text = Display.displays[0].renderingWidth + " x " + Display.displays[0].renderingHeight + " @ " + Screen.currentResolution.refreshRateRatio + "Hz";
        }
        if (autoplayToggle != null)
        {
            LaneInputManager laneInputManager = FindAnyObjectByType<LaneInputManager>();
            laneInputManager.autoPlayEnabled = autoplayToggle.isOn;
        }
    }
}
