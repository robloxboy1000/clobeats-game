using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class UIUpdater : MonoBehaviour
{
    public TMPro.TMP_Text scoreText;
    public GameObject scoreObject;
    public TMPro.TMP_Text comboText;
    public TMPro.TMP_Text comboDotsText;
    public TMPro.TMP_Text notesHitText;
    public NewRockMeter rockMeterSlider;
    public StarMeter spMeter;

    public GameObject songInfoPanel;
    public TMPro.TMP_Text currentBPMText;
    public TMPro.TMP_Text currentTickText;
    public TMPro.TMP_Text currentEventText;
    private NoteSpawner noteSpawner;
    private LaneInputManager lim;

    float savesscore = 0f;
    int savedcombo = 1;
    int comboDotsCount = 0;
    public int savednotesHit = 0;
    int rockMeter = 50;
    float spMeterRead = 0;
    public float tempo = 120.000f;

    public int combolimit = 4;

    public bool inStar = false;

    public enum UpdateType
    {
        Visuals,
        ScoreAdd,
        ScoreMinus,
    }

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
        spMeter.value = spMeterRead;
    }
    public void UpdateForNoteHit()
    {
        lim = FindAnyObjectByType<LaneInputManager>();
        if (lim != null)
        {
            UpdateScore();
            UpdateCombo(UpdateType.ScoreAdd);
            UpdateNotesHit();
            UpdateRockMeter();
            if (!inStar)
            {
                UpdateStarMeter(0.25f);
            }
        }
        GuitarPlayer player = FindAnyObjectByType<GuitarPlayer>();
        if (player != null)
        {
            player.NoteHit();
        }
        
    }

    public void StarPowerToggle(bool toggle)
    {
        SFXPlayer sFX = FindAnyObjectByType<SFXPlayer>();
        MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
        inStar = toggle;

        if (toggle)
        {
            combolimit = 8;
            savedcombo = savedcombo * 2;
            sFX.PlayClip("Star_Deployed");
            //sFX.PlayClip("FeverCheer1");
            musicPlayer.ToggleReverb(true);
        }
        else
        {
            combolimit = 4;
            sFX.PlayClip("Star_Release");
            musicPlayer.ToggleReverb(false);
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
    public void UpdateStarMeter(float amount)
    {
        spMeterRead = Mathf.Clamp(spMeterRead + amount, 0, 100);
        spMeter.value = spMeterRead;
    }
    public void UpdateScore()
    {
        savesscore += 50f * savedcombo;
        Scoring scoring = FindAnyObjectByType<Scoring>();
        if (scoring != null)
        {
            scoring.currentScore += 50 * savedcombo;
        }
        scoreText.text = savesscore.ToString("F0");
    }
    public void UpdateScoreSustain(float amount)
    {
        savesscore += amount;
        Scoring scoring = FindAnyObjectByType<Scoring>();
        if (scoring != null)
        {
            scoring.currentScore += (int)amount;
        }
        scoreText.text = savesscore.ToString("F0");
    }
    public void UpdateCombo(UpdateType type = UpdateType.ScoreAdd)
    {
        if (type == UpdateType.ScoreAdd)
        {
            if (savednotesHit % 10 == 0 && savednotesHit != 0)
            {
                savedcombo += 1; // Increase combo every 10 notes hit
                comboDotsCount = 0;
            }
            comboDotsCount += 1;
        }
        else if (type == UpdateType.ScoreMinus)
        {
            if (savednotesHit % 10 == 0 && savednotesHit != 0)
            {
                savedcombo -= 1; // decrease combo every 10 notes hit
                comboDotsCount = 0;
            }
            comboDotsCount -= 1;
        }
        else if (type == UpdateType.Visuals)
        {
            comboText.text = savedcombo.ToString() + "x";
            comboDotsText.text = new string('.', comboDotsCount);

            if (savedcombo == 2)
            {
                comboText.color = new Color(1f, 0.5f, 0f);
                comboDotsText.color = new Color(1f, 0.5f, 0f);
            }
            else if (savedcombo == 3)
            {
                comboText.color = Color.green;
                comboDotsText.color = Color.green;
            }
            else if (savedcombo == 4)
            {
                comboText.color = new Color(1f, 0f, 1f);
                comboDotsText.color = new Color(1f, 0f, 1f);
            }
            else
            {
                if (inStar)
                {
                    comboText.color = Color.cyan;
                    comboDotsText.color = Color.cyan;
                }
                else
                {
                    comboText.color = Color.white;
                    comboDotsText.color = Color.white;
                }
            
            }
        }
        
        
        if (savedcombo >= combolimit)
        {
            savedcombo = combolimit; // Limit combo to 4x
            comboDotsCount = 10;
        }
        
        
    }
    public void UpdateNotesHit()
    {
        savednotesHit += 1;
        Scoring scoring = FindAnyObjectByType<Scoring>();
        if (scoring != null)
        {
            scoring.currentNotesHit += 1;
        }
        notesHitText.text = savednotesHit.ToString();
    }
    public void ResetCombo()
    {
        Scoring scoring = FindAnyObjectByType<Scoring>();
        if (scoring != null)
        {
            scoring.currentNotesMissed += 1;
        }
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
            SFXPlayer sFXPlayer = FindAnyObjectByType<SFXPlayer>();
            sFXPlayer.PlayComboLostClip();
            if (gp != null)
            {
                Animation highwayAnim = gp.GetComponent<Animation>();
                highwayAnim.Stop();
                highwayAnim.Play("ComboLostShake");
            }
            savedcombo = 1;
            savednotesHit = 0;
            comboDotsCount = 0;
            comboText.text = savedcombo.ToString() + "x";
            notesHitText.text = savednotesHit.ToString();
        }
    }
    public void DecreaseRockMeter()
    {
        rockMeter = Mathf.Clamp(rockMeter - 1, 0, 100);
        rockMeterSlider.value = rockMeter;
    }
    
    public void UpdateSongInfo(string title, string artist, int year)
    {
        songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().text = title;
        songInfoPanel.transform.Find("ArtistText - UI").GetComponent<TMPro.TMP_Text>().text = "by " + artist + ", " + year.ToString();
    }
    public void SetSongInfoOpacity(float amount)
    {
        songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color = new Color(songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color.r,
        songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color.g,
        songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color.b,
        Mathf.Clamp01(amount));

        songInfoPanel.transform.Find("ArtistText - UI").GetComponent<TMPro.TMP_Text>().color = new Color(songInfoPanel.transform.Find("TitleText - UI").GetComponent<TMPro.TMP_Text>().color.r,
        songInfoPanel.transform.Find("ArtistText - UI").GetComponent<TMPro.TMP_Text>().color.g,
        songInfoPanel.transform.Find("ArtistText - UI").GetComponent<TMPro.TMP_Text>().color.b,
        Mathf.Clamp01(amount));
    }
    public void UpdateBPM(double bpm)
    {
        currentBPMText.text = "BPM: " + bpm.ToString("F1");
    }
    public void UpdateCurrentTick(int tick)
    {
        currentTickText.text = $"Tick: {tick}"; // Append the current tick to the BPM text
    }
    public async Task UpdateCurrentEvent(string value)
    {
        currentEventText.text = $"GlobalEvent: {value}";
        //Debug.Log("Current MIDI text event: " + value);
        await Task.Yield();
    }

    public IEnumerator SongInfoAnim(float duration = 6f)
    {
        if (songInfoPanel != null)
        {
            SetSongInfoOpacity(1);
            yield return new WaitForSeconds(duration);
            SetSongInfoOpacity(0);
        }
    }

    public void ScoreVisibility(bool toggle)
    {
        if (toggle)
        {
            scoreObject.SetActive(true);
        }
        else
        {
            scoreObject.SetActive(false);
        }
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
            //UpdateLoadingPhrase(songFolderLoader.loadingPhrase);
            UpdateCurrentTick(noteSpawner.currentTick);
            tempo = SyncInfoToTempo(noteSpawner.FindSyncForTick(noteSpawner.currentTick));
            UpdateBPM(tempo);
            await UpdateCurrentEvent(noteSpawner.GetEventInfoStringOnTick(noteSpawner.currentTick));
            UpdateCombo(UpdateType.Visuals);
            if (inStar)
            {
                UpdateStarMeter(-Time.deltaTime * 5f);
                //savedcombo = savedcombo * 2;
            }
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


