using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GradeUpdater : MonoBehaviour
{
    public Slider targetSlider;
    public TMPro.TMP_Text valueText;
    // Start is called before the first frame update
    void Start()
    {
        targetSlider.onValueChanged.AddListener(UpdateText);
        UpdateText(targetSlider.value);
    }

    void UpdateText(float value)
    {
        if (valueText != null)
        {
            valueText.text = "R.M. Value: " + value.ToString("F2");
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
