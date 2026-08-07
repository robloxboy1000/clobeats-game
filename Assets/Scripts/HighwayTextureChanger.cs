using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HighwayTextureChanger : MonoBehaviour
{
    public SpriteRenderer[] sprites;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string dataPath = documentsPath + Path.DirectorySeparatorChar + "CloBeats" + Path.DirectorySeparatorChar + "highways";
        string[] highways = Directory.GetFiles(dataPath);
        System.Random random = new System.Random();
        int randomIndex = random.Next(highways.Length);
        string randomHighway = highways[randomIndex];
        foreach (SpriteRenderer sprite in sprites)
        {
            sprite.sprite = AlbumLoader.LoadSpriteFromFile(randomHighway);
        }
    }

    public void TrySetSpriteColor(float r, float g, float b)
    {
        try
        {
            foreach (SpriteRenderer sprite in sprites)
            {
                sprite.color = new Color(r, g, b);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[HighwayTextureChanger.TrySetSpriteColor] Failed to set sprite(s) color: " + ex.Message);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
