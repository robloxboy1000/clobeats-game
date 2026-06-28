using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;

public class TMPInstanceMaker : MonoBehaviour
{
    static TMPInstanceMaker _instance;
    public static TMPInstanceMaker Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find an existing instance in scene
                _instance = FindAnyObjectByType<TMPInstanceMaker>();
                if (_instance == null)
                {
                    var go = new GameObject("_TMPInstanceMaker");
                    // hide from scene hierarchy during edit/play to avoid clutter
                    #if UNITY_EDITOR
                    go.hideFlags = HideFlags.HideAndDontSave;
                    #endif
                    _instance = go.AddComponent<TMPInstanceMaker>();
                }
            }
            return _instance;
        }
    }
    public Dictionary<int, GameObject> textObjects = new Dictionary<int, GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int CreateTextObject(string text, Transform parent, Vector2 position, Vector3 rotation, float size)
    {
        int hashCode = text.GetHashCode();
        GameObject textObject = new GameObject(hashCode.ToString());
        textObject.transform.SetParent(parent);
        textObject.transform.position = position;
        textObject.transform.rotation = Quaternion.Euler(rotation);
        var tmpComp = textObject.AddComponent<TextMeshProUGUI>();
        tmpComp.text = text;
        tmpComp.fontSize = size;
        tmpComp.alignment = TextAlignmentOptions.Center;
        
        textObjects.Add(hashCode, textObject);
        Debug.Log("[TMPInstanceMaker.CreateTextObject] TMP Text created with text \"" + text + "\" and hash code " + hashCode);
        return hashCode;
    }

    public IEnumerator CreateTextObjectAtGameTime(float gameTime, string text, Transform parent, Vector2 position, Vector3 rotation, float size)
    {
        while (Time.time < gameTime)
        {
            yield return null;
        }
        CreateTextObject(text, parent, position, rotation, size);
    }

    public IEnumerator CreateTextObjectAtDSPTime(double dspTime, string text, Transform parent, Vector2 position, Vector3 rotation, float size)
    {
        while (AudioSettings.dspTime < dspTime)
        {
            yield return null;
        }
        CreateTextObject(text, parent, position, rotation, size);
    }

    public void DeleteTextObject(int hash)
    {
        if (textObjects.TryGetValue(hash, out var textObject))
        {
            Destroy(textObject);
            textObjects.Remove(hash);
        }
        else
        {
            Debug.LogWarning("[TMPInstanceMaker.DeleteTextObject] Text object not found at hash code: " + hash);
        }
    }
}
