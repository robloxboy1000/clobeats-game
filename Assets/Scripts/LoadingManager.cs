using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public TMPro.TextMeshProUGUI loadingText; // Reference to your UI Text
    public Image loadingImage; // Reference to your UI Image (optional)
    public Canvas loadingCanvas; // Reference to your loading canvas

    private AsyncOperation asyncLoad;
    Color invisibleColor;

    void Start()
    {
        if (loadingImage != null)
        {
            invisibleColor = loadingImage.color;
            invisibleColor.a = 0f;
        }
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(false); // Hide loading canvas initially
            DontDestroyOnLoad(loadingCanvas.gameObject); // Persist across scenes
        }
    }
    public async void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, bool enableFade = true)
    {
        await System.Threading.Tasks.Task.Delay(500); // Small delay to ensure UI updates
        StartCoroutine(LoadAsynchronously(sceneName, mode));
    }

    IEnumerator FadeOutCoroutine()
    {
        float duration = 1f; // Duration for the fill animation
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fillAmount = Mathf.Lerp(1f, 0f, elapsed / duration);
            if (loadingImage != null)
            {
                Color tempColor = loadingImage.color;
                tempColor.a = fillAmount;
                loadingImage.color = tempColor;
            }
            yield return null;
        }

        if (loadingImage != null)
        {
            loadingImage.color = invisibleColor;
        }
    }

    IEnumerator FadeInCoroutine()
    {
        float duration = 1f; // Duration for the fill animation
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fillAmount = Mathf.Lerp(0f, 1f, elapsed / duration);
            if (loadingImage != null)
            {
                Color tempColor = loadingImage.color;
                tempColor.a = fillAmount;
                loadingImage.color = tempColor;
            }
            yield return null;
        }

        if (loadingImage != null)
        {
            loadingImage.color = invisibleColor;
        }
    }


    IEnumerator LoadAsynchronously(string sceneName, LoadSceneMode mode)
    {
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(true); // Hide loading canvas
        }
        StartCoroutine(FadeInCoroutine());
        yield return new WaitForSecondsRealtime(1f);
        if (loadingText != null)
        {
            loadingText.enabled = true; // Show loading text
        }
        asyncLoad = SceneManager.LoadSceneAsync(sceneName, mode);
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = false; // Prevent immediate activation
        

            while (!asyncLoad.isDone)
            {
                
                // Update loading bar and text
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f); // Progress goes from 0 to 0.9
                
                if (loadingText != null)
                {
                    loadingText.text = "Loading: " + (progress * 100).ToString("F0") + "%";
                }
                else
                {
                    Debug.LogWarning("Loading Text reference is missing! (Progress: " + (progress * 100).ToString("F0") + "%)");
                }

                // If scene is almost loaded, allow activation (or wait for user input)
                if (asyncLoad.progress >= 0.9f)
                {
                    if (loadingText != null)
                    {
                        loadingText.text = "Loading: 100%";
                        loadingText.enabled = false; // Hide loading textS
                    }
                    asyncLoad.allowSceneActivation = true;
                    StartCoroutine(FadeOutCoroutine());
                    yield return new WaitForSecondsRealtime(1f);
                    if (loadingCanvas != null)
                    {
                        loadingCanvas.gameObject.SetActive(false); // Hide loading canvas
                    }

                }

                yield return null;
            }
            
        }
    }
}
