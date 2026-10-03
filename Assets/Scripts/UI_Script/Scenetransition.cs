using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Transition ข้าม Scene พร้อมหน้าโหลดที่เป็นอนิเมชั่นตัวละครวิ่งกลางจอ (ไม่มี Loading Bar):
/// จอค่อยๆ มืด -> แสดงตัวละครวิ่งระหว่างโหลด (หน่วงเวลาตามที่ตั้ง) -> จอค่อยๆ สว่างขึ้นใน Scene ใหม่
/// ไม่ต้องสร้าง object เองใน Scene สคริปต์จะสร้าง UI ให้อัตโนมัติและอยู่ข้าม Scene ได้
///
/// วิธีเรียกใช้จากที่ไหนก็ได้:  SceneTransition.Load("Level1");
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    private CanvasGroup fadeGroup;     // จอดำคลุมทั้งหน้า
    private CanvasGroup loadingGroup;  // กลุ่มที่เก็บตัวละครวิ่ง
    private bool busy;

    // ตัวละครวิ่งกลางจอ (เล่นทีละเฟรมจากรูปในโฟลเดอร์ Resources)
    private Image runner;
    private Sprite[] frames;
    private int frameIndex;
    private float frameTimer;

    // ---------- ปรับแต่งตรงนี้ ----------
    private static readonly Color BackgroundColor = Color.black;
    private const string FramesFolder = "LoadingRun"; // Assets/Resources/LoadingRun/
    private const float RunnerFps = 12f;              // ความเร็วอนิเมชั่น (เฟรมต่อวินาที)
    private const float DefaultFadeDuration = 0.4f;
    private const float DefaultMinLoadTime = 2.0f;    // หน่วงหน้าโหลดอย่างน้อยกี่วินาที

    /// <summary>โหลด Scene พร้อม transition และตัวละครวิ่ง</summary>
    /// <param name="minLoadTime">เวลาขั้นต่ำที่แสดงหน้าโหลด (วินาที) ใส่ 0 ถ้าไม่อยากหน่วง</param>
    public static void Load(string sceneName, float fadeDuration = DefaultFadeDuration,
                            float minLoadTime = DefaultMinLoadTime)
    {
        if (instance == null) Create();
        if (instance.busy) return; // กันกดซ้ำระหว่างกำลังเปลี่ยน Scene

        instance.StartCoroutine(instance.Routine(sceneName, fadeDuration, minLoadTime));
    }

    // ---------- สร้าง UI ทั้งหมดด้วยโค้ด ----------

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
        canvas.sortingOrder = 999; // อยู่หน้าสุดเสมอ

        // จอดำ
        fadeGroup = gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        Image bg = CreateImage("Background", transform, BackgroundColor);
        Stretch(bg.rectTransform);

        // กลุ่มหน้าโหลด (ซ่อนไว้ก่อน)
        GameObject loadingObj = new GameObject("Loading", typeof(RectTransform));
        loadingObj.transform.SetParent(transform, false);
        Stretch((RectTransform)loadingObj.transform);
        loadingGroup = loadingObj.AddComponent<CanvasGroup>();
        loadingGroup.alpha = 0f;

        // ตัวละครวิ่งกลางจอ
        frames = Resources.LoadAll<Sprite>(FramesFolder);
        System.Array.Sort(frames, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        if (frames.Length > 0)
        {
            runner = CreateImage("Runner", loadingObj.transform, Color.white);
            runner.sprite = frames[0];
            runner.preserveAspect = true;
            SetAnchored(runner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(-200f, -165f), new Vector2(200f, 165f)); // กรอบ 400x330 กลางจอ
        }
        else
        {
            Debug.LogWarning("SceneTransition: ไม่พบรูปใน Resources/" + FramesFolder);
        }
    }

    // ---------- ลำดับการเปลี่ยน Scene ----------

    private IEnumerator Routine(string sceneName, float fadeDuration, float minLoadTime)
    {
        busy = true;
        Time.timeScale = 1f;
        fadeGroup.blocksRaycasts = true; // บล็อกการคลิกระหว่าง transition

        // 1) จอค่อยๆ มืด
        yield return Fade(fadeGroup, 0f, 1f, fadeDuration);

        // 2) แสดงตัวละครวิ่ง แล้วเริ่มโหลดแบบ async (ยังไม่ให้สลับ Scene จนกว่าจะพร้อมและครบเวลาหน่วง)
        loadingGroup.alpha = 1f;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float timer = 0f;
        while (!(op.progress >= 0.9f && timer >= minLoadTime))
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3) สลับไป Scene ใหม่
        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        // 4) ซ่อนตัวละคร แล้วจอค่อยๆ สว่างขึ้น
        loadingGroup.alpha = 0f;
        yield return Fade(fadeGroup, 1f, 0f, fadeDuration);

        fadeGroup.blocksRaycasts = false;
        busy = false;
    }

    // เล่นอนิเมชั่นทีละเฟรม (ทำงานเฉพาะตอนหน้าโหลดแสดงอยู่)
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

    // ---------- ฟังก์ชันช่วยสร้าง UI ----------

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