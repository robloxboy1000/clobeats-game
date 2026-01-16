using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CameraTimelineEditor : EditorWindow
{
    [System.Serializable]
    public class VecData { public float x; public float y; public float z; public Vector3 ToV3() => new Vector3(x, y, z); public static VecData FromV3(Vector3 v) => new VecData { x = v.x, y = v.y, z = v.z }; }
    [System.Serializable]
    public class PostProcData {}

    [System.Serializable]
    public class Keyframe { public float tick; public VecData position; public VecData rotation; public float focalLength; public PostProcData postProc;}

    [System.Serializable]
    public class KeyframeCollection { public Keyframe[] keyframes; }

    Camera targetCamera;
    List<Keyframe> frames = new List<Keyframe>();
    Vector2 scroll;
    float scrubTick = 0f;
    public AudioClip audioClip;
    AudioSource previewAudioSource;
    bool isPlaying = false;
    double lastEditorTime = 0.0;
    public float ticksPerSecond = 1000f;
    AudioClip transientPreviewClip = null;
    int previewChannels = 0;
    int previewFrequency = 0;

    RenderTexture cameraPreviewRT;
    public int previewWidth = 640;
    public int previewHeight = 360;
    public bool livePreview = true;

    [MenuItem("Window/CloBeats/Venue/Camera Timeline Editor")]
    public static void ShowWindow() { GetWindow<CameraTimelineEditor>("Camera Timeline"); }

    void OnGUI()
    {
        // split UI: left = timeline/editor, right = camera preview
        EditorGUILayout.BeginHorizontal();

        // Left column: main timeline/editor UI
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.58f));
        EditorGUILayout.BeginHorizontal();
        targetCamera = (Camera)EditorGUILayout.ObjectField("Camera", targetCamera, typeof(Camera), true);
        if (GUILayout.Button("Use Scene Camera", GUILayout.Width(120))) { if (SceneView.lastActiveSceneView != null) targetCamera = SceneView.lastActiveSceneView.camera; Repaint(); }
        if (GUILayout.Button("Load", GUILayout.Width(60))) Load();
        if (GUILayout.Button("Save", GUILayout.Width(60))) Save();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        ticksPerSecond = EditorGUILayout.FloatField("Ticks/sec", ticksPerSecond);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Keyframe at Camera")) AddFromCamera();
        if (GUILayout.Button("Add Empty")) frames.Add(new Keyframe { tick = scrubTick, position = VecData.FromV3(Vector3.zero), rotation = VecData.FromV3(Vector3.zero) });
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        float minTick = frames.Count > 0 ? frames.Min(f => f.tick) : 0f;
        float maxTick = frames.Count > 0 ? frames.Max(f => f.tick) : 1000f;
        EditorGUI.BeginChangeCheck();
        scrubTick = EditorGUILayout.Slider("Scrub Tick", scrubTick, minTick, maxTick);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyTickToCamera(scrubTick);
            // play a tiny PCM preview at the scrub position
            PlayScrubPreview(scrubTick);
            if (livePreview) RenderCameraPreview();
            Repaint();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(isPlaying ? "Pause" : "Play", GUILayout.Width(80)))
        {
            if (isPlaying) PausePreview();
            else StartPreview(false);
        }
        if (GUILayout.Button("Stop", GUILayout.Width(60))) { StopPreview(); scrubTick = minTick; ApplyTickToCamera(scrubTick); }
        if (GUILayout.Button("Apply Scrub To Camera")) ApplyTickToCamera(scrubTick);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(340));
        for (int i = 0; i < frames.Count; i++)
        {
            var k = frames[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            k.tick = EditorGUILayout.FloatField("Tick", k.tick);
            if (GUILayout.Button("Set From Camera", GUILayout.Width(110))) SetKeyframeFromCamera(k);
            if (GUILayout.Button("Apply", GUILayout.Width(60))) ApplyKeyframeToCamera(k);
            if (GUILayout.Button("X", GUILayout.Width(24))) { frames.RemoveAt(i); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
            EditorGUILayout.EndHorizontal();

            Vector3 pos = EditorGUILayout.Vector3Field("Position", k.position != null ? k.position.ToV3() : Vector3.zero);
            Vector3 rot = EditorGUILayout.Vector3Field("Rotation", k.rotation != null ? k.rotation.ToV3() : Vector3.zero);
            k.position = VecData.FromV3(pos);
            k.rotation = VecData.FromV3(rot);
            k.focalLength = EditorGUILayout.FloatField("Zoom", k.focalLength);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Sort by Tick")) frames = frames.OrderBy(f => f.tick).ToList();

        EditorGUILayout.EndVertical(); // end left column

        // Right column: camera preview
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Camera Preview", EditorStyles.boldLabel);
        livePreview = EditorGUILayout.Toggle("Live Preview", livePreview);
        EditorGUILayout.BeginHorizontal();
        previewWidth = EditorGUILayout.IntField("Width", previewWidth);
        previewHeight = EditorGUILayout.IntField("Height", previewHeight);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Capture Now")) RenderCameraPreview();

        // allocate RT if needed
        EnsurePreviewRT();

        Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(true));
        if (cameraPreviewRT != null)
        {
            EditorGUI.DrawPreviewTexture(previewRect, cameraPreviewRT, null, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUI.DrawRect(previewRect, Color.black);
        }

        EditorGUILayout.EndVertical(); // end right column
        EditorGUILayout.EndHorizontal(); // end main split

        
    }

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        StopPreview();
        if (cameraPreviewRT != null)
        {
            cameraPreviewRT.Release();
            Object.DestroyImmediate(cameraPreviewRT);
            cameraPreviewRT = null;
        }
    }

    void StartPreview(bool scrubbing)
    {
        if (frames == null || frames.Count == 0) return;
        isPlaying = true;
        lastEditorTime = EditorApplication.timeSinceStartup;
        if (audioClip != null)
        {
            // create a hidden GameObject with AudioSource for precise preview control
            if (previewAudioSource == null)
            {
                GameObject go = new GameObject("CameraTimelineEditor_AudioPreview");
                go.hideFlags = HideFlags.HideAndDontSave;
                previewAudioSource = go.AddComponent<AudioSource>();
                previewAudioSource.playOnAwake = false;
            }
            previewAudioSource.clip = audioClip;
            previewAudioSource.time = scrubTick / ticksPerSecond;
            if (scrubbing)
            {
                return;
            }
            else
            {
                if (previewAudioSource.mute)
                {
                    previewAudioSource.mute = false;
                }
                previewAudioSource.Play();
            }
        }
    }

    void PausePreview()
    {
        isPlaying = false;
        if (previewAudioSource != null && previewAudioSource.isPlaying)
        {
            previewAudioSource.Pause();
        }
    }

    void StopPreview()
    {
        isPlaying = false;
        if (previewAudioSource != null)
        {
            if (previewAudioSource.isPlaying) previewAudioSource.Stop();
            var go = previewAudioSource.gameObject;
            previewAudioSource = null;
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    void OnEditorUpdate()
    {
        if (!isPlaying) return;
        double now = EditorApplication.timeSinceStartup;
        double dt = now - lastEditorTime;
        lastEditorTime = now;
        if (audioClip != null && previewAudioSource != null)
        {
            if (previewAudioSource.clip == null) return;
            if (!previewAudioSource.isPlaying)
            {
                // reached end
                StopPreview();
                return;
            }
            scrubTick = previewAudioSource.time * ticksPerSecond;
            ApplyTickToCamera(scrubTick);
            Repaint();
        }
        else
        {
            // no audio, advance scrub by dt
            scrubTick += (float)(dt * ticksPerSecond);
            ApplyTickToCamera(scrubTick);
            Repaint();
        }
    }

    void PlayScrubPreview(float tick)
    {
        if (audioClip == null) return;
        if (previewAudioSource == null)
        {
            GameObject go = new GameObject("CameraTimelineEditor_AudioPreview");
            go.hideFlags = HideFlags.HideAndDontSave;
            previewAudioSource = go.AddComponent<AudioSource>();
            previewAudioSource.playOnAwake = false;
            previewAudioSource.clip = null;
        }
        previewAudioSource.clip = null;

        int channels = audioClip.channels;
        int freq = audioClip.frequency;
        float tickSeconds = tick / ticksPerSecond;

        // desired preview duration: 1 tick, but clamp to a sensible min/max
        float duration = Mathf.Clamp(1f / ticksPerSecond, 0.01f, 0.2f);
        int frames = Mathf.Max(64, Mathf.CeilToInt(duration * freq));

        // ensure we don't read past end
        int startSample = Mathf.Clamp(Mathf.FloorToInt(tickSeconds * freq), 0, Mathf.Max(0, audioClip.samples - frames - 1));
        float[] data = new float[frames * channels];
        if (!audioClip.GetData(data, startSample)) return;

        // apply a short fade in/out to avoid clicks
        int fadeSamples = Mathf.Min(16, frames / 4);
        for (int i = 0; i < fadeSamples; i++)
        {
            float w = (i / (float)fadeSamples);
            float gainIn = 0.5f * (1f - Mathf.Cos(Mathf.PI * w));
            float gainOut = 0.5f * (1f - Mathf.Cos(Mathf.PI * (1f - (i / (float)fadeSamples))));
            float gain = Mathf.Min(gainIn, gainOut);
            for (int c = 0; c < channels; c++)
            {
                int idxIn = i * channels + c;
                int idxOut = (frames - 1 - i) * channels + c;
                data[idxIn] *= gainIn;
                data[idxOut] *= gainOut;
            }
        }

        // create or reuse transient preview clip
        if (transientPreviewClip == null || previewChannels != channels || previewFrequency != freq || transientPreviewClip.samples != frames)
        {
            if (transientPreviewClip != null) Object.DestroyImmediate(transientPreviewClip);
            transientPreviewClip = AudioClip.Create("_scrub_preview", frames, channels, freq, false);
            previewChannels = channels;
            previewFrequency = freq;
        }

        transientPreviewClip.SetData(data, 0);
        previewAudioSource.PlayOneShot(transientPreviewClip);
    }

    void EnsurePreviewRT()
    {
        if (previewWidth <= 0) previewWidth = 640;
        if (previewHeight <= 0) previewHeight = 360;
        if (cameraPreviewRT == null || cameraPreviewRT.width != previewWidth || cameraPreviewRT.height != previewHeight)
        {
            if (cameraPreviewRT != null)
            {
                cameraPreviewRT.Release();
                Object.DestroyImmediate(cameraPreviewRT);
            }
            cameraPreviewRT = new RenderTexture(previewWidth, previewHeight, 16, RenderTextureFormat.ARGB32);
            cameraPreviewRT.hideFlags = HideFlags.HideAndDontSave;
            cameraPreviewRT.Create();
        }
    }

    void RenderCameraPreview()
    {
        if (targetCamera == null) return;
        EnsurePreviewRT();
        var prevRT = targetCamera.targetTexture;
        try
        {
            targetCamera.targetTexture = cameraPreviewRT;
            targetCamera.Render();
        }
        finally
        {
            targetCamera.targetTexture = prevRT;
        }
        Repaint();
    }

    void AddFromCamera()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        frames.Add(new Keyframe { tick = scrubTick, position = VecData.FromV3(targetCamera.transform.position), rotation = VecData.FromV3(targetCamera.transform.eulerAngles), focalLength = targetCamera.focalLength });
    }

    void SetKeyframeFromCamera(Keyframe k)
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        k.position = VecData.FromV3(targetCamera.transform.position);
        k.rotation = VecData.FromV3(targetCamera.transform.eulerAngles);
        k.focalLength = targetCamera.focalLength;
    }

    void ApplyKeyframeToCamera(Keyframe k)
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        Undo.RecordObject(targetCamera.transform, "Apply keyframe");
        targetCamera.transform.position = k.position != null ? k.position.ToV3() : Vector3.zero;
        targetCamera.transform.eulerAngles = k.rotation != null ? k.rotation.ToV3() : Vector3.zero;
        targetCamera.focalLength = Mathf.Clamp(k.focalLength, 50, 200);
        EditorUtility.SetDirty(targetCamera.transform);
    }

    void ApplyTickToCamera(float tick)
    {
        if (frames == null || frames.Count == 0) return;
        var sorted = frames.OrderBy(f => f.tick).ToList();
        if (tick <= sorted[0].tick) { ApplyKeyframeToCamera(sorted[0]); return; }
        if (tick >= sorted[sorted.Count - 1].tick) { ApplyKeyframeToCamera(sorted[sorted.Count - 1]); return; }
        Keyframe prev = sorted[0], next = sorted[sorted.Count - 1];
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (sorted[i].tick <= tick && tick <= sorted[i + 1].tick) { prev = sorted[i]; next = sorted[i + 1]; break; }
        }
        float span = next.tick - prev.tick;
        float t = span <= 0f ? 0f : Mathf.Clamp01((tick - prev.tick) / span);
        Vector3 p = Vector3.Lerp(prev.position != null ? prev.position.ToV3() : Vector3.zero, next.position != null ? next.position.ToV3() : Vector3.zero, t);
        Quaternion r = Quaternion.Slerp(Quaternion.Euler(prev.rotation != null ? prev.rotation.ToV3() : Vector3.zero), Quaternion.Euler(next.rotation != null ? next.rotation.ToV3() : Vector3.zero), t);
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;
        Undo.RecordObject(targetCamera.transform, "Scrub camera");
        targetCamera.transform.position = p;
        targetCamera.transform.rotation = r;
        targetCamera.focalLength = Mathf.Clamp(prev.focalLength, 50, 200);
        EditorUtility.SetDirty(targetCamera.transform);
    }

    

    void Save()
    {
        var col = new KeyframeCollection { keyframes = frames.OrderBy(f => f.tick).ToArray() };
        string json = JsonUtility.ToJson(col, true);
        string path = EditorUtility.SaveFilePanel("Save Camera Timeline", "", "venueAnim.json", "json");
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllText(path, json);
        Debug.Log("Saved camera timeline to " + path);
    }

    void Load()
    {
        string path = EditorUtility.OpenFilePanel("Open Camera Timeline", "", "json");
        if (string.IsNullOrEmpty(path)) return;
        string json = File.ReadAllText(path);
        try
        {
            var col = JsonUtility.FromJson<KeyframeCollection>(json);
            frames = col != null && col.keyframes != null ? col.keyframes.ToList() : new List<Keyframe>();
            Repaint();
            Debug.Log("Loaded " + frames.Count + " keyframes from " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to load timeline: " + ex.Message);
        }
    }
}
