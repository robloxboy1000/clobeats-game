using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using ManagedBass;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using System.IO;
using System.Runtime.InteropServices;

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
    public double currentTimeDSP = 0;
    public double currentTime = 0;
    public float previousTime = 0f;
    public float NSSonglength = 0;
    public double songLength = 0;
    public double graceEndSeconds = 4.0;
    float[] spectrumData = new float[512];
    int reverbFX = 0;
    // DSP fallback for platforms / channels that don't support ChannelSetFX
    int reverbDSP = 0;
    ManagedBass.DSPProcedure reverbDspProc = null;
    // per-channel delay buffers for stereo wet output
    float[][] reverbDelayBuffers = null;
    int reverbFramePos = 0; // frame index into per-channel delay buffers
    int reverbDelayLen = 0; // number of frames in delay buffer
    int reverbDspChannels = 0;
    bool reverbUsingDspFallback = false;
    // Fallback parameters
    float reverbFallbackDelaySeconds = 0.12f;
    float reverbFallbackFeedback = 0.45f;
    float reverbFallbackMix = 0.4f; // wet mix (scale applied to delayed signal)
    float reverbWetGain = 1.2f; // amplify wet signal slightly
    int[] reverbChannelOffset = null; // per-channel stereo offset in frames
    // temporary buffers used by the DSP (cached to avoid allocations)
    float[] reverbTmpFloat = null;
    short[] reverbTmpShort = null;

    public bool seamlessMode = true;

    // DryWetMidi playback
    private Playback midiPlayback = null;
    private OutputDevice midiOutput = null;
    private MidiFile currentMidiFile = null;
    private Coroutine midiScheduledCoroutine = null;

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
                    songStreamHandle = Bass.CreateStream(audioClipPath, 0, 0, BassFlags.Prescan);
                    if (songStreamHandle == 0) { Debug.LogError("Failed to create BASS stream: " + Bass.LastError); MessageBox.Instance.Show("Failed to create BASS stream: " + Bass.LastError + "<br>Audio playback Failed.");}
                }
                catch (Exception ex)
                {
                    Debug.LogError("Exception creating BASS stream: " + ex.Message);
                }
            }
        }
        else
        {
            Debug.LogError("No AudioClip provided to loadAudio.");
        }
    }
    public async Task loadSongMidi(string audioClipPath)
    {
        if (string.IsNullOrEmpty(audioClipPath))
        {
            Debug.LogError("No AudioClip provided to loadAudio.");
            return;
        }

        NSSonglength = noteSpawner != null ? noteSpawner.songLengthInTicks : NSSonglength;

        try
        {
            await Task.Yield();

            // Dispose previous MIDI playback if present
            if (midiPlayback != null)
            {
                try { midiPlayback.Stop(); midiPlayback.Dispose(); } catch { }
                midiPlayback = null;
            }
            if (midiOutput != null)
            {
                try { midiOutput.Dispose(); } catch { }
                midiOutput = null;
            }
            currentMidiFile = null;

            // Read MIDI file
            MidiFile midiFile = MidiFile.Read(audioClipPath);
            currentMidiFile = midiFile;

            // Select output device (prefer Microsoft GS Wavetable Synth)
            try
            {
                try
                {
                    midiOutput = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
                }
                catch
                {
                    int devCount = OutputDevice.GetDevicesCount();
                    if (devCount > 0) midiOutput = OutputDevice.GetByIndex(0);
                    else
                    {
                        Debug.LogError("No MIDI output devices available on system.");
                        MessageBox.Instance.Show("No MIDI output devices available.");
                        return;
                    }
                }

                // Create playback that will send events to the selected output device
                midiPlayback = midiFile.GetPlayback(midiOutput);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to create MIDI playback: " + ex.Message);
                MessageBox.Instance.Show("Failed to create MIDI playback: " + ex.Message + "<br>Audio playback Failed.");
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception loading MIDI file: " + ex.Message);
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
        // Schedule MIDI playback if a MIDI playback was prepared
        if (midiPlayback != null)
        {
            if (midiScheduledCoroutine != null) StopCoroutine(midiScheduledCoroutine);
            midiScheduledCoroutine = StartCoroutine(StartMidiAt(dspTime));
        }
        
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

    private System.Collections.IEnumerator StartMidiAt(double dspStart)
    {
        while (AudioSettings.dspTime < dspStart)
        {
            yield return null;
        }
        if (midiPlayback != null)
        {
            try
            {
                midiPlayback.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to start MIDI playback at scheduled time: " + ex.Message);
            }
        }
        midiScheduledCoroutine = null;
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
            yield return null;
        }

        if (fillAmount == 0)
        {
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
        // Resume MIDI playback if available
        if (midiPlayback != null)
        {
            try
            {
                midiPlayback.MoveToTime(new MetricTimeSpan(TimeSpan.FromSeconds(pausedElapsedDsp)));
                midiPlayback.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to resume MIDI playback: " + ex.Message);
            }
        }
        // Adjust DSP anchor so DSP-derived elapsed time continues from where we paused
        dspSongStart = AudioSettings.dspTime - pausedElapsedDsp;
        isPaused = false;
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
        if (midiScheduledCoroutine != null)
        {
            StopCoroutine(midiScheduledCoroutine);
            midiScheduledCoroutine = null;
        }
        if (songStreamHandle != 0 && bassInitialized)
        {
            try { previousTime = (float)Bass.ChannelBytes2Seconds(songStreamHandle, Bass.ChannelGetPosition(songStreamHandle)); } catch { }
            try
            {
                if (reverbFX != 0)
                {
                    try { Bass.ChannelRemoveFX(songStreamHandle, reverbFX); } catch { }
                    reverbFX = 0;
                }
                if (reverbUsingDspFallback || reverbDSP != 0)
                {
                    try { Bass.ChannelRemoveDSP(songStreamHandle, reverbDSP); } catch { }
                    reverbDSP = 0;
                    reverbUsingDspFallback = false;
                }
            }
            catch { }
            try { Bass.ChannelStop(songStreamHandle); } catch { }
            try { Bass.StreamFree(songStreamHandle); } catch { }
            songStreamHandle = 0;
        }
        if (midiPlayback != null)
        {
            try { midiPlayback.Stop(); midiPlayback.Dispose(); } catch { }
            midiPlayback = null;
        }
        if (midiOutput != null)
        {
            try { midiOutput.Dispose(); } catch { }
            midiOutput = null;
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
        if (midiPlayback != null)
        {
            try { midiPlayback.Stop(); } catch { }
        }
        

        // record paused elapsed DSP time and stop visuals
        pausedElapsedDsp = GetElapsedTimeDsp();
        isPaused = true;
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
        if (midiPlayback != null)
        {
            try
            {
                midiPlayback.MoveToTime(new MetricTimeSpan(TimeSpan.FromSeconds(time)));
                midiPlayback.Stop();
                midiPlayback.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to restart MIDI playback: " + ex.Message);
            }
        }
    }

    float musicPollInterval = 0.5f;
    float lastMusicCheckTime = 0f;
    
    // Update is called once per frame
    void Update()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (Time.time - lastMusicCheckTime > musicPollInterval)
        {
            lastMusicCheckTime = Time.time;
            if (gameManager.inSong && GetElapsedTimeDsp() != noteSpawner.GetTimeInSecondsAtTick(noteSpawner.currentTick) && !isPaused && !seamlessMode)
            {
                RestartAt(GetElapsedTimeDsp());
            }
        }

        
        if (noteSpawner == null)
        {
            noteSpawner = FindFirstObjectByType<NoteSpawner>();
        }
        if (noteSpawner != null)
        {
            // Provide DSP-derived elapsed seconds (may be negative before scheduled start)
            double dspElapsed = GetElapsedTimeDsp();
            if (gameManager.inSong) 
            {
                noteSpawner.UpdateCurrentTick((float)dspElapsed);
            }
            else
            {
                noteSpawner.UpdateCurrentTick(-0.01f);
            }
            currentTimeDSP = dspElapsed;
            currentTime = GetElapsedTime();
            songLength = GetSongLength();
            
            if (gameManager.inSong && currentTimeDSP >= songLength + graceEndSeconds)
            {
                StartCoroutine(EndSong());
            }
        }

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
        if (videoPlayer != null && !videoPlayer.isPlaying && videoPlayer.isPrepared && currentTime > previousTime && !isPaused && gameManager.inSong)
        {
            Debug.Log("Syncing video to audio at time: " + currentTime);
            videoPlayer.time = currentTime;
            videoPlayer.Play();
        }
        else if (videoPlayer != null && videoPlayer.isPrepared && !videoPlayer.isPlaying && videoPlayer.time == videoPlayer.length)
        {
            videoPlayer.Stop();
            videoPlayer.url = string.Empty;
            videoPlayer.enabled = false;
        }
    }
    public IEnumerator EndSong(bool songCleared = true)
    {
        Debug.Log("[MusicPlayer.EndSong] Unloading song...");
        UIUpdater ui = FindAnyObjectByType<UIUpdater>();
        if (ui != null)
        {
            ui.ScoreVisibility(false);
        }
        GameManager gameManager = FindAnyObjectByType<GameManager>();
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
        Scoring scoring = FindAnyObjectByType<Scoring>();
        if (scoring != null && !songCleared)
        {
            scoring.playerTag = PlayerPrefs.GetString("Username") != string.Empty ? PlayerPrefs.GetString("Username") : System.Environment.UserName;
            scoring.Save(gameManager.savePath);
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
            gp.SetActive(false);
        }
        VenueAnimationPlayer.Instance.Unload();
        Debug.Log("[MusicPlayer.EndSong] Song unloaded.");
        if (songCleared) StartCoroutine(gameManager.PlayerRocksAnim());
        yield return null;
    }
    public double GetElapsedTime()
    {
        if (midiPlayback != null)
        {
            try
            {
                var mt = midiPlayback.GetCurrentTime<MetricTimeSpan>();
                return mt.TotalSeconds;
            }
            catch { return 0f; }
        }
        if (bassInitialized && songStreamHandle != 0)
        {
            try
            {
                long pos = Bass.ChannelGetPosition(songStreamHandle);
                double secs = Bass.ChannelBytes2Seconds(songStreamHandle, pos);
                return secs;
            }
            catch { return 0f; }
        }
        return 0f;
    }
    public double GetSongLength()
    {
        if (midiPlayback != null)
        {
            try
            {
                var dur = midiPlayback.GetDuration<MetricTimeSpan>();
                return dur.TotalSeconds;
            }
            catch { return 0f; }
        }
        if (bassInitialized && songStreamHandle != 0)
        {
            try
            {
                long length = Bass.ChannelGetLength(songStreamHandle);
                double secs = Bass.ChannelBytes2Seconds(songStreamHandle, length);
                return secs;
            }
            catch { return 0f; }
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

                int level = Bass.ChannelGetData(songStreamHandle, spectrumData, 1);
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
                    int fxHandle = Bass.ChannelSetFX(songStreamHandle, EffectType.Echo, 0);
                    if (fxHandle == 0)
                    {
                        var lastErr = Bass.LastError;
                        Debug.LogError($"ChannelSetFX failed: {lastErr} ({(int)lastErr}). This usually means the effect type/format isn't supported for that channel.");
                        try
                        {
                            ChannelInfo info;
                            Bass.ChannelGetInfo(songStreamHandle, out info);
                            Debug.Log($"Channel info - Freq: {info.Frequency}, Chans: {info.Channels}, Flags: {info.Flags}, Resolution: {info.Resolution}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("Failed to retrieve channel info: " + ex.Message);
                        }
                        // Try DSP fallback if FX unavailable
                        EnableReverbDspFallback();
                    }
                    else
                    {
                        reverbFX = fxHandle;
                        Debug.Log(reverbFX + " Added reverb/echo to song stream. LastError: " + Bass.LastError);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Exception adding reverb FX: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    if (reverbFX != 0)
                    {
                        Bass.ChannelRemoveFX(songStreamHandle, reverbFX);
                        reverbFX = 0;
                    }
                    if (reverbUsingDspFallback || reverbDSP != 0)
                    {
                        DisableReverbDspFallback();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Failed to remove reverb FX: " + ex.Message);
                }
            }
        }
    }

    private void EnableReverbDspFallback()
    {
        try
        {
            ChannelInfo info;
            if (!Bass.ChannelGetInfo(songStreamHandle, out info))
            {
                Debug.LogWarning("EnableReverbDspFallback: Failed to get channel info.");
                return;
            }

            reverbDspChannels = Math.Max(1, info.Channels);
            int freq = Math.Max(4000, info.Frequency);
            reverbDelayLen = Math.Max(64, (int)(freq * reverbFallbackDelaySeconds));
            // allocate per-channel delay buffers
            reverbDelayBuffers = new float[reverbDspChannels][];
            for (int c = 0; c < reverbDspChannels; c++)
            {
                reverbDelayBuffers[c] = new float[reverbDelayLen];
            }
            reverbFramePos = 0;
            // set small stereo offset for widening if 2+ channels
            reverbChannelOffset = new int[reverbDspChannels];
            if (reverbDspChannels >= 2)
            {
                int stereoOffset = Math.Max(1, (int)(freq * 0.006f)); // ~6ms offset
                reverbChannelOffset[0] = 0;
                reverbChannelOffset[1] = stereoOffset;
                for (int c = 2; c < reverbDspChannels; c++) reverbChannelOffset[c] = stereoOffset * (c % 2 == 0 ? -1 : 1);
            }
            else
            {
                reverbChannelOffset[0] = 0;
            }

            reverbDspProc = new ManagedBass.DSPProcedure(ReverbDsp);
            reverbDSP = Bass.ChannelSetDSP(songStreamHandle, reverbDspProc, IntPtr.Zero, 0);
            if (reverbDSP == 0)
            {
                Debug.LogError("EnableReverbDspFallback: ChannelSetDSP failed: " + Bass.LastError);
                reverbUsingDspFallback = false;
            }
            else
            {
                reverbUsingDspFallback = true;
                Debug.Log($"Enabled DSP reverb fallback (handle={reverbDSP}) delaySamples={reverbDelayLen} channels={reverbDspChannels}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("EnableReverbDspFallback exception: " + ex.Message);
        }
    }

    private void DisableReverbDspFallback()
    {
        try
        {
            if (reverbDSP != 0 && songStreamHandle != 0)
            {
                Bass.ChannelRemoveDSP(songStreamHandle, reverbDSP);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("DisableReverbDspFallback exception: " + ex.Message);
        }
        reverbDSP = 0;
        reverbUsingDspFallback = false;
        reverbDelayBuffers = null;
        reverbTmpFloat = null;
        reverbTmpShort = null;
        reverbChannelOffset = null;
        reverbFramePos = 0;
        reverbDelayLen = 0;
    }

    // Simple mono delay-based reverb/echo DSP callback (applies same effect to all channels)
    private void ReverbDsp(int handle, int channel, IntPtr buffer, int length, IntPtr user)
    {
        try
        {
            bool floatDsp = Bass.FloatingPointDSP;
            if (floatDsp)
            {
                int floats = length / 4;
                    if (floats <= 0) return;
                    if (reverbTmpFloat == null || reverbTmpFloat.Length < floats) reverbTmpFloat = new float[floats];
                    Marshal.Copy(buffer, reverbTmpFloat, 0, floats);

                    int frames = floats / Math.Max(1, reverbDspChannels);
                    for (int f = 0; f < frames; f++)
                    {
                        int idx = f * reverbDspChannels;
                        // process each channel separately, keep dry + add wet
                        for (int c = 0; c < reverbDspChannels; c++)
                        {
                            int s = idx + c;
                            float inC = reverbTmpFloat[s];
                            float delayed = 0f;
                            if (reverbDelayBuffers != null && reverbDelayBuffers.Length > c)
                            {
                                int readPos = reverbFramePos + (reverbChannelOffset != null ? reverbChannelOffset[c] : 0);
                                // wrap
                                readPos %= reverbDelayLen;
                                if (readPos < 0) readPos += reverbDelayLen;
                                delayed = reverbDelayBuffers[c][readPos];
                            }
                            // wet is delayed signal amplified
                            float wetAdd = delayed * reverbFallbackMix * reverbWetGain;
                            float outSample = inC + wetAdd;
                            // clamp
                            outSample = Mathf.Clamp(outSample, -1f, 1f);
                            reverbTmpFloat[s] = outSample;
                            // update delay buffer (write current input + feedback)
                            if (reverbDelayBuffers != null && reverbDelayBuffers.Length > c)
                            {
                                reverbDelayBuffers[c][reverbFramePos] = inC + delayed * reverbFallbackFeedback;
                            }
                        }
                        reverbFramePos++;
                        if (reverbFramePos >= reverbDelayLen) reverbFramePos = 0;
                    }
                    Marshal.Copy(reverbTmpFloat, 0, buffer, floats);
            }
            else
            {
                int shorts = length / 2;
                if (shorts <= 0) return;
                if (reverbTmpShort == null || reverbTmpShort.Length < shorts) reverbTmpShort = new short[shorts];
                Marshal.Copy(buffer, reverbTmpShort, 0, shorts);

                int frames = shorts / Math.Max(1, reverbDspChannels);
                for (int f = 0; f < frames; f++)
                {
                    int idx = f * reverbDspChannels;
                    for (int c = 0; c < reverbDspChannels; c++)
                    {
                        int s = idx + c;
                        float inC = reverbTmpShort[s] / 32768f;
                        float delayed = 0f;
                        if (reverbDelayBuffers != null && reverbDelayBuffers.Length > c)
                        {
                            int readPos = reverbFramePos + (reverbChannelOffset != null ? reverbChannelOffset[c] : 0);
                            readPos %= reverbDelayLen;
                            if (readPos < 0) readPos += reverbDelayLen;
                            delayed = reverbDelayBuffers[c][readPos];
                        }
                        float wetAdd = delayed * reverbFallbackMix * reverbWetGain;
                        float outSample = inC + wetAdd;
                        outSample = Mathf.Clamp(outSample, -1f, 1f);
                        int v = (int)Mathf.Clamp(outSample * 32767f, short.MinValue, short.MaxValue);
                        reverbTmpShort[s] = (short)v;
                        if (reverbDelayBuffers != null && reverbDelayBuffers.Length > c)
                        {
                            reverbDelayBuffers[c][reverbFramePos] = inC + delayed * reverbFallbackFeedback;
                        }
                    }
                    reverbFramePos++;
                    if (reverbFramePos >= reverbDelayLen) reverbFramePos = 0;
                }
                Marshal.Copy(reverbTmpShort, 0, buffer, shorts);
            }
        }
        catch
        {
            // Must not throw from DSP
        }
    }
    
}