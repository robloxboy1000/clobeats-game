using UnityEngine;
using System;
using System.Collections.Generic;
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
using System.Linq;
using System.Runtime.InteropServices;

public class MusicPlayer : MonoBehaviour
{
    public double musicVolume = 1.0;
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
    //ManagedBass.DSPProcedure reverbDspProc = null;
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

    [System.Serializable]
    public class AudioStem
    {
        public string name;
        public int handle;
        public List<int> childHandles = new List<int>();
        public bool isPlaying;
        public float volume = 1f;
        public bool reverbEnabled;
        public int reverbFxHandle;
        public int reverbDspHandle;
    }

    private readonly Dictionary<string, AudioStem> stemChannels = new Dictionary<string, AudioStem>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, int> stemReverbFxByHandle = new Dictionary<int, int>();
    private readonly Dictionary<int, int> stemReverbDspByHandle = new Dictionary<int, int>();
    private readonly Dictionary<int, bool> stemReverbDspFallbackByHandle = new Dictionary<int, bool>();

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

    public async Task TestFullAudio()
    {
        Debug.Log("[MusicPlayer.TestFullAudio] Testing BASS Audio");
        await Task.Delay(Mathf.CeilToInt((float)await TestBASSAudio(Path.Combine(Application.streamingAssetsPath, "soundcheck.wav")) * 1000));
        Debug.Log("[MusicPlayer.TestFullAudio] Testing Unity Audio");
        await Task.Delay(Mathf.CeilToInt(TestUnityAudio(Path.Combine(Application.streamingAssetsPath, "soundcheck.wav")) * 1000));
    }

    public async Task<double> TestBASSAudio(string audioPath)
    {
        int testStream = await LoadRegularAudio(audioPath);
        if (testStream != 0)
        {
            Bass.ChannelPlay(testStream);
            long byteLength = Bass.ChannelGetLength(testStream);
            double secsLength = Bass.ChannelBytes2Seconds(testStream, byteLength);
            return secsLength;
        }
        else
        {
            Debug.LogError("[MusicPlayer.TestBASS] Failed to test BASS audio playback: " + Bass.LastError);
            return 0.0;
        }
    }

