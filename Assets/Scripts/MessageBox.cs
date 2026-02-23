using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageBox : MonoBehaviour
{
    public GameObject mbPrefab;
    public static MessageBox Instance;
    void Awake()
    {
        if (Instance == null) Instance = this; else if (Instance != this) Destroy(gameObject);
    }
    public void Show(string text, string caption = "Message")
    {
        if (mbPrefab != null)
        {
            GameObject instance = Instantiate(mbPrefab);
            TMPro.TextMeshProUGUI title = instance.transform.Find("Canvas").Find("MessageBox").Find("Title").gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            TMPro.TextMeshProUGUI text1 = instance.transform.Find("Canvas").Find("MessageBox").Find("Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>();

            title.text = caption;
            text1.text = text;
        }
    }
}
