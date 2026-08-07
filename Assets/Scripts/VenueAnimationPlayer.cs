using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Text;
using System.Text.RegularExpressions;
using System;

public class VenueAnimationPlayer : MonoBehaviour
{
    public static VenueAnimationPlayer Instance { get; private set; }
    public enum AnimationType
    {
        JSON,
        MIDI
    }
    public AnimationType type = AnimationType.MIDI;

    public GameObject mainCamera = null;
    public string cameraAnimationFile;
    public float currentTick = -1f;
    public float currentRealtimeSecs = -1f;
    NoteSpawner ns;

    // --- Scripting support ---
    [System.Serializable]
    public class ScriptDef { public string name; public string body; }
    [System.Serializable]
    public class AnimDef { public AnimationClip clip; }

    [System.Serializable]
    public class ScriptEvent { public float tick; public string scriptName; public string[] args; [System.NonSerialized] public bool executed; }

    public List<ScriptDef> scripts = new List<ScriptDef>();
    public List<AnimationClip> preMadeAnims = new List<AnimationClip>();
    public List<ScriptEvent> scriptEvents = new List<ScriptEvent>();

    // function registry: name -> action(args)
    Dictionary<string, Action<List<string>>> fnRegistry = new Dictionary<string, Action<List<string>>>();
    float lastSecondForScripts = 0f;
    float lastTickForMIDICues = 0f;
    float lastTickForLyrics = 0f;

    [System.Serializable]
    public class ScriptBundle { public ScriptDef[] scripts; public ScriptEvent[] scriptEvents; }


    void Start()
    {
        ns = FindAnyObjectByType<NoteSpawner>();
    }

