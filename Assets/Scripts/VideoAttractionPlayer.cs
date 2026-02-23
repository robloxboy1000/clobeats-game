using UnityEngine;
using UnityEngine.Video;

public class VideoAttractionPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string videoURL;
    bool isInMainMenu = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    void Awake()
    {
        isInMainMenu = false;
        if (videoPlayer != null)
        {
            videoURL = PlayerPrefs.GetString("VideoAttractPath", Application.streamingAssetsPath + "/video_attract.webm");
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

    public void Skip()
    {
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
                        isInMainMenu = true;
                    }
                    else
                    {
                        
                    }
                }
            }
        }
    }
    public void End()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.time = 0;
            LoadingManager loader = FindAnyObjectByType<LoadingManager>();
            if (loader != null)
            {
                if (!isInMainMenu)
                {
                    loader.LoadScene("MainMenu");
                    isInMainMenu = true;
                }
                else
                {
                    
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown)
        {
            Skip();
        }

        if (!videoPlayer.isPlaying && videoPlayer.time == videoPlayer.length)
        {
            End();
        }
    }
}
