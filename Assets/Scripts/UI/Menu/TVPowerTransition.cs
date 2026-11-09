using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TVPowerTransition : MonoBehaviour
{
    private static TVPowerTransition instance;

    private RectTransform topBlack;
    private RectTransform bottomBlack;
    private RectTransform lineRect;
    private CanvasGroup lineGroup;
    private bool busy;

    public static void PlayOpen()
    {
        EnsureInstance();
        instance.StartCoroutine(instance.Animate(opening: true));
    }

    public static IEnumerator Close()
    {
        EnsureInstance();
        yield return instance.Animate(opening: false);
    }

    static void EnsureInstance()
    {
        if (instance != null) return;

        var root = new GameObject("TVPowerTransition");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<TVPowerTransition>();
        instance.Build();
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        topBlack = CreateImage("Top Black", transform, Color.black).rectTransform;
        bottomBlack = CreateImage("Bottom Black", transform, Color.black).rectTransform;

        var line = new GameObject("CRT Line", typeof(RectTransform), typeof(CanvasGroup));
        line.transform.SetParent(transform, false);
        var lineRect = line.GetComponent<RectTransform>();
        lineRect.anchorMin = Vector2.zero;
        lineRect.anchorMax = Vector2.one;
        lineRect.offsetMin = Vector2.zero;
        lineRect.offsetMax = Vector2.zero;
        this.lineRect = lineRect;
        lineGroup = line.GetComponent<CanvasGroup>();
        lineGroup.blocksRaycasts = false;
        CreateLine("Line Glow", line.transform, new Color(1f, 1f, 1f, 0.5f), 12f);
        CreateLine("Line Core", line.transform, Color.white, 2f);

        SetWindow(0f);
        lineGroup.alpha = 0f;
        this.lineRect.localScale = new Vector3(0f, 1f, 1f);
    }

    IEnumerator Animate(bool opening)
    {
        if (busy) yield break;
        busy = true;

        float duration = opening ? 0.7f : 0.55f;
        float elapsed = 0f;
        lineGroup.alpha = 1f;
        SetWindow(opening ? 0f : 1f);
        lineRect.localScale = new Vector3(opening ? 0.01f : 1f, 1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = opening ? 1f - Mathf.Pow(1f - t, 3f) : t * t;
            SetWindow(opening ? eased : 1f - eased);
            float lineWidth = opening
                ? Mathf.Lerp(0.01f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.25f)))
                : 1f - Mathf.SmoothStep(0.68f, 1f, t);
            lineRect.localScale = new Vector3(lineWidth, 1f, 1f);
            lineGroup.alpha = opening
                ? 1f - Mathf.SmoothStep(0.4f, 1f, t)
                : 1f - Mathf.SmoothStep(0.78f, 1f, t);
            yield return null;
        }

        SetWindow(opening ? 1f : 0f);
        lineGroup.alpha = 0f;
        lineRect.localScale = new Vector3(opening ? 1f : 0f, 1f, 1f);
        busy = false;
    }

    void SetWindow(float visibleHeight)
    {
        visibleHeight = Mathf.Clamp01(visibleHeight);
        topBlack.anchorMin = new Vector2(0f, 0.5f + visibleHeight * 0.5f);
        topBlack.anchorMax = Vector2.one;
        topBlack.offsetMin = Vector2.zero;
        topBlack.offsetMax = Vector2.zero;

        bottomBlack.anchorMin = Vector2.zero;
        bottomBlack.anchorMax = new Vector2(1f, 0.5f - visibleHeight * 0.5f);
        bottomBlack.offsetMin = Vector2.zero;
        bottomBlack.offsetMax = Vector2.zero;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void CreateLine(string name, Transform parent, Color color, float height)
    {
        var image = CreateImage(name, parent, color);
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(0f, height);
    }
}
