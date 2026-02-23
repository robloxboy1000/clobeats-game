using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VenueLoader : MonoBehaviour
{
    private SongFolderLoader songFolderLoader;
    // Start is called before the first frame update
    void Start()
    {
    }
    void Awake()
    {
        if (PlayerPrefs.GetInt("EnableVenue") == 1)
        {
            SceneManager.LoadSceneAsync(2, LoadSceneMode.Additive);
        }
        else
        {
            if (SceneManager.GetSceneByBuildIndex(2).isLoaded)
            {
                SceneManager.UnloadSceneAsync(2);
            }
            
            if (songFolderLoader != null && songFolderLoader.songVideoClipPathSet)
            {
                SceneManager.LoadSceneAsync(5, LoadSceneMode.Additive);
                SceneManager.UnloadSceneAsync(4);
            }
            else
            {
                SceneManager.LoadSceneAsync(4, LoadSceneMode.Additive);
            }
        }
        
    }
    // Update is called once per frame
    void Update()
    {
        songFolderLoader = FindAnyObjectByType<SongFolderLoader>();
    }
}
