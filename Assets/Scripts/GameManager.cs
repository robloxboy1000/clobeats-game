using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

public class GameManager : MonoBehaviour
{

    // Start is called before the first frame update
    void Start()
    {

        
    }
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Gameplay") // Gameplay scene
        {
            //WaitForSecondsRealtime wait = new WaitForSecondsRealtime(120f);
            //StartCoroutine(PlaySong(wait));
        }
    }

    public IEnumerator DelayedPlaySong(WaitForSecondsRealtime wait)
    {
        yield return wait;
        UIUpdater uiUpdater = FindAnyObjectByType<UIUpdater>();
        if (uiUpdater != null)
        {
            uiUpdater.songInfoPanel.SetActive(false);
        }
        NoteSpawner noteSpawner = FindAnyObjectByType<NoteSpawner>();
        if (noteSpawner != null)
        {
            noteSpawner.Play();
        }
        yield return null;
    }

    public IEnumerator PlaySong()
    {
        UIUpdater uiUpdater = FindAnyObjectByType<UIUpdater>();
        GameObject highway = GameObject.Find("Highway");
        GameObject strikeline = GameObject.Find("Strikeline");
        if (uiUpdater != null)
        {
            uiUpdater.songInfoPanel.SetActive(true);
            uiUpdater.loadingOverlay.SetActive(false);
        }
        if (highway != null)
        {
            highway.transform.position = new Vector3(0, -6, 6);
        }
        if (strikeline != null)
        {
            strikeline.transform.position = new Vector3(0, -4, 0);
        }
        
        yield return new WaitForSecondsRealtime(0.1f);

        if (SceneManager.GetSceneByName("3DVenue").isLoaded)
        {
            GameObject venue = GameObject.Find("3DVenue_Camera");
            VenueAnimationManager venueAnimManager = FindAnyObjectByType<VenueAnimationManager>();
            if (venue != null)
            {
                Animation venueAnim = venue.GetComponent<Animation>();
                venueAnim.Play("VenueEntry");
                if (venueAnimManager != null)
                {
                    venueAnimManager.leftDoor.GetComponent<Animation>().Play("RightDoorOpen");
                    venueAnimManager.rightDoor.GetComponent<Animation>().Play("LeftDoorOpen");
                }
                yield return new WaitForSecondsRealtime(10f);
            }
        }
        if (uiUpdater != null)
        {
            uiUpdater.InitializeUI();
            uiUpdater.songInfoPanel.SetActive(false);
        }
        if (strikeline != null)
        {
            strikeline.transform.position = new Vector3(0, 0, 0);
        }
        if (highway != null)
        {
            Animation highwayAnim = highway.GetComponent<Animation>();
            highwayAnim.Play("ShowHighway");
            yield return new WaitForSecondsRealtime(1f);
        }
        VenueAnimationPlayer venueAnimationPlayer = FindAnyObjectByType<VenueAnimationPlayer>();
        if (venueAnimationPlayer != null)
        {
            venueAnimationPlayer.Load();
        }
        MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
        if (musicPlayer != null)
        {
            musicPlayer.dspSongStart = AudioSettings.dspTime;
        }
        NoteSpawner noteSpawner = FindAnyObjectByType<NoteSpawner>();
        if (noteSpawner != null)
        {
            noteSpawner.Play();
        }
    }

    public void ResetAllValues()
    {
        NoteSpawner noteSpawner = FindAnyObjectByType<NoteSpawner>();
        if (noteSpawner != null)
        {
            if (noteSpawner.notes.Count > 0)
            {
                noteSpawner.notes.Clear();
            }
            if (noteSpawner.syncTrack.Count > 0)
            {
                noteSpawner.syncTrack.Clear();
            }
            if (noteSpawner.events.Count > 0)
            {
                noteSpawner.events.Clear();
            }
            noteSpawner.songLengthInTicks = 0;
            noteSpawner.currentTick = 0;
        }

        GlobalMoveY globalMoveY = FindAnyObjectByType<GlobalMoveY>();
        if (globalMoveY != null)
        {
            if (globalMoveY.objectsToMove.Count > 0)
            {
                foreach (GameObject go in globalMoveY.objectsToMove)
                {
                    if (go != null)
                    Destroy(go);
                }
                globalMoveY.objectsToMove.Clear();
            }
        }
    }

    public static async Task GetStringFromAddr(string addr)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("CloBeats/0.0.1");
            try
            {
                await client.GetStringAsync(addr);
            }
            catch (Exception ex)
            {
                Debug.LogError("Server error occoured: " + ex.Message);
            }
        }
    }

    public static async Task PostStringToAddr(string addr, string value)
    {
        HttpContent content = new StringContent(value, Encoding.UTF8);
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("CloBeats/0.0.1");
            try
            {
                await client.PostAsync(addr, content);
            }
            catch (Exception ex)
            {
                Debug.LogError("Server error occoured: " + ex.Message);
                
            }
        }
    }

    public static async Task PostJSONToAddr(string addr, string value)
    {
        HttpContent content = new StringContent(value, Encoding.UTF8, "application/json");
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("CloBeats/0.0.1");
            
            var response = await client.PostAsync(addr, content);

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                Debug.Log("JSON POST Successful. Response: " + responseString);
            }
            else
            {
                Debug.LogError("JSON POST failed with status code " + response.StatusCode);
            }
        }
    }

    public static Texture2D FlipTextureVerticallyGPU(Texture2D original)
    {
        RenderTexture rt = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        // Vector2(1, -1) scales the Y axis by -1 (flips vertically)
        Graphics.Blit(original, rt, new Vector2(1, -1), new Vector2(0, 1)); 

        Texture2D flipped = new Texture2D(original.width, original.height);
        RenderTexture.active = rt;
        flipped.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        flipped.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return flipped;
    }



    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0)
        {
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (musicPlayer != null)
            {
                musicPlayer.pauseAudio();
            }
        }
        else if (!Application.isFocused)
        {
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (musicPlayer != null)
            {
                //musicPlayer.setVolume(0.05f);
            }
        }
        else if (Application.isFocused)
        {
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (musicPlayer != null)
            {
                musicPlayer.setVolume(1f);
            }
        }
        else
        {
            MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
            if (musicPlayer != null)
            {
                musicPlayer.resumeAudio();
            }
        }

    }
    
}
