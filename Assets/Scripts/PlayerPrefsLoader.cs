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

public class PlayerPrefsLoader : MonoBehaviour
{
    private TMPro.TMP_InputField pathInputField;
    private TMPro.TMP_InputField songsFolderPathInputField;
    private Slider speedInputField;
    private Button playButton;
    private Toggle venueToggle;
    private Button clearSettingsButton;
    private TMPro.TMP_Dropdown difficultyDropdown;
    private Button mainMenuButton;
    private Toggle enableBarBeatsToggle;
    private Toggle autoplayToggle;
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
    void Awake()
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
            PlayerPrefs.SetInt("EnableBarBeats", enableBarBeatsToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("EnableAutoplay", autoplayToggle.isOn ? 1 : 0);
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
        mainMenuButton.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("SelectedFolderPath", pathInputField.text);
            PlayerPrefs.SetFloat("Hyperspeed", speedInputField.value);
            PlayerPrefs.SetInt("EnableVenue", venueToggle.isOn ? 1 : 0);
            string selectedDifficulty = difficultyDropdown.options[difficultyDropdown.value].text;
            PlayerPrefs.SetString("SelectedDifficulty", selectedDifficulty);
            PlayerPrefs.SetInt("EnableBarBeats", enableBarBeatsToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("EnableAutoplay", autoplayToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("GraphicsQuality", qualityDropdown.value);
            PlayerPrefs.Save();

            LoadingManager loader = FindFirstObjectByType<LoadingManager>();
            if (loader != null)
            {
                loader.LoadScene("MainMenu");
            }
            else
            {
                Debug.LogError("LoadingManager not found in scene!");
            }
        });

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
            MessageBox.Instance.Show("Please re-map controls before continuing.");
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
    public async void LoadGame()
    {
        if (indefiniteLoadingScreen != null)
        {
            indefiniteLoadingScreen.SetActive(true);
        }
        await LoadWholeGame();
    }
    public async Task LoadWholeGame(float timeout = 600000)
    {
        Debug.Log("Loading game...");

        
        
        if (songItemNames.Count == 0)
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string songFoldersPath = documentsPath + "/CloBeats/songs";
                if (songFoldersPath != string.Empty)
                {
                    if (!File.Exists(songFoldersPath))
                    {
                        Directory.CreateDirectory(songFoldersPath);
                    }
                    string[] directories = Directory.GetDirectories(songFoldersPath);
                    string[] cachedSongsFile = File.ReadAllLines(documentsPath + "/CloBeats/cbfoldercache");
                    if (cachedSongsFile != null)
                    {
                        if (directories == cachedSongsFile)
                        {
                            Debug.Log("Folders are equal to cache.");
                            foreach (string dir in cachedSongsFile)
                            {
                                //Debug.Log("Cached folders added: " + songItemNames.Count);
                                songItemNames.Add(dir);
                                await Task.Yield();
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Folders are not equal to cache. Rebuilding...");
                            using (StreamWriter streamWriter = new StreamWriter(documentsPath + "/CloBeats/cbfoldercache"))
                            {
                                foreach (string line in directories)
                                {
                                    streamWriter.WriteLine(line);
                                }
                                Debug.Log($"Cache rebuilded successfully to {documentsPath + "/CloBeats/cbfoldercache"}");
                            }
                            foreach (string dir in cachedSongsFile)
                            {
                                //Debug.Log("Cached folders added: " + songItemNames.Count);
                                songItemNames.Add(dir);
                                await Task.Yield();
                            }
                        }
                    }
                    GameManager gameManager = FindAnyObjectByType<GameManager>();
                    gameManager.songFolders = songItemNames;
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeout)))
                    {
                        try
                        {
                            await gameManager.CacheSongs(true);
                        }
                        catch (OperationCanceledException)
                        {
                            throw new Exception("Parsing timed out after " + timeout + " milliseconds.");
                        }
                    }
                    
                }
                else
                {
                    Debug.LogError("PlayerPrefs 'SongsFolderPath' is empty");
                }
                
            }
            catch (Exception ex)
            {
                Debug.LogError("Song listing failed: " + ex.Message);
            }
        }
        else
        {
            return;
        }
        

        Shader.WarmupAllShaders();
        ShaderOven shaderOven = FindFirstObjectByType<ShaderOven>();
        shaderOven.shaders.WarmUp();
        if (indefiniteLoadingScreen != null)
        {
            indefiniteLoadingScreen.SetActive(false);
        }
        LoadingManager loader = FindFirstObjectByType<LoadingManager>();
        if (loader != null)
        {
            loader.LoadScene("HS_Screen", LoadSceneMode.Single, true);
            await Task.Delay(6000);
            loader.LoadScene("MainMenu", LoadSceneMode.Single, true);
        }
        else
        {
            Debug.LogError("LoadingManager not found in scene!");
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
    }
}
