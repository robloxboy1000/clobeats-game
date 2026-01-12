using System;
using System.Runtime.InteropServices;

public class WindowHandleHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    public static IntPtr GetActiveWindowHandle()
    {
        return GetForegroundWindow();
    }
}