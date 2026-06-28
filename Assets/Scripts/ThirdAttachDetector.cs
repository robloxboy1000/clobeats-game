#if UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class ThirdAttachDetector : MonoBehaviour
{
    public bool nativeMode = true;
    bool isDebuggerPresent = false;

    private const int WDA_NONE = 0x00000000;
    private const int WDA_MONITOR = 0x00000001;

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    // Start is called before the first frame update
    void Start()
    {
        SetWindowAffinity();
    }

    void Awake()
    {
        
    }

    void SetWindowAffinity()
    {
        IntPtr hwnd = GetActiveWindow();
        if (hwnd != IntPtr.Zero)
        {
            bool result = SetWindowDisplayAffinity(hwnd, WDA_MONITOR);
            if (!result)
            {
                Debug.LogError("Failed to set window display affinity.");
            }
            else
            {
                Debug.Log("Window display affinity set successfully.");
            }
        }
        else
        {
            Debug.LogError("Failed to get active window handle.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (nativeMode)
        {
            [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
            static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);
            
            CheckRemoteDebuggerPresent(System.Diagnostics.Process.GetCurrentProcess().Handle, ref isDebuggerPresent);
        }
        else
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                isDebuggerPresent = true;
            }
            else
            {
                isDebuggerPresent = false;
            }
        }
    }
}
#endif