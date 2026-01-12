using System.Collections;
using UnityEngine;

// "nomming" is already handled by NoteDeletor.
public class LightUpAndNom : MonoBehaviour
{
    public GameObject strumHat;
    public GameObject hopoHat;
    public KeyCode keyToPress;
    public KeyCode strumUpKey;
    public KeyCode strumKey;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(keyToPress) && Input.GetKeyDown(strumKey)
        || (Input.GetKeyDown(keyToPress) && Input.GetKey(strumKey)))
        {
            
                StartCoroutine(hit());
            
        }
        if (Input.GetKey(keyToPress) && Input.GetKeyDown(strumUpKey)
        || (Input.GetKeyDown(keyToPress) && Input.GetKey(strumUpKey)))
        {
            
                StartCoroutine(hit());
            
        }
        if (Input.GetKeyDown(keyToPress) && !Input.GetKey(strumKey) && !Input.GetKey(strumUpKey))
        {
            //Debug.Log("Player HOPOed");
            StartCoroutine(hopoHitActive());
        }
        if (Input.GetKeyUp(keyToPress))
        {
            StartCoroutine(hopoHitInactive());
        }
        

    }
    IEnumerator hit()
    {
        strumHat.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        strumHat.SetActive(false);
    }
    IEnumerator hopoHitActive()
    {
        hopoHat.SetActive(true);
        yield return new WaitForSeconds(0.05f);
        hopoHat.GetComponentInChildren<BoxCollider>().enabled = false;
    }
    IEnumerator hopoHitInactive()
    {
        hopoHat.SetActive(false);
        yield return new WaitForSeconds(0.05f);
        hopoHat.GetComponentInChildren<BoxCollider>().enabled = true;
    }


}
