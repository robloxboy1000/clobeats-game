using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using System;
using System.IO;

public class VideoAttractionPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string videoURL;
    private IDisposable inputSystemListener;
    bool isInMainMenu = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    void Awake()
    {
        isInMainMenu = false;
        inputSystemListener = InputSystem.onAnyButtonPress.Call(OnButtonPressed);
        if (videoPlayer != null)
        {
            videoURL = PlayerPrefs.GetString("VideoAttractPath", Path.Combine(Application.streamingAssetsPath, "video_attract.webm"));
            videoPlayer.url = videoURL;
            if (!videoPlayer.isPrepared)
            {
                videoPlayer.Prepare();
            }
            else
            {
            }
            if (!videoPlayer.isPlaying)
            {
                videoPlayer.Play();
            }
        }
    }

    void OnDestroy()
    {
        inputSystemListener.Dispose();
    }

    void OnButtonPressed(InputControl button)
    {
        var device = button.device;
        if (device is Keyboard || device is Gamepad)
        {
            Debug.Log(button.name + " Pressed from Video Attraction");
            if (videoPlayer != null)
            {
                if (videoPlayer.isPlaying)
                {
                    videoPlayer.Stop();
                    videoPlayer.time = 0;
                    LoadingManager loader = FindAnyObjectByType<LoadingManager>();
                    if (loader != null)
                    {
                        if (!isInMainMenu)
                        {
                            loader.LoadScene("MainMenu");
                        }
                        else
                        {
                            
                        }
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
