using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UGUIMenuList : MonoBehaviour
{
    private static readonly string[] SupportedAudioFormats = { ".wav", ".ogg", ".mp3", ".opus" };
    private static readonly string[] SupportedImageFormats = { ".jpg", ".jpeg", ".png", ".dds", ".webp" };

    [Header("List")]
    public GameObject listItemPrefab;
    public Transform contentContainer;
    public Transform rootTransform;
    public ScrollRect scrollRect;
    //public Button regenerateItemsButton;
    public bool rebuildOnEnable = true;
    public bool wrapSelection = true;
    public bool autoScrollToSelection = true;
    public float scrollPadding = 8f;
    public bool clickPlaysSelection = false;

    [Header("Preview")]
    public MenuManager menuManager;
    public RawImage albumImage;
    public TextMeshProUGUI songTitleText;
    public TextMeshProUGUI songArtistText;
    public Texture placeholderAlbumTexture;
    public bool playPreviewAudio = true;
    [Range(0f,1f)]
    public float previewVolume = 1.0f;

    [Header("Selection Style")]
    public Color selectedItemColor = new Color(0.18f, 0.52f, 1f, 1f);
    public Color selectedTextColor = Color.white;

    public List<string> itemNames = new List<string>();

    public class ListObject
    {
        public string songPath;
        public int songID;
    }

    private class SongMenuItem
    {
        public GameManager.SongEntryInfo entry;
        public GameObject view;
        public Button button;
        public Graphic background;
        public TextMeshProUGUI titleText;
        public Color normalBackgroundColor;
        public Color normalTextColor;
    }

    private List<SongMenuItem> items = new List<SongMenuItem>();
    private List<GameObject> instantiatedListItems = new List<GameObject>();
    private Dictionary<int, SongMenuItem> itemsBySongNumber = new Dictionary<int, SongMenuItem>();
    private Dictionary<int, SongMenuItem> itemsByCachedId = new Dictionary<int, SongMenuItem>();
    private Dictionary<int, ListObject> itemPaths = new Dictionary<int, ListObject>();

    private int selectedIndex = -1;
    private int rebuildVersion;

    public int currentSelectedItem
    {
        get => selectedIndex;
        set => SelectAt(value);
    }

    private void Awake()
    {
        ResolveReferences();
    }

    async void OnEnable()
    {
        if (rebuildOnEnable)
        {
            await RebuildAsync();
        }
    }

    void OnDisable()
    {
        rebuildVersion++;
    }

    public async Task RebuildAsync()
    {
        int version = ++rebuildVersion;
        ResolveReferences();
        await ClearItemObjects();

        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("Cannot build song list because GameManager was not found.");
            return;
        }

        foreach (GameManager.SongEntryInfo entry in gameManager.cachedEntries) // attempting dont sort entries
        {
            if (version != rebuildVersion)
            {
                Debug.LogWarning("Rebuild version is invalid.");
                return;
            }

            await AddEntryAsync(entry);
        }

        SongFolderLoader songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
        if (songFolderLoader != null)
        {
            songFolderLoader.ClearValues();
        }

        if (version == rebuildVersion && items.Count > 0)
        {
            SelectAt(0, true);
        }
    }

    public void SelectNext()
    {
        SelectAt(selectedIndex + 1);
    }

    public void SelectPrevious()
    {
        SelectAt(selectedIndex - 1);
    }

    public bool SelectBySongNumber(int songNumber)
    {
        if (!itemsBySongNumber.TryGetValue(songNumber, out SongMenuItem item))
        {
            return false;
        }

        SelectItem(item, true);
        return true;
    }

    public bool SelectByCachedSongId(int cachedSongId)
    {
        if (!itemsByCachedId.TryGetValue(cachedSongId, out SongMenuItem item))
        {
            return false;
        }

        SelectItem(item, true);
        return true;
    }

    public void SelectAt(int index)
    {
        SelectAt(index, true);
    }

    public void ConfirmSelection()
    {
        if (menuManager != null)
        {
            menuManager.Submit(1);
        }
    }

    public bool TryGetSelectedSong(out string songPath, out int cachedSongId)
    {
        songPath = string.Empty;
        cachedSongId = 0;

        if (selectedIndex < 0 || selectedIndex >= items.Count)
        {
            return false;
        }

        GameManager.SongEntryInfo entry = items[selectedIndex].entry;
        if (entry == null)
        {
            return false;
        }

        songPath = entry.songPath;
        cachedSongId = entry.cachedSongID;
        return !string.IsNullOrEmpty(songPath) && cachedSongId > 0;
    }

    public void OnSelectedChanged()
    {
        SelectAt(currentSelectedItem);
    }

    public void CheckSelectedEntry(int id)
    {
        if (SelectBySongNumber(id))
        {
            return;
        }

        if (SelectByCachedSongId(id))
        {
            return;
        }

        SelectAt(id);
    }

    private void SelectAt(int index, bool updatePreview)
    {
        if (items.Count == 0)
        {
            selectedIndex = -1;
            UpdateSelectionVisuals();
            return;
        }

        int resolvedIndex = wrapSelection
            ? WrapIndex(index, items.Count)
            : Mathf.Clamp(index, 0, items.Count - 1);

        SelectItem(items[resolvedIndex], updatePreview);
    }

    private void SelectItem(SongMenuItem item, bool updatePreview, bool forcePreview = false)
    {
        if (item == null)
        {
            return;
        }

        int index = items.IndexOf(item);
        if (index < 0)
        {
            return;
        }

        bool changed = selectedIndex != index;
        selectedIndex = index;
        UpdateSelectionVisuals();
        FocusSelectedItem(item);
        ScrollToSelectedItem();

        if (updatePreview && (changed || forcePreview))
        {
            PreviewSelectedItem(item);
        }
    }

    private async Task ClearItemObjects()
    {
        foreach (GameObject item in instantiatedListItems.ToList())
        {
            if (item != null)
            {
                Destroy(item);
            }

            await Task.Yield();
        }

        instantiatedListItems.Clear();
        itemNames.Clear();
        items.Clear();
        itemsBySongNumber.Clear();
        itemsByCachedId.Clear();
        itemPaths.Clear();
        selectedIndex = -1;
    }

    private async Task AddEntryAsync(GameManager.SongEntryInfo entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.songPath))
        {
            Debug.LogWarning("Song entry is null or path is missing.");
            return;
        }

        if (!await HasPlayableAudioAsync(entry.songPath))
        {
            Debug.LogWarning("Song entry audio is missing.");
            return;
        }

        if (listItemPrefab == null || contentContainer == null)
        {
            Debug.LogWarning("Song list is missing a list item prefab or content container.");
            return;
        }

        GameObject view = Instantiate(listItemPrefab, contentContainer);
        view.name = entry.songNumber.ToString();
        instantiatedListItems.Add(view);

        SongMenuItem item = new SongMenuItem
        {
            entry = entry,
            view = view,
            button = view.GetComponent<Button>(),
            background = view.GetComponent<Graphic>(),
            titleText = FindTitleText(view.transform)
        };

        if (item.button == null)
        {
            item.button = view.AddComponent<Button>();
        }

        if (item.background == null)
        {
            item.background = item.button.targetGraphic;
        }

        if (item.titleText != null)
        {
            item.titleText.text = string.IsNullOrWhiteSpace(entry.songTitle) ? Path.GetFileName(entry.songPath) : entry.songTitle;
            item.normalTextColor = item.titleText.color;
        }

        if (item.background != null)
        {
            item.normalBackgroundColor = item.background.color;
        }

        item.button.onClick.AddListener(() =>
        {
            SelectItem(item, true, true);
            if (clickPlaysSelection)
            {
                ConfirmSelection();
            }
        });

        AddSelectionTriggers(view, item);

        items.Add(item);
        itemNames.Add(item.titleText != null ? item.titleText.text : view.name);
        itemsBySongNumber[entry.songNumber] = item;
        itemsByCachedId[entry.cachedSongID] = item;
        itemPaths[entry.songNumber] = new ListObject
        {
            songPath = entry.songPath,
            songID = entry.cachedSongID
        };
        Debug.Log("Added entry: " + entry.songTitle);
        await Task.Yield();
    }

    private void PreviewSelectedItem(SongMenuItem item)
    {
        if (item == null || item.entry == null)
        {
            return;
        }

        GameManager.SongEntryInfo entry = item.entry;
        if (songTitleText != null)
        {
            songTitleText.text = string.IsNullOrWhiteSpace(entry.songTitle) ? "Unknown Title" : entry.songTitle;
        }

        if (songArtistText != null)
        {
            songArtistText.text = string.IsNullOrWhiteSpace(entry.songArtist) ? "Unknown Artist" : entry.songArtist;
        }

        if (albumImage != null)
        {
            albumImage.texture = LoadAlbumTexture(entry.songPath);
        }

        if (menuManager != null)
        {
            menuManager.SetCurrentPreview(entry.songPath, entry.cachedSongID);
        }

        if (playPreviewAudio)
        {
            PlayPreviewAudio(entry);
        }
    }

    private void PlayPreviewAudio(GameManager.SongEntryInfo entry)
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        MusicPlayer musicPlayer = FindFirstObjectByType<MusicPlayer>();

        if (gameManager == null || musicPlayer == null)
        {
            return;
        }

        if (!gameManager.cachedAudioClips.TryGetValue(entry.cachedSongID, out AudioClip clip))
        {
            return;
        }

        if (musicPlayer.previewAudioPlaying)
        {
            musicPlayer.StopPreviewAudio();
        }

        StartCoroutine(musicPlayer.PlayPooledPreviewAudio(clip, entry.songPreviewStartTime, previewVolume));
    }

    private Texture LoadAlbumTexture(string songPath)
    {
        Texture fallback = placeholderAlbumTexture != null
            ? placeholderAlbumTexture
            : Resources.Load<Texture>("newAlbumPlaceholder");

        try
        {
            string imagePath = Directory.GetFiles(songPath)
                .FirstOrDefault(path => SupportedImageFormats.Contains(Path.GetExtension(path).ToLowerInvariant()));

            if (string.IsNullOrEmpty(imagePath))
            {
                return fallback;
            }

            Texture2D loadedTexture = AlbumLoader.LoadImageFromFile(imagePath);
            return loadedTexture != null ? loadedTexture : fallback;
        }
        catch (Exception ex)
        {
            Debug.LogError("Fallback to placeholder album because: " + ex.Message);
            return fallback;
        }
    }

    private async Task<bool> HasPlayableAudioAsync(string songPath)
    {
        try
        {
            string[] files = await Task.Run(() => Directory.GetFiles(songPath));
            return files.Any(path =>
                string.Equals(Path.GetFileNameWithoutExtension(path), "song", StringComparison.OrdinalIgnoreCase)
                && SupportedAudioFormats.Contains(Path.GetExtension(path).ToLowerInvariant()));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Skipping song folder because it could not be scanned: " + ex.Message);
            return false;
        }
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < items.Count; i++)
        {
            SongMenuItem item = items[i];
            bool selected = i == selectedIndex;

            if (item.background != null)
            {
                item.background.color = selected ? selectedItemColor : item.normalBackgroundColor;
            }

            if (item.titleText != null)
            {
                item.titleText.color = selected ? selectedTextColor : item.normalTextColor;
            }
        }
    }

    private void FocusSelectedItem(SongMenuItem item)
    {
        if (item == null || item.view == null || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(item.view);
    }

    private void ScrollToSelectedItem()
    {
        if (!autoScrollToSelection || scrollRect == null || items.Count <= 1 || selectedIndex < 0)
        {
            return;
        }

        SongMenuItem selectedItem = items[selectedIndex];
        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport != null
            ? scrollRect.viewport
            : scrollRect.GetComponent<RectTransform>();
        RectTransform selectedRect = selectedItem.view != null
            ? selectedItem.view.transform as RectTransform
            : null;

        if (content == null || viewport == null || selectedRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        Bounds viewportBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, viewport);
        Bounds selectedBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, selectedRect);
        float scrollableHeight = content.rect.height - viewportBounds.size.y;

        if (scrollableHeight <= 0f)
        {
            return;
        }

        float paddedViewportMin = viewportBounds.min.y + scrollPadding;
        float paddedViewportMax = viewportBounds.max.y - scrollPadding;
        float targetNormalizedPosition = scrollRect.verticalNormalizedPosition;

        if (selectedBounds.min.y < paddedViewportMin)
        {
            float targetViewportMin = selectedBounds.min.y - scrollPadding;
            targetNormalizedPosition = (targetViewportMin - content.rect.yMin) / scrollableHeight;
        }
        else if (selectedBounds.max.y > paddedViewportMax)
        {
            float targetViewportMin = selectedBounds.max.y + scrollPadding - viewportBounds.size.y;
            targetNormalizedPosition = (targetViewportMin - content.rect.yMin) / scrollableHeight;
        }
        else
        {
            return;
        }

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(targetNormalizedPosition);
    }

    private void AddSelectionTriggers(GameObject view, SongMenuItem item)
    {
        EventTrigger trigger = view.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = view.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener(_ => SelectItem(item, true));
        trigger.triggers.Add(pointerEnter);

        EventTrigger.Entry select = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        select.callback.AddListener(_ => SelectItem(item, true));
        trigger.triggers.Add(select);
    }

    private void ResolveReferences()
    {
        if (menuManager == null)
        {
            menuManager = FindAnyObjectByType<MenuManager>();
        }

        if (scrollRect == null && contentContainer != null)
        {
            scrollRect = contentContainer.GetComponentInParent<ScrollRect>();
        }

        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>(true);
        }

        if (contentContainer == null && scrollRect != null)
        {
            contentContainer = scrollRect.content;
        }

        /*if (rootTransform == null && menuManager != null && menuManager.ReturnMenuGO(MenuManager.QuickPlayMenuId) != null)
        {
            rootTransform = menuManager.ReturnMenuGO(MenuManager.QuickPlayMenuId).transform;
        }*/

        Transform songInfoPanel = rootTransform != null ? FindDeepChild(rootTransform, "SongInfoPanel") : null;
        if (songInfoPanel != null)
        {
            if (albumImage == null)
            {
                Transform album = FindDeepChild(songInfoPanel, "AlbumImage");
                albumImage = album != null ? album.GetComponent<RawImage>() : null;
            }

            if (songTitleText == null)
            {
                Transform title = FindDeepChild(songInfoPanel, "SIPSongTitleText");
                songTitleText = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            }

            if (songArtistText == null)
            {
                Transform artist = FindDeepChild(songInfoPanel, "SIPSongArtistText");
                songArtistText = artist != null ? artist.GetComponent<TextMeshProUGUI>() : null;
            }
        }
    }

    private static TextMeshProUGUI FindTitleText(Transform root)
    {
        Transform marquee = root.Find("Marqee1/SongtitleText");
        if (marquee != null)
        {
            return marquee.GetComponent<TextMeshProUGUI>();
        }

        Transform namedText = FindDeepChild(root, "SongtitleText");
        if (namedText != null)
        {
            return namedText.GetComponent<TextMeshProUGUI>();
        }

        return root.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform match = FindDeepChild(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static int WrapIndex(int index, int count)
    {
        if (count <= 0)
        {
            return -1;
        }

        int wrapped = index % count;
        return wrapped < 0 ? wrapped + count : wrapped;
    }
}