    public void TryToggleCamera(bool toggle)
    {
        try
        {
            if (mainCamera != null)
            {
                mainCamera.SetActive(toggle);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[VenueAnimationPlayer.TryToggleCamera] Unable to find venue camera: " + ex.Message);
        }
        
    }

    public void TryToggleHighwayCam(bool toggle)
    {
        try
        {
            GameObject camera = GameObject.Find("Highway_cam");
            if (camera)
            {
                camera.SetActive(toggle);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[VenueAnimationPlayer.TryToggleHighwayCam] Failed to toggle highway camera: " + ex.Message);
        }
    }
    public void TryCueCamAnim(GameObject camera, AnimationClip clip)
    {
        try
        {
            if (camera)
            {
                if (clip)
                {
                    Animation animation = camera.GetComponent<Animation>();
                    if (animation)
                    {
                        animation.clip = clip;
                        animation.Play(PlayMode.StopAll);
                    }
                    else
                    {
                        animation = camera.AddComponent<Animation>();
                        animation.clip = clip;
                        animation.Play(PlayMode.StopAll);
                    }
                }
            }

        }
        catch (Exception ex)
        {
            Debug.LogError("[VenueAnimationPlayer.TryCueCamAnim] Failed to cue camera animation: " + ex.Message);
        }
    }

    public GameObject TrySpawnNewCamera(int ID, AnimationClip animationClip, Vector3 position, Vector3 rotation, bool includeFrameLimit = true, int framerateLimit = 60)
    {
        try
        {
            GameObject cameraObj = new GameObject("VenueCamera_" + ID);
            Camera cam = cameraObj.AddComponent<Camera>();
            cameraObj.transform.position = position;
            cameraObj.transform.rotation = Quaternion.Euler(rotation);
            if (includeFrameLimit)
            {
                CameraEffects effects = cameraObj.AddComponent<CameraEffects>();
                effects.fps = framerateLimit;
            }
            if (animationClip)
            {
                Animation camAnim = cameraObj.AddComponent<Animation>();
                camAnim.clip = animationClip;
                camAnim.AddClip(animationClip, animationClip.name);
                camAnim.Play(animationClip.name);
            }
            return cameraObj;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[VenueAnimationPlayer.TrySpawnNewCamera] Failed to spawn camera: " + ex.Message);
            return null;
        }
    }

    public void TryDeleteSpawnedCamera(int ID)
    {
        try
        {
            GameObject cameraObj = GameObject.Find("VenueCamera_" + ID);
            if (cameraObj)
            {
                Destroy(cameraObj);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[VenueAnimationPlayer.TryDeleteSpawnedCamera] Failed to delete spawned camera: " + ex.Message);
        }
    }

    public void Load()
    {
        try
        {
            if (type == AnimationType.JSON && cameraAnimationFile != null || cameraAnimationFile != "")
            {
                string path = Path.GetFullPath(cameraAnimationFile);
                if (File.Exists(path))
                {
                    if (Path.GetFileNameWithoutExtension(path) == "scripts")
                    {
                        LoadScriptsFromFile(path);
                    }
                } 
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

    public void Unload()
    {
        try
        {
            foreach (var ev in scriptEvents) 
            {
                ev.executed = false;
            }
            scripts.Clear();
            scriptEvents.Clear();
            cameraAnimationFile = string.Empty;
        }
        catch
        {
            
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
        RegisterBuiltins();
    }


    

    // Load scripts and events from a JSON file (runtime)
    public void LoadScriptsFromFile(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("VenueAnimationPlayer: script file not found: " + path);
            return;
        }
        string json = File.ReadAllText(path);
        try
        {
            var bundle = JsonUtility.FromJson<ScriptBundle>(json);
            if (bundle != null)
            {
                scripts = bundle.scripts != null ? bundle.scripts.ToList() : new List<ScriptDef>();
                scriptEvents = bundle.scriptEvents != null ? bundle.scriptEvents.ToList() : new List<ScriptEvent>();
                // reset executed flags
                foreach (var ev in scriptEvents) ev.executed = false;
                Debug.Log("VenueAnimationPlayer: loaded " + scripts.Count + " scripts and " + scriptEvents.Count + " events from " + path);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("VenueAnimationPlayer: Failed to parse scripts file: " + ex.Message);
        }
    }

    
    void Update()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (SceneManager.GetSceneByBuildIndex(2).isLoaded)
        {
            if (!mainCamera)
            {
                mainCamera = TrySpawnNewCamera(Mathf.Abs(GetHashCode()), null, new Vector3(0,0,0), new Vector3(0,180,0),false);
            }
        }
        
        
        if (ns == null) ns = FindAnyObjectByType<NoteSpawner>();
        if (ns != null)
        {
            currentTick = ns.currentTick; // follow tempo map for MIDI venue cues
        }
        MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
        if (musicPlayer)
        {
            currentRealtimeSecs = (float)musicPlayer.currentTimeDSP;
        }

        // scripting: detect seek backwards and reset executed flags
        if (scriptEvents != null && scriptEvents.Count > 0)
        {
            if (currentRealtimeSecs < lastSecondForScripts)
            {
                foreach (var ev in scriptEvents) ev.executed = false;
            }

            foreach (var ev in scriptEvents)
            {
                if (!ev.executed && currentRealtimeSecs >= ev.tick)
                {
                    ExecuteScriptEvent(ev);
                    ev.executed = true;
                }
            }

            lastSecondForScripts = currentRealtimeSecs;
        }

        if (gameManager.currentSongVenueCueEvents != null && gameManager.currentSongVenueCueEvents.Count > 0)
        {
            if (currentTick < lastTickForMIDICues)
            {
                foreach (var ev in gameManager.currentSongVenueCueEvents) ev.passed = false;
            }

            foreach (var ev in gameManager.currentSongVenueCueEvents)
            {
                if (!ev.passed && currentTick >= ev.spawnTime)
                {
                    ExecuteScriptByString(ev.value);
                    ev.passed = true;
                }
            }

            lastTickForMIDICues = currentTick;
        }

        if (gameManager.currentSongLyrics != null && gameManager.currentSongLyrics.Count > 0)
        {
            if (currentTick < lastTickForLyrics)
            {
                foreach (var ev in gameManager.currentSongLyrics) ev.passed = false;
            }

            foreach (var ev in gameManager.currentSongLyrics)
            {
                if (!ev.passed && currentTick >= ev.spawnTick)
                {
                    if (ev.sungNote == 3520 || ev.value.StartsWith("[")) continue; // ignore "[play]" notes
                    //Debug.Log(ev.value + " (Freq=" + ev.sungNote + ", StartTick=" + ev.spawnTick +", EndTick=" + (ev.spawnTick + ev.length) + ")"); // log lyrics to console for now
                    Debug.Log("$beep," + ev.sungNote + "," + ev.lengthMs); // custom debug console synth
                    //NAudioBeepSynth.PlayToneNAudio(double.Parse(ev.sungNote), (int)ev.length);
                    ev.passed = true;
                }
            }

            lastTickForLyrics = currentTick;
        }

    }

    // --- Scripting runtime methods ---
    void RegisterBuiltins()
    {
        fnRegistry.Clear();

        fnRegistry["venue.camera.move"] = (args) =>
        {
            float x = ParseF(args, 0), y = ParseF(args, 1), z = ParseF(args, 2);
            if (mainCamera) mainCamera.transform.position = new Vector3(x, y, z);
        };

        fnRegistry["venue.camera.rotate"] = (args) =>
        {
            float x = ParseF(args, 0), y = ParseF(args, 1), z = ParseF(args, 2);
            if (mainCamera) mainCamera.transform.rotation = Quaternion.Euler(x, y, z);
        };

        fnRegistry["venue.camera.fov"] = (args) =>
        {
            float f = ParseF(args, 0);
            Camera cam = mainCamera.GetComponent<Camera>();
            cam.fieldOfView = f;
        };

        fnRegistry["venue.camera.cue"] = (args) =>
        {
            if (mainCamera)
            {
                AnimationClip clip = Resources.Load<AnimationClip>(args[0]);
                if (clip)
                {
                    TryCueCamAnim(mainCamera.gameObject, clip);
                }
            }
        };

        fnRegistry["venue.camera.toggle"] = (args) =>
        {
            bool tgle = bool.Parse(args[0]);
            if (mainCamera)
            {
                TryToggleCamera(tgle);
            }
        };

        fnRegistry["highway.camera.move"] = (args) =>
        {
            float x = ParseF(args, 0), y = ParseF(args, 1), z = ParseF(args, 2);
            GameObject camera = GameObject.Find("Highway_cam");
            if (camera) camera.transform.position = new Vector3(x, y, z);
        };

        fnRegistry["highway.camera.rotate"] = (args) =>
        {
            float x = ParseF(args, 0), y = ParseF(args, 1), z = ParseF(args, 2);
            GameObject camera = GameObject.Find("Highway_cam");
            if (camera) camera.transform.rotation = Quaternion.Euler(x, y, z);
        };

        fnRegistry["highway.camera.fov"] = (args) =>
        {
            float f = ParseF(args, 0);
            GameObject camera = GameObject.Find("Highway_cam");
            Camera camera1 = camera.GetComponent<Camera>();
            if (camera) camera1.fieldOfView = f;
        };

        fnRegistry["highway.camera.cue"] = (args) =>
        {
            GameObject camera = GameObject.Find("Highway_cam");
            if (camera)
            {
                AnimationClip clip = Resources.Load<AnimationClip>(args[0]);
                if (clip)
                {
                    TryCueCamAnim(camera.gameObject, clip);
                }
            }
        };

        fnRegistry["highway.camera.toggle"] = (args) =>
        {
            bool tgle = bool.Parse(args[0]);
            TryToggleHighwayCam(tgle);
        };

        fnRegistry["highway.fretboard.color"] = (args) =>
        {
            float colR = ParseF(args, 0);
            float colG = ParseF(args, 1);
            float colB = ParseF(args, 2);
            HighwayTextureChanger highwayTexture = FindAnyObjectByType<HighwayTextureChanger>();
            if (highwayTexture)
            {
                highwayTexture.TrySetSpriteColor(colR, colG, colB);
            }
        };

        fnRegistry["highway.main.opacity"] = (args) =>
        {
            float alpha = ParseF(args, 0);
            
            GuitarPlayer highwayTexture = FindAnyObjectByType<GuitarPlayer>();
            if (highwayTexture)
            {
                highwayTexture.SetOpacity(alpha);
            }
        };

        fnRegistry["spawn"] = (args) =>
        {
            if (args.Count == 0) return;
            var prefab = Resources.Load<GameObject>(args[0]);
            if (prefab == null) return;
            Vector3 pos = mainCamera ? mainCamera.transform.position + mainCamera.transform.forward * 5f : Vector3.zero;
            UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
        };

        fnRegistry["setActive"] = (args) =>
        {
            if (args.Count < 2) return;
            var go = GameObject.Find(args[0]);
            if (go != null) go.SetActive(bool.TryParse(args[1], out var b) && b);
        };

        fnRegistry["venue.lighting.stage.light4.center.default"] = (args) =>
        {
            if (args.Count == 0) return;
            float bright = ParseF(args, 0);
            float colR = ParseF(args, 1);
            float colG = ParseF(args, 2);
            float colB = ParseF(args, 3);
            LightingManager lightingManager = FindAnyObjectByType<LightingManager>();
            if (lightingManager)
            lightingManager.StageLight(0,
            bright,
            new Vector2(90, 0),
            new Color(colR, colG, colB));
        };
        fnRegistry["venue.lighting.stage.light3.center.default"] = (args) =>
        {
            if (args.Count == 0) return;
            float bright = ParseF(args, 0);
            float colR = ParseF(args, 1);
            float colG = ParseF(args, 2);
            float colB = ParseF(args, 3);
            LightingManager lightingManager = FindAnyObjectByType<LightingManager>();
            if (lightingManager)
            lightingManager.StageLight(1,
            bright,
            new Vector2(90, 0),
            new Color(colR, colG, colB));
        };
        fnRegistry["venue.lighting.stage.light2.center.default"] = (args) =>
        {
            if (args.Count == 0) return;
            float bright = ParseF(args, 0);
            float colR = ParseF(args, 1);
            float colG = ParseF(args, 2);
            float colB = ParseF(args, 3);
            LightingManager lightingManager = FindAnyObjectByType<LightingManager>();
            if (lightingManager)
            lightingManager.StageLight(2,
            bright,
            new Vector2(90, 0),
            new Color(colR, colG, colB));
        };
        fnRegistry["venue.lighting.stage.light1.center.default"] = (args) =>
        {
            if (args.Count == 0) return;
            float bright = ParseF(args, 0);
            float colR = ParseF(args, 1);
            float colG = ParseF(args, 2);
            float colB = ParseF(args, 3);
            LightingManager lightingManager = FindAnyObjectByType<LightingManager>();
            if (lightingManager)
            lightingManager.StageLight(3,
            bright,
            new Vector2(90, 0),
            new Color(colR, colG, colB));
        };

        fnRegistry["closeWithoutSaving"] = (args) =>
        {
            if (args.Count > 0) return;
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            StartCoroutine(musicPlayer.EndSong(false));
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            gameManager.ResetAllValues();
            gameManager.ExitGame(false);
        };


    }

    void ExecuteScriptEvent(ScriptEvent ev)
    {
        if (ev == null) return;
        // find script def by name
        var def = scripts.Find(s => s.name == ev.scriptName);
        if (def != null)
        {
            ExecuteScriptBody(def.body, ev.args);
        }
        else
        {
            // allow calling a single function name directly
            if (fnRegistry.TryGetValue(ev.scriptName, out var fn)) fn(ev.args != null ? ev.args.ToList() : new List<string>());
            else Debug.LogWarning("Script or function not found: " + ev.scriptName);
        }
    }
    void ExecuteScriptByString(string ev)
    {
        try
        {
            if (string.IsNullOrEmpty(ev)) return;
            // format: "function|arg1,arg2" or just "function"
            string funcName;
            string argsText = string.Empty;
            int sep = ev.IndexOf('|');
            if (sep >= 0)
            {
                funcName = ev.Substring(0, sep);
                if (sep + 1 < ev.Length) argsText = ev.Substring(sep + 1);
            }
            else
            {
                funcName = ev;
            }

            // parse arguments using the existing ParseArgs (handles quoted strings)
            List<string> argSplits = ParseArgs(argsText);

            // find script def by name
            var def = scripts.Find(s => s.name == funcName);
            if (def != null)
            {
                ExecuteScriptBody(def.body, argSplits.ToArray());
                return;
            }

            // allow calling a single function name directly
            if (fnRegistry.TryGetValue(funcName, out var fn)) fn(argSplits != null ? argSplits : new List<string>());
            else Debug.LogWarning("Script or function not found: " + funcName);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[VenueAnimationPlayer.ExecuteScriptByString] An error occured when parsing script string: " + ex.Message);
        }
    }
    void CueAnimEvent(NoteSpawner.GlobalEventInfo ev)
    {
        if (ev == null) return;
        Regex regex = new Regex("\\[[^\\]]*\\]", RegexOptions.IgnoreCase);
        var def = preMadeAnims.Find(s => s.name == regex.Match(ev.value).Value);
        if (def != null)
        {
            TryCueCamAnim(mainCamera.gameObject, def);
        }
        else
        {
            if (mainCamera)
            {
                AnimationClip clip = Resources.Load<AnimationClip>(regex.Match(ev.value).Value);
                if (clip)
                {
                    TryCueCamAnim(mainCamera.gameObject, clip);
                }
                else
                {
                    Debug.LogWarning("Animation not found (tick " + ev.spawnTime + ", " + ev.spawnTimeMs + " ms): " + regex.Match(ev.value).Value);
                }
            }
        }
    }

    // Public helper to execute a script or function immediately (editor/runtime)
    public void ExecuteScriptNow(string scriptName, string[] args = null)
    {
        if (string.IsNullOrEmpty(scriptName)) return;
        var def = scripts.Find(s => s.name == scriptName);
        if (def != null)
        {
            ExecuteScriptBody(def.body, args);
            return;
        }
        if (fnRegistry.TryGetValue(scriptName, out var fn))
        {
            fn(args != null ? args.ToList() : new List<string>());
            return;
        }
        Debug.LogWarning("ExecuteScriptNow: script or function not found: " + scriptName);
    }

    void ExecuteScriptBody(string bodyTemplate, string[] invocationArgs)
    {
        if (string.IsNullOrEmpty(bodyTemplate)) return;
        string body = bodyTemplate;
        if (invocationArgs != null)
        {
            for (int i = 0; i < invocationArgs.Length; i++)
                body = body.Replace("$" + i, invocationArgs[i]);
        }

        var statements = body.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var stmt in statements)
        {
            string s = stmt.Trim();
            if (string.IsNullOrEmpty(s)) continue;
            int p = s.IndexOf('(');
            if (p < 0) continue;
            string fname = s.Substring(0, p).Trim();
            int end = s.LastIndexOf(')');
            if (end < p) end = s.Length - 1;
            string argText = s.Substring(p + 1, end - (p + 1));
            var argList = ParseArgs(argText);
            if (fnRegistry.TryGetValue(fname, out var fn)) fn(argList);
            else Debug.LogWarning("Script function not found: " + fname);
        }
    }

    List<string> ParseArgs(string argText)
    {
        var res = new List<string>();
        if (string.IsNullOrEmpty(argText)) return res;
        int i = 0, n = argText.Length;
        var sb = new StringBuilder();
        bool inQuote = false;
        char quote = '"';
        for (; i < n; i++)
        {
            char c = argText[i];
            if (!inQuote && (c == '"' || c == '\'')) { inQuote = true; quote = c; continue; }
            if (inQuote)
            {
                if (c == quote) { inQuote = false; continue; }
                if (c == '\\' && i + 1 < n) { i++; sb.Append(argText[i]); continue; }
                sb.Append(c); continue;
            }
            if (c == ',') { res.Add(sb.ToString().Trim()); sb.Length = 0; continue; }
            sb.Append(c);
        }
        if (sb.Length > 0) res.Add(sb.ToString().Trim());
        return res;
    }

    float ParseF(List<string> args, int i)
    {
        if (args != null && args.Count > i && float.TryParse(args[i], out var v)) return v;
        return 0f;
    }
}
