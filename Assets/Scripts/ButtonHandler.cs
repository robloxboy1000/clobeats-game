using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Windows.Forms;
using UnityEngine.SceneManagement;
using System.IO;
using SimpleFileBrowser;

public class ButtonHandler : MonoBehaviour
{
    public UnityEngine.UI.Button myButton; // Assign this in the Inspector

        void Start()
        {
            
            if (myButton != null)
            {
                // Add a listener to the button's onClick event
                myButton.onClick.AddListener(OnButtonClick);
            }
        }
    void OnButtonClick()
    {
        Debug.Log("Button clicked: " + myButton.name);
        if (myButton.name == "SelectFolderButton")
        {
            FileBrowser.ShowLoadDialog( ( paths ) =>
            {
                Debug.Log( "Selected: " + paths[0] );
                TMPro.TMP_InputField inputField = FindAnyObjectByType<TMPro.TMP_InputField>();
                if (inputField != null)
                {
                    inputField.text = paths[0];
                }
            },
			() => { 
                Debug.Log( "Canceled" ); 
            },
				FileBrowser.PickMode.Folders, false, null, null, "Select Folder", "Select" );

        }
        else if (myButton.name == "LoadGameplaySceneButton")
        {
            TMPro.TMP_InputField inputField = FindAnyObjectByType<TMPro.TMP_InputField>();
            if (inputField != null)
            {
                string folderPath = inputField.text;
                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    PlayerPrefs.SetString("SelectedFolderPath", folderPath);
                    SceneManager.LoadSceneAsync(1);
                }

            }
        }
        else if (myButton.name == "ExitButton")
        {
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ResetAllValues();
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            UnityEngine.Application.Quit();
        }
        else if (myButton.name == "DismissButton")
        {
            GameObject exceptionPanel = GameObject.Find("ErrorMessage(Clone)");
            if (exceptionPanel != null)
            {
                Destroy(exceptionPanel);
            }
        }
        else if (myButton.name == "ResumeButton")
        {
            OldInputManager inputManager = FindAnyObjectByType<OldInputManager>();
            GameObject pauseMenu = inputManager.pauseMenu;
            if (pauseMenu != null)
            {
                pauseMenu.SetActive(false);
                Time.timeScale = 1f; // Resume the game
                MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
                musicPlayer.resumeAudio();
                inputManager.isPaused = false;
            }
        }
        else if (myButton.name == "RestartButton")
        {
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ResetAllValues();
            }
            Time.timeScale = 1f; // Ensure time scale is reset
            LoadingManager loader = FindAnyObjectByType<LoadingManager>();
            if (loader != null)
            {
                loader.LoadScene("Gameplay");
            }
        }
        else if (myButton.name == "ExitToPreloaderButton")
        {
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ResetAllValues();
            }
            Time.timeScale = 1f; // Ensure time scale is reset
            LoadingManager loader = FindAnyObjectByType<LoadingManager>();
            if (loader != null)
            {
                loader.LoadScene("MainMenu");
            }
        }
        else if (myButton.name == "ExitToDesktopButton")
        {
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.ResetAllValues();
            }
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (musicPlayer != null)
            {
                musicPlayer.stopAudio();
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            UnityEngine.Application.Quit();
        }
        else if (myButton.name == "HSContinueButton")
        {
            LoadingManager loader = FindAnyObjectByType<LoadingManager>();
            if (loader != null)
            {
                loader.LoadScene("VideoAttractionScene", LoadSceneMode.Single, false);
            }
        }
        else if (myButton.name == "SuppExitButton")
        {
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (musicPlayer != null)
            {
                musicPlayer.stopAudio();
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            UnityEngine.Application.Quit();
        }
    }
}
