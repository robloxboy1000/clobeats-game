using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class AlbumLoader : MonoBehaviour
{
    public string albumArtPath;

    public void LoadAlbumData(System.Action<Texture> onLoaded)
    {
        // Placeholder for album art loading logic
        Texture albumArt = System.IO.File.Exists(albumArtPath) ? 
            LoadImageFromFile(albumArtPath) : null;
        onLoaded?.Invoke(albumArt);
    }

    /// <summary>
    /// Loads a Texture2D from a local file path.
    /// </summary>
    /// <param name="filePath">The full path to the image file (e.g., "C:/Users/User/image.png").</param>
    /// <returns>The loaded Texture2D, or null if the file doesn't exist or loading fails.</returns>
    public static Texture2D LoadImageFromFile(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath); // Read the image file into a byte array
            tex = new Texture2D(2, 2); // Create a new Texture2D. Size doesn't matter, LoadImage will auto-resize
            // Load the image data into the texture. Returns true if successful
            if (!ImageConversion.LoadImage(tex, fileData))
            {
                Debug.LogError("Failed to decode image from path: " + filePath);
                return null;
            }
        }
        else
        {
            Debug.LogError("File not found at path: " + filePath);
        }
        return tex;
    }
}
