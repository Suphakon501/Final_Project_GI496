using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    private CanvasGroup fadeGroup;
    private CanvasGroup loadingGroup;
    private bool busy;

    private Image runner;
    private Sprite[] frames;
    private int frameIndex;
    private float frameTimer;

    private static readonly Color BackgroundColor = Color.black;
    private const string FramesFolder = "LoadingRun";
    private const float RunnerFps = 12f;
    private const float DefaultFadeDuration = 0.4f;
    private const float DefaultMinLoadTime = 2.0f;

    public static void Load(string sceneName, float fadeDuration = DefaultFadeDuration,
                            float minLoadTime = DefaultMinLoadTime)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneTransition] Cannot load scene '{sceneName}': add it to File > Build Profiles > Scene List");
            return;
        }

        if (instance == null) Create();
        if (instance.busy) return;

        instance.StartCoroutine(instance.Routine(sceneName, fadeDuration, minLoadTime));
    }

    private static void Create()
    {
        GameObject root = new GameObject("SceneTransition");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<SceneTransition>();
        instance.Build();
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        fadeGroup = gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        Image bg = CreateImage("Background", transform, BackgroundColor);
        Stretch(bg.rectTransform);

        GameObject loadingObj = new GameObject("Loading", typeof(RectTransform));
        loadingObj.transform.SetParent(transform, false);
        Stretch((RectTransform)loadingObj.transform);
        loadingGroup = loadingObj.AddComponent<CanvasGroup>();
        loadingGroup.alpha = 0f;

        frames = Resources.LoadAll<Sprite>(FramesFolder);
        System.Array.Sort(frames, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        if (frames.Length > 0)
        {
            runner = CreateImage("Runner", loadingObj.transform, Color.white);
            runner.sprite = frames[0];
            runner.preserveAspect = true;
            SetAnchored(runner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-200f, -165f), new Vector2(200f, 165f));
        }
        else
        {
            Debug.LogWarning("SceneTransition: no sprites found in Resources/" + FramesFolder);
        }
    }

    private IEnumerator Routine(string sceneName, float fadeDuration, float minLoadTime)
    {
        busy = true;
        Time.timeScale = 1f;
        fadeGroup.blocksRaycasts = true;

        yield return Fade(fadeGroup, 0f, 1f, fadeDuration);

        loadingGroup.alpha = 1f;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float timer = 0f;
        while (!(op.progress >= 0.9f && timer >= minLoadTime))
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        loadingGroup.alpha = 0f;
        yield return Fade(fadeGroup, 1f, 0f, fadeDuration);

        fadeGroup.blocksRaycasts = false;
        busy = false;
    }

    private void Update()
    {
        if (runner == null || loadingGroup == null || loadingGroup.alpha <= 0f) return;

        frameTimer += Time.unscaledDeltaTime;
        float step = 1f / RunnerFps;
        if (frameTimer >= step)
        {
            frameTimer -= step;
            frameIndex = (frameIndex + 1) % frames.Length;
            runner.sprite = frames[frameIndex];
        }
    }

    private IEnumerator Fade(CanvasGroup g, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        g.alpha = to;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(RectTransform rect, Vector2 min, Vector2 max,
                                    Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
