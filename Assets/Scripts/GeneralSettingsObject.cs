using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class GeneralSettingsObject : MonoBehaviour
{
    public string songsFolderPath = "";
    public string songDifficultyString = "Expert";
    public string username = "";
    public string serverAddress = "clobeats.pixlplaya5.xyz:8090";

    //TMPro.TMP_InputField songsFolderPathInputField;
    TMPro.TMP_Dropdown songDifficultyDropdown;
    //TMPro.TMP_InputField usernameInputField;
    //TMPro.TMP_InputField serverAddressInputField;
    Slider fpsSlider;
    Slider hpSlider;
    Toggle enableVenueToggle;
    Toggle enableTMToggle;

    //TMPro.TMP_InputField resInput;
    TMPro.TMP_Dropdown resDropdown;
    Button saveSettingsButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //songsFolderPath = PlayerPrefs.GetString("SongsFolderPath", string.Empty);
        songDifficultyString = PlayerPrefs.GetString("SelectedDifficulty");
        //username = PlayerPrefs.GetString("Username", string.Empty);
        //serverAddress = PlayerPrefs.GetString("ServerAddress", string.Empty);

        songDifficultyDropdown = transform.Find("SongDifficultyObject/DiffDropdown").
        gameObject.GetComponent<TMPro.TMP_Dropdown>();

        hpSlider = transform.Find("HyperspeedSliderObject/HyperspeedSlider").
        gameObject.GetComponent<Slider>();

        enableVenueToggle = transform.Find("EnableVenueObject/VenueToggle").
        gameObject.GetComponent<Toggle>();

        enableTMToggle = transform.Find("EnableTMObject/TMToggle").
        gameObject.GetComponent<Toggle>();

        fpsSlider = transform.Find("FramerateSliderObject/FPSSlider").
        gameObject.GetComponent<Slider>();

        resDropdown = transform.Find("ResolutionDropdownObject/ResDropdownOptions").
        gameObject.GetComponent<TMPro.TMP_Dropdown>();
        List<Resolution> options = new List<Resolution>();
        List<string> options1 = new List<string>();
        if (resDropdown != null)
        {
            foreach (var res in Screen.resolutions)
            {
                options.Add(res);
                options1.Add(res.width + "x" + res.height + "@" + res.refreshRateRatio);
            }
            
            resDropdown.AddOptions(options1);
            resDropdown.onValueChanged.AddListener((int value) =>
            {
                Screen.SetResolution(options[value].width, options[value].height, FullScreenMode.FullScreenWindow, options[value].refreshRateRatio);
            });
        }

        saveSettingsButton = GameObject.Find("SaveSettingsButton").gameObject.GetComponent<Button>(); // outside local transform

        saveSettingsButton.onClick.AddListener(() =>
        {
            Debug.Log("Saving general settings...");
            //PlayerPrefs.SetString("SongsFolderPath", songsFolderPathInputField.text);
            PlayerPrefs.SetString("SelectedDifficulty", songDifficultyString);
            //PlayerPrefs.SetString("Username", usernameInputField.text);
            //PlayerPrefs.SetString("ServerAddress", serverAddressInputField.text);
            PlayerPrefs.SetFloat("Hyperspeed", hpSlider.value);
            PlayerPrefs.SetInt("EnableVenue", enableVenueToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("EnableBarBeats", enableTMToggle.isOn ? 1 : 0);

            PlayerPrefs.SetInt("Framerate", (int)fpsSlider.value);
            PlayerPrefs.SetString("Resolution", resDropdown.options[resDropdown.value].text);
            PlayerPrefs.Save();

            Application.targetFrameRate = (int)fpsSlider.value;

            
        });
    }
    void OnEnable()
    {
        
    }

    void Awake()
    {
        
    }

    public void MainVolumeControl(float vol)
    {

        Debug.Log ( "vol is: " + vol );
    }


    private async Task TestServerAtAddr(string addr)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("CloBeats/0.0.1");
            try
            {
                var response = await client.GetStringAsync(addr);
                if (response != null)
                {
                    Debug.Log("Recieved string: " + response);
                    MenuManager menuManager = FindAnyObjectByType<MenuManager>();
                    //await menuManager.ConnectionSuccessful();
                }
                
            }
            catch (Exception ex)
            {
                Debug.LogError("Server error occoured: " + ex.Message);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (hpSlider != null)
        {
            TMPro.TextMeshProUGUI hpText = transform.Find("HyperspeedSliderObject").
            gameObject.transform.Find("NumericHS").
            gameObject.GetComponent<TMPro.TextMeshProUGUI>();

            hpText.text = hpSlider.value.ToString();
        }
        if (fpsSlider != null)
        {
            TMPro.TextMeshProUGUI fpsText = transform.Find("FramerateSliderObject").
            gameObject.transform.Find("NumericFPS").
            gameObject.GetComponent<TMPro.TextMeshProUGUI>();

            fpsText.text = fpsSlider.value.ToString();
            UnityEngine.Application.targetFrameRate = (int)fpsSlider.value;
        }
        if (songDifficultyDropdown != null)
        {
            songDifficultyString = PlayerPrefs.GetString("SelectedDifficulty");
            int savedDifficulty = songDifficultyString switch
            {
                "Easy" => 0,
                "Medium" => 1,
                "Hard" => 2,
                "Expert" => 3,
                _ => 0,
            };
            if (savedDifficulty >= 0 && savedDifficulty < songDifficultyDropdown.options.Count)
            {
                songDifficultyDropdown.value = savedDifficulty;
            }
            songDifficultyDropdown.onValueChanged.AddListener(SDD_OnValueChanged);
        }
    }
    void SDD_OnValueChanged(int value)
    {
        string savedDifficulty = value switch
        {
            0 => "Easy",
            1 => "Medium",
            2 => "Hard",
            3 => "Expert",
            _ => string.Empty,
        };
        PlayerPrefs.SetString("SelectedDifficulty", savedDifficulty);
        PlayerPrefs.Save();
    }
}
