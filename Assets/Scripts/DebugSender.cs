using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class DebugSender : MonoBehaviour
{
    TcpClient client;
    StreamWriter writer;

    void Start()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 5000);
            writer = new StreamWriter(client.GetStream());
            writer.AutoFlush = true;

            Application.logMessageReceived += OnLog;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to connect: " + ex.Message); // warning to allow other scripts to execute
        }
    }

    void OnLog(string condition, string stackTrace, LogType type)
    {
        if (writer == null)
            return;
        if (condition.StartsWith("$beep"))
        {
            writer.WriteLine(condition);
        }
        else
        {
            writer.WriteLine($"[{Time.time}]: [{type}]: \"{condition}\"\r\n{stackTrace}");
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;

        writer?.Dispose();
        client?.Close();
    }

}