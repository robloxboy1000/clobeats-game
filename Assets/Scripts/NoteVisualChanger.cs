using UnityEngine;

public class NoteVisualChanger : MonoBehaviour
{
    public bool isStrum = true;
    public bool isHOPO = false;
    public bool isTap = false;

    public GameObject noteObject;
    public GameObject hopoNoteObject;
    public GameObject tapNoteObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isStrum)
        {
            if (noteObject != null)
            {
                noteObject.SetActive(true);
            }
            if (hopoNoteObject != null)
            {
                hopoNoteObject.SetActive(false);
            }
            if (tapNoteObject != null)
            {
                tapNoteObject.SetActive(false);
            }
        }
        if (isHOPO)
        {
            if (noteObject != null)
            {
                noteObject.SetActive(false);
            }
            if (hopoNoteObject != null)
            {
                hopoNoteObject.SetActive(true);
            }
            if (tapNoteObject != null)
            {
                tapNoteObject.SetActive(false);
            }
        }
        if (isTap)
        {
            if (noteObject != null)
            {
                noteObject.SetActive(false);
            }
            if (hopoNoteObject != null)
            {
                hopoNoteObject.SetActive(false);
            }
            if (tapNoteObject != null)
            {
                tapNoteObject.SetActive(true);
            }
        }
    }
}
