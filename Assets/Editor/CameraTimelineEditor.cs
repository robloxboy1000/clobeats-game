using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CameraTimelineEditor : EditorWindow
{
    float scrubTick = 0f;
    bool isPlaying = false;
    double lastEditorTime = 0.0;

    // Scripting editor target
    VenueAnimationPlayer targetPlayer;
    Vector2 scriptsScroll = Vector2.zero;
    int interpStartIndex = -1;
    int interpEndIndex = -1;
    int interpStepMs = 1;
    bool interpReplaceExistingBetween = true;
    bool interpInterpolateArgs = true;

    [System.Serializable]
    class ScriptBundle { public VenueAnimationPlayer.ScriptDef[] scripts; public VenueAnimationPlayer.ScriptEvent[] scriptEvents; }

    

    [MenuItem("Window/CloBeats/Venue/Animation Timeline Editor")]
    public static void ShowWindow() { GetWindow<CameraTimelineEditor>("Animation Timeline"); }

    void OnGUI()
    {

        // Left column: main timeline/editor UI
        EditorGUILayout.BeginVertical();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scripts / Script Events", EditorStyles.boldLabel);
        targetPlayer = (VenueAnimationPlayer)EditorGUILayout.ObjectField("Target Player", targetPlayer, typeof(VenueAnimationPlayer), true);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find Player in Scene")) { targetPlayer = FindObjectOfType<VenueAnimationPlayer>(); }
        if (GUILayout.Button("Load Scripts")) { if (targetPlayer != null) LoadScriptsFromDialog(); }
        if (GUILayout.Button("Save Scripts")) { if (targetPlayer != null) SaveScriptsToDialog(); }
        EditorGUILayout.EndHorizontal();

        if (targetPlayer != null)
        {
            if (targetPlayer.scripts == null) targetPlayer.scripts = new List<VenueAnimationPlayer.ScriptDef>();
            if (targetPlayer.scriptEvents == null) targetPlayer.scriptEvents = new List<VenueAnimationPlayer.ScriptEvent>();

            scriptsScroll = EditorGUILayout.BeginScrollView(scriptsScroll);
            EditorGUILayout.LabelField("Script Definitions", EditorStyles.boldLabel);
            for (int si = 0; si < targetPlayer.scripts.Count; si++)
            {
                var sd = targetPlayer.scripts[si];
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                sd.name = EditorGUILayout.TextField("Name", sd.name);
                sd.body = EditorGUILayout.TextArea(sd.body, GUILayout.Height(60));
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(targetPlayer, "Edit Script"); EditorUtility.SetDirty(targetPlayer); }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("X", GUILayout.Width(24))) { Undo.RecordObject(targetPlayer, "Remove Script"); targetPlayer.scripts.RemoveAt(si); EditorUtility.SetDirty(targetPlayer); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Script")) { Undo.RecordObject(targetPlayer, "Add Script"); targetPlayer.scripts.Add(new VenueAnimationPlayer.ScriptDef { name = "newScript", body = "" }); EditorUtility.SetDirty(targetPlayer); }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Script Events (one-shot)", EditorStyles.boldLabel);
            for (int ei = 0; ei < targetPlayer.scriptEvents.Count; ei++)
            {
                var ev = targetPlayer.scriptEvents[ei];
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                ev.tick = EditorGUILayout.FloatField("Tick (ms)", ev.tick);
                ev.scriptName = EditorGUILayout.TextField("Script/Function", ev.scriptName);
                string argsCsv = EditorGUILayout.TextField("Args (comma)", ev.args != null ? string.Join(",", ev.args) : "");
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetPlayer, "Edit Script Event");
                    ev.args = string.IsNullOrEmpty(argsCsv) ? new string[0] : argsCsv.Split(',').Select(s => s.Trim()).ToArray();
                    EditorUtility.SetDirty(targetPlayer);
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play Now", GUILayout.Width(80))) { targetPlayer.ExecuteScriptNow(ev.scriptName, ev.args); }
                // Set Start / End buttons
                GUIStyle small = new GUIStyle(GUI.skin.button) { fixedWidth = 28 };
                if (interpStartIndex == ei) GUI.backgroundColor = Color.green; else GUI.backgroundColor = Color.white;
                if (GUILayout.Button("S", small)) { interpStartIndex = ei; }
                GUI.backgroundColor = Color.white;
                if (interpEndIndex == ei) GUI.backgroundColor = Color.yellow; else GUI.backgroundColor = Color.white;
                if (GUILayout.Button("E", small)) { interpEndIndex = ei; }
                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("X", GUILayout.Width(24))) { Undo.RecordObject(targetPlayer, "Remove Event"); targetPlayer.scriptEvents.RemoveAt(ei); EditorUtility.SetDirty(targetPlayer); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); continue; }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Event at Scrub")) { Undo.RecordObject(targetPlayer, "Add Script Event"); targetPlayer.scriptEvents.Add(new VenueAnimationPlayer.ScriptEvent { tick = scrubTick, scriptName = "", args = new string[0] }); EditorUtility.SetDirty(targetPlayer); }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Interpolate Events", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Start:", GUILayout.Width(40));
            EditorGUILayout.LabelField(interpStartIndex >= 0 && interpStartIndex < targetPlayer.scriptEvents.Count ? (interpStartIndex + ": " + targetPlayer.scriptEvents[interpStartIndex].scriptName) : "None");
            EditorGUILayout.LabelField("End:", GUILayout.Width(40));
            EditorGUILayout.LabelField(interpEndIndex >= 0 && interpEndIndex < targetPlayer.scriptEvents.Count ? (interpEndIndex + ": " + targetPlayer.scriptEvents[interpEndIndex].scriptName) : "None");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            interpStepMs = EditorGUILayout.IntField("Step (ms)", interpStepMs);
            interpReplaceExistingBetween = EditorGUILayout.ToggleLeft("Replace existing", interpReplaceExistingBetween, GUILayout.Width(140));
            interpInterpolateArgs = EditorGUILayout.ToggleLeft("Interpolate args", interpInterpolateArgs, GUILayout.Width(140));
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Interpolate Events Between Start/End")) { InterpolateEventsBetween(targetPlayer, interpStartIndex, interpEndIndex, Mathf.Max(1, interpStepMs), interpReplaceExistingBetween, interpInterpolateArgs); }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical(); // end left column

        
    }

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnEditorUpdate()
    {
        if (!isPlaying) return;
        double now = EditorApplication.timeSinceStartup;
        double dt = now - lastEditorTime;
        lastEditorTime = now;
    }

    void SaveScriptsToDialog()
    {
        if (targetPlayer == null) { Debug.LogWarning("No target player selected"); return; }
        var bundle = new ScriptBundle { scripts = targetPlayer.scripts != null ? targetPlayer.scripts.ToArray() : new VenueAnimationPlayer.ScriptDef[0], scriptEvents = targetPlayer.scriptEvents != null ? targetPlayer.scriptEvents.ToArray() : new VenueAnimationPlayer.ScriptEvent[0] };
        string json = JsonUtility.ToJson(bundle, true);
        string path = EditorUtility.SaveFilePanel("Save Scripts", "", "scripts.json", "json");
        if (string.IsNullOrEmpty(path)) return;
        File.WriteAllText(path, json);
        Debug.Log("Saved scripts to " + path);
    }

    void LoadScriptsFromDialog()
    {
        if (targetPlayer == null) { Debug.LogWarning("No target player selected"); return; }
        string path = EditorUtility.OpenFilePanel("Open Scripts JSON", "", "json");
        if (string.IsNullOrEmpty(path)) return;
        LoadScriptsFromFile(path);
    }

    void LoadScriptsFromFile(string path)
    {
        if (targetPlayer == null) return;
        string json = File.ReadAllText(path);
        try
        {
            var bundle = JsonUtility.FromJson<ScriptBundle>(json);
            Undo.RecordObject(targetPlayer, "Load Scripts");
            targetPlayer.scripts = bundle.scripts != null ? bundle.scripts.ToList() : new List<VenueAnimationPlayer.ScriptDef>();
            targetPlayer.scriptEvents = bundle.scriptEvents != null ? bundle.scriptEvents.ToList() : new List<VenueAnimationPlayer.ScriptEvent>();
            EditorUtility.SetDirty(targetPlayer);
            EditorSceneManager.MarkSceneDirty(targetPlayer.gameObject.scene);
            Repaint();
            Debug.Log("Loaded " + targetPlayer.scripts.Count + " scripts and " + targetPlayer.scriptEvents.Count + " events from " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to load scripts: " + ex.Message);
        }
    }

    void InterpolateEventsBetween(VenueAnimationPlayer player, int startIndex, int endIndex, int stepMs, bool replaceBetween, bool interpolateArgs)
    {
        if (player == null) { Debug.LogWarning("No player selected"); return; }
        if (startIndex < 0 || endIndex < 0 || startIndex >= player.scriptEvents.Count || endIndex >= player.scriptEvents.Count) { EditorUtility.DisplayDialog("Interpolate", "Please select valid Start and End events.", "OK"); return; }
        var evStart = player.scriptEvents[startIndex];
        var evEnd = player.scriptEvents[endIndex];
        if (evStart.scriptName != evEnd.scriptName) { EditorUtility.DisplayDialog("Interpolate", "Start and End events must have the same script/function name.", "OK"); return; }

        int sTick = Mathf.RoundToInt(Mathf.Min(evStart.tick, evEnd.tick));
        int eTick = Mathf.RoundToInt(Mathf.Max(evStart.tick, evEnd.tick));
        if (eTick <= sTick) { EditorUtility.DisplayDialog("Interpolate", "End tick must be greater than Start tick.", "OK"); return; }

        List<VenueAnimationPlayer.ScriptEvent> newEvents = new List<VenueAnimationPlayer.ScriptEvent>();
        string scriptName = evStart.scriptName;
        var aStart = evStart.args ?? new string[0];
        var aEnd = evEnd.args ?? new string[0];
        int maxArgs = Mathf.Max(aStart.Length, aEnd.Length);

        for (int t = sTick + stepMs; t < eTick; t += stepMs)
        {
            float f = (float)(t - sTick) / (float)(eTick - sTick);
            List<string> argsOut = new List<string>();
            for (int ai = 0; ai < maxArgs; ai++)
            {
                string sa = ai < aStart.Length ? aStart[ai] : (aStart.Length > 0 ? aStart[aStart.Length - 1] : "");
                string sb = ai < aEnd.Length ? aEnd[ai] : (aEnd.Length > 0 ? aEnd[aEnd.Length - 1] : "");
                if (interpolateArgs)
                {
                    if (float.TryParse(sa, NumberStyles.Float, CultureInfo.InvariantCulture, out float fa) && float.TryParse(sb, NumberStyles.Float, CultureInfo.InvariantCulture, out float fb))
                    {
                        float iv = Mathf.Lerp(fa, fb, f);
                        argsOut.Add(iv.ToString("G", CultureInfo.InvariantCulture));
                    }
                    else if (sa == sb) argsOut.Add(sa);
                    else argsOut.Add(sa);
                }
                else
                {
                    argsOut.Add(sa);
                }
            }

            var ne = new VenueAnimationPlayer.ScriptEvent { tick = t, scriptName = scriptName, args = argsOut.ToArray() };
            newEvents.Add(ne);
        }

        Undo.RecordObject(player, "Interpolate Script Events");
        if (replaceBetween)
        {
            player.scriptEvents.RemoveAll(ev => ev.tick > sTick && ev.tick < eTick && ev.scriptName == scriptName);
        }
        player.scriptEvents.AddRange(newEvents);
        player.scriptEvents = player.scriptEvents.OrderBy(ev => ev.tick).ToList();
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Repaint();
    }
}
