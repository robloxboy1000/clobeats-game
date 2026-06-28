using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StarMeter : MonoBehaviour
{
    public float minValue = 0f;      // Minimum value of the slider
    public float maxValue = 100f;    // Maximum value of the slider
    public UnityEvent<float> onValueChanged; // Event to notify value changes
    private float currentValue;      // Current value of the slider
    public float minZPosition = -2f; // Minimum Z position of the handle
    public float maxZPosition = 2f;  // Maximum Z position of the handle

    public GameObject fill;
    private Vector3 initialFillScale;
    private Vector3 initialFillLocalPosition;

    public float value
    {
        get => currentValue;
        set 
        { 
            if (currentValue != value) // Check if the value is actually different
            {
                currentValue = value;
                OnValueChanged(); // Call your method or trigger an event
            }
            SetHandlePosition(Mathf.Lerp(minZPosition, maxZPosition, (value - minValue) / (maxValue - minValue)));
        }
    }
    public void SetHandlePosition(float zPosition)
    {
        float clampedZ = Mathf.Clamp(zPosition, minZPosition, maxZPosition);
        float normalizedZPosition = (clampedZ - minZPosition) / (maxZPosition - minZPosition);
        currentValue = Mathf.Lerp(minValue, maxValue, normalizedZPosition);
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, clampedZ);
        UpdateFillIndicator(currentValue);
        onValueChanged.Invoke(currentValue);
    }
    private void Start()
    {
        if (fill != null)
        {
            initialFillScale = fill.transform.localScale;
            initialFillLocalPosition = fill.transform.localPosition;
            UpdateFillIndicator(currentValue);
        }
    }

    private void UpdateFillIndicator(float newValue)
    {
        if (fill == null)
            return;

        float normalized = 0f;
        if (!Mathf.Approximately(maxValue, minValue))
            normalized = Mathf.InverseLerp(minValue, maxValue, newValue);

        // Scale the fill's Y based on the normalized value (0..1) (fill updates via Y scale)
        Vector3 s = initialFillScale;
        s.y = Mathf.Max(0.0001f, initialFillScale.y * normalized);
        fill.transform.localScale = s;

        // Position the fill so its center sits between minZ and the handle position
        float handleZ = Mathf.Lerp(minZPosition, maxZPosition, normalized);
        Vector3 p = initialFillLocalPosition;
        p.z = minZPosition + (handleZ - minZPosition) * 0.5f;
        fill.transform.localPosition = p;
        var render = fill.GetComponent<Renderer>();
        if (newValue < 50)
        {
            render.material.color = Color.clear;
        }
        else
        {
            render.material.color = Color.cyan;
        }

    }

    private void OnValueChanged()
    {
        UIUpdater updater = FindAnyObjectByType<UIUpdater>();
        if (value == 0)
        {
            updater.StarPowerToggle(false);
        }
        else if (value == 50 && !updater.inStar)
        {
            // alert star power ready
            Debug.Log("[StarMeter] Star Power is ready to deploy.");
        }
    }
}
