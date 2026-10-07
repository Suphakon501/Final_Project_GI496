using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// หลอด progress ใต้จอ: ตัววิ่งสีแดงวิ่งจาก START ไป FINISH ผ่าน checkpoint
// ตอนนี้ยังเป็นตัวอย่างชั่วคราว: ถึง checkpoint ถัดไปทุก segmentDuration วินาที
// ของจริงภายหลัง: ปิด useInternalTimer แล้วเรียก SetProgress(0..1) จากเวลาเพลง/ด่านแทน
// แปะบน Image ของหลอด (ProgressBar) แล้วมันสร้าง checkpoint / จุดผู้เล่น / ตัววิ่ง เป็นลูกให้เอง
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ProgressTracker : MonoBehaviour
{
    [Header("เวลา (ตัวอย่างชั่วคราว)")]
    [SerializeField] private bool useInternalTimer = true;
    [SerializeField] private float segmentDuration = 20f;    // กี่วินาทีถึงจุดถัดไป
    [SerializeField] private int segmentCount = 4;           // จำนวนช่วง (checkpoint ตรงกลาง = segmentCount - 1)
    [SerializeField] private bool smoothMovement = true;     // true = ค่อยๆ วิ่ง, false = กระโดดไปทีละจุดทุก segmentDuration

    [Header("ระยะบนหลอด (หน่วย UI จากขอบซ้าย/ขวาของกรอบ)")]
    [SerializeField] private float startInset = 30f;         // จุดเริ่ม (ตรงหัวหลอด)
    [SerializeField] private float endInset = 30f;           // จุดจบ (ตรงท้ายหลอด)
    [SerializeField] private float lineYOffset = 0f;         // เลื่อนเส้นที่วางของขึ้น/ลง

    [Header("Checkpoint (MarkProgress)")]
    [SerializeField] private Sprite checkpointSprite;
    [SerializeField] private Sprite reachedCheckpointSprite; // ปล่อยว่างได้ ถ้าใส่ จะเปลี่ยนรูปตอนวิ่งผ่านแล้ว
    [SerializeField] private float checkpointSize = 22f;

    [Header("จุดผู้เล่นบนหลอด (MarkPlayer)")]
    [SerializeField] private Sprite playerMarkSprite;
    [SerializeField] private float playerMarkSize = 30f;

    [Header("ตัววิ่ง (Run_1 - Run_8)")]
    [SerializeField] private Sprite[] runFrames = new Sprite[8];
    [SerializeField] private float runnerHeight = 70f;
    [SerializeField] private float runnerYOffset = 8f;       // ยกตัววิ่งขึ้นจากเส้น
    [SerializeField] private float runnerXOffset = 0f;       // เลื่อนตัววิ่งซ้าย/ขวาเทียบกับจุดผู้เล่น
    [SerializeField] private float runFps = 8f;              // GIF ต้นฉบับประมาณ 120-130ms ต่อเฟรม

    [Header("ถึง FINISH")]
    public UnityEvent onFinished;

    [Header("ตัวอย่างใน Editor (ตอนยังไม่กด Play)")]
    [Range(0f, 1f)][SerializeField] private float previewProgress = 0.35f;

    const string CheckpointName = "Checkpoint";
    const string PlayerMarkName = "PlayerMark";
    const string RunnerName = "Runner";

    private readonly List<Image> checkpoints = new List<Image>();
    private Image playerMark;
    private Image runner;

    private float elapsed;
    private float progress;
    private bool finished;
    private float runTimer;
    private int runFrame;
    private bool dirty = true;

    public float Progress => progress;
    public bool IsFinished => finished;
    public int ReachedCheckpoints => Mathf.FloorToInt(progress * segmentCount + 0.0001f);

    void OnEnable()
    {
        CollectChildren();
        dirty = true;
    }

    void OnValidate()
    {
        segmentCount = Mathf.Max(1, segmentCount);
        segmentDuration = Mathf.Max(0.1f, segmentDuration);
        dirty = true;
    }

    void OnRectTransformDimensionsChange()
    {
        dirty = true;
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        elapsed = 0f;
        finished = false;
        SetProgressInternal(0f);
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            if (dirty) Layout(previewProgress);
            return;
        }

        bool paused = PlayerController.isGameOver || finished;

        if (useInternalTimer && !paused)
        {
            elapsed += Time.deltaTime;
            float total = segmentDuration * segmentCount;
            float p = smoothMovement
                ? elapsed / total
                : Mathf.Floor(elapsed / segmentDuration) / segmentCount;
            SetProgressInternal(p);
        }
        else if (dirty)
        {
            Layout(progress);
        }

        AnimateRunner(paused);
    }

    // LevelManager / ChartPlayer เรียก: เลิกจับเวลาเอง ให้ระบบด่านป้อนความคืบหน้าแทน
    public void SetExternalControl()
    {
        useInternalTimer = false;
    }

    // สำหรับของจริงภายหลัง: ป้อนความคืบหน้า 0-1 เอง (ต้องปิด useInternalTimer)
    public void SetProgress(float value)
    {
        SetProgressInternal(value);
    }

    public void ResetProgress()
    {
        elapsed = 0f;
        finished = false;
        SetProgressInternal(0f);
    }

    void SetProgressInternal(float value)
    {
        progress = Mathf.Clamp01(value);
        Layout(progress);

        if (!finished && progress >= 1f)
        {
            finished = true;
            Debug.Log("[ProgressTracker] ถึง FINISH แล้ว");
            onFinished?.Invoke();
        }
    }

    // ---------- วาง / จัดตำแหน่ง ----------

    void Layout(float p)
    {
        dirty = false;
        var rt = (RectTransform)transform;
        Rect r = rt.rect;
        float left = r.xMin + startInset;
        float right = r.xMax - endInset;
        float y = r.center.y + lineYOffset;

        // checkpoint ตรงกลาง (ไม่รวมหัว/ท้าย เพราะมีป้าย START / FINISH อยู่แล้ว)
        int needed = Mathf.Max(0, segmentCount - 1);
        while (checkpoints.Count < needed) checkpoints.Add(CreateImage(CheckpointName));
        for (int i = 0; i < checkpoints.Count; i++)
        {
            var img = checkpoints[i];
            bool used = i < needed;
            if (img.gameObject.activeSelf != used) img.gameObject.SetActive(used);
            if (!used) continue;

            float t = (i + 1f) / segmentCount;
            bool reached = p >= t - 0.0001f;
            img.sprite = reached && reachedCheckpointSprite != null ? reachedCheckpointSprite : checkpointSprite;
            img.enabled = img.sprite != null;
            Place(img, rt, new Vector2(Mathf.Lerp(left, right, t), y), new Vector2(checkpointSize, checkpointSize), new Vector2(0.5f, 0.5f));
        }

        float x = Mathf.Lerp(left, right, p);

        if (playerMark == null) playerMark = CreateImage(PlayerMarkName);
        playerMark.sprite = playerMarkSprite;
        playerMark.enabled = playerMarkSprite != null;
        Place(playerMark, rt, new Vector2(x, y), new Vector2(playerMarkSize, playerMarkSize), new Vector2(0.5f, 0.5f));
        playerMark.transform.SetAsLastSibling();

        if (runner == null) runner = CreateImage(RunnerName);
        Sprite frame = CurrentRunFrame();
        runner.sprite = frame;
        runner.enabled = frame != null;
        float aspect = frame != null ? frame.rect.width / frame.rect.height : 1f;
        // pivot ล่าง-กลาง: ตัววิ่งยืนอยู่บนจุดผู้เล่น
        Place(runner, rt, new Vector2(x + runnerXOffset, y + runnerYOffset), new Vector2(runnerHeight * aspect, runnerHeight), new Vector2(0.5f, 0f));
        runner.transform.SetAsLastSibling();
    }

    static void Place(Image img, RectTransform parent, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        var irt = img.rectTransform;
        irt.anchorMin = irt.anchorMax = parent.pivot; // ให้ anchoredPosition ใช้พิกัดเดียวกับ parent.rect
        irt.pivot = pivot;
        irt.sizeDelta = size;
        irt.anchoredPosition = pos;
    }

    // ---------- ตัววิ่ง ----------

    void AnimateRunner(bool paused)
    {
        if (runner == null || runFrames == null || runFrames.Length == 0) return;

        if (paused)
        {
            if (finished && runFrame != 0) { runFrame = 0; runner.sprite = CurrentRunFrame(); } // ถึงเส้นชัยแล้วหยุดท่าแรก
            return;
        }

        runTimer += Time.deltaTime;
        float frameTime = 1f / Mathf.Max(1f, runFps);
        while (runTimer >= frameTime)
        {
            runTimer -= frameTime;
            runFrame = (runFrame + 1) % runFrames.Length;
            runner.sprite = CurrentRunFrame();
        }
    }

    Sprite CurrentRunFrame()
    {
        if (runFrames == null || runFrames.Length == 0) return null;
        var s = runFrames[Mathf.Clamp(runFrame, 0, runFrames.Length - 1)];
        return s != null ? s : runFrames[0];
    }

    // ---------- ลูก ----------

    void CollectChildren()
    {
        checkpoints.Clear();
        playerMark = null;
        runner = null;
        foreach (Transform child in transform)
        {
            var img = child.GetComponent<Image>();
            if (img == null) continue;
            if (child.name == CheckpointName) checkpoints.Add(img);
            else if (child.name == PlayerMarkName) playerMark = img;
            else if (child.name == RunnerName) runner = img;
        }
    }

    Image CreateImage(string objectName)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }
}
