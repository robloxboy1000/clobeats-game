using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class UIUpdater : MonoBehaviour
{
    public TMPro.TMP_Text scoreText;
    public TMPro.TMP_Text comboText;
    public TMPro.TMP_Text comboDotsText;
    public TMPro.TMP_Text notesHitText;
    public NewRockMeter rockMeterSlider;
    public GameObject loadingOverlay;
    public GameObject songInfoPanel;
    public TMPro.TMP_Text currentBPMText;
    public TMPro.TMP_Text currentTickText;
    public TMPro.TMP_Text currentEventText;
    private NoteSpawner noteSpawner;
    private LaneInputManager lim;

    float savesscore = 0f;
    int savedcombo = 1;
    int comboDotsCount = 0;
    int savednotesHit = 0;
    int rockMeter = 50;
    public float tempo = 120.000f;

    public int combolimit = 4;

    // Start is called before the first frame update
    void Start()
    {
        
        
    }
    public void InitializeUI()
    {
        scoreText.text = "0";
        comboText.text = "1x";
        comboDotsText.text = "";
        notesHitText.text = "0";
        rockMeterSlider.value = rockMeter;
    }
    public void UpdateForNoteHit()
    {
        lim = FindAnyObjectByType<LaneInputManager>();
        if (lim != null)
        {
            Debug.Log("Held lanes count: " + lim.GetHeldLanes());
            UpdateScore();
            UpdateCombo();
            UpdateNotesHit();
            UpdateRockMeter();
            
        }
        
    }
    public void UpdateForNoteMiss()
    {
        ResetCombo();
        DecreaseRockMeter();
    }
    public void UpdateForSustainHold(float sustainAmount)
    {
        UpdateScoreSustain(sustainAmount);
    }
    public void UpdateRockMeter()
    {
        rockMeter = Mathf.Clamp(rockMeter + 1, 0, 100);
        rockMeterSlider.value = rockMeter;
    }
    public void UpdateScore()
    {
        savesscore += 50f * savedcombo;
        scoreText.text = savesscore.ToString("F0");
    }
    public void UpdateScoreSustain(float amount)
    {
        savesscore += amount;
        scoreText.text = savesscore.ToString("F0");
    }
    public void UpdateCombo()
    {

        if (savednotesHit % 10 == 0 && savednotesHit != 0)
        {
            savedcombo += 1; // Increase combo every 10 notes hit
            comboDotsCount = 0;
        }
        
        if (savedcombo >= combolimit)
        {
            savedcombo = combolimit; // Limit combo to 4x
        }
        
        comboText.text = savedcombo.ToString() + "x";
        comboDotsCount += 1;
        comboDotsText.text = new string('.', comboDotsCount);
    }
    public void UpdateNotesHit()
    {
        savednotesHit += 1;
        notesHitText.text = savednotesHit.ToString();
    }
    public void ResetCombo()
    {
        if (savedcombo == 1)
        {
            return;
        }
        if (savednotesHit == 0)
        {
            return;
        }
        else
        {
            GameObject gp = GameObject.Find("GuitarPlayer");
            if (gp != null)
            {
                Animation highwayAnim = gp.GetComponent<Animation>();
                highwayAnim.Stop();
                highwayAnim.Play("ComboLostShake");
            }
            savedcombo = 1;
            savednotesHit = 0;
            comboText.text = savedcombo.ToString() + "x";
            notesHitText.text = savednotesHit.ToString();
        }
        
    }
    public void DecreaseRockMeter()
    {
        rockMeter = Mathf.Clamp(rockMeter - 1, 0, 100);
        rockMeterSlider.value = rockMeter;
    }
    
    public void UpdateSongInfo(string title, string artist)
    {
        songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().text = title;
        songInfoPanel.transform.Find("ArtistText - UI").GetComponent<TMPro.TMP_Text>().text = "by " + artist;
    }
    public void UpdateBPM(double bpm)
    {
        currentBPMText.text = "BPM: " + bpm.ToString("F1");
    }
    public void UpdateCurrentTick(int tick)
    {
        currentTickText.text = $"Tick: {tick}"; // Append the current tick to the BPM text
    }

    public void UpdateLoadingPhrase(string value)
    {
        TMPro.TextMeshProUGUI loadingPhraseText = loadingOverlay.transform.Find("LoadingPhraseText").GetComponent<TMPro.TextMeshProUGUI>();
        loadingPhraseText.text = value;
    }
    public async Task UpdateCurrentEvent(string value)
    {
        currentEventText.text = $"GlobalEvent: {value}";
        //Debug.Log("Current global event: " + value);
        await Task.Yield();
    }

    // Update is called once per frame
    async void Update()
    {
        if (noteSpawner == null)
        {
            noteSpawner = GameObject.FindAnyObjectByType<NoteSpawner>();
            if (noteSpawner == null)
            {
                
            }
        }
        else
        {
            SongFolderLoader songFolderLoader = FindAnyObjectByType<SongFolderLoader>();
            UpdateLoadingPhrase(songFolderLoader.loadingPhrase);
            UpdateCurrentTick(noteSpawner.currentTick);
            tempo = SyncInfoToTempo(noteSpawner.FindSyncForTick(noteSpawner.currentTick));
            UpdateBPM(tempo);
            await UpdateCurrentEvent(noteSpawner.GetEventInfoStringOnTick(noteSpawner.currentTick));
        }
    }

    public float SyncInfoToTempo(NoteSpawner.SyncInfo syncInfo)
    {
        if (syncInfo != null)
        {
            return syncInfo.bpm;
        }
        else
        {
            return 0f;
        }
    }
}


