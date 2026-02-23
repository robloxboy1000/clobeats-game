using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class VenueAnimationPlayer : MonoBehaviour
{
    public static VenueAnimationPlayer Instance { get; private set; }
    public Camera mainCamera = null;
    public string cameraAnimationFile;
    public float currentTick = 0f;
    MusicPlayer musicPlayer;
    bool mainCameraFound = false;

    [System.Serializable]
    public class VecData
    {
        public float x;
        public float y;
        public float z;

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [System.Serializable]
    public class Keyframe
    {
        public float tick;
        public VecData position;
        public VecData rotation;
        public float focalLength;
    }

    [System.Serializable]
    public class KeyframeCollection
    {
        public Keyframe[] keyframes;
    }

    // parsed and sorted keyframes
    List<Keyframe> parsedKeyframes = new List<Keyframe>();

    public bool preMadeAnimMode = true;
    [System.Serializable]
    public class ClipCue
    {
        public float tick;
        public string clipName;

        // runtime
        [System.NonSerialized]
        public bool played = false;
    }

    [System.Serializable]
    public class ClipCueCollection { public ClipCue[] cues; }

    public List<ClipCue> clipCues = new List<ClipCue>();
    public GameObject clipTarget;
    public string cueFilePath;
    float lastTick = 0f;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        musicPlayer = FindAnyObjectByType<MusicPlayer>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (clipTarget == null && mainCamera != null) clipTarget = mainCamera.gameObject;
        // try auto-load cues if a path was provided
        if (!string.IsNullOrEmpty(cueFilePath))
        {
            string full = Path.GetFullPath(cueFilePath);
            if (File.Exists(full)) LoadCues(full);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        mainCameraFound = false;
    }

    public void Load()
    {
        try
        {
            if (cameraAnimationFile != null || cameraAnimationFile != "")
            {
                string path = Path.GetFullPath(cameraAnimationFile);
                if (File.Exists(path)) ReadFile(path);
            }
            else
            {
                return;
            }
        }
        catch
        {
            return;
        }
        
    }
    

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ReadFile(string file)
    {
        if (!File.Exists(file))
        {
            Debug.LogError("VenueAnimationPlayer: file not found: " + file);
            return;
        }

        string json = File.ReadAllText(file);

        // Unity's JsonUtility expects a wrapper object for arrays. The JSON must look like:
        // { "keyframes": [ { "tick": 0, "position": {"x":0,"y":0,"z":0}, "rotation": {"x":0,"y":0,"z":0} }, ... ] }
        try
        {
            KeyframeCollection col = JsonUtility.FromJson<KeyframeCollection>(json);
            if (col != null && col.keyframes != null && col.keyframes.Length > 0)
            {
                parsedKeyframes = col.keyframes.ToList();
                parsedKeyframes = parsedKeyframes.OrderBy(k => k.tick).ToList();
                Debug.Log("VenueAnimationPlayer: loaded " + parsedKeyframes.Count + " keyframes from " + file);
                return;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("VenueAnimationPlayer: JsonUtility parsing failed: " + ex.Message);
        }

        Debug.LogError("VenueAnimationPlayer: Failed to parse keyframes. Make sure JSON uses the shape:\n{ \"keyframes\": [ { \"tick\": 0, \"position\": {\"x\":0,\"y\":0,\"z\":0}, \"rotation\": {\"x\":0,\"y\":0,\"z\":0} } ] }");
    }

    public void LoadCues(string file)
    {
        if (!File.Exists(file))
        {
            Debug.LogError("VenueAnimationPlayer: cue file not found: " + file);
            return;
        }
        string json = File.ReadAllText(file);
        try
        {
            ClipCueCollection col = JsonUtility.FromJson<ClipCueCollection>(json);
            if (col != null && col.cues != null)
            {
                clipCues = col.cues.ToList();
                clipCues = clipCues.OrderBy(c => c.tick).ToList();
                // reset runtime flags
                foreach (var c in clipCues) c.played = false;
                Debug.Log("VenueAnimationPlayer: loaded " + clipCues.Count + " clip cues from " + file);
                return;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("VenueAnimationPlayer: cue parsing failed: " + ex.Message);
        }

        Debug.LogError("VenueAnimationPlayer: Failed to parse cues JSON. Expected { \"cues\": [ { \"tick\": 0, \"clipName\": \"name\" } ] }");
    }

    void Update()
    {
        if (musicPlayer == null) musicPlayer = FindAnyObjectByType<MusicPlayer>();
        if (!mainCameraFound)
        {
            if (Camera.main != null)
            {
                mainCamera = Camera.main;
                mainCameraFound = true;
            }
        }
        if (musicPlayer != null)
        {
            currentTick = musicPlayer.GetElapsedTime() * 1000f;
        }
        else
        {
            return;
        }

        // detect rewind/seek backwards and reset played flags
        if (currentTick < lastTick)
        {
            foreach (var c in clipCues) c.played = false;
        }

        if (parsedKeyframes == null || parsedKeyframes.Count == 0) return;

        if (preMadeAnimMode && clipCues != null && clipCues.Count > 0)
        {
            // play any cues that we've passed and haven't played yet
            foreach (var cue in clipCues)
            {
                if (!cue.played && currentTick >= cue.tick)
                {
                    PlayClipCue(cue);
                    cue.played = true;
                }
            }
            lastTick = currentTick;
            return;
        }

        // before first keyframe
        if (currentTick <= parsedKeyframes[0].tick)
        {
            ApplyKeyframe(parsedKeyframes[0]);
            return;
        }

        // after last keyframe
        if (currentTick >= parsedKeyframes[parsedKeyframes.Count - 1].tick)
        {
            ApplyKeyframe(parsedKeyframes[parsedKeyframes.Count - 1]);
            return;
        }

        // find surrounding keyframes
        Keyframe prev = parsedKeyframes[0];
        Keyframe next = parsedKeyframes[parsedKeyframes.Count - 1];
        for (int i = 0; i < parsedKeyframes.Count - 1; i++)
        {
            if (parsedKeyframes[i].tick <= currentTick && currentTick <= parsedKeyframes[i + 1].tick)
            {
                prev = parsedKeyframes[i];
                next = parsedKeyframes[i + 1];
                break;
            }
        }

        float span = next.tick - prev.tick;
        float t = span <= 0 ? 0f : Mathf.Clamp01((currentTick - prev.tick) / span);

        Vector3 posA = prev.position != null ? prev.position.ToVector3() : Vector3.zero;
        Vector3 posB = next.position != null ? next.position.ToVector3() : Vector3.zero;
        Vector3 rotA = prev.rotation != null ? prev.rotation.ToVector3() : Vector3.zero;
        Vector3 rotB = next.rotation != null ? next.rotation.ToVector3() : Vector3.zero;
        float focA = prev.focalLength;
        float focB = next.focalLength;

        Vector3 p = Vector3.Lerp(posA, posB, t);
        Quaternion r = Quaternion.Slerp(Quaternion.Euler(rotA), Quaternion.Euler(rotB), t);

        float fl = Mathf.Lerp(Mathf.Clamp(focA, 50, 200), Mathf.Clamp(focB, 50, 200), t);

        if (mainCamera != null)
        {
            mainCamera.transform.position = p;
            mainCamera.transform.rotation = r;
            mainCamera.focalLength = fl;
        }
    }

    void ApplyKeyframe(Keyframe k)
    {
        if (k == null) return;
        if (mainCamera == null) return;
        Vector3 p = k.position != null ? k.position.ToVector3() : Vector3.zero;
        Quaternion r = k.rotation != null ? Quaternion.Euler(k.rotation.ToVector3()) : Quaternion.identity;
        mainCamera.transform.position = p;
        mainCamera.transform.rotation = r;
    }

    void PlayClipCue(ClipCue cue)
    {
        if (cue == null || string.IsNullOrEmpty(cue.clipName)) return;
        if (clipTarget == null)
        {
            if (mainCamera != null) clipTarget = mainCamera.gameObject;
            else return;
        }

        // use legacy Animation component for simplicity: play clip by name
        var anim = clipTarget.GetComponent<Animation>();
        if (anim == null)
        {
            anim = clipTarget.AddComponent<Animation>();
            anim.playAutomatically = false;
        }

        // try to find clip on the Animation component
        AnimationClip clip = anim.GetClip(cue.clipName);
        if (clip == null)
        {
            // try to load from Resources (clip should be placed under a Resources folder)
            clip = Resources.Load<AnimationClip>(cue.clipName);
            if (clip != null)
            {
                anim.AddClip(clip, cue.clipName);
            }
        }

        if (clip != null)
        {
            anim.Stop();
            anim.Play(cue.clipName);
            Debug.Log("VenueAnimationPlayer: Playing clip cue '" + cue.clipName + "' at tick " + cue.tick);
        }
        else
        {
            Debug.LogWarning("VenueAnimationPlayer: Clip '" + cue.clipName + "' not found on target and not in Resources.");
        }
    }
}
