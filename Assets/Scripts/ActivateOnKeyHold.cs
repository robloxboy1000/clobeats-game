using UnityEngine;

public class ActivateOnKeyHold : MonoBehaviour
{
    public KeyCode heldKey = KeyCode.Tab;
    public GameObject objectToActivate;
    public bool toggleMode = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!toggleMode)
        {
            if (Input.GetKeyDown(heldKey))
            {
                if (objectToActivate != null)
                {
                    objectToActivate.SetActive(true);
                }
            }
            else
            {
                if (objectToActivate != null)
                {
                    objectToActivate.SetActive(false);
                }
            }
        }
        else
        {
            if (Input.GetKeyUp(heldKey))
            {
                if (objectToActivate != null)
                {
                    if (objectToActivate.activeSelf)
                    {
                        objectToActivate.SetActive(false);
                    }
                    else
                    {
                        objectToActivate.SetActive(true);
                    }
                }
            }
        }
        
    }
}
