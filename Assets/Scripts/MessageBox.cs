using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class MessageBox : MonoBehaviour
{
    public GameObject mbPrefab;
    public static MessageBox Instance;
    void Awake()
    {
        if (Instance == null) Instance = this; else if (Instance != this) Destroy(gameObject);
    }
    public void Show(string text, string caption, Action actionOnMessageClose, bool muteGame = true)
    {
        if (mbPrefab != null)
        {
            GameObject instance = Instantiate(mbPrefab);
            TMPro.TextMeshProUGUI title = instance.transform.Find("Canvas/MessageBox/Title").gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            TMPro.TextMeshProUGUI text1 = instance.transform.Find("Canvas/MessageBox/Text").gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            Button button = instance.transform.Find("Canvas/MessageBox/DismissButton2").gameObject.GetComponent<Button>();

            title.text = caption;
            text1.text = text;
            if (muteGame)
            {
                MusicPlayer musicPlayer = FindAnyObjectByType<MusicPlayer>();
                musicPlayer.MuteAllAudio(true);
            }
            if (button != null)
            {
                if (actionOnMessageClose != null) button.onClick.AddListener(() => actionOnMessageClose.Invoke());
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }

        }
    }
}
