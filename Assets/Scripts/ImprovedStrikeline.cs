using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImprovedStrikeline : MonoBehaviour
{
    public GameObject flamePrefab;
    public GameObject SustainFlamePrefab;
    public GameObject sustainSparksPrefab;

    public GameObject greenBase;
    public GameObject redBase;
    public GameObject yellowBase;
    public GameObject blueBase;
    public GameObject orangeBase;

    Dictionary<int, GameObject> activeSustainSparks;

    UIUpdater uiUpdater;

    void Awake()
    {
        activeSustainSparks = new Dictionary<int, GameObject>();
        uiUpdater = FindFirstObjectByType<UIUpdater>();
    }

    public void HitNote(float xOffset = 0f)
    {
        Vector3 flamePosition = new Vector3(xOffset, gameObject.transform.position.y, gameObject.transform.position.z);
        StartCoroutine(NoteFlame(flamePosition, 0.2f));
        if (uiUpdater != null)
        {
            uiUpdater.UpdateForNoteHit();
        }
    }

    public void MissNote()
    {
        if (uiUpdater != null)
        {
            uiUpdater.UpdateForNoteMiss();
        }
    }

    public void HitSustain(float xOffset = 0f)
    {
        Vector3 flamePosition = new Vector3(xOffset, gameObject.transform.position.y, gameObject.transform.position.z);
        StartCoroutine(SustainFlame(flamePosition, 0.2f));
        EnableSustainSparks(flamePosition);
    }

    // co-routines
    public IEnumerator NoteFlame(Vector3 fret, float duration = 0.1f)
    {
        GameObject flame = Instantiate(flamePrefab, fret, Quaternion.identity);
        yield return new WaitForSeconds(duration);
        Destroy(flame);
    }
    public IEnumerator SustainFlame(Vector3 fret, float duration = 0.2f)
    {
        GameObject sustainFlame = Instantiate(SustainFlamePrefab, fret, Quaternion.identity);
        yield return new WaitForSeconds(duration);
        Destroy(sustainFlame);
    }
    public void EnableSustainSparks(Vector3 fret)
    {
        try
        {
            Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
            activeSustainSparks.Add((int)fret.x, Instantiate(sustainSparksPrefab, fret, rotation));
        }
        catch (System.Exception)
        {
            Destroy(activeSustainSparks[(int)fret.x]);
            activeSustainSparks.Remove((int)fret.x);
        }
        
        
    }
    public void DisableSustainSparks(int xOffset)
    {
        if (activeSustainSparks != null)
        {
            if (activeSustainSparks.ContainsKey(xOffset))
            {
                Destroy(activeSustainSparks[xOffset]);
                activeSustainSparks.Remove(xOffset);
            }
        }
    }
    public void HoldLane(int laneIndex)
    {
        switch (laneIndex)
        {
            case 0:
                Green(true);
                break;
            case 1:
                Red(true);
                break;
            case 2:
                Yellow(true);
                break;
            case 3:
                Blue(true);
                break;
            case 4:
                Orange(true);
                break;
            default:
                break;
        }
    }

    public void ReleaseLane(int laneIndex)
    {
        switch (laneIndex)
        {
            case 0:
                Green(false);
                break;
            case 1:
                Red(false);
                break;
            case 2:
                Yellow(false);
                break;
            case 3:
                Blue(false);
                break;
            case 4:
                Orange(false);
                break;
            default:
                break;
        }
    }

    public void Green(bool on)
    {
        if (on)
        {
            if (greenBase == null) greenBase = GameObject.Find("Base_Green");
            Renderer greenRenderer = greenBase.GetComponent<Renderer>();
            greenRenderer.material.color = Color.green;
            return;
        }
        else
        {
            if (greenBase == null) greenBase = GameObject.Find("Base_Green");
            Renderer greenRenderer = greenBase.GetComponent<Renderer>();
            greenRenderer.material.color = Color.black;
            return;
        }
    }
    
    public void Red(bool on)
    {
        if (on)
        {
            if (redBase == null) redBase = GameObject.Find("Base_Red");
            Renderer redRenderer = redBase.GetComponent<Renderer>();
            redRenderer.material.color = Color.red;
            return;
        }
        else
        {
            if (redBase == null) redBase = GameObject.Find("Base_Red");
            Renderer redRenderer = redBase.GetComponent<Renderer>();
            redRenderer.material.color = Color.black;
            return;
        }
    }

    public void Yellow(bool on)
    {
        if (on)
        {
            if (yellowBase == null) yellowBase = GameObject.Find("Base_Yellow");
            Renderer yellowRenderer = yellowBase.GetComponent<Renderer>();
            yellowRenderer.material.color = Color.yellow;
            return;
        }
        else
        {
            if (yellowBase == null) yellowBase = GameObject.Find("Base_Yellow");
            Renderer yellowRenderer = yellowBase.GetComponent<Renderer>();
            yellowRenderer.material.color = Color.black;
            return;
        }
    }

    public void Blue(bool on)
    {
        if (on)
        {
            if (blueBase == null) blueBase = GameObject.Find("Base_Blue");
            Renderer blueRenderer = blueBase.GetComponent<Renderer>();
            blueRenderer.material.color = Color.blue;
            return;
        }
        else
        {
            if (blueBase == null) blueBase = GameObject.Find("Base_Blue");
            Renderer blueRenderer = blueBase.GetComponent<Renderer>();
            blueRenderer.material.color = Color.black;
            return;
        }
    }

    public void Orange(bool on)
    {
        if (on)
        {
            if (orangeBase == null) orangeBase = GameObject.Find("Base_Orange");
            Renderer orangeRenderer = orangeBase.GetComponent<Renderer>();
            orangeRenderer.material.color = new Color(1f, 0.5f, 0f);
            return;
        }
        else
        {
            if (orangeBase == null) orangeBase = GameObject.Find("Base_Orange");
            Renderer orangeRenderer = orangeBase.GetComponent<Renderer>();
            orangeRenderer.material.color = Color.black;
            return;
        }
    }
    
}
