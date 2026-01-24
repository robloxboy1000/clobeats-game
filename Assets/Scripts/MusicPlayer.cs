using UnityEngine;
using UnityEngine.Analytics;
using System;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.Video;

using System.Collections.Generic;
using MoonscraperEngine.Audio;
using ManagedBass;

using System.Threading.Tasks;

public class MusicPlayer : MonoBehaviour
{

    public VideoPlayer videoPlayer;


    public bool previewAudioPlaying = false;

    public double dspSongStart = 0.0;
    public bool isPaused = false;
    public double pausedElapsedDsp = 0.0;

    private Coroutine bassScheduledCoroutine = null;

    string videoURL;
    NoteSpawner noteSpawner;
    bool bassInitialized = false;

    // ManagedBass stream handles
    private int songStreamHandle = 0;
    private int previewStreamHandle = 0;


    public float currentTime = 0f;
    public float previousTime = 0f;

    public float currentTimeInSamples = 0f;

    public double currentTimeInDSP = 0.0;
    public float NSSonglength = 0;


    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        
        Debug.Log("Using BASS AudioManager for playback.");
        if (!bassInitialized)
        {
            InitBASS();
        }
        
        
    }

    
    private void InitBASS()
    {
        try
        {
            if (!Bass.Init())
            {
                if (Bass.LastError == Errors.Already)
                {
                    Debug.Log("BASS already initialized.");
                    bassInitialized = true;
                }
                else
                {
                    Debug.LogError("BASS init failed: " + Bass.LastError);
                    bassInitialized = false;
                }
            }
            else
            {
                bassInitialized = true;
                Debug.Log("BASS initialized.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("BASS initialization exception: " + ex.Message);
            bassInitialized = false;
        }
    }
    public async Task loadSongAudio(string audioClipPath)
    {
        if (audioClipPath != null)
        {
            if (Path.GetFileName(audioClipPath).Contains("song", StringComparison.OrdinalIgnoreCase))
            {
                if (!bassInitialized) InitBASS();
                if (bassInitialized)
                {
                    Debug.Log("Loading song audio (ManagedBass) from path: " + audioClipPath);
                    NSSonglength = noteSpawner.songLengthInTicks;
                    try
                    {
                        await Task.Yield();
                        if (songStreamHandle != 0) { Bass.StreamFree(songStreamHandle); songStreamHandle = 0; }
                        // Create stream for file path
                        songStreamHandle = Bass.CreateStream(audioClipPath, 0, 0, BassFlags.Default);
                        if (songStreamHandle == 0) Debug.LogError("Failed to create BASS stream: " + Bass.LastError);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Exception creating BASS stream: " + ex.Message);
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
        // clear any paused state so DSP elapsed advances normally
        isPaused = false;
        pausedElapsedDsp = 0.0;

        // Do not start Video immediately — schedule audio, and start video when DSP time reaches dspSongStart
        if (videoPlayer != null)
        {
            try
            {
                videoPlayer.time = 0;
                videoPlayer.Pause();
            }
            catch { }
        }

        
        // Schedule BASS playback: wait until the DSP time is reached then start BASS streams
        if (bassScheduledCoroutine != null) StopCoroutine(bassScheduledCoroutine);
        bassScheduledCoroutine = StartCoroutine(StartBASSAt(dspTime));
        
    }

        private System.Collections.IEnumerator StartBASSAt(double dspStart)
        {
            // wait until the system DSP clock reaches the start time
            while (AudioSettings.dspTime < dspStart)
            {
                yield return null;
            }
            // start BASS streams synchronized
            if (bassInitialized)
            {
                try
                {
                    if (songStreamHandle != 0)
                    {
                        Bass.ChannelPlay(songStreamHandle);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to start BASS streams at scheduled time: " + ex.Message);
                }
            }
            bassScheduledCoroutine = null;
            yield break;
        }

    // Return elapsed song time in seconds according to the DSP clock.
    // If audio hasn't started yet this returns a negative time until dspSongStart.
    public double GetElapsedTimeDsp()
    {
        if (isPaused) return pausedElapsedDsp;
        return AudioSettings.dspTime - dspSongStart;
    }

    // Convenience: returns 0..end for code expecting non-negative elapsed seconds
    public double GetClampedElapsedTimeDsp()
    {
        return Math.Max(0.0, GetElapsedTimeDsp());
    }

    public async Task PlayPreviewAudio(string filePath, float startPoint = 0)
    {
        if (!bassInitialized) InitBASS();
        if (bassInitialized)
        {
            MenuManager menuManager = FindAnyObjectByType<MenuManager>();
            menuManager.loadingPreviewImage.SetActive(true);
            Debug.Log("Loading preview (ManagedBass) from path: " + filePath);
            try
            {
                if (previewStreamHandle != 0) { Bass.StreamFree(previewStreamHandle); previewStreamHandle = 0; }
                long previewStartBytePosition = Bass.ChannelSeconds2Bytes(previewStreamHandle, (double)(startPoint / 1000f));
                previewStreamHandle = Bass.CreateStream(filePath, 0, 0, BassFlags.Default);
                Bass.ChannelPlay(previewStreamHandle);
                Bass.ChannelSetPosition(previewStreamHandle, previewStartBytePosition);
                await Task.Yield();
                menuManager.loadingPreviewImage.SetActive(false);
                previewAudioPlaying = true;
                if (previewStreamHandle == 0) Debug.LogError("Failed to create BASS preview stream: " + Bass.LastError);
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception creating BASS preview stream: " + ex.Message);
            }
        }
    }
    public void StopPreviewAudio()
    {
        if (previewStreamHandle != 0 && bassInitialized)
        {
            try { Bass.ChannelStop(previewStreamHandle); } catch { }
            try { Bass.StreamFree(previewStreamHandle); } catch { }
            previewStreamHandle = 0;
            previewAudioPlaying = false;
        }
    }

    public void PlayOneShotAudio(string filePath)
    {
        if (!bassInitialized) InitBASS();
        if (bassInitialized)
        {
            int oneShotStream = 0;
            Debug.Log("Loading one-shot (ManagedBass) from path: " + filePath);
            try
            {
                if (oneShotStream != 0)
                {
                    Bass.StreamFree(oneShotStream);
                    oneShotStream = 0;
                }
                oneShotStream = Bass.CreateStream(filePath, 0, 0, BassFlags.Default);
                Bass.ChannelPlay(oneShotStream);
            }
            catch (Exception ex)
            {
                Debug.LogError("Exception creating BASS one-shot stream: " + ex.Message);
            }
        }
    }


    
    public void resumeAudio()
    {
        if (bassInitialized)
        {
            try
            {
                if (songStreamHandle != 0)
                {
                    long pos = Bass.ChannelSeconds2Bytes(songStreamHandle, previousTime);
                    Bass.ChannelSetPosition(songStreamHandle, pos);
                    Bass.ChannelPlay(songStreamHandle);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to resume BASS streams: " + ex.Message);
            }
        }
        // Adjust DSP anchor so DSP-derived elapsed time continues from where we paused
        dspSongStart = AudioSettings.dspTime - pausedElapsedDsp;
        isPaused = false;
        // Resume visuals/movement
        var gmResume = FindAnyObjectByType<GlobalMoveY>();
        if (gmResume != null) gmResume.isMoving = true;
        if (videoPlayer != null)
        {
            try
            {
                videoPlayer.time = pausedElapsedDsp;
                videoPlayer.Play();
            }
            catch { }
        }
    }
    public void stopAudio()
    {
        if (bassScheduledCoroutine != null)
        {
            StopCoroutine(bassScheduledCoroutine);
            bassScheduledCoroutine = null;
        }
        if (songStreamHandle != 0 && bassInitialized)
        {
            try { previousTime = (float)Bass.ChannelBytes2Seconds(songStreamHandle, Bass.ChannelGetPosition(songStreamHandle)); } catch { }
            try { Bass.ChannelStop(songStreamHandle); } catch { }
            try { Bass.StreamFree(songStreamHandle); } catch { }
            songStreamHandle = 0;
        }
        if (videoPlayer != null) videoPlayer.Stop();
        try { Bass.Free(); } catch { }
        bassInitialized = false;
    }
    public void pauseAudio()
    { 
        
        if (videoPlayer != null) videoPlayer.Pause();
        if (bassInitialized)
        {
            if (songStreamHandle != 0)
            {
                try { previousTime = (float)Bass.ChannelBytes2Seconds(songStreamHandle, Bass.ChannelGetPosition(songStreamHandle)); } catch { }
                try { Bass.ChannelPause(songStreamHandle); } catch { }
            }
        }
        

        // record paused elapsed DSP time and stop visuals
        pausedElapsedDsp = GetElapsedTimeDsp();
        isPaused = true;
        var gm = FindAnyObjectByType<GlobalMoveY>();
        if (gm != null) gm.isMoving = false;
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
            // Provide DSP-derived elapsed seconds (may be negative before scheduled start)
            double dspElapsed = GetElapsedTimeDsp();
            noteSpawner.UpdateCurrentTick((float)dspElapsed);
        }

        if (noteSpawner != null)
        {
            currentTime = GetElapsedTime();
            
            if (NSSonglength > 0 && currentTime >= GetSongLength())
            {
                Debug.Log("Song ended.");
                NSSonglength = 0;
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


        videoPlayer = FindFirstObjectByType<VideoPlayer>();

        // Start video playback exactly when the DSP start time is reached
        if (videoPlayer != null && !videoPlayer.isPlaying && dspSongStart > 0.0 && AudioSettings.dspTime >= dspSongStart)
        {
            try
            {
                // align video to audio elapsed
                double elapsed = GetClampedElapsedTimeDsp();
                videoPlayer.time = elapsed;
                videoPlayer.Play();
            }
            catch { }
        }

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
        if (bassInitialized && songStreamHandle != 0)
        {
            try
            {
                long pos = Bass.ChannelGetPosition(songStreamHandle);
                double secs = Bass.ChannelBytes2Seconds(songStreamHandle, pos);
                return (float)secs;
            }
            catch { return 0f; }
        }
        return 0f;
    }
    public float GetSongLength()
    {
        if (bassInitialized && songStreamHandle != 0)
        {
            try
            {
                long length = Bass.ChannelGetLength(songStreamHandle);
                double secs = Bass.ChannelBytes2Seconds(songStreamHandle, length);
                return (float)secs;
            }
            catch { return 0f;}
        }
        return 0f;
    }
    public int GetSongAudioLevel()
    {
        if (bassInitialized && songStreamHandle != 0)
        {
            try
            {
                int level = Bass.ChannelGetLevel(songStreamHandle);
                return level;
            }
            catch { return 0; }
        }
        return 0;
    }
    
    
}