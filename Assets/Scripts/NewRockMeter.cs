using System.Collections;
using System.Collections.Generic;
using Leguar.LowHealth;
using UnityEngine;
using UnityEngine.Events;

public class NewRockMeter : MonoBehaviour
{
    public float minValue = 0f;      // Minimum value of the slider
    public float maxValue = 100f;    // Maximum value of the slider
    public UnityEvent<float> onValueChanged; // Event to notify value changes
    private float currentValue;      // Current value of the slider


    public float minZPosition = -2.2f; // Minimum Z position of the handle
    public float maxZPosition = 2.2f;  // Maximum Z position of the handle
    public GameObject greenIndicator;
    public GameObject yellowIndicator;
    public GameObject redIndicator;


    public float value
    {
        get { return currentValue; }
        set { SetHandlePosition(Mathf.Lerp(minZPosition, maxZPosition, (value - minValue) / (maxValue - minValue))); }
    }

    public void SetHandlePosition(float zPosition)
    {
        // Clamp the xPosition and zPosition to be within the defined range

        float clampedZ = Mathf.Clamp(zPosition, minZPosition, maxZPosition);

        // Calculate the normalized positions (0 to 1)

        float normalizedZPosition = (clampedZ - minZPosition) / (maxZPosition - minZPosition);

        // Map the normalized positions to the value range
        currentValue = Mathf.Lerp(minValue, maxValue, normalizedZPosition);

        // Update the handle's position in the 3D space
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, clampedZ);
        UpdateIndicators();

        // Invoke the event to notify listeners of the value change
        onValueChanged.Invoke(currentValue);

        LowHealthController healthController = FindAnyObjectByType<LowHealthController>();
        if (healthController)
        {
            healthController.SetPlayerHealthInstantly(currentValue / maxValue);
        }
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (currentValue <= 0f && gameManager.allowFail)
        {
            gameManager.FailSong();
        }
    }
    private void UpdateIndicators()
    {
        if (currentValue >= 70f)
        {
            greenIndicator.SetActive(true);
            yellowIndicator.SetActive(false);
            redIndicator.SetActive(false);
        }
        else if (currentValue >= 30f)
        {
            greenIndicator.SetActive(false);
            yellowIndicator.SetActive(true);
            redIndicator.SetActive(false);
        }
        else
        {
            greenIndicator.SetActive(false);
            yellowIndicator.SetActive(false);
            redIndicator.SetActive(true);
        }
    }

}
