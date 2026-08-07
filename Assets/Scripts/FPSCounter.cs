using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    public TMPro.TMP_Text fpsText; // Assign a UI Text component in the Inspector
    private float deltaTime = 0.0f;
    public float currentFps = 0.0f;
    public float updateTime = 1f;
    public float gameTime = 0f;
    public float lastGameTime = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f; // Smoothen the delta time
        gameTime = Time.time - lastGameTime;
        if (gameTime >= updateTime)
        {
            currentFps = 1.0f / deltaTime;
            fpsText.text = Mathf.RoundToInt(currentFps).ToString() + " FPS";
            lastGameTime = Time.time;
            gameTime = 0f;
        }
    }
}
