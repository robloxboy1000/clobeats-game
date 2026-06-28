using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArgumentHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // Retrieves all arguments passed to the application
        string[] args = System.Environment.GetCommandLineArgs();

        foreach (string arg in args)
        {
            if (arg == "--autoplay")
            {
                LaneInputManager laneInputManager = FindAnyObjectByType<LaneInputManager>();
                laneInputManager.autoPlayEnabled = true;
            }
            else if (arg == "--disablePreLoading")
            {
                PlayerPrefsLoader pPl = FindAnyObjectByType<PlayerPrefsLoader>();
                pPl.autoLoad = false;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
