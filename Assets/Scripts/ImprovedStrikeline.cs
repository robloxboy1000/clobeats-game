using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImprovedStrikeline : MonoBehaviour
{
    public GameObject greenFlamePrefab;
    public GameObject redFlamePrefab;
    public GameObject yellowFlamePrefab;
    public GameObject blueFlamePrefab;
    public GameObject orangeFlamePrefab;
    public GameObject purpleFlamePrefab;
    
    public GameObject SustainFlamePrefab;
    public GameObject sustainSparksPrefab;

    public GameObject SLGreenTopPrefab;
    public GameObject SLRedTopPrefab;
    public GameObject SLYellowTopPrefab;
    public GameObject SLBlueTopPrefab;
    public GameObject SLOrangeTopPrefab;

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
        StartCoroutine(NoteFlame(flamePosition, 0.5f));
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
        StartCoroutine(SustainFlame(flamePosition, 0.5f));
        EnableSustainSparks(Mathf.CeilToInt(xOffset));
    }

    // co-routines
    public IEnumerator NoteFlame(Vector3 fret, float duration = 0.5f)
    {
        if (fret.x == -2)
        {
            if (greenFlamePrefab != null)
            {
                Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);
                GameObject flame = Instantiate(greenFlamePrefab, fret, rotation);
                yield return new WaitForSeconds(duration);
                Destroy(flame);
            }
            else
            {
                yield return null;
            }
        }
        else if (fret.x == -1)
        {
            if (redFlamePrefab != null)
            {
                Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);
                GameObject flame = Instantiate(redFlamePrefab, fret, rotation);
                yield return new WaitForSeconds(duration);
                Destroy(flame);
            }
            else
            {
                yield return null;
            }
        }
        else if (fret.x == 0)
        {
            if (yellowFlamePrefab != null)
            {
                Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);
                GameObject flame = Instantiate(yellowFlamePrefab, fret, rotation);
                yield return new WaitForSeconds(duration);
                Destroy(flame);
            }
            else
            {
                yield return null;
            }
        }
        else if (fret.x == 1)
        {
            if (blueFlamePrefab != null)
            {
                Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);
                GameObject flame = Instantiate(blueFlamePrefab, fret, rotation);
                yield return new WaitForSeconds(duration);
                Destroy(flame);
            }
            else
            {
                yield return null;
            }
        }
        else if (fret.x == 2)
        {
            if (orangeFlamePrefab != null)
            {
                Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);
                GameObject flame = Instantiate(orangeFlamePrefab, fret, rotation);
                yield return new WaitForSeconds(duration);
                Destroy(flame);
            }
            else
            {
                yield return null;
            }
        }
        else if (fret.x == 7)
        {
            if (purpleFlamePrefab != null)
            {
                Vector3 openPos = new Vector3(0, 0, 0);
                Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);
                GameObject flame = Instantiate(purpleFlamePrefab, openPos, rotation);
                yield return new WaitForSeconds(duration);
                Destroy(flame);
            }
            else
            {
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("invalid fret index: " + fret.x);
        }
        
    }
    public IEnumerator SustainFlame(Vector3 fret, float duration = 0.5f)
    {
        if (SustainFlamePrefab != null)
        {
            GameObject sustainFlame = Instantiate(SustainFlamePrefab, fret, Quaternion.identity);
            yield return new WaitForSeconds(duration);
            Destroy(sustainFlame);
        }
        else
        {
            yield return null;
        }
    }

    public void ResetAnims()
    {
        GameObject gp = GameObject.Find("GuitarPlayer");
        if (gp != null)
        {
            gp.transform.position = new Vector3(0,0,0);

        }
        transform.position = new Vector3(0,0,0);
        if (SLGreenTopPrefab != null)
        {
            Animation topAnim = SLGreenTopPrefab.GetComponent<Animation>();
            if (topAnim != null)
            {
                topAnim.Stop();
            }
            SLGreenTopPrefab.transform.position = new Vector3(-2, 0, 0);
        }
        if (SLRedTopPrefab != null)
        {
            Animation topAnim = SLRedTopPrefab.GetComponent<Animation>();
            if (topAnim != null)
            {
                topAnim.Stop();
            }
            SLRedTopPrefab.transform.position = new Vector3(-1, 0, 0);
        }
        if (SLYellowTopPrefab != null)
        {
            Animation topAnim = SLYellowTopPrefab.GetComponent<Animation>();
            if (topAnim != null)
            {
                topAnim.Stop();
            }
            SLYellowTopPrefab.transform.position = new Vector3(0, 0, 0);
        }
        if (SLBlueTopPrefab != null)
        {
            Animation topAnim = SLBlueTopPrefab.GetComponent<Animation>();
            if (topAnim != null)
            {
                topAnim.Stop();
            }
            SLBlueTopPrefab.transform.position = new Vector3(1, 0, 0);
        }
        if (SLOrangeTopPrefab != null)
        {
            Animation topAnim = SLOrangeTopPrefab.GetComponent<Animation>();
            if (topAnim != null)
            {
                topAnim.Stop();
            }
            SLOrangeTopPrefab.transform.position = new Vector3(2, 0, 0);
        }    
    }

    public IEnumerator RippleAnim()
    {
        
        SLTopHit(0);
        yield return new WaitForSeconds(0.2f);
        SLTopHit(1);
        yield return new WaitForSeconds(0.2f);
        SLTopHit(2);
        yield return new WaitForSeconds(0.2f);
        SLTopHit(3);
        yield return new WaitForSeconds(0.2f);
        SLTopHit(4);
        yield return new WaitForSeconds(0.2f);
        
        //ResetAnims();
    }

    public void SLTopHit(float laneIndex)
    {
        if (laneIndex == 0)
        {
            if (SLGreenTopPrefab != null)
            {
                Animation topAnim = SLGreenTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit0");
                }
            }
        }
        else if (laneIndex == 1)
        {
            if (SLRedTopPrefab != null)
            {
                Animation topAnim = SLRedTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit1");
                }
            }
        }
        else if (laneIndex == 2)
        {
            if (SLYellowTopPrefab != null)
            {
                Animation topAnim = SLYellowTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit2");
                }
            }
        }
        else if (laneIndex == 3)
        {
            if (SLBlueTopPrefab != null)
            {
                Animation topAnim = SLBlueTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit3");
                }
            }
        }
        else if (laneIndex == 4)
        {
            if (SLOrangeTopPrefab != null)
            {
                Animation topAnim = SLOrangeTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit4");
                }
            }
        }
        else if (laneIndex == 7)
        {
            if (SLGreenTopPrefab != null)
            {
                Animation topAnim = SLGreenTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit0");
                }
            }
            if (SLRedTopPrefab != null)
            {
                Animation topAnim = SLRedTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit1");
                }
            }
            if (SLYellowTopPrefab != null)
            {
                Animation topAnim = SLYellowTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit2");
                }
            }
            if (SLBlueTopPrefab != null)
            {
                Animation topAnim = SLBlueTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit3");
                }
            }
            if (SLOrangeTopPrefab != null)
            {
                Animation topAnim = SLOrangeTopPrefab.GetComponent<Animation>();
                if (topAnim != null)
                {
                    topAnim.Stop();
                    topAnim.Play("TopHit4");
                }
            }
        }
        else
        {
            Debug.LogWarning("Invalid fret lane index: " + laneIndex);
        }
    }
    public void EnableSustainSparks(int fret)
    {
        if (sustainSparksPrefab != null)

        try
        {
            Quaternion rotation = Quaternion.Euler(0f, 180f, 0f);
            Vector3 position = new Vector3(fret, 0, 0);
            activeSustainSparks.Add(fret+2, Instantiate(sustainSparksPrefab, position, rotation));
        }
        catch (System.Exception)
        {
            if (activeSustainSparks.TryGetValue(fret+2, out GameObject spark))
            {
                Destroy(spark);
                activeSustainSparks.Remove(fret+2);
            }
            
        }
    }
    public void DisableSustainSparks(int xOffset)
    {
        if (sustainSparksPrefab != null)
        if (activeSustainSparks != null)
        {
            if (activeSustainSparks.TryGetValue(xOffset+2, out GameObject spark))
            {
                Destroy(spark);
                activeSustainSparks.Remove(xOffset+2);
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
