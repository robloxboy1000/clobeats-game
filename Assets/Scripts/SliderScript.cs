using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.GetComponent<Slider>().onValueChanged.AddListener(OnSliderValueChanged);
    }
    void OnSliderValueChanged(float value)
    {
        TMPro.TMP_Text valueText = gameObject.transform.Find("ValueText").GetComponent<TMPro.TMP_Text>();
        if (valueText != null)
        {
            valueText.text = value.ToString("F0");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
