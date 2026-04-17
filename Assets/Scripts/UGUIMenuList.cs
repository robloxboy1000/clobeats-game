using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using System.Linq;
using Rewired.Integration.UnityUI;

public class UGUIMenuList : MonoBehaviour
{
    // Reference to your list item prefab (must be in Assets)
    public GameObject listItemPrefab;

    
    // Reference to the content container (where items will be instantiated)
    public Transform contentContainer;
    public Transform rootTransform;
    public List<string> itemNames = new List<string>();
    List<GameObject> instantiatedListItems = new List<GameObject>();

    public class ListObject
    {
        public string songPath;
        public int songID;
    }

    Dictionary<int, ListObject> itemPaths = new Dictionary<int, ListObject>();

    public Button regenerateItemsButton;

    public int currentSelectedItem = 0;

    public int OICCount = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    async void OnEnable()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        await ClearItemObjects();
        itemNames.Clear();
        // cachedEntries is a Dictionary<int, SongEntryInfo> — iterate values to list entries
        foreach (var entry in gameManager.cachedEntries.OrderBy(k => k.Value.songNumber))
        {
            await GenerateList(entry.Value);
        }
        SongFolderLoader songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
        songFolderLoader.ClearValues();
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
                foreach (var entry in gameManager.cachedEntries.OrderBy(k => k.Value.songNumber))
                {
                    await GenerateList(entry.Value);
                }
                SongFolderLoader songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
                songFolderLoader.ClearValues();
            });
        }
    }


    // Update is called once per frame
    async void Update()
    {
        GameObject currentHoveringButton = EventSystem.current.currentSelectedGameObject;
        if (currentHoveringButton != null)
        {
            var hover = currentHoveringButton.gameObject.GetComponent<HoverEventSender>();
            if (hover != null)
            {
                if (hover.isHovering)
                {
                    currentSelectedItem = int.Parse(currentHoveringButton.name);
                    if (currentSelectedItem != int.Parse(currentHoveringButton.name))
                    {
                        OICCount = 0;
                    }
                    await OnItemClicked(itemPaths[currentSelectedItem].songPath, itemPaths[currentSelectedItem].songID, itemPaths[currentSelectedItem].songID);
                    OICCount++;
                }
            }
        }
        
    }
    async Task ClearItemObjects()
    {
        if (instantiatedListItems != null)
        {
            try
            {
                // Iterate over a copy to allow safe removal
                foreach (GameObject obj in instantiatedListItems.ToList())
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                    await Task.Yield();
                }
                instantiatedListItems.Clear();
            }
            catch
            {
                
            }
        }
    }
    async Task GenerateList(GameManager.SongEntryInfo item)
    {
        MenuManager menuManager = FindAnyObjectByType<MenuManager>();
        // Instantiate a new item for each entry in the data list
        if (Path.GetFileName(item.songPath).StartsWith("sub_")) return;
        var songFolderFiles = await Task.Run(() => Directory.GetFiles(item.songPath));
        List<string> supportedFormats = new List<string> { "wav", "ogg", "mp3" };
        var songMatch = songFolderFiles
            .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
            .FirstOrDefault(x => x.name == "song" && supportedFormats.Contains(x.ext));
        if (songMatch != null)
        if (!File.Exists(songMatch.path)) return;
            
        // Instantiate the prefab inside the specified container
        GameObject newItem = Instantiate(listItemPrefab, contentContainer);
        instantiatedListItems.Add(newItem);
            
        // Find the text component within the new item and set its value
        // (You might need a dedicated script for complex prefabs)
        GameObject songArtistTextObject = newItem.transform.Find("Marqee1").Find("SongArtistText").gameObject;
        GameObject songTitleTextObject = newItem.transform.Find("Maquee2").Find("SongTitleText").gameObject;
        GameObject songNumberTextObject = newItem.transform.Find("SongNumberText").gameObject;

        TMPro.TextMeshProUGUI songArtistText = songArtistTextObject.GetComponent<TMPro.TextMeshProUGUI>();
        TMPro.TextMeshProUGUI songTitleText = songTitleTextObject.GetComponent<TMPro.TextMeshProUGUI>();
        TMPro.TextMeshProUGUI songNumberText = songNumberTextObject.GetComponent<TMPro.TextMeshProUGUI>();

        GameManager gameManager = FindAnyObjectByType<GameManager>();

        GameManager.SongEntryInfo songEntry = gameManager.GetCachedSongEntry(item.cachedSongID);
        if (songEntry != null)
        {
            if (songNumberText != null)
            {
                songNumberText.text = "(" + songEntry.songNumber + ")";
            }
            if (songArtistText != null)
            {
                var stm = newItem.transform.Find("Marqee1").GetComponent<ScrollingTextManager>();
                songArtistText.text = songEntry.songArtist;
                stm.SetText(songEntry.songArtist);
            }
            if (songTitleText != null)
            {
                var stm = newItem.transform.Find("Maquee2").GetComponent<ScrollingTextManager>();
                songTitleText.text = songEntry.songTitle;
                stm.SetText(songEntry.songTitle);
            }
        }
  
        Selectable button = newItem.GetComponent<Selectable>();
        if (button != null)
        {
            button.name = songEntry.songNumber.ToString();
            itemPaths.Add(songEntry.songNumber, new ListObject
            {
                songPath = songEntry.songPath,
                songID = songEntry.cachedSongID
            });
        } 
        await Task.Yield();
    }

    
    async Task OnItemClicked(string name, int id, int lastID)
    {
        if (OICCount != 0) return;
        Debug.Log($"Clicked on: {name}, {id}, {lastID}");
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
                string[] detectedImages = Directory.GetFiles(name);
                List<string> supportedImages = new List<string> { "jpg","jpeg","png","dds" };
                var imageMatch = detectedImages
                    .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                    .FirstOrDefault(x => supportedImages.Contains(x.ext));
                if (imageMatch != null)
                {
                    Texture2D loadedTexture = AlbumLoader.LoadImageFromFile(imageMatch.path);
                    if (loadedTexture != null)
                    {
                        albumTexture.texture = loadedTexture;
                    }
                    else
                    {
                        albumTexture.texture = Resources.Load<Texture>("newAlbumPlaceholder");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Fallback to placeholder album because: " + ex.Message);
                albumTexture.texture = Resources.Load<Texture>("newAlbumPlaceholder");
            }
            GameManager.SongEntryInfo songEntry = gameManager.GetCachedSongEntry(id);
            if (songEntry != null)
            {
                if (name != songEntry.songPath) return;
                sipSongArtistText.text = songEntry.songArtist;
                sipSongTitleText.text = songEntry.songTitle;

                var songFolderFiles = await Task.Run(() => Directory.GetFiles(songEntry.songPath));
                List<string> supportedFormats = new List<string> { "wav", "ogg", "mp3" };
                var songMatch = songFolderFiles
                    .Select(f => new { path = f, name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant(), ext = Path.GetExtension(f).TrimStart('.').ToLowerInvariant() })
                    .FirstOrDefault(x => x.name == "song" && supportedFormats.Contains(x.ext));

                MusicPlayer musicPlayer = FindFirstObjectByType<MusicPlayer>();
                if (musicPlayer != null)
                {
                    if (musicPlayer.previewAudioPlaying)
                    {
                        musicPlayer.StopPreviewAudio();
                        await Task.Delay(1000);
                        if (songMatch != null)
                        if (File.Exists(songMatch.path))
                        {
                            StartCoroutine(musicPlayer.PlayPreviewAudio(songMatch.path, songEntry.songPreviewStartTime));
                        }
                    }
                    else
                    {
                        await Task.Delay(1000);
                        if (songMatch != null)
                        if (File.Exists(songMatch.path))
                        {
                            StartCoroutine(musicPlayer.PlayPreviewAudio(songMatch.path, songEntry.songPreviewStartTime));
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
