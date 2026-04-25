using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScrollViewAdapter : MonoBehaviour
{
    public GameObject prefab;
    public TextMeshPro countText;
    public ScrollRect scrollView;
    public RectTransform content;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void UpdateItems()
    {
        FetchItemModelFromServer("test", results => OnRecievedNewModel(results));
    }

    void OnRecievedNewModel(ItemModel model)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        var instance = Instantiate(prefab.gameObject) as GameObject;
        instance.transform.SetParent(content, false);
    }


    void InitializeItemView()
    {
        
    }

    void FetchItemModelFromServer(string value, Action<ItemModel> onDone)
    {
        var results = new ItemModel();
        results = new ItemModel();
        results.valueText2 = value;
        onDone(results);
    }

    public class ItemView
    {
        public TextMeshPro valueText;

        public ItemModel Model
        {
            set
            {
                if (value != null)
                {
                    valueText.text = value.valueText2;
                }
            }
        }
        public ItemView(Transform rootView)
        {
            valueText = rootView.Find("Marqee1/SongtitleText").GetComponent<TextMeshPro>();
        }
    }

    public class ItemModel
    {
        public string valueText2;
    }
}
