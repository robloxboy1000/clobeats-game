using UnityEngine;

public class HoldFPS : MonoBehaviour
{
    public KeyCode holdKey;
    void Start()
    {
        
    }
    void Update()
    {
        if (Input.GetKeyDown(holdKey))
        {
            Application.targetFrameRate = 1;
        }
        else
        {
            Application.targetFrameRate = int.MaxValue;
        }
    }
}
