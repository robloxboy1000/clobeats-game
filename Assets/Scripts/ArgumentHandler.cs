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
            else if (arg == "--debug")
            {
                PlayerPrefsLoader pPl = FindAnyObjectByType<PlayerPrefsLoader>();
                pPl.autoLoad = false;
            }
            else if (arg == "--path")
            {
                
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
