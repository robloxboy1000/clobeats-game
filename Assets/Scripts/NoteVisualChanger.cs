using UnityEngine;

public class NoteVisualChanger : MonoBehaviour
{
    public enum NoteType
    {
        Forced,
        HOPO,
        Tap,
        Any,
        FretRelease
    }

    public NoteType currentNoteType;

    public GameObject noteObject;
    public GameObject hopoNoteObject;
    public GameObject tapNoteObject;
    public GameObject fretReleaseNoteObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (currentNoteType == NoteType.Forced)
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
            if (fretReleaseNoteObject != null)
            {
                fretReleaseNoteObject.SetActive(false);
            }
        }
        if (currentNoteType == NoteType.HOPO)
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
            if (fretReleaseNoteObject != null)
            {
                fretReleaseNoteObject.SetActive(false);
            }
        }
        if (currentNoteType == NoteType.Tap)
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
            if (fretReleaseNoteObject != null)
            {
                fretReleaseNoteObject.SetActive(false);
            }
        }
        if (currentNoteType == NoteType.FretRelease)
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
                tapNoteObject.SetActive(false);
            }
            if (fretReleaseNoteObject != null)
            {
                fretReleaseNoteObject.SetActive(true);
            }
        }
    }
}
