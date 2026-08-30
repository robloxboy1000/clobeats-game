using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    [Serializable]
    public class LightingCue
    {
        public List<LightingKeyframe> lightingSeq = new List<LightingKeyframe>();
        public string cueName;
    }
    [Serializable]
    public class LightingKeyframe
    {
        public int tick;
        public int lightIDToAffect; // -1 should affect all lights
        public bool useLightBar = false;
        public Vector2 lightPosition;
        public bool randomizePosition = false;
        public Color lightColor;
        public bool randomizeColor = false;
        public float lightIntensity;
        public bool interpolate = true;
        public bool loopStart = false;
        public bool loopEnd = false;
        public bool passed;
        [NonSerialized] public Color resolvedColor;
        [NonSerialized] public Vector2 resolvedPosition;
        [NonSerialized] public Color[] resolvedColors;
        [NonSerialized] public Vector2[] resolvedPositions;
    }
    public List<GameObject> stageLights = new List<GameObject>();
    public List<LightingCue> lightingCues = new List<LightingCue>();
    public GameObject lightBar;
    public int lightingTick = 0;
    public float lightingTicksPerSecond = 1000f;
    int activeCueId = 0;

    public IEnumerator PlayCue(string cueName)
    {
        int thisCueId = ++activeCueId;
        if (string.IsNullOrWhiteSpace(cueName))
        {
            Debug.LogWarning("LightingManager: Cannot play an unnamed lighting cue.");
            yield break;
        }

        LightingCue cueToPlay = lightingCues.Find(c => c != null && c.cueName == cueName);
        if (cueToPlay == null)
        {
            Debug.LogWarning("LightingManager: Lighting cue not found: " + cueName);
            yield break;
        }

        // Normalize the serialized sequence before playback so unsorted keyframes
        // are corrected for this and future plays of the cue.
        cueToPlay.lightingSeq.RemoveAll(keyframe => keyframe == null);
        cueToPlay.lightingSeq.Sort((a, b) => a.tick.CompareTo(b.tick));
        List<LightingKeyframe> keyframes = new List<LightingKeyframe>(cueToPlay.lightingSeq);

        if (keyframes.Count == 0)
        {
            lightingTick = 0;
            yield break;
        }

        // Every cue starts from its own zero-based clock.
        lightingTick = 0;

        foreach (LightingKeyframe keyframe in keyframes)
        {
            keyframe.passed = false;
            keyframe.resolvedColor = keyframe.randomizeColor
                ? UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f)
                : keyframe.lightColor;
            keyframe.resolvedPosition = keyframe.randomizePosition
                ? new Vector2(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f))
                : keyframe.lightPosition;
            ResolvePerLightValues(keyframe);
        }

        // Use the lights' current positions as the starting state for the cue.
        // The first cue keyframe can therefore transition from the live scene state.
        LightingKeyframe previous = CreateInitialPositionKeyframe(keyframes[0]);
        int loopStartIndex = keyframes.FindIndex(keyframe => keyframe.loopStart);
        int loopEndIndex = keyframes.FindIndex(keyframe => keyframe.loopEnd);
        bool hasLoop = loopStartIndex >= 0 && loopEndIndex > loopStartIndex;
        int keyframeIndex = 0;

        while (keyframeIndex < keyframes.Count)
        {
            if (thisCueId != activeCueId) yield break;
            LightingKeyframe keyframe = keyframes[keyframeIndex];
            if (keyframe.passed)
            {
                previous = keyframe;
                keyframeIndex++;
                continue;
            }

            bool canInterpolate = previous != null &&
                keyframe.interpolate &&
                previous.lightIDToAffect == keyframe.lightIDToAffect &&
                previous.useLightBar == keyframe.useLightBar &&
                keyframe.tick > previous.tick;

            if (canInterpolate)
            {
                while (lightingTick < keyframe.tick)
                {
                    float t = Mathf.Clamp01((lightingTick - previous.tick) / (float)(keyframe.tick - previous.tick));
                    ApplyInterpolatedLighting(previous, keyframe, t);
                    if (thisCueId != activeCueId) yield break;
                    yield return AdvanceLightingTick();
                }
            }
            else
            {
                // A non-interpolated keyframe applies immediately at its tick.
                while (lightingTick < keyframe.tick)
                {
                    if (thisCueId != activeCueId) yield break;
                    yield return AdvanceLightingTick();
                }
            }

            ApplyLightingKeyframe(keyframe);
            keyframe.passed = true;
            previous = keyframe;
            keyframeIndex++;

            if (hasLoop && keyframeIndex > loopEndIndex)
            {
                // Restart at the loop-start keyframe until another cue invalidates this one.
                lightingTick = keyframes[loopStartIndex].tick;
                for (int i = loopStartIndex; i <= loopEndIndex; i++)
                {
                    keyframes[i].passed = false;
                    keyframes[i].resolvedColor = keyframes[i].randomizeColor
                        ? UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f)
                        : keyframes[i].lightColor;
                    keyframes[i].resolvedPosition = keyframes[i].randomizePosition
                        ? new Vector2(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f))
                        : keyframes[i].lightPosition;
                    ResolvePerLightValues(keyframes[i]);
                }
                previous = null;
                keyframeIndex = loopStartIndex;
            }
        }

        // Keep the public clock at the end of the completed sequence.
        lightingTick = keyframes[keyframes.Count - 1].tick;
    }

    void ResolvePerLightValues(LightingKeyframe keyframe)
    {
        if (keyframe.lightIDToAffect < 0 && !keyframe.useLightBar)
        {
            keyframe.resolvedColors = new Color[stageLights.Count];
            keyframe.resolvedPositions = new Vector2[stageLights.Count];
            for (int lightId = 0; lightId < stageLights.Count; lightId++)
            {
                keyframe.resolvedColors[lightId] = keyframe.randomizeColor
                    ? UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f)
                    : keyframe.lightColor;
                keyframe.resolvedPositions[lightId] = keyframe.randomizePosition
                    ? new Vector2(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f))
                    : keyframe.lightPosition;
            }
        }
    }

    LightingKeyframe CreateInitialPositionKeyframe(LightingKeyframe firstKeyframe)
    {
        LightingKeyframe initial = new LightingKeyframe
        {
            tick = 0,
            lightIDToAffect = firstKeyframe.lightIDToAffect,
            useLightBar = firstKeyframe.useLightBar,
            lightPosition = firstKeyframe.lightPosition,
            resolvedPosition = firstKeyframe.resolvedPosition,
            resolvedColor = firstKeyframe.resolvedColor,
            lightColor = firstKeyframe.lightColor,
            lightIntensity = firstKeyframe.lightIntensity,
            passed = true
        };

        if (firstKeyframe.lightIDToAffect < 0 && !firstKeyframe.useLightBar)
        {
            initial.resolvedColors = new Color[stageLights.Count];
            initial.resolvedPositions = new Vector2[stageLights.Count];
            for (int lightId = 0; lightId < stageLights.Count; lightId++)
            {
                initial.resolvedColors[lightId] = firstKeyframe.resolvedColors[lightId];
                initial.resolvedPositions[lightId] = GetStageLightPosition(lightId);
            }
        }
        else if (!firstKeyframe.useLightBar && firstKeyframe.lightIDToAffect >= 0)
        {
            initial.resolvedPosition = GetStageLightPosition(firstKeyframe.lightIDToAffect);
        }

        return initial;
    }

    Vector2 GetStageLightPosition(int lightId)
    {
        if (lightId < 0 || lightId >= stageLights.Count || stageLights[lightId] == null)
        {
            return Vector2.zero;
        }

        Transform lightRoot = stageLights[lightId].transform;
        Transform lightX = lightRoot.Find("Spotlight" + lightId + "X");
        float x = lightX != null ? lightX.localEulerAngles.x : lightRoot.localEulerAngles.x;
        float y = lightRoot.localEulerAngles.y;
        return new Vector2(x, y);
    }

    object AdvanceLightingTick()
    {
        lightingTick = Mathf.Min(
            lightingTick + Mathf.Max(1, Mathf.RoundToInt(Time.deltaTime * lightingTicksPerSecond)),
            int.MaxValue);
        return null;
    }

    void ApplyLightingKeyframe(LightingKeyframe keyframe)
    {
        if (keyframe.useLightBar)
        {
            LightBar(keyframe.resolvedColor);
            return;
        }

        if (keyframe.lightIDToAffect < 0 && keyframe.resolvedColors != null && keyframe.resolvedPositions != null)
        {
            for (int lightId = 0; lightId < stageLights.Count; lightId++)
            {
                StageLight(lightId, keyframe.lightIntensity, keyframe.resolvedPositions[lightId], keyframe.resolvedColors[lightId]);
            }
            return;
        }

        ApplyLightingValues(
            keyframe.lightIDToAffect,
            keyframe.useLightBar,
            keyframe.resolvedPosition,
            keyframe.resolvedColor,
            keyframe.lightIntensity);
    }

    void ApplyInterpolatedLighting(LightingKeyframe previous, LightingKeyframe current, float t)
    {
        float intensity = Mathf.Lerp(previous.lightIntensity, current.lightIntensity, t);

        if (current.useLightBar)
        {
            LightBar(Color.Lerp(previous.resolvedColor, current.resolvedColor, t));
            return;
        }

        if (current.lightIDToAffect < 0 &&
            previous.resolvedColors != null && current.resolvedColors != null &&
            previous.resolvedPositions != null && current.resolvedPositions != null)
        {
            int count = Mathf.Min(stageLights.Count, Mathf.Min(previous.resolvedColors.Length, current.resolvedColors.Length));
            for (int lightId = 0; lightId < count; lightId++)
            {
                StageLight(
                    lightId,
                    intensity,
                    Vector2.Lerp(previous.resolvedPositions[lightId], current.resolvedPositions[lightId], t),
                    Color.Lerp(previous.resolvedColors[lightId], current.resolvedColors[lightId], t));
            }
            return;
        }

        ApplyLightingValues(
            current.lightIDToAffect,
            false,
            Vector2.Lerp(previous.resolvedPosition, current.resolvedPosition, t),
            Color.Lerp(previous.resolvedColor, current.resolvedColor, t),
            intensity);
    }

    void ApplyLightingValues(int lightID, bool useLightBar, Vector2 position, Color color, float intensity)
    {
        if (useLightBar)
        {
            LightBar(color);
            return;
        }

        if (lightID < 0)
        {
            for (int lightId = 0; lightId < stageLights.Count; lightId++)
            {
                StageLight(lightId, intensity, position, color);
            }
        }
        else
        {
            StageLight(lightID, intensity, position, color);
        }
    }

    public void LightBar(Color color)
    {
        if (lightBar != null)
        {
            var volLight = lightBar.GetComponent<VolumetricLines.VolumetricLineBehavior>();
            volLight.LineColor = color; // must include alpha channel
        }
    }

    public void StageLight(int lightID, float intensity, Vector2 rotation, Color color)
    {
        try
        {
            
        
            if (stageLights[lightID].gameObject != null)
            {
                var light = stageLights[lightID].GetComponentInChildren<Light>();
                if (light != null)
                {
                    light.intensity = intensity;
                    light.color = color;
                }
                GameObject lightX = stageLights[lightID].transform.Find("Spotlight" + lightID + "X").gameObject;
                GameObject lightY = stageLights[lightID];

                if (lightX != null && lightY != null)
                {
                    lightX.transform.rotation = Quaternion.Euler(rotation.x, 0 , 0);
                    lightY.transform.rotation = Quaternion.Euler(0, rotation.y, 0);
                }

            }
            else
            {
                Debug.LogWarning("LightingManager: Invalid stage light ID: " + lightID);
            }

        }
        catch (Exception ex)
        {
            Debug.LogWarning("LightingManager: Exception Occoured: " + ex.Message);
        }
    }
    public void StageLightPointAtGameObject(int lightID, GameObject go, float intensity, Color color)
    {
        try
        {
            if (stageLights[lightID].gameObject != null)
            {
                var light = stageLights[lightID].GetComponentInChildren<Light>();
                if (light != null)
                {
                    light.intensity = intensity;
                    light.color = color;
                }
                GameObject lightX = stageLights[lightID].transform.Find("Spotlight" + lightID + "X").gameObject;
                GameObject lightY = stageLights[lightID];

                if (lightX != null && lightY != null)
                {
                    lightX.transform.rotation = Quaternion.Euler(go.transform.localPosition.x, 0 , 0);
                    lightY.transform.rotation = Quaternion.Euler(0, go.transform.localPosition.y, 0);
                    lightX.transform.LookAt(go.transform);
                }
            }
            else
            {
                Debug.LogWarning("LightingManager: Invalid stage light ID: " + lightID);
            }

        }
        catch (Exception ex)
        {
            Debug.LogWarning("LightingManager: Exception Occoured: " + ex.Message);
        }
    }
}
