using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScrollingTextManager : MonoBehaviour
{
    public TextMeshProUGUI tmpObject;
    [Tooltip("Pixels per second")]
    public float scrollSpeed = 100f;
    [Tooltip("Space between repeated copies in pixels")]
    public float gap = 20f;
    [Header("Edge Fade")]
    public bool edgeFadeEnabled = true;
    [Tooltip("Width of the fade overlay in pixels")]
    public float edgeWidth = 64f;
    [Tooltip("Color used for the fade overlays (usually background color)")]
    public Color fadeColor = Color.black;

    private TextMeshProUGUI cloneTextObject;
    private RectTransform rectA;
    private RectTransform rectB;
    private float textWidth;
    private Image leftFadeImage;
    private Image rightFadeImage;
    private Sprite leftFadeSprite;
    private Sprite rightFadeSprite;

    void Awake()
    {
        if (tmpObject == null)
        {
            Debug.LogError("tmpObject is not assigned on ScrollingTextManager.");
            enabled = false;
            return;
        }

        rectA = tmpObject.GetComponent<RectTransform>();

        // Create a sibling clone so both texts can slide independently
        cloneTextObject = Instantiate(tmpObject, tmpObject.transform.parent);
        rectB = cloneTextObject.GetComponent<RectTransform>();

        // Ensure same text
        cloneTextObject.text = tmpObject.text;

        // Force TMP to update preferredWidth
        tmpObject.ForceMeshUpdate();
        textWidth = tmpObject.preferredWidth;

        // Use left pivot so anchoredPosition is straightforward
        rectA.pivot = new Vector2(0f, 0.5f);
        rectB.pivot = new Vector2(0f, 0.5f);

        // Place them side-by-side
        rectA.anchoredPosition = new Vector2(0, 1f);
        rectB.anchoredPosition = new Vector2(textWidth + gap, 1f);

        // ensure parent has a RectMask2D so children are clipped to bounds
        Transform parent = tmpObject.transform.parent;
        if (parent != null && parent.GetComponent<RectMask2D>() == null)
        {
            parent.gameObject.AddComponent<RectMask2D>();
        }

        if (edgeFadeEnabled)
            CreateEdgeFades();
    }

    private void CreateEdgeFades()
    {
        Transform parent = rectA.parent;

        // Left fade
        GameObject leftGO = new GameObject("LeftEdgeFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        leftGO.transform.SetParent(parent, false);
        leftFadeImage = leftGO.GetComponent<Image>();
        leftFadeImage.raycastTarget = false;
        leftFadeSprite = CreateGradientSprite((int)Mathf.Max(1, edgeWidth), true);
        leftFadeImage.sprite = leftFadeSprite;
        leftFadeImage.type = Image.Type.Simple;
        RectTransform leftRT = leftGO.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0f, 0f);
        leftRT.anchorMax = new Vector2(0f, 1f);
        leftRT.pivot = new Vector2(0f, 0.5f);
        leftRT.sizeDelta = new Vector2(edgeWidth, 0f);
        leftRT.anchoredPosition = Vector2.zero;

        // Right fade
        GameObject rightGO = new GameObject("RightEdgeFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rightGO.transform.SetParent(parent, false);
        rightFadeImage = rightGO.GetComponent<Image>();
        rightFadeImage.raycastTarget = false;
        rightFadeSprite = CreateGradientSprite((int)Mathf.Max(1, edgeWidth), false);
        rightFadeImage.sprite = rightFadeSprite;
        rightFadeImage.type = Image.Type.Simple;
        RectTransform rightRT = rightGO.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(1f, 0f);
        rightRT.anchorMax = new Vector2(1f, 1f);
        rightRT.pivot = new Vector2(1f, 0.5f);
        rightRT.sizeDelta = new Vector2(edgeWidth, 0f);
        rightRT.anchoredPosition = Vector2.zero;

        // Ensure fades render above the text
        leftGO.transform.SetAsLastSibling();
        rightGO.transform.SetAsLastSibling();
    }

    private Sprite CreateGradientSprite(int width, bool left)
    {
        int w = Mathf.Max(1, width);
        int h = 4;
        Texture2D tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < w; x++)
        {
            float t = (w == 1) ? 0f : (float)x / (w - 1);
            float a = left ? (1f - t) : t; // left: opaque at left -> transparent to right
            Color col = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Clamp01(a * fadeColor.a));
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, col);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    void Update()
    {
        float move = scrollSpeed * Time.deltaTime;

        rectA.anchoredPosition -= new Vector2(move, 0f);
        rectB.anchoredPosition -= new Vector2(move, 0f);

        // When one text has completely passed to the left, move it to the right of the other
        if (rectA.anchoredPosition.x <= -textWidth - gap)
        {
            rectA.anchoredPosition = new Vector2(rectB.anchoredPosition.x + textWidth + gap, rectA.anchoredPosition.y);
        }

        if (rectB.anchoredPosition.x <= -textWidth - gap)
        {
            rectB.anchoredPosition = new Vector2(rectA.anchoredPosition.x + textWidth + gap, rectB.anchoredPosition.y);
        }
    }

    // Call to update the displayed text at runtime
    public void SetText(string text)
    {
        if (tmpObject == null || cloneTextObject == null) return;
        tmpObject.text = text;
        cloneTextObject.text = text;
        tmpObject.ForceMeshUpdate();
        textWidth = tmpObject.preferredWidth;
        rectA.anchoredPosition = Vector2.zero;
        rectB.anchoredPosition = new Vector2(textWidth + gap, 0f);
    }
}