    public float TestUnityAudio(string audioPath)
    {
        AudioClip testClip = LoadUnityAudioFile(audioPath);
        GameObject go = new GameObject("TestClip");
        go.transform.SetParent(this.transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.clip = testClip;
        src.volume = 1;
        src.Play();
        return testClip.length;
    }
    public AudioClip LoadUnityAudioFile(string filePath)
    {
        string uriPath = new System.Uri(filePath).AbsoluteUri;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uriPath, AudioType.UNKNOWN))
        {
            uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error loading audio clip: " + uwr.error);
                return null;
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);
                if (clip != null)
                {
                    return clip;
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public async Task<int> LoadRegularAudio(string audioClipPath)
    {
        if (audioClipPath != null)
        {
            if (!bassInitialized) InitBASS();
            if (bassInitialized)
            {
                Debug.Log("Loading regular audio (ManagedBass) from path: " + audioClipPath);
                try
                {
                    await Task.Yield();
                    // Create stream for file path
                    int streamHandle = Bass.CreateStream(audioClipPath, 0, 0, BassFlags.Prescan);
                    if (streamHandle == 0)
                    { 
                        Debug.LogError("Failed to create BASS stream: " + Bass.LastError);
                        MessageBox.Instance.Show("Failed to create BASS stream: " + Bass.LastError + "<br>Audio playback Failed.", "Error", null);
                        return 0;
                    }
                    else
                    {
                        return streamHandle;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Exception creating BASS stream: " + ex.Message);
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
        else
        {
            Debug.LogError("No AudioClip provided to loadAudio.");
            return 0;
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
                    if (songStreamHandle == 0) { Debug.LogError("Failed to create BASS stream: " + Bass.LastError); MessageBox.Instance.Show("Failed to create BASS stream: " + Bass.LastError + "<br>Audio playback Failed.", "Error", null);}
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
                        MessageBox.Instance.Show("No MIDI output devices available.", "Error", null);
                        return;
                    }
                }

                // Create playback that will send events to the selected output device
                midiPlayback = midiFile.GetPlayback(midiOutput);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to create MIDI playback: " + ex.Message);
                MessageBox.Instance.Show("Failed to create MIDI playback: " + ex.Message + "<br>Audio playback Failed.", "Error", null);
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
        /*if (bassInitialized)
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
        }*/
        PlayAllStems();
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

    public IEnumerator PlayPooledPreviewAudio(AudioClip audioClip, float startPoint = 0, float volume = 1.0f)
    {
        if (audioClip != null)
        {
            previewAudioStream.clip = audioClip;
            previewAudioStream.time = startPoint / 1000;
            previewAudioStream.loop = true;
            previewAudioStream.Play();
            previewAudioPlaying = true;
            yield return StartCoroutine(FadeInCoroutine(volume));
            yield return null;
        }
    }
    public IEnumerator StopPreviewAudio(float fadeFromVol)
    {
        if (previewAudioStream != null)
        {
            yield return StartCoroutine(FadeOutCoroutine(fadeFromVol));
            yield return new WaitForSeconds(1.0f); // coroutines dont give execution length
            previewAudioStream.Stop();
            previewAudioStream.clip = null;
            previewAudioStream.time = 0;
            previewAudioPlaying = false;
            yield return null;
        }
    }
    IEnumerator FadeOutCoroutine(float lerpFromVol)
    {
        float duration = 1f; // Duration for the fill animation
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fillAmount = Mathf.Lerp(lerpFromVol, 0f, elapsed / duration);
            if (previewAudioStream != null)
            {
                previewAudioStream.volume = fillAmount;
            }
            yield return null;
        }
    }
    IEnumerator FadeInCoroutine(float lerpToVol)
    {
        float duration = 1f; // Duration for the fill animation
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fillAmount = Mathf.Lerp(0f, lerpToVol, elapsed / duration);
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

        if (isPaused)
        {
            double stemResumeTime = Math.Max(0.0, pausedElapsedDsp);
            foreach (var pair in stemChannels)
            {
                if (pair.Value == null || pair.Value.handle == 0) continue;
                try
                {
                    Bass.ChannelSetPosition(pair.Value.handle, Bass.ChannelSeconds2Bytes(pair.Value.handle, stemResumeTime));
                    Bass.ChannelPlay(pair.Value.handle);
                    foreach (var childHandle in pair.Value.childHandles)
                    {
                        if (childHandle != 0)
                        {
                            Bass.ChannelSetPosition(childHandle, Bass.ChannelSeconds2Bytes(childHandle, stemResumeTime));
                            Bass.ChannelPlay(childHandle);
                        }
                    }
                    pair.Value.isPlaying = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to resume stem '" + pair.Key + "' from paused time: " + ex.Message);
                }
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
    public void MuteAllAudio(bool toggle)
    {
        if (toggle)
        {
            if (bassInitialized)
            {
                if (songStreamHandle != 0)
                {
                    try { Bass.ChannelSetAttribute(songStreamHandle, ChannelAttribute.Volume, 0.0); } catch { }
                }
            }
            if (previewAudioPlaying)
            {
                previewAudioStream.volume = 0f;
            }
        }
        else
        {
            if (bassInitialized)
            {
                if (songStreamHandle != 0)
                {
                    try { Bass.ChannelSetAttribute(songStreamHandle, ChannelAttribute.Volume, 1.0); } catch { }
                }
            }
            if (previewAudioPlaying)
            {
                previewAudioStream.volume = 1f;
            }
        }
    }

    public void SetMusicVolume(double amount)
    {
        if (bassInitialized)
        {
            if (songStreamHandle != 0)
            {
                try { Bass.ChannelSetAttribute(songStreamHandle, ChannelAttribute.Volume, amount); } catch { }
            }
        }
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
                midiPlayback.Stop();
                midiPlayback.MoveToTime(new MetricTimeSpan(TimeSpan.FromSeconds(time)));
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
                //RestartAt(GetElapsedTimeDsp());
            }
        }

        //SetMusicVolume(musicVolume);
        SetAllStemVolume((float)musicVolume);
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
            //currentTime = GetElapsedTime();
            songLength = noteSpawner.songLengthInTicks / 1000;
            
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
    // Unloads song resources and plays "You Rock" animation if player successfully cleared the song
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
        //stopAudio(false);
        StopAllStems(false);
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
        
        VenueAnimationPlayer.Instance.Unload();
        VenueAnimationPlayer.Instance.TryToggleHighwayCam(false);
        Debug.Log("[MusicPlayer.EndSong] Song unloaded.");
        if (songCleared) StartCoroutine(gameManager.PlayerRocksAnim());
        yield return null;
    }
    // returns non-DSP elapsed time
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
    // returns song audio length
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
    // unused, returns non-stem song peak amplitude
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
    // unused, returns sample data
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
    #region Stem API
    private string GetSupportedStemFilePath(string songFolderPath, string stemName)
    {
        if (string.IsNullOrEmpty(songFolderPath) || !Directory.Exists(songFolderPath))
        {
            return null;
        }

        var loader = SongFolderLoader.Instance;
        var supportedExtensions = loader != null && loader.supportedFormats != null && loader.supportedFormats.Count > 0
            ? loader.supportedFormats
            : new List<string> { "wav", "ogg", "mp3", "opus", "mid", "midi" };

        foreach (var extension in supportedExtensions)
        {
            var candidatePath = Path.Combine(songFolderPath, stemName + "." + extension.TrimStart('.'));
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        foreach (var file in Directory.GetFiles(songFolderPath))
        {
            var fileStem = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
            if (string.Equals(fileStem, stemName, StringComparison.OrdinalIgnoreCase)
                && supportedExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)))
            {
                return file;
            }
        }

        return null;
    }

    private List<string> GetStemFilesByName(string songFolderPath, string stemName)
    {
        if (string.IsNullOrEmpty(songFolderPath) || !Directory.Exists(songFolderPath))
        {
            return new List<string>();
        }

        var loader = SongFolderLoader.Instance;
        var supportedExtensions = loader != null && loader.supportedFormats != null && loader.supportedFormats.Count > 0
            ? loader.supportedFormats
            : new List<string> { "wav", "ogg", "mp3", "opus", "mid", "midi" };

        var matchingFiles = Directory.GetFiles(songFolderPath)
            .Where(file =>
            {
                var fileStem = Path.GetFileNameWithoutExtension(file);
                var extension = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
                return supportedExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase))
                    && string.Equals(fileStem, stemName, StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(file => file)
            .ToList();

        if (matchingFiles.Count > 0)
        {
            return matchingFiles;
        }

        if (string.Equals(stemName, "drums", StringComparison.OrdinalIgnoreCase))
        {
            return Directory.GetFiles(songFolderPath)
                .Where(file =>
                {
                    var fileStem = Path.GetFileNameWithoutExtension(file);
                    var extension = Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
                    return supportedExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase))
                        && fileStem.StartsWith("drums_", StringComparison.OrdinalIgnoreCase)
                        && fileStem.Length > "drums_".Length;
                })
                .OrderBy(file => file)
                .ToList();
        }

        return new List<string>();
    }

    public void LoadAvailableStemsInPath(string songFolderPath)
    {
        if (string.IsNullOrEmpty(songFolderPath) || !Directory.Exists(songFolderPath))
        {
            Debug.LogError("[MusicPlayer.LoadAvailableStemsInPath] Invalid song folder path: " + songFolderPath);
            return;
        }

        string[] stemNames = { "song", "guitar", "rhythm", "bass", "keys", "drums", "vocals" };
        foreach (var stemName in stemNames)
        {
            if (string.Equals(stemName, "drums", StringComparison.OrdinalIgnoreCase))
            {
                var drumFiles = GetStemFilesByName(songFolderPath, stemName);
                if (drumFiles.Count > 1)
                {
                    if (LoadSplitStemAudio(stemName, drumFiles) != null)
                    {
                        Debug.Log("[MusicPlayer.LoadAvailableStemsInPath] Loaded mixed-down drums audio from " + drumFiles.Count + " split files.");
                    }
                    continue;
                }
            }

            string stemPath = GetSupportedStemFilePath(songFolderPath, stemName);
            if (string.IsNullOrEmpty(stemPath))
            {
                continue;
            }

            if (LoadStemAudio(stemName, stemPath) != null)
            {
                Debug.Log("[MusicPlayer.LoadAvailableStemsInPath] Loaded " + stemName + " audio from " + Path.GetFileName(stemPath) + ".");
            }
        }
    }

    private AudioStem LoadSplitStemAudio(string stemName, List<string> drumFiles)
    {
        if (drumFiles == null || drumFiles.Count == 0)
        {
            return null;
        }

        if (stemChannels.TryGetValue(stemName, out var existingStem) && existingStem.handle != 0)
        {
            StopStem(stemName, true);
        }

        var primaryStem = LoadStemAudio(stemName, drumFiles[0]);
        if (primaryStem == null)
        {
            return null;
        }

        primaryStem.childHandles.Clear();
        for (int i = 1; i < drumFiles.Count; i++)
        {
            int childHandle = Bass.CreateStream(drumFiles[i], 0, 0, BassFlags.Prescan);
            if (childHandle == 0)
            {
                Debug.LogWarning("LoadSplitStemAudio: failed to create split drums handle for '" + drumFiles[i] + "': " + Bass.LastError);
                continue;
            }

            Bass.ChannelSetAttribute(childHandle, ChannelAttribute.Volume, 1f);
            primaryStem.childHandles.Add(childHandle);
        }

        return primaryStem;
    }

    public AudioStem LoadStemAudio(string stemName, string audioClipPath, float volume = 1f, bool autoplay = false)
    {
        if (string.IsNullOrWhiteSpace(stemName))
        {
            Debug.LogError("LoadStemAudio: stemName is required.");
            return null;
        }
        if (string.IsNullOrEmpty(audioClipPath))
        {
            Debug.LogError("LoadStemAudio: audioClipPath is required for stem '" + stemName + "'.");
            return null;
        }

        if (!bassInitialized) InitBASS();
        if (!bassInitialized) return null;

        if (stemChannels.TryGetValue(stemName, out var existingStem) && existingStem.handle != 0)
        {
            StopStem(stemName, true);
        }

        int stemHandle = Bass.CreateStream(audioClipPath, 0, 0, BassFlags.Prescan);
        if (stemHandle == 0)
        {
            Debug.LogError("LoadStemAudio: failed to create stem handle for '" + stemName + "': " + Bass.LastError);
            return null;
        }

        Bass.ChannelSetAttribute(stemHandle, ChannelAttribute.Volume, Mathf.Clamp01(volume));

        var stem = new AudioStem
        {
            name = stemName,
            handle = stemHandle,
            volume = Mathf.Clamp01(volume),
            isPlaying = autoplay,
            reverbEnabled = false,
            reverbFxHandle = 0,
            reverbDspHandle = 0
        };

        stemChannels[stemName] = stem;

        if (autoplay)
        {
            Bass.ChannelPlay(stemHandle);
        }

        return stem;
    }

    private void ResumeStemChannel(int handle, double resumeSeconds)
    {
        if (handle == 0) return;

        try
        {
            if (isPaused && resumeSeconds >= 0.0)
            {
                long pos = Bass.ChannelSeconds2Bytes(handle, resumeSeconds);
                Bass.ChannelSetPosition(handle, pos);
            }

            Bass.ChannelPlay(handle);
        }
        catch (Exception ex)
        {
            Debug.LogError("ResumeStemChannel failed for handle '" + handle + "': " + ex.Message);
        }
    }

    public void PlayStem(string stemName)
    {
        if (stemChannels.TryGetValue(stemName, out var stem) && stem.handle != 0)
        {
            try
            {
                double resumeSeconds = isPaused ? Math.Max(0.0, pausedElapsedDsp) : -1.0;
                ResumeStemChannel(stem.handle, resumeSeconds);
                foreach (var childHandle in stem.childHandles)
                {
                    if (childHandle != 0)
                    {
                        ResumeStemChannel(childHandle, resumeSeconds);
                    }
                }
                stem.isPlaying = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("PlayStem failed for '" + stemName + "': " + ex.Message);
            }
        }
    }


    public void PauseStem(string stemName)
    {
        if (stemChannels.TryGetValue(stemName, out var stem) && stem.handle != 0)
        {
            pausedElapsedDsp = GetElapsedTimeDsp();
            isPaused = true;
            try
            {
                Bass.ChannelPause(stem.handle);
                foreach (var childHandle in stem.childHandles)
                {
                    if (childHandle != 0)
                    {
                        Bass.ChannelPause(childHandle);
                    }
                }
                stem.isPlaying = false;
            }
            catch (Exception ex)
            {
                Debug.LogError("PauseStem failed for '" + stemName + "': " + ex.Message);
            }
        }
    }

    public void PlayAllStems()
    {
        double resumeSeconds = isPaused ? Math.Max(0.0, pausedElapsedDsp) : -1.0;

        foreach (var pair in stemChannels)
        {
            if (pair.Value == null || pair.Value.handle == 0) continue;
            try
            {
                ResumeStemChannel(pair.Value.handle, resumeSeconds);
                foreach (var childHandle in pair.Value.childHandles)
                {
                    if (childHandle != 0)
                    {
                        ResumeStemChannel(childHandle, resumeSeconds);
                    }
                }
                pair.Value.isPlaying = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("PlayAllStems failed for '" + pair.Key + "': " + ex.Message);
            }
        }
    }

    public void PauseAllStems()
    {
        pausedElapsedDsp = GetElapsedTimeDsp();
        isPaused = true;

        foreach (var pair in stemChannels)
        {
            if (pair.Value == null || pair.Value.handle == 0) continue;
            try
            {
                Bass.ChannelPause(pair.Value.handle);
                foreach (var childHandle in pair.Value.childHandles)
                {
                    if (childHandle != 0)
                    {
                        Bass.ChannelPause(childHandle);
                    }
                }
                pair.Value.isPlaying = false;
            }
            catch (Exception ex)
            {
                Debug.LogError("PauseAllStems failed for '" + pair.Key + "': " + ex.Message);
            }
        }
    }

    public void StopAllStems(bool freeStems = true)
    {
        foreach (var pair in new List<KeyValuePair<string, AudioStem>>(stemChannels))
        {
            if (pair.Value == null || pair.Value.handle == 0) continue;
            try
            {
                ToggleReverb(false, pair.Value.handle);
                foreach (var childHandle in pair.Value.childHandles)
                {
                    if (childHandle != 0)
                    {
                        ToggleReverb(false, childHandle);
                        Bass.ChannelStop(childHandle);
                        if (freeStems)
                        {
                            Bass.StreamFree(childHandle);
                        }
                    }
                }
                Bass.ChannelStop(pair.Value.handle);
                if (freeStems)
                {
                    Bass.StreamFree(pair.Value.handle);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("StopAllStems failed for '" + pair.Key + "': " + ex.Message);
            }
            finally
            {
                pair.Value.isPlaying = false;
                pair.Value.childHandles.Clear();
                if (freeStems)
                {
                    pair.Value.handle = 0;
                }
            }
        }
    }

    public void StopStem(string stemName, bool freeStem = true)
    {
        if (stemChannels.TryGetValue(stemName, out var stem) && stem.handle != 0)
        {
            try
            {
                ToggleReverb(false, stem.handle);
                Bass.ChannelStop(stem.handle);
                foreach (var childHandle in stem.childHandles)
                {
                    if (childHandle != 0)
                    {
                        ToggleReverb(false, childHandle);
                        Bass.ChannelStop(childHandle);
                        if (freeStem)
                        {
                            Bass.StreamFree(childHandle);
                        }
                    }
                }
                if (freeStem)
                {
                    Bass.StreamFree(stem.handle);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("StopStem failed for '" + stemName + "': " + ex.Message);
            }
            finally
            {
                stem.isPlaying = false;
                stem.childHandles.Clear();
                stem.handle = 0;
                stemChannels.Remove(stemName);
            }
        }
    }

    public void SetStemVolume(string stemName, float volume)
    {
        if (stemChannels.TryGetValue(stemName, out var stem) && stem.handle != 0)
        {
            var clamped = Mathf.Clamp01(volume);
            stem.volume = clamped;
            try { Bass.ChannelSetAttribute(stem.handle, ChannelAttribute.Volume, clamped); } catch { }
            foreach (var childHandle in stem.childHandles)
            {
                if (childHandle != 0)
                {
                    try { Bass.ChannelSetAttribute(childHandle, ChannelAttribute.Volume, clamped); } catch { }
                }
            }
        }
    }

    public void SetAllStemVolume(float volume)
    {
        var clamped = Mathf.Clamp01(volume);
        foreach (var pair in stemChannels)
        {
            if (pair.Value == null || pair.Value.handle == 0) continue;
            pair.Value.volume = clamped;
            try { Bass.ChannelSetAttribute(pair.Value.handle, ChannelAttribute.Volume, clamped); } catch { }
            foreach (var childHandle in pair.Value.childHandles)
            {
                if (childHandle != 0)
                {
                    try { Bass.ChannelSetAttribute(childHandle, ChannelAttribute.Volume, clamped); } catch { }
                }
            }
        }
    }

    public void ToggleStemReverb(string stemName, bool on)
    {
        if (stemChannels.TryGetValue(stemName, out var stem))
        {
            ToggleReverb(on, stem.handle);
            foreach (var childHandle in stem.childHandles)
            {
                if (childHandle != 0)
                {
                    ToggleReverb(on, childHandle);
                }
            }
            stem.reverbEnabled = on;
        }
    }

    public int GetStemHandle(string stemName)
    {
        return stemChannels.TryGetValue(stemName, out var stem) ? stem.handle : 0;
    }
    #endregion

    // starpower reverb (per handle, so stems can be affected independently from the main song stream)
    public void ToggleReverb(bool on, int handleToAffect)
    {
        if (bassInitialized && handleToAffect != 0)
        {
            if (on)
            {
                try
                {
                    if (stemReverbFxByHandle.TryGetValue(handleToAffect, out var existingFx) && existingFx != 0)
                    {
                        return;
                    }

                    int fxHandle = Bass.ChannelSetFX(handleToAffect, EffectType.Echo, 0);
                    if (fxHandle == 0)
                    {
                        var lastErr = Bass.LastError;
                        Debug.LogError($"ChannelSetFX failed: {lastErr} ({(int)lastErr}). This usually means the effect type/format isn't supported for that channel.");
                        try
                        {
                            ChannelInfo info;
                            Bass.ChannelGetInfo(handleToAffect, out info);
                            Debug.Log($"Channel info - Freq: {info.Frequency}, Chans: {info.Channels}, Flags: {info.Flags}, Resolution: {info.Resolution}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("Failed to retrieve channel info: " + ex.Message);
                        }
                        EnableReverbDspFallback(handleToAffect);
                    }
                    else
                    {
                        stemReverbFxByHandle[handleToAffect] = fxHandle;
                        Debug.Log(fxHandle + " Added reverb/echo to handle " + handleToAffect + ". LastError: " + Bass.LastError);
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
                    if (stemReverbFxByHandle.TryGetValue(handleToAffect, out var fxHandle) && fxHandle != 0)
                    {
                        Bass.ChannelRemoveFX(handleToAffect, fxHandle);
                        stemReverbFxByHandle.Remove(handleToAffect);
                    }
                    if (stemReverbDspFallbackByHandle.TryGetValue(handleToAffect, out var isFallback) && isFallback)
                    {
                        DisableReverbDspFallback(handleToAffect);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Failed to remove reverb FX: " + ex.Message);
                }
            }
        }
    }
    // DSP fallback for starpower reverb if BASS ChannelSetFX doesn't work (enables)
    private void EnableReverbDspFallback(int handleToAffect)
    {
        try
        {
            ChannelInfo info;
            if (!Bass.ChannelGetInfo(handleToAffect, out info))
            {
                Debug.LogWarning("EnableReverbDspFallback: Failed to get channel info.");
                return;
            }

            int dspChannels = Math.Max(1, info.Channels);
            int freq = Math.Max(4000, info.Frequency);
            int delayLen = Math.Max(64, (int)(freq * reverbFallbackDelaySeconds));

            var delayBuffers = new float[dspChannels][];
            for (int c = 0; c < dspChannels; c++)
            {
                delayBuffers[c] = new float[delayLen];
            }

            var channelOffset = new int[dspChannels];
            if (dspChannels >= 2)
            {
                int stereoOffset = Math.Max(1, (int)(freq * 0.006f));
                channelOffset[0] = 0;
                channelOffset[1] = stereoOffset;
                for (int c = 2; c < dspChannels; c++) channelOffset[c] = stereoOffset * (c % 2 == 0 ? -1 : 1);
            }
            else
            {
                channelOffset[0] = 0;
            }

            var dspProc = new ManagedBass.DSPProcedure((h, ch, buffer, length, user) =>
            {
                try
                {
                    bool floatDsp = Bass.FloatingPointDSP;
                    if (floatDsp)
                    {
                        int floats = length / 4;
                        if (floats <= 0) return;
                        var tmpFloat = new float[floats];
                        Marshal.Copy(buffer, tmpFloat, 0, floats);

                        int frames = floats / Math.Max(1, dspChannels);
                        for (int f = 0; f < frames; f++)
                        {
                            int idx = f * dspChannels;
                            for (int c = 0; c < dspChannels; c++)
                            {
                                int s = idx + c;
                                float inC = tmpFloat[s];
                                float delayed = 0f;
                                int readPos = (reverbFramePos + channelOffset[c]) % delayLen;
                                if (readPos < 0) readPos += delayLen;
                                if (delayBuffers[c].Length > 0)
                                {
                                    delayed = delayBuffers[c][readPos];
                                }
                                float wetAdd = delayed * reverbFallbackMix * reverbWetGain;
                                float outSample = inC + wetAdd;
                                outSample = Mathf.Clamp(outSample, -1f, 1f);
                                tmpFloat[s] = outSample;
                                delayBuffers[c][reverbFramePos] = inC + delayed * reverbFallbackFeedback;
                            }
                            reverbFramePos++;
                            if (reverbFramePos >= delayLen) reverbFramePos = 0;
                        }
                        Marshal.Copy(tmpFloat, 0, buffer, floats);
                    }
                    else
                    {
                        int shorts = length / 2;
                        if (shorts <= 0) return;
                        var tmpShort = new short[shorts];
                        Marshal.Copy(buffer, tmpShort, 0, shorts);

                        int frames = shorts / Math.Max(1, dspChannels);
                        for (int f = 0; f < frames; f++)
                        {
                            int idx = f * dspChannels;
                            for (int c = 0; c < dspChannels; c++)
                            {
                                int s = idx + c;
                                float inC = tmpShort[s] / 32768f;
                                float delayed = 0f;
                                int readPos = (reverbFramePos + channelOffset[c]) % delayLen;
                                if (readPos < 0) readPos += delayLen;
                                if (delayBuffers[c].Length > 0)
                                {
                                    delayed = delayBuffers[c][readPos];
                                }
                                float wetAdd = delayed * reverbFallbackMix * reverbWetGain;
                                float outSample = inC + wetAdd;
                                outSample = Mathf.Clamp(outSample, -1f, 1f);
                                int v = (int)Mathf.Clamp(outSample * 32767f, short.MinValue, short.MaxValue);
                                tmpShort[s] = (short)v;
                                delayBuffers[c][reverbFramePos] = inC + delayed * reverbFallbackFeedback;
                            }
                            reverbFramePos++;
                            if (reverbFramePos >= delayLen) reverbFramePos = 0;
                        }
                        Marshal.Copy(tmpShort, 0, buffer, shorts);
                    }
                }
                catch { }
            });

            int dspHandle = Bass.ChannelSetDSP(handleToAffect, dspProc, IntPtr.Zero, 0);
            if (dspHandle == 0)
            {
                Debug.LogError("EnableReverbDspFallback: ChannelSetDSP failed: " + Bass.LastError);
                stemReverbDspFallbackByHandle[handleToAffect] = false;
            }
            else
            {
                stemReverbDspFallbackByHandle[handleToAffect] = true;
                stemReverbDspByHandle[handleToAffect] = dspHandle;
                Debug.Log($"Enabled DSP reverb fallback (handle={dspHandle}) delaySamples={delayLen} channels={dspChannels}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("EnableReverbDspFallback exception: " + ex.Message);
        }
    }
    // DSP fallback for starpower reverb if BASS ChannelSetFX doesn't work (disables)
    private void DisableReverbDspFallback(int handleToAffect)
    {
        try
        {
            if (stemReverbDspByHandle.TryGetValue(handleToAffect, out var dspHandle) && dspHandle != 0)
            {
                Bass.ChannelRemoveDSP(handleToAffect, dspHandle);
                stemReverbDspByHandle.Remove(handleToAffect);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("DisableReverbDspFallback exception: " + ex.Message);
        }
        stemReverbDspFallbackByHandle[handleToAffect] = false;
    }

    // Simple mono delay-based reverb/echo DSP callback (applies same effect to all channels) (logic)
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