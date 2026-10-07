using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ProgressTracker : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private bool useInternalTimer = true;
    [SerializeField] private float segmentDuration = 20f;
    [SerializeField] private int segmentCount = 4;
    [SerializeField] private bool smoothMovement = true;

    [Header("Bar Layout")]
    [SerializeField] private float startInset = 30f;
    [SerializeField] private float endInset = 30f;
    [SerializeField] private float lineYOffset = 0f;

    [Header("Checkpoint (MarkProgress)")]
    [SerializeField] private Sprite checkpointSprite;
    [SerializeField] private Sprite reachedCheckpointSprite;
    [SerializeField] private float checkpointSize = 22f;

    [Header("Player Mark")]
    [SerializeField] private Sprite playerMarkSprite;
    [SerializeField] private float playerMarkSize = 30f;

    [Header("Runner")]
    [SerializeField] private Sprite[] runFrames = new Sprite[8];
    [SerializeField] private float runnerHeight = 70f;
    [SerializeField] private float runnerYOffset = 8f;
    [SerializeField] private float runnerXOffset = 0f;
    [SerializeField] private float runFps = 8f;

    [Header("Events")]
    public UnityEvent onFinished;

    [Header("Editor Preview")]
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

    public void SetExternalControl()
    {
        useInternalTimer = false;
    }

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
            Debug.Log("[ProgressTracker] finished");
            onFinished?.Invoke();
        }
    }

    void Layout(float p)
    {
        dirty = false;
        var rt = (RectTransform)transform;
        Rect r = rt.rect;
        float left = r.xMin + startInset;
        float right = r.xMax - endInset;
        float y = r.center.y + lineYOffset;

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
        Place(runner, rt, new Vector2(x + runnerXOffset, y + runnerYOffset), new Vector2(runnerHeight * aspect, runnerHeight), new Vector2(0.5f, 0f));
        runner.transform.SetAsLastSibling();
    }

    static void Place(Image img, RectTransform parent, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        var irt = img.rectTransform;
        irt.anchorMin = irt.anchorMax = parent.pivot;
        irt.pivot = pivot;
        irt.sizeDelta = size;
        irt.anchoredPosition = pos;
    }

    void AnimateRunner(bool paused)
    {
        if (runner == null || runFrames == null || runFrames.Length == 0) return;

        if (paused)
        {
            if (finished && runFrame != 0) { runFrame = 0; runner.sprite = CurrentRunFrame(); }
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
