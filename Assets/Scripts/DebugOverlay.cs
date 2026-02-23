using UnityEngine;

public class DebugOverlay : MonoBehaviour
{
    public GameObject loadMidiObject;
    public GameObject loadChartObject;
    public GameObject autoplayToggleObject;

    UnityEngine.UI.Button loadMidiButton;
    UnityEngine.UI.Button loadChartButton;
    UnityEngine.UI.Toggle autoplayToggle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        loadMidiButton = loadMidiObject.transform.Find("SubmitButton").gameObject.GetComponent<UnityEngine.UI.Button>();
        loadChartButton = loadChartObject.transform.Find("SubmitButton").gameObject.GetComponent<UnityEngine.UI.Button>();
        autoplayToggle = autoplayToggleObject.transform.Find("AutoplayToggle").gameObject.GetComponent<UnityEngine.UI.Toggle>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
