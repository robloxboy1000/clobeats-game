using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CameraTimelineEditor : EditorWindow
{
    [System.Serializable]
    public enum KeyframeType
    {
        Unknown = 0,
        Transform = 1,
        Sprite = 2,
        UIElement = 3,
        Model = 4
    }

    [System.Serializable]
    public class VecData 
    { 
        public float x; 
        public float y; 
        public float z; 
        public Vector3 ToV3() => new Vector3(x, y, z);
        public static VecData FromV3(Vector3 v) => new VecData { x = v.x, y = v.y, z = v.z }; 
    }

    [System.Serializable]
    public class Keyframe
    {
        public float tick;
        public VecData position;
        public VecData rotation;
        public float focalLength;
        public string trackId; // id of the track/target this keyframe affects
        public KeyframeType type = KeyframeType.Transform;
        public string assetPath; // optional asset/sprite/prefab path for non-transform keyframes
    }

    [System.Serializable]
    public class KeyframeCollection
    {
        public Keyframe[] keyframes;
    }

    Camera targetCamera;
    List<Keyframe> frames = new List<Keyframe>();
    
    // Tracks / hierarchy-like menu
    [System.Serializable]
    public class TrackItem { public string id; public string name; public UnityEngine.Object asset; public bool expanded = true; }
    List<TrackItem> tracks = new List<TrackItem>();
    int selectedTrackIndex = -1;
    
    Vector2 scroll;
    float scrubTick = 0f;
    public AudioClip audioClip;
    AudioSource previewAudioSource;
    bool isPlaying = false;
    double lastEditorTime = 0.0;
    public float ticksPerSecond = 1000f;
    public float animLength = 1000f;
    public float pixelsPerMs = 0.2f;
    Vector2 timelineScroll = Vector2.zero;
    Texture2D keyIcon = null;
    bool isDraggingKey = false;
    int draggingKeyIndex = -1;
    AudioClip transientPreviewClip = null;
    int previewChannels = 0;
    int previewFrequency = 0;

    RenderTexture cameraPreviewRT;
    public int previewWidth = 640;
    public int previewHeight = 360;
    public bool livePreview = true;
    

    [MenuItem("Window/CloBeats/Venue/Animation Timeline Editor")]
    public static void ShowWindow() { GetWindow<CameraTimelineEditor>("Animation Timeline"); }

    void OnGUI()
    {
        // split UI: left = timeline/editor, right = camera preview
        EditorGUILayout.BeginHorizontal();

        // Left column: main timeline/editor UI
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.58f));
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Load", GUILayout.Width(60))) Load();
        if (GUILayout.Button("Save", GUILayout.Width(60))) Save();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        ticksPerSecond = EditorGUILayout.FloatField("ms/sec", ticksPerSecond);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        animLength = EditorGUILayout.FloatField("Length (ms)", animLength);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Keyframe at Camera")) AddFromCamera();
        if (GUILayout.Button("Add Empty")) frames.Add(new Keyframe { tick = scrubTick, position = VecData.FromV3(Vector3.zero), rotation = VecData.FromV3(Vector3.zero) });
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        // Tracks / hierarchy-like menu
        EditorGUILayout.LabelField("Tracks", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected GameObject")) AddTrackFromSelection();
        if (GUILayout.Button("Add Prefab...", GUILayout.Width(120)))
        { 
            string path = EditorUtility.OpenFilePanel("Select Prefab", "", "prefab");
            if (!string.IsNullOrEmpty(path))
            {
                string rel = path.Contains("Assets") ? path.Substring(path.IndexOf("Assets")) : path;
                var go = (UnityEngine.GameObject)AssetDatabase.LoadAssetAtPath(rel, typeof(UnityEngine.GameObject));
                if (go != null) AddTrack(go);
            }
        }
        if (GUILayout.Button("Import FBX...", GUILayout.Width(120)))
        { 
            string path = EditorUtility.OpenFilePanel("Select Prefab", "", "fbx");
            if (!string.IsNullOrEmpty(path))
            {
                string rel = path.Contains("Assets") ? path.Substring(path.IndexOf("Assets")) : path;
                var go = (UnityEngine.GameObject)AssetDatabase.LoadAssetAtPath(rel, typeof(UnityEngine.GameObject));
                if (go != null) AddTrack(go);
            }
        }
        EditorGUILayout.EndHorizontal();
        for (int ti = 0; ti < tracks.Count; ti++)
        {
            var tr = tracks[ti];
            EditorGUILayout.BeginHorizontal("box");
            tr.expanded = EditorGUILayout.Foldout(tr.expanded, tr.name);
            if (GUILayout.Button("Select", GUILayout.Width(60))) { Selection.activeObject = tr.asset; }
            if (GUILayout.Button("X", GUILayout.Width(24))) { tracks.RemoveAt(ti); if (selectedTrackIndex == ti) selectedTrackIndex = -1; EditorGUILayout.EndHorizontal(); continue; }
            EditorGUILayout.EndHorizontal();
            if (tr.expanded)
            {
                EditorGUILayout.ObjectField("Asset", tr.asset, typeof(UnityEngine.Object), true);
            }
        }

        EditorGUILayout.Space();
        float minTick = frames.Count > 0 ? frames.Min(f => f.tick) : 0f;
        float maxTick = frames.Count > 0 ? frames.Max(f => f.tick) : animLength;

        

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

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(240));
        for (int i = 0; i < frames.Count; i++)
        {
            var k = frames[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            k.trackId = EditorGUILayout.TextField("Track ID", k.trackId);
            k.tick = EditorGUILayout.FloatField("Position in ms", k.tick);
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

        if (GUILayout.Button("Sort by ms value")) frames = frames.OrderBy(f => f.tick).ToList();

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

        EditorGUILayout.Space();
        

        EditorGUILayout.EndVertical(); // end right column
        EditorGUILayout.EndHorizontal(); // end main split

        // Timeline horizontal area
        float timelineHeight = 240f;
        float innerWidth = Mathf.Max(animLength * pixelsPerMs, position.width * 0.5f);
        Rect outerRect = GUILayoutUtility.GetRect(Mathf.Min(position.width * 0.58f - 20, innerWidth), timelineHeight, GUILayout.ExpandWidth(true));
        Rect innerRect = new Rect(0, 0, innerWidth, timelineHeight);
        timelineScroll = GUI.BeginScrollView(outerRect, timelineScroll, innerRect, true, false);
        // background
        EditorGUI.DrawRect(new Rect(0, 0, innerRect.width, innerRect.height), new Color(0.12f, 0.12f, 0.12f));

        // draw time grid
        float gridMs = 100f; // grid every 100ms
        for (float g = 0; g <= animLength; g += gridMs)
        {
            float gx = g * pixelsPerMs;
            EditorGUI.DrawRect(new Rect(gx, 0, 1, innerRect.height), new Color(0.18f, 0.18f, 0.18f));
        }

        // load key icon
        if (keyIcon == null) keyIcon = (Texture2D)Resources.Load("keyframe_icon");
        float rowHeight = 22f;
        int rows = Mathf.Max(1, tracks.Count);

        // draw track rows
        for (int r = 0; r < rows; r++)
        {
            EditorGUI.DrawRect(new Rect(0, r * rowHeight, innerRect.width, rowHeight), new Color(0, 0, 0, 0.06f));
            if (r < tracks.Count)
            {
                var tr = tracks[r];
                GUI.Label(new Rect(4, r * rowHeight + 2, 200, 18), tr.name, EditorStyles.label);
            }
        }

        // draw keyframes as icons
        for (int i = 0; i < frames.Count; i++)
        {
            var k = frames[i];
            int row = 0;
            if (!string.IsNullOrEmpty(k.trackId))
            {
                int idx = tracks.FindIndex(t => t.id == k.trackId);
                if (idx >= 0) row = idx;
            }
            float x = k.tick * pixelsPerMs;
            float iconSize = 14f;
            float y = row * rowHeight + (rowHeight - iconSize) * 0.5f;
            Rect iconRect = new Rect(x - iconSize * 0.5f, y, iconSize, iconSize);
            if (keyIcon != null)
            {
                if (GUI.Button(iconRect, new GUIContent(keyIcon), GUIStyle.none))
                {
                    scrubTick = k.tick;
                    ApplyTickToCamera(scrubTick);
                    PlayScrubPreview(scrubTick);
                    if (livePreview) RenderCameraPreview();
                    Repaint();
                }
            }
            else
            {
                if (GUI.Button(iconRect, "o", GUIStyle.none))
                {
                    scrubTick = k.tick;
                    ApplyTickToCamera(scrubTick);
                    PlayScrubPreview(scrubTick);
                    if (livePreview) RenderCameraPreview();
                    Repaint();
                }
            }

            // handle dragging
            Event e = Event.current;
            if (e.type == EventType.MouseDown && iconRect.Contains(e.mousePosition))
            {
                isDraggingKey = true;
                draggingKeyIndex = i;
                e.Use();
            }
            if (isDraggingKey && draggingKeyIndex == i && e.type == EventType.MouseDrag)
            {
                float newTick = (e.mousePosition.x + timelineScroll.x) / pixelsPerMs;
                frames[i].tick = Mathf.Clamp(newTick, 0f, animLength);
                Repaint();
                e.Use();
            }
            if (e.type == EventType.MouseUp && isDraggingKey && draggingKeyIndex == i)
            {
                isDraggingKey = false;
                draggingKeyIndex = -1;
                e.Use();
            }
        }

        GUI.EndScrollView();
        
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

    // Tracks helpers
    void AddTrackFromSelection()
    {
        if (Selection.activeGameObject == null) return;
        var go = Selection.activeGameObject;
        AddTrack(go);
    }

    void AddTrack(UnityEngine.Object asset)
    {
        if (asset == null) return;
        TrackItem ti = new TrackItem { id = System.Guid.NewGuid().ToString(), name = asset.name, asset = asset, expanded = true };
        tracks.Add(ti);
    }

    TrackItem FindTrackById(string id)
    {
        return tracks.Find(t => t.id == id);
    }

    // Apply a keyframe to a target asset (simple cases)
    void ApplyKeyframeToTarget(Keyframe k)
    {
        if (k == null) return;
        if (string.IsNullOrEmpty(k.trackId)) return;
        var tr = FindTrackById(k.trackId);
        if (tr == null) return;
        var obj = tr.asset as GameObject;
        if (obj == null) return;

        switch (k.type)
        {
            case KeyframeType.Transform:
                Undo.RecordObject(obj.transform, "Apply Keyframe");
                obj.transform.position = k.position != null ? k.position.ToV3() : obj.transform.position;
                obj.transform.rotation = Quaternion.Euler(k.rotation != null ? k.rotation.ToV3() : obj.transform.eulerAngles);
                EditorUtility.SetDirty(obj.transform);
                break;
            case KeyframeType.Sprite:
                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    var sp = Resources.Load<Sprite>(k.assetPath);
                    if (sp != null) { Undo.RecordObject(sr, "Apply Sprite Keyframe"); sr.sprite = sp; EditorUtility.SetDirty(sr); }
                }
                break;
            case KeyframeType.UIElement:
                var img = obj.GetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    var sp = Resources.Load<Sprite>(k.assetPath);
                    if (sp != null) { Undo.RecordObject(img, "Apply UI Sprite"); img.sprite = sp; EditorUtility.SetDirty(img); }
                }
                break;
            case KeyframeType.Model:
                // If a prefab was assigned, replace first child with prefab instance
                if (!string.IsNullOrEmpty(k.assetPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k.assetPath);
                    if (prefab != null)
                    {
                        // remove existing children
                        while (obj.transform.childCount > 0) { var c = obj.transform.GetChild(0); Undo.DestroyObjectImmediate(c.gameObject); }
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        if (inst != null) { Undo.RegisterCreatedObjectUndo(inst, "Instantiate Model"); inst.transform.SetParent(obj.transform, false); }
                    }
                }
                break;
        }
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
