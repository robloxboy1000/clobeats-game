using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System;

public class GeneralSettingsObject : MonoBehaviour
{
    public string songsFolderPath = "";
    public string songDifficultyString = "Expert";
    public string username = "";
    public string serverAddress = "clobeats.pixlplaya5.xyz:8090";

    TMPro.TMP_InputField songsFolderPathInputField;
    TMPro.TMP_Dropdown songDifficultyDropdown;
    TMPro.TMP_InputField usernameInputField;
    TMPro.TMP_InputField serverAddressInputField;
    Slider hpSlider;
    Toggle enableVenueToggle;
    Toggle enableTMToggle;

    Button findServerButton;
    Button saveSettingsButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        songsFolderPath = PlayerPrefs.GetString("SongsFolderPath", string.Empty);
        songDifficultyString = PlayerPrefs.GetString("SelectedDifficulty", "Easy");
        username = PlayerPrefs.GetString("Username", string.Empty);
        serverAddress = PlayerPrefs.GetString("ServerAddress", string.Empty);

        songsFolderPathInputField = transform.Find("SongsFolderPathObject").
        gameObject.transform.Find("SongsPathInputField").
        gameObject.GetComponent<TMPro.TMP_InputField>();

        songDifficultyDropdown = transform.Find("SongDifficultyObject").
        gameObject.transform.Find("DifficultyDropdownOptions").
        gameObject.GetComponent<TMPro.TMP_Dropdown>();

        usernameInputField = transform.Find("UsernameObject").
        gameObject.transform.Find("UsernameInputField").
        gameObject.GetComponent<TMPro.TMP_InputField>();

        serverAddressInputField = transform.Find("ServerAddressObject").
        gameObject.transform.Find("ServerInputField").
        gameObject.GetComponent<TMPro.TMP_InputField>();

        hpSlider = transform.Find("HyperspeedSliderObject").
        gameObject.transform.Find("HyperspeedSlider").
        gameObject.GetComponent<Slider>();

        enableVenueToggle = transform.Find("EnableVenueObject").
        gameObject.transform.Find("VenueToggle").
        gameObject.GetComponent<Toggle>();

        enableTMToggle = transform.Find("EnableTMObject").
        gameObject.transform.Find("TMToggle").
        gameObject.GetComponent<Toggle>();



        findServerButton = transform.Find("FindServerButton").gameObject.GetComponent<Button>();
        saveSettingsButton = transform.Find("SaveSettingsButton").gameObject.GetComponent<Button>();

        

        findServerButton.onClick.AddListener(async () =>
        {
            Debug.Log("Testing server...");
            await TestServerAtAddr(serverAddress);
        });

        saveSettingsButton.onClick.AddListener(() =>
        {
            Debug.Log("Saving general settings...");
            PlayerPrefs.SetString("SongsFolderPath", songsFolderPathInputField.text);
            PlayerPrefs.SetString("SelectedDifficulty", songDifficultyString);
            PlayerPrefs.SetString("Username", usernameInputField.text);
            PlayerPrefs.SetString("ServerAddress", serverAddressInputField.text);
            PlayerPrefs.SetFloat("Hyperspeed", hpSlider.value);
            PlayerPrefs.SetInt("EnableVenue", enableVenueToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("EnableBarBeats", enableTMToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
        });
    }

    void Awake()
    {
        
    }

    public void MainVolumeControl(System.Single vol)
    {

        Debug.Log ( "vol is: " + vol );
    }


    private async Task TestServerAtAddr(string addr)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("request CloBeats/0.0.1");
            try
            {
                var response = await client.GetStringAsync(addr);
                if (response != null)
                {
                    Debug.Log("Recieved string: " + response);
                    MenuManager menuManager = FindAnyObjectByType<MenuManager>();
                    await menuManager.ConnectionSuccessful();
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
        if (serverAddressInputField != null)
        {
            serverAddress = serverAddressInputField.text;
        }
        if (songsFolderPathInputField != null)
        {
            songsFolderPath = songsFolderPathInputField.text;
        }
        if (usernameInputField != null)
        {
            username = usernameInputField.text;
        }
        if (hpSlider != null)
        {
            TMPro.TextMeshProUGUI hpText = transform.Find("HyperspeedSliderObject").
            gameObject.transform.Find("NumericHS").
            gameObject.GetComponent<TMPro.TextMeshProUGUI>();

            hpText.text = hpSlider.value.ToString();
        }
        if (songDifficultyDropdown != null)
        {
            int savedDifficulty = PlayerPrefs.GetString("SelectedDifficulty", "Easy") switch
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
                songDifficultyString = PlayerPrefs.GetString("SelectedDifficulty", "Easy"); 
                PlayerPrefs.SetString("SelectedDifficulty", songDifficultyString);
            }
        }
    }
}
