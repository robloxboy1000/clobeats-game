using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ExceptionManager : MonoBehaviour
{
    public GameObject exceptionPanel; // UI panel to display exception messages
    public TMPro.TMP_InputField debugLogField;
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
        System.Console.WriteLine($"[{Time.time}]: [{type}]: \"{logString}\"");

        //using (StreamWriter sw = new StreamWriter(Path.Combine(Application.persistentDataPath, $"cb_log_{Time.time}.log")))
        //{
        //    sw.WriteLine($"[{Time.time}]: [{type}]: \"{logString}\"");
        //    sw.Close();
        //}
        if (debugLogField != null)
        {
            debugLogField.text += $"[{Time.time}]: [{type}]: \"{logString}\"\r\n";
        }
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
