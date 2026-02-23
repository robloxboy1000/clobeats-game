using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using System.Linq;

public class UGUIMenuList : MonoBehaviour
{
    // Reference to your list item prefab (must be in Assets)
    public GameObject listItemPrefab;

    
    // Reference to the content container (where items will be instantiated)
    public Transform contentContainer;
    public Transform rootTransform;
    public List<string> itemNames = new List<string>();
    List<GameObject> instantiatedListItems = new List<GameObject>();

    public Button regenerateItemsButton;
    public int itemCount = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    async void OnEnable()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        await ClearItemObjects();
        itemNames.Clear();
        await GenerateList(gameManager.songFolders);
    }

    void Awake()
    {
        if (regenerateItemsButton != null)
        {
            regenerateItemsButton.onClick.AddListener(async () =>
            {
                GameManager gameManager = FindAnyObjectByType<GameManager>();
                await ClearItemObjects();
                itemNames.Clear();
                await GenerateList(gameManager.songFolders);
            });
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
    async Task ClearItemObjects()
    {
        if (instantiatedListItems != null)
        {
            try
            {
                foreach (GameObject obj in instantiatedListItems)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                        instantiatedListItems.Remove(obj);
                        await Task.Yield();
                    }
                }
            }
            catch
            {
                
            }
        }
    }
    async Task GenerateList(List<string> items)
    {
        MenuManager menuManager = FindAnyObjectByType<MenuManager>();
        // Instantiate a new item for each entry in the data list
        foreach (string itemName in items)
        {
            Debug.Log("Item name: " + itemName);
            Debug.Log("Directory name: " + Path.GetFileName(itemName));
            if (Path.GetFileName(itemName).StartsWith("sub_")) continue;
            var songFolderFiles = await Task.Run(() => Directory.GetFiles(itemName));
            List<string> supportedFormats = new List<string> { "wav", "ogg", "mp3" };
            var songMatch = songFolderFiles
                .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                .FirstOrDefault(x => x.name == "song-mixed" && supportedFormats.Contains(x.ext));
            if (songMatch != null)
            if (!File.Exists(songMatch.path)) continue;
            
            // Instantiate the prefab inside the specified container
            GameObject newItem = Instantiate(listItemPrefab, contentContainer);
            instantiatedListItems.Add(newItem);
            itemCount++;
            // Find the text component within the new item and set its value
            // (You might need a dedicated script for complex prefabs)
            GameObject songArtistTextObject = newItem.transform.Find("SongArtistText").gameObject;
            GameObject songTitleTextObject = newItem.transform.Find("SongTitleText").gameObject;

            TMPro.TextMeshProUGUI songArtistText = songArtistTextObject.GetComponent<TMPro.TextMeshProUGUI>();
            TMPro.TextMeshProUGUI songTitleText = songTitleTextObject.GetComponent<TMPro.TextMeshProUGUI>();

            GameManager gameManager = FindAnyObjectByType<GameManager>();

            GameManager.SongEntryInfo songEntry = gameManager.GetCachedSongEntry(itemCount - 1);
            if (songEntry != null)
            {
                if (songArtistText != null)
                {
                    songArtistText.text = "(" + itemCount + ") " + songEntry.songArtist;
                }
                if (songTitleText != null)
                {
                    songTitleText.text = songEntry.songTitle;
                }
            }
            
            
            Button button = newItem.GetComponent<Button>();
            if (button != null)
            {
                button.name = itemCount.ToString();
                button.onClick.AddListener(async () => await OnItemClicked(itemName, int.Parse(button.name)));
            }

            
            if (menuManager != null)
            {
                menuManager.loadingPanel.SetActive(true);
            }
            await Task.Yield();
        }
        
        if (menuManager != null)
        {
            menuManager.loadingPanel.SetActive(false);
            SongFolderLoader songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
            songFolderLoader.ClearValues();
        }
    }

    
    async Task OnItemClicked(string name, int id)
    {
        Debug.Log("Clicked on: " + name);
        GameObject songInfoPanel = rootTransform.Find("SongInfoPanel").gameObject;
        if (songInfoPanel != null)
        {
            GameObject albumImage = songInfoPanel.transform.Find("AlbumImage").gameObject;
            GameObject sipSongTitleTextObject = songInfoPanel.transform.Find("SIPSongTitleText").gameObject;
            GameObject sipSongArtistTextObject = songInfoPanel.transform.Find("SIPSongArtistText").gameObject;
            TMPro.TextMeshProUGUI sipSongArtistText = sipSongArtistTextObject.GetComponent<TMPro.TextMeshProUGUI>();
            TMPro.TextMeshProUGUI sipSongTitleText = sipSongTitleTextObject.GetComponent<TMPro.TextMeshProUGUI>();
            RawImage albumTexture = albumImage.GetComponent<RawImage>();
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            try
            {
                Texture2D loadedTexture = AlbumLoader.LoadImageFromFile(name + @"\album.jpg");
                if (loadedTexture != null)
                {
                    albumTexture.texture = loadedTexture;
                }
                else
                {
                    albumTexture.texture = Resources.Load<Texture>("albumPlaceholder");
                }
                
            }
            catch (Exception ex)
            {
                Debug.LogError("Fallback to placeholder album because: " + ex.Message);
                albumTexture.texture = Resources.Load<Texture>("albumPlaceholder");
            }
            GameManager.SongEntryInfo songEntry = gameManager.GetCachedSongEntry(id - 1);
            if (songEntry != null)
            {
                if (name != songEntry.songPath) return;
                sipSongArtistText.text = songEntry.songArtist;
                sipSongTitleText.text = songEntry.songTitle;

                var songFolderFiles = await Task.Run(() => Directory.GetFiles(songEntry.songPath));
                List<string> supportedFormats = new List<string> { "wav", "ogg", "mp3" };
                var songMatch = songFolderFiles
                    .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                    .FirstOrDefault(x => x.name == "song-mixed" && supportedFormats.Contains(x.ext));

                MusicPlayer musicPlayer = FindFirstObjectByType<MusicPlayer>();
                if (musicPlayer != null)
                {
                    if (musicPlayer.previewAudioPlaying)
                    {
                        musicPlayer.StopPreviewAudio();
                        if (songMatch != null)
                        if (File.Exists(songMatch.path))
                        {
                            await musicPlayer.PlayPreviewAudio(songMatch.path, songEntry.songPreviewStartTime);
                        }
                    }
                    else
                    {
                        if (songMatch != null)
                        if (File.Exists(songMatch.path))
                        {
                            await musicPlayer.PlayPreviewAudio(songMatch.path, songEntry.songPreviewStartTime);
                        }
                    }
                }
            }
            

            MenuManager menuManager = FindAnyObjectByType<MenuManager>();
            if (menuManager != null)
            {
                menuManager.currentPreviewingSongPath = name;
                menuManager.currentPreviewingID = id;
            }
        }
    }
}
