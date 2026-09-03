using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public const string StartMenuId = "start";
    private const string MainMenuId = "main";
    public const string QuickPlayMenuId = "quickplay";
    private const string OptionsMenuId = "options";
    private const string ExitMenuId = "exit";

    public enum MenuAction
    {
        None,
        ShowMenu,
        StartGame,
        PlaySelectedSong,
        Back,
        ConfirmExit,
        QuitGame
    }

    [Serializable]
    public class MenuScreen
    {
        public string id;
        public GameObject panel;
        public bool showLogo = true;
        public Selectable firstSelected;
    }

    [Serializable]
    public class MenuButtonBinding
    {
        public Button button;
        public MenuAction action = MenuAction.ShowMenu;
        public string targetMenuId;
        public int legacySubmitIndex = -1;
    }

    [Header("Scene References")]
    public GameObject logoObject;

    [Header("Flexible Menu Setup")]
    public string firstMenuId = StartMenuId;
    public List<MenuScreen> menuScreens = new List<MenuScreen>();
    public List<MenuButtonBinding> menuButtons = new List<MenuButtonBinding>();
    public bool useDefaultSceneLayout = true;
    public bool selectFirstControlOnOpen = true;

    [Header("Song Selection")]
    public UGUIMenuList songList;
    public Color accentColor = Color.blue;
    public string currentPreviewingSongPath = string.Empty;
    public int currentPreviewingID = 0;

    private readonly Dictionary<string, MenuScreen> screensById = new Dictionary<string, MenuScreen>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, MenuButtonBinding> legacySubmitBindings = new Dictionary<int, MenuButtonBinding>();
    private string currentMenuId = string.Empty;
    private bool isLaunchingSong;

    private void Awake()
    {
        RebuildLookups();
        WireButtons();
        ShowMenu(string.IsNullOrWhiteSpace(firstMenuId) ? StartMenuId : firstMenuId);
    }

    public void OpenMenu(string menuId)
    {
        ShowMenu(menuId);
    }

    public void OpenQuickPlayPanel()
    {
        ShowMenu(QuickPlayMenuId);
    }

    public void OpenOptionsPanel()
    {
        ShowMenu(OptionsMenuId);
    }

    public bool IsMenuOpen(string menuId)
    {
        return string.Equals(currentMenuId, menuId, StringComparison.OrdinalIgnoreCase);
    }

    public void SetCurrentPreview(string songPath, int cachedSongId)
    {
        currentPreviewingSongPath = songPath ?? string.Empty;
        currentPreviewingID = cachedSongId;
    }


    public async void Submit(int menuIndex = 0)
    {
        if (isLaunchingSong)
        {
            return;
        }

        if (menuIndex == 1)
        {
            if (IsMenuOpen(QuickPlayMenuId))
            {
                await PlaySelectedSongAsync();
                return;
            }
            else
            {
                ShowMenu(QuickPlayMenuId);
                return;
            }
        }

        if (menuIndex == 4)
        {
            ShowMenu(OptionsMenuId);
            return;
        }

        if (legacySubmitBindings.TryGetValue(menuIndex, out MenuButtonBinding binding))
        {
            await RunMenuActionAsync(binding.action, binding.targetMenuId);
            return;
        }

        if (IsMenuOpen(ExitMenuId))
        {
            ShowMenu(MainMenuId);
        }
        else if (IsMenuOpen(StartMenuId))
        {
            ShowMenu(MainMenuId);
        }
    }

    public void SubmitCurrentSelection()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
        {
            Submit();
            return;
        }

        Button button = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
        if (button != null && button.IsInteractable())
        {
            button.onClick.Invoke();
            return;
        }

        Submit();
    }

    public void SelectNextControl()
    {
        SelectSiblingControl(1);
    }

    public void SelectPreviousControl()
    {
        SelectSiblingControl(-1);
    }

    public void Exit()
    {
        if (IsMenuOpen(QuickPlayMenuId))
        {
            StopPreviewAudio();
            accentColor = Color.blue;
            ShowMenu(MainMenuId);
        }
        else if (IsMenuOpen(OptionsMenuId))
        {
            ShowMenu(MainMenuId);
        }
        else if (IsMenuOpen(MainMenuId))
        {
            ShowMenu(ExitMenuId);
        }
        else if (IsMenuOpen(ExitMenuId))
        {
            QuitGame();
        }
        else if (IsMenuOpen(StartMenuId))
        {
            ShowMenu(MainMenuId);
        }
        else if (IsMenuOpen("null"))
        {
            if (SceneManager.GetSceneByName("Gameplay").isLoaded) // if null menu was loaded and in gameplay
            {
                return;
            }
            //ShowMenu(MainMenuId);
        }
        else if (IsMenuOpen(currentMenuId))
        {
            ShowMenu(MainMenuId);
        }
    }

    public async Task PlaySelectedSongAsync()
    {
        if (!IsMenuOpen(QuickPlayMenuId))
        {
            return;
        }
        if (isLaunchingSong)
        {
            return;
        }

        if ((string.IsNullOrEmpty(currentPreviewingSongPath) || currentPreviewingID <= 0) && songList != null)
        {
            if (songList.TryGetSelectedSong(out string selectedPath, out int selectedId))
            {
                SetCurrentPreview(selectedPath, selectedId);
            }
        }

        if (string.IsNullOrEmpty(currentPreviewingSongPath) || currentPreviewingID <= 0)
        {
            Debug.LogWarning("No song is selected yet.");
            return;
        }

        isLaunchingSong = true;
        try
        {
            LoadingManager loadingManager = FindAnyObjectByType<LoadingManager>();
            SongFolderLoader songFolderLoader = FindFirstObjectByType<SongFolderLoader>();
            GameManager gameManager = FindAnyObjectByType<GameManager>();

            if (loadingManager == null)
            {
                Debug.LogError("LoadingManager not found in scene.");
                return;
            }

            if (songFolderLoader == null)
            {
                Debug.LogError("SongFolderLoader not found in scene.");
                return;
            }

            if (gameManager == null)
            {
                Debug.LogError("GameManager not found in scene.");
                return;
            }

            PlayerPrefs.SetString("SelectedFolderPath", currentPreviewingSongPath);
            PlayerPrefs.Save();

            gameManager.currentSongID = currentPreviewingID - 1;
            Debug.Log("[MenuManager] Loading selected song. (Include loading visual)");
            StopPreviewAudio();
            ShowMenu("songentry");
            //await gameManager.PlaySongGlobal(currentPreviewingSongPath);
            //ShowMenu("null");
        }
        finally
        {
            isLaunchingSong = false;
        }
    }

    

    private void BuildDefaultConfiguration()
    {
        if (!useDefaultSceneLayout)
        {
            return;
        }

        /*AddScreenIfMissing(StartMenuId, startPanel, true);
        AddScreenIfMissing(MainMenuId, mainMenuPanel, true);
        AddScreenIfMissing(QuickPlayMenuId, quickplayPanel, false);
        AddScreenIfMissing(OptionsMenuId, optionsPanel, false);
        AddScreenIfMissing(ExitMenuId, exitgamePanel, true);

        if (menuButtons.Count == 0)
        {
            AddButtonBinding(FindButton(mainMenuPanel, "quickplay"), MenuAction.ShowMenu, QuickPlayMenuId, 1);
            AddButtonBinding(FindButton(mainMenuPanel, "options"), MenuAction.ShowMenu, OptionsMenuId, 4);
        }*/
    }

    private void AddScreenIfMissing(string id, GameObject panel, bool showLogo)
    {
        if (panel == null || string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        for (int i = 0; i < menuScreens.Count; i++)
        {
            if (string.Equals(menuScreens[i].id, id, StringComparison.OrdinalIgnoreCase))
            {
                if (menuScreens[i].panel == null)
                {
                    menuScreens[i].panel = panel;
                }

                return;
            }
        }

        menuScreens.Add(new MenuScreen
        {
            id = id,
            panel = panel,
            showLogo = showLogo,
            firstSelected = panel.GetComponentInChildren<Selectable>(true)
        });
    }

    private void AddButtonBinding(Button button, MenuAction action, string targetMenuId, int legacySubmitIndex)
    {
        if (button == null)
        {
            return;
        }

        menuButtons.Add(new MenuButtonBinding
        {
            button = button,
            action = action,
            targetMenuId = targetMenuId,
            legacySubmitIndex = legacySubmitIndex
        });
    }

    private void RebuildLookups()
    {
        screensById.Clear();
        legacySubmitBindings.Clear();

        foreach (MenuScreen screen in menuScreens)
        {
            if (screen == null || string.IsNullOrWhiteSpace(screen.id) || screen.panel == null)
            {
                continue;
            }

            screensById[screen.id] = screen;
        }

        foreach (MenuButtonBinding binding in menuButtons)
        {
            if (binding == null || binding.legacySubmitIndex < 0)
            {
                continue;
            }

            legacySubmitBindings[binding.legacySubmitIndex] = binding;
        }
    }

    private void WireButtons()
    {
        foreach (MenuButtonBinding binding in menuButtons)
        {
            if (binding == null || binding.button == null)
            {
                continue;
            }

            MenuButtonBinding capturedBinding = binding;
            binding.button.onClick.AddListener(async () =>
            {
                await RunMenuActionAsync(capturedBinding.action, capturedBinding.targetMenuId);
            });
        }
    }

    private async Task RunMenuActionAsync(MenuAction action, string targetMenuId)
    {
        try
        {
            switch (action)
            {
                case MenuAction.ShowMenu:
                    ShowMenu(targetMenuId);
                    break;
                case MenuAction.StartGame:
                    ShowMenu(MainMenuId);
                    break;
                case MenuAction.PlaySelectedSong:
                    await PlaySelectedSongAsync();
                    break;
                case MenuAction.Back:
                    Exit();
                    break;
                case MenuAction.ConfirmExit:
                    ShowMenu(ExitMenuId);
                    break;
                case MenuAction.QuitGame:
                    QuitGame();
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public async void ShowMenu(string menuId)
    {
        if (string.IsNullOrWhiteSpace(menuId))
        {
            return;
        }

        

        if (screensById.Count == 0)
        {
            RebuildLookups();
        }

        if (!screensById.TryGetValue(menuId, out MenuScreen targetScreen))
        {
            Debug.LogWarning("Menu screen not configured: " + menuId);
            return;
        }

        foreach (MenuScreen screen in screensById.Values)
        {
            if (screen.panel != null)
            {
                screen.panel.SetActive(screen == targetScreen);
            }
        }

        currentMenuId = targetScreen.id;

        if (logoObject != null)
        {
            logoObject.SetActive(targetScreen.showLogo);
        }

        if (selectFirstControlOnOpen && EventSystem.current != null)
        {
            Selectable selected = targetScreen.firstSelected != null
                ? targetScreen.firstSelected
                : targetScreen.panel.GetComponentInChildren<Selectable>(true);

            EventSystem.current.SetSelectedGameObject(selected != null ? selected.gameObject : null);
        }

        if (menuId == QuickPlayMenuId)
        {
            UGUIMenuList menuList = FindAnyObjectByType<UGUIMenuList>();
            await menuList.RebuildAsync();
        }
    }

    public GameObject ReturnMenuGO(string menuId)
    {
        if (string.IsNullOrWhiteSpace(menuId))
        {
            return null;
        }

        if (!screensById.TryGetValue(menuId, out MenuScreen targetScreen))
        {
            Debug.LogWarning("Menu screen not configured: " + menuId);
            return null;
        }

        if (targetScreen != null)
        {
            ShowMenu(menuId);
            return targetScreen.panel;
        }
        else
        {
            return null;
        }
    }

    private void SelectSiblingControl(int direction)
    {
        if (EventSystem.current == null || string.IsNullOrEmpty(currentMenuId))
        {
            return;
        }

        if (!screensById.TryGetValue(currentMenuId, out MenuScreen screen) || screen.panel == null)
        {
            return;
        }

        Selectable[] controls = screen.panel.GetComponentsInChildren<Selectable>(false);
        if (controls == null || controls.Length == 0)
        {
            return;
        }

        GameObject current = EventSystem.current.currentSelectedGameObject;
        int currentIndex = -1;
        for (int i = 0; i < controls.Length; i++)
        {
            if (controls[i] != null && controls[i].gameObject == current)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = currentIndex < 0 ? 0 : WrapIndex(currentIndex + direction, controls.Length);
        EventSystem.current.SetSelectedGameObject(controls[nextIndex].gameObject);
    }

    private void StopPreviewAudio()
    {
        MusicPlayer musicPlayer = FindFirstObjectByType<MusicPlayer>();
        UGUIMenuList menuList = FindAnyObjectByType<UGUIMenuList>();
        if (musicPlayer != null && menuList != null && musicPlayer.previewAudioPlaying)
        {
            StartCoroutine(musicPlayer.StopPreviewAudio(menuList.previewVolume));
        }
    }

    private void QuitGame()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        gameManager.ExitGame(true);
    }

    private static Button FindButton(GameObject root, string objectName)
    {
        GameObject child = root != null ? FindChildGameObject(root.transform, objectName) : null;
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static GameObject FindChildGameObject(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        Transform direct = root.Find(objectName);
        if (direct != null)
        {
            return direct.gameObject;
        }

        foreach (Transform child in root)
        {
            GameObject match = FindChildGameObject(child, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform root, string objectName) where T : Component
    {
        GameObject child = FindChildGameObject(root, objectName);
        return child != null ? child.GetComponent<T>() : null;
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
