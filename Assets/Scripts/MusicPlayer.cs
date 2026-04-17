using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Video;
using ManagedBass;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;

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
    public AudioSource previewAudioStream;
    public float currentTime = 0f;
    public float previousTime = 0f;
    public float NSSonglength = 0;
    float[] spectrumData = new float[512];
    int reverbFX = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
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
                        songStreamHandle = Bass.CreateStream(audioClipPath, 0, 0, BassFlags.Prescan | BassFlags.FX);
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

    public IEnumerator PlayPreviewAudio(string filePath, float startPoint = 0, AudioType audioType = AudioType.OGGVORBIS)
    {
        string uriPath = new System.Uri(filePath).AbsoluteUri;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uriPath, audioType))
        {
            yield return uwr.SendWebRequest(); // Wait for the request to complete
            yield return Task.Yield();
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error loading audio clip: " + uwr.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
                if (clip != null)
                {
                    previewAudioStream.clip = clip;
                    previewAudioStream.time = startPoint / 1000;
                    previewAudioStream.volume = 0;
                    
                    StartCoroutine(FadeInCoroutine());
                    previewAudioPlaying = true;
                }
            }
        }
    }
    public void StopPreviewAudio()
    {
        if (previewAudioStream != null)
        {
            StartCoroutine(FadeOutCoroutine());
        }
    }
    IEnumerator FadeOutCoroutine()
    {
        float duration = 1f; // Duration for the fill animation
        float elapsed = 0f;
        float fillAmount = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fillAmount = Mathf.Lerp(1f, 0f, elapsed / duration);
            if (previewAudioStream != null)
            {
                previewAudioStream.volume = fillAmount;
            }
            
        }
        if (fillAmount == 0)
        {
            yield return null;
            previewAudioStream.Stop();
            previewAudioStream.clip = null;
            previewAudioStream.time = 0;
            previewAudioPlaying = false;
        }
    }
    IEnumerator FadeInCoroutine()
    {
        float duration = 1f; // Duration for the fill animation
        float elapsed = 0f;
        
        previewAudioStream.Play();
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fillAmount = Mathf.Lerp(0f, 1f, elapsed / duration);
            if (previewAudioStream != null)
            {
                previewAudioStream.volume = fillAmount;
            }
            yield return null;
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
    public void stopAudio(bool freeBass = true)
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
        if (freeBass)
        {
            try { Bass.Free(); } catch { }
            bassInitialized = false;
        }
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
        GlobalMoveY gm = FindAnyObjectByType<GlobalMoveY>();
        if (gm != null) gm.isMoving = false;
    }
    public void RestartAt(double time)
    {
        if (isPaused) return;
        if (bassInitialized)
        {
            if (songStreamHandle != 0)
            {
                long restartPos = Bass.ChannelSeconds2Bytes(songStreamHandle, time);
                Bass.ChannelStop(songStreamHandle);
                Bass.ChannelSetPosition(songStreamHandle, restartPos);
                Bass.ChannelPlay(songStreamHandle);
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
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
            
            if (gameManager.inSong && (currentTime * 1000) >= noteSpawner.songLengthInTicks)
            {
                Debug.Log("song ended");
                StartCoroutine(EndSong());
            }
        }

        // "audio buffer" vsync must be on or FPS must be over 60
        //if (gameManager.inSong && GetElapsedTimeDsp() != noteSpawner.currentTick && !isPaused)
        //{
        //    RestartAt(GetElapsedTimeDsp());
        //}


        videoPlayer = FindFirstObjectByType<VideoPlayer>();

        // Start video playback exactly when the DSP start time is reached
        if (videoPlayer != null && !videoPlayer.isPlaying && dspSongStart > 0.0 && AudioSettings.dspTime >= dspSongStart && !isPaused)
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
        if (videoPlayer != null && !videoPlayer.isPlaying && videoPlayer.isPrepared && currentTime > previousTime && !isPaused)
        {
            Debug.Log("Syncing video to audio at time: " + currentTime);
            videoPlayer.time = currentTime;
            videoPlayer.Play();
        }
    }
    public IEnumerator EndSong()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        Debug.Log("Song ended. (called manually)");
        gameManager.inSong = false;
        gameManager.currentSongLengthInTicks = 0;
        if (noteSpawner != null)
        {
            noteSpawner.songLengthInTicks = 0;
        }
        stopAudio(false);
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        GlobalMoveY globalMoveY = FindFirstObjectByType<GlobalMoveY>();
        Scoring scoring = FindAnyObjectByType<Scoring>();
        if (scoring != null)
        {
            scoring.Save(gameManager.savePath);
        }
        if (globalMoveY != null)
        {
            globalMoveY.isMoving = false;
        }
        if (gameManager != null)
        {
            gameManager.ResetAllValues();
        }
        ImprovedStrikeline strikeline = FindAnyObjectByType<ImprovedStrikeline>();
        if (strikeline != null)
        {
            strikeline.ResetAnims();
        }
        GameObject gp = GameObject.Find("GuitarPlayer");
        if (gp != null)
        {
            Animation highwayAnim = gp.GetComponent<Animation>();
            highwayAnim.Play("HideHighway");
            yield return new WaitForSecondsRealtime(1f);
            highwayAnim.Stop();
            gp.SetActive(false);
        }
        
        LoadingManager loadingManager = FindFirstObjectByType<LoadingManager>();
        if (loadingManager != null)
        {
            loadingManager.LoadScene("ScoreScreen", LoadSceneMode.Single, false);
        }
        yield return null;
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
    public short GetSongAudioLevel()
    {
        if (bassInitialized && songStreamHandle != 0)
        {
            try
            {
                short level = (short)Bass.ChannelGetLevel(songStreamHandle);
                return level;
            }
            catch { return 0; }
        }
        return 0;
    }
    public int GetSongData()
    {
        if (bassInitialized && songStreamHandle != 0)
        {
            try
            {

                int level = Bass.ChannelGetData(songStreamHandle, spectrumData, (int)Bass.ChannelGetLength(songStreamHandle));
                return level;
            }
            catch { return 0; }
        }
        return 0;
    }
    public void ToggleReverb(bool on)
    {
        
        if (bassInitialized && songStreamHandle != 0)
        {
            if (on)
            {
                try
                {
                    reverbFX = Bass.ChannelSetFX(songStreamHandle, EffectType.DXReverb, 0);
                }
                catch {  }
            }
            else
            {
                try
                {
                    Bass.ChannelRemoveFX(songStreamHandle, reverbFX);
                }
                catch {  }
            }
        }
        
    }
    
}