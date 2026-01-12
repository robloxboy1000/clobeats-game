using UnityEngine;

[System.Serializable]
public class GuitarPlayer : MonoBehaviour
{
    
    
    public void SetUsername(string value)
    {
        PlayerPrefs.SetString("Username", value);
    }

    static string GetUsername()
    {
        return PlayerPrefs.GetString("Username", "Guest");
    }

    
    void Update()
    {
        
    }
}
