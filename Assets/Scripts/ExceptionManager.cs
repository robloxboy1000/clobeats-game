using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExceptionManager : MonoBehaviour
{
    public GameObject exceptionPanel; // UI panel to display exception messages
    void Awake()
    {
        Application.logMessageReceived += HandleLog;
        DontDestroyOnLoad(gameObject); // Optional: if you want it persistent
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
        {
            // This block will be executed whenever an unhandled exception occurs
            Debug.LogError($"Caught unhandled exception: {logString}\nStackTrace: {stackTrace}");

            // Add your custom logic here:
            // - Log to a file or remote service
            // - Display a user-facing error message
            // - Trigger a graceful shutdown
            // - etc.
            if (exceptionPanel != null)
            {
                GameObject ePanel = Instantiate(exceptionPanel);
                ePanel.SetActive(true);
                GameObject errorTextObject = ePanel.transform.Find("Canvas").Find("MessageBox").Find("ExceptionText").gameObject;
                TMPro.TMP_Text exceptionText = errorTextObject.GetComponentInChildren<TMPro.TMP_Text>();
                if (exceptionText != null)
                {
                    exceptionText.text = $"An unexpected error occurred:\n{logString}\n\nPlease restart the application.";
                }
            }
        }
    }
}
