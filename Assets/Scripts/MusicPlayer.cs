using UnityEngine;
using UnityEngine.Analytics;
using System;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.Video;

using System.Collections.Generic;
using MoonscraperEngine.Audio;
using System.Threading.Tasks;

public class MusicPlayer : MonoBehaviour
{

    public VideoPlayer videoPlayer;
    public AudioSource songAudioSource;
    public AudioSource guitarAudioSource;
    public AudioSource previewAudioSource;
    public AudioSource sfxAudioSource;

    [Tooltip("Enable this to use Unity Audio instead of BASS, which BASS has a bug that i cannot fix.")]
    public bool useUnityAudio = false;

    public bool previewAudioPlaying = false;

    public double dspSongStart = 0.0;


    string videoURL;
    NoteSpawner noteSpawner;
    bool bassInitialized = false;

    AudioStream songAudioStream;
    AudioStream guitarAudioStream;

    public float currentTime = 0f;
    public float previousTime = 0f;

    public float currentTimeInSamples = 0f;

    public double currentTimeInDSP = 0.0;

    IList<AudioStream> audioStreams = new List<AudioStream>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        if (useUnityAudio)
        {
            Debug.Log("Using Unity AudioSource for playback.");
        }
        else
        {
            Debug.Log("Using BASS AudioManager for playback.");
            if (!bassInitialized)
            {
                InitBASS();
            }
        }
        
    }

    
    private void InitBASS()
    {
        if (AudioManager.Init(out var errMsg))
        {
            bassInitialized = true;
            Debug.Log("BASS initialized successfully.");
        }
        else
        {
            Debug.LogError("BASS initialization failed. Error: " + errMsg);
        }
    }
    public void loadAudio(string audioClipPath)
    {
        if (audioClipPath != null)
        {
            if (Path.GetFileName(audioClipPath).Contains("song", StringComparison.OrdinalIgnoreCase))
            {
                if (!useUnityAudio)
                {
                    if (bassInitialized)
                    {
                        Debug.Log("Loading song audio from path: " + audioClipPath);
                        songAudioStream = AudioManager.LoadStream(audioClipPath);
                    }
                }
                else
                {
                    Debug.Log("Loading song audio into Unity AudioSource from path: " + audioClipPath);
                    using var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + audioClipPath, AudioType.OGGVORBIS);
                    var operation = www.SendWebRequest();
                    while (!operation.isDone) { }
                    AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        songAudioSource.clip = clip;
                    }
                    else
                    {
                        Debug.LogError("Failed to load AudioClip from path: " + audioClipPath);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("No AudioClip provided to loadAudio.");
        }
    }
    public void loadGuitarAudio(string guitarClipPath)
    {
        if (guitarClipPath != null)
        {
            if (Path.GetFileName(guitarClipPath).Contains("guitar", StringComparison.OrdinalIgnoreCase))
            {
                if (!useUnityAudio)
                {
                    if (bassInitialized)
                    {
                        Debug.Log("Loading guitar audio from path: " + guitarClipPath);
                        guitarAudioStream = AudioManager.LoadStream(guitarClipPath);
                        audioStreams.Add(guitarAudioStream);
                    }
                }
                else
                {
                    Debug.Log("Loading guitar audio into Unity AudioSource from path: " + guitarClipPath);
                    using var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + guitarClipPath, AudioType.OGGVORBIS);
                    var operation = www.SendWebRequest();
                    while (!operation.isDone) { }
                    AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        songAudioSource.clip = clip;
                    }
                    else
                    {
                        Debug.LogError("Failed to load AudioClip from path: " + guitarClipPath);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("No AudioClip provided to loadAudio.");
        }
    }
    public void loadVideo(string videoClipPath)
    {
        if (!string.IsNullOrEmpty(videoClipPath))
        {
            videoURL = videoClipPath;
            if (videoPlayer != null)
            {
                if (!videoPlayer.isPrepared)
                {
                    videoPlayer.url = videoClipPath;
                    videoPlayer.Prepare();
                }
            }
            else
            {
                Debug.LogError("No VideoPlayer found in the scene to load video.");
            }
        }
        else
        {
            Debug.LogError("No videoClipPath provided to loadVideo.");
        }
    }

    // Schedule playback at a DSP time (AudioSettings.dspTime + offset)
    public void PlayScheduled(double dspTime)
    {
        dspSongStart = dspTime;

        if (videoPlayer != null) videoPlayer.Play();
        if (useUnityAudio)
        {
            if (songAudioSource != null)
            {
                songAudioSource.PlayScheduled(dspTime);
            }
            if (guitarAudioSource != null)
            {
                guitarAudioSource.PlayScheduled(dspTime);
            }
            else
            {
                return;
            }
        }
    }

    // Return elapsed song time in seconds according to the DSP clock.
    // If audio hasn't started yet this returns a negative time until dspSongStart.
    public double GetElapsedTimeDsp()
    {
        return AudioSettings.dspTime - dspSongStart;
    }

    // Convenience: returns 0..end for code expecting non-negative elapsed seconds
    public double GetClampedElapsedTimeDsp()
    {
        return Math.Max(0.0, GetElapsedTimeDsp());
    }

    public void NoDelayPlayAudio()
    {
        Debug.Log("Playing audio streams.");
        if (videoPlayer != null) videoPlayer.Play();
        if (useUnityAudio)
        {
            if (songAudioSource != null)
            {
                songAudioSource.Play();
            }
            if (guitarAudioSource != null)
            {
                guitarAudioSource.Play();
            }
            return;
        }
        if (songAudioStream != null && guitarAudioStream != null)
        {
            if (bassInitialized)
            {
                if (songAudioStream.PlaySynced(0, audioStreams))
                {
                    Debug.Log("Audio streams playing successfully.");
                }
                else
                {
                    Debug.LogError("Failed to play audio streams.");
                }
            }
        }
    }
    public async Task PlayPreviewAudio(string filePath, float startPoint = 0)
    {
        if (previewAudioSource != null)
        {
            using var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.OGGVORBIS);
            var operation = www.SendWebRequest();
            while (!operation.isDone) { await Task.Yield(); }
            AudioClip audioClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
            if (audioClip != null)
            {
                previewAudioSource.clip = audioClip;
                previewAudioSource.time = startPoint;
                previewAudioSource.Play();
                previewAudioPlaying = true;
            }
        }
    }
    public void StopPreviewAudio()
    {
        if (previewAudioSource != null)
        {
            previewAudioSource.Stop();
            previewAudioPlaying = false;
        }
    }


    
    public void resumeAudio()
    {
        if (useUnityAudio)
        {
            if (songAudioSource != null)
            {
                songAudioSource.UnPause();
            }
            if (guitarAudioSource != null)
            {
                guitarAudioSource.UnPause();
            }
            return;
        }
        if (songAudioStream != null && guitarAudioStream != null)
        {
            if (bassInitialized)
            {
                if (songAudioStream.PlaySynced(previousTime, audioStreams))
                {
                    Debug.Log("Audio streams playing successfully.");
                }
                else
                {
                    Debug.LogError("Failed to play audio streams.");
                }
            }
        }
        if (videoPlayer != null) videoPlayer.Play();
    }
    public void stopAudio()
    {
        if (useUnityAudio)
        {
            if (songAudioSource != null)
            {
                songAudioSource.Stop();
            }
            if (guitarAudioSource != null)
            {
                guitarAudioSource.Stop();
            }
            if (previewAudioSource != null)
            {
                previewAudioSource.Stop();
            }
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
            return;
        }
        if (songAudioStream != null)
        {
            previousTime = (float)songAudioStream.CurrentPositionSeconds;
            songAudioStream.Stop();
            songAudioStream.Dispose();
        }
        if (guitarAudioStream != null)
        {
            previousTime = (float)guitarAudioStream.CurrentPositionSeconds;
            guitarAudioStream.Stop();
            guitarAudioStream.Dispose();
        }
        if (videoPlayer != null) videoPlayer.Stop();
        AudioManager.Dispose();
        bassInitialized = false;
    }
    public void setPitch(float pitch)
    {
        if (useUnityAudio)
        {
            if (songAudioSource != null)
            {
                songAudioSource.pitch = pitch;
            }
            if (guitarAudioSource != null)
            {
                guitarAudioSource.pitch = pitch;
            }
            return;
        }
    }
    public void setVolume(float volume)
    {
        if (useUnityAudio)
        {
            if (songAudioSource != null)
            {
                songAudioSource.volume = volume;
            }
            if (guitarAudioSource != null)
            {
                guitarAudioSource.volume = volume;
            }
            return;
        }
        if (bassInitialized)
        {
            if (songAudioStream != null)
            {
                AudioManager.SetAttribute(songAudioStream, AudioAttributes.Volume, volume);
            }
            if (guitarAudioStream != null)
            {
                AudioManager.SetAttribute(guitarAudioStream, AudioAttributes.Volume, volume);
            }
            
        }
    }
    public void pauseAudio()
    { 
        if (useUnityAudio)
        {
            if (songAudioSource != null)
            {
                songAudioSource.Pause();
            }
            if (guitarAudioSource != null)
            {
                guitarAudioSource.Pause();
            }
            return;
        }
        if (videoPlayer != null) videoPlayer.Pause();
        if (songAudioStream != null)
        {
            previousTime = (float)songAudioStream.CurrentPositionSeconds;
            songAudioStream.Stop();
        }
        if (guitarAudioStream != null)
        {
            previousTime = (float)guitarAudioStream.CurrentPositionSeconds;
            guitarAudioStream.Stop();
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (noteSpawner == null)
        {
            noteSpawner = FindFirstObjectByType<NoteSpawner>();
        }

        if (noteSpawner != null)
        {
            if (bassInitialized)
            {
                if (songAudioStream != null)
                {
                    currentTime = (float)songAudioStream.CurrentPositionSeconds;
                }
            }
            else
            {
                currentTime = 0f;
            }

            if (useUnityAudio)
            {
                if (songAudioSource != null)
                {
                    setPitch(Time.timeScale);
                    currentTime = songAudioSource.time;
                    currentTimeInSamples = songAudioSource.timeSamples;
                    currentTimeInDSP = GetClampedElapsedTimeDsp();
                    if (noteSpawner.songLengthInTicks > 0 && currentTime >= (noteSpawner.songLengthInTicks / 1000f))
                    {
                        Debug.Log("Song ended.");
                        stopAudio();
                        GlobalMoveY globalMoveY = FindFirstObjectByType<GlobalMoveY>();
                        if (globalMoveY != null)
                        {
                            globalMoveY.isMoving = false;
                        }
                        GameManager gameManager = FindFirstObjectByType<GameManager>();
                        if (gameManager != null)
                        {
                            gameManager.ResetAllValues();
                        }
                        LoadingManager loadingManager = FindFirstObjectByType<LoadingManager>();
                        if (loadingManager != null)
                        {
                            loadingManager.LoadScene("ScoreScreen", LoadSceneMode.Single, false);
                        }
                    }
                }
                return;
            }
            else
            {
                if (songAudioStream != null)
                {
                    if (noteSpawner.songLengthInTicks > 0 && currentTime >= (noteSpawner.songLengthInTicks / 1000f))
                    {
                        stopAudio();
                        GlobalMoveY globalMoveY = FindFirstObjectByType<GlobalMoveY>();
                        if (globalMoveY != null)
                        {
                            globalMoveY.isMoving = false;
                        }
                        LoadingManager loadingManager = FindFirstObjectByType<LoadingManager>();
                        if (loadingManager != null)
                        {
                            loadingManager.LoadScene("ScoreScreen", LoadSceneMode.Single, false);
                        }
                    }
                }
            }
        }


        videoPlayer = FindFirstObjectByType<VideoPlayer>();

        // Using the new Input System
        if (UnityEngine.InputSystem.Keyboard.current.leftCtrlKey.isPressed && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("Reloading video URL: " + videoURL);
            loadVideo(videoURL);
        }
        if (videoPlayer != null && !videoPlayer.isPlaying && videoPlayer.isPrepared && currentTime > previousTime)
        {
            Debug.Log("Syncing video to audio at time: " + currentTime);
            videoPlayer.time = currentTime;
            videoPlayer.Play();
        }
    }
    public float GetElapsedTime()
    {
        if (useUnityAudio)
        {
            return songAudioSource != null ? songAudioSource.time : 0f;
        }
        return songAudioStream != null ? songAudioStream.CurrentPositionSeconds : 0f;
    }
    
    
}