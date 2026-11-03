using System.Collections.Generic;
using UnityEngine;

public enum HitResult { Perfect, Good, Bad, Miss }

public class MiniLipid : MonoBehaviour
{
    private static readonly List<MiniLipid> active = new List<MiniLipid>();
    public static IReadOnlyList<MiniLipid> Active => active;

    [Header("Settings")]
    public KeyCode assignedKey;
    [SerializeField] private float moveSpeed = 3.0f;
    public float hitLineX = -3.0f;

    [Header("4-Lane Y Positions (Top to Bottom: ซ้าย -> ล่าง -> ขึ้น -> ขวา)")]
    [SerializeField] private float yPosTop = 3.0f;     // แถวที่ 1 (บนสุด: ซ้าย / A) -> พิกัด 3.0
    [SerializeField] private float yPosMid1 = 1.0f;    // แถวที่ 2 (ถัดลงมา: ล่าง / S) -> พิกัด 1.0
    [SerializeField] private float yPosMid2 = -1.0f;   // แถวที่ 3 (ถัดลงมา: ขึ้น / W) -> พิกัด -1.0
    [SerializeField] private float yPosBottom = -3.0f; // แถวที่ 4 (ล่างสุด: ขวา / D) -> พิกัด -3.0

    [Header("Progressive Speed Settings")]
    [SerializeField] private float maxMoveSpeed = 13.0f;
    [SerializeField] private float speedIncreaseRate = 0.05f;

    [Header("Timing Window (seconds)")]
    [SerializeField] private float perfectWindow = 0.09f;
    [SerializeField] private float goodWindow = 0.2f;
    [SerializeField] private float missAfter = 0.23f;

    [Header("Particle Effects")]
    [SerializeField] private GameObject goodParticlePrefab;
    [SerializeField] private GameObject perfectParticlePrefab;
    [SerializeField] private GameObject badMissParticlePrefab;
    [SerializeField] private float particleLifetime = 0.6f;

    private LipidMovement parentLipid;
    private bool isFinished = false;
    private bool hasTargetTime = false;
    private float targetSongTime;
    private float holdDuration;
    private bool holdStarted;
    private LineRenderer holdTrail;
    private SpriteRenderer noteRenderer;

    public bool IsHold => holdDuration > 0f;
    public bool HoldStarted => holdStarted;
    public int HoldTickCount => !holdStarted || !UsingSongClock
        ? 0
        : Mathf.FloorToInt(Mathf.Max(0f, BeatManager.instance.songPositionInSeconds - targetSongTime) / BeatManager.instance.BeatDuration * 2f);

    public float MoveSpeed
    {
        get
        {
            float timePassed = Time.timeSinceLevelLoad;
            float calculatedSpeed = moveSpeed + (timePassed * speedIncreaseRate);
            return Mathf.Min(calculatedSpeed, maxMoveSpeed);
        }
    }

    bool UsingSongClock => hasTargetTime && BeatManager.instance != null;

    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);

    void Update()
    {
        if (PauseMenu.IsPaused || PlayerController.isGameOver || isFinished) return;

        if (UsingSongClock) FollowSongTime();
        else if (!holdStarted)
        {
            transform.position += Vector3.left * MoveSpeed * Time.deltaTime;
        }

        if (!holdStarted && SecondsUntilLine() < -missAfter)
        {
            if (PlayerController.instance != null) PlayerController.instance.RegisterMissedNote();
            ConsumeMini(HitResult.Miss);
        }
    }

    public void InitializeMini(KeyCode key, LipidMovement owner)
    {
        assignedKey = key;
        parentLipid = owner;

        // จัดตำแหน่ง Y ตามเลนใหม่ (ซ้าย -> ล่าง -> ขึ้น -> ขวา)
        Vector3 pos = transform.position;
        if (assignedKey == KeyCode.A) pos.y = yPosTop;       // บนสุด (ซ้าย) -> 3.0
        else if (assignedKey == KeyCode.S) pos.y = yPosMid1;  // ถัดลงมา (ล่าง) -> 1.0
        else if (assignedKey == KeyCode.W) pos.y = yPosMid2;  // ถัดลงมา (ขึ้น) -> -1.0
        else if (assignedKey == KeyCode.D) pos.y = yPosBottom;// ล่างสุด (ขวา) -> -3.0
        transform.position = pos;
    }

    public void SetTargetSongTime(float songTime)
    {
        hasTargetTime = true;
        targetSongTime = songTime;
        FollowSongTime();
    }

    public void SetHoldDuration(float seconds)
    {
        holdDuration = Mathf.Max(0f, seconds);
        if (holdDuration <= 0f) return;

        var trailObject = new GameObject("HoldTrail");
        trailObject.transform.SetParent(transform, false);
        trailObject.transform.localPosition = Vector3.zero;
        holdTrail = trailObject.AddComponent<LineRenderer>();
        holdTrail.useWorldSpace = false;
        holdTrail.positionCount = 2;
        holdTrail.SetPosition(0, Vector3.zero);
        holdTrail.SetPosition(1, Vector3.right * (holdDuration * MoveSpeed));
        holdTrail.startWidth = 0.16f;
        holdTrail.endWidth = 0.16f;
        holdTrail.startColor = new Color(1f, 0.82f, 0.2f, 0.9f);
        holdTrail.endColor = new Color(1f, 0.45f, 0.05f, 0.45f);
        holdTrail.sortingOrder = 4;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) holdTrail.material = new Material(shader);
    }

    public bool TryStartHold()
    {
        if (!IsHold || holdStarted || Mathf.Abs(SecondsUntilLine()) > missAfter) return false;
        holdStarted = true;
        noteRenderer = GetComponent<SpriteRenderer>();
        if (noteRenderer != null)
            noteRenderer.color = new Color(0.45f, 1f, 0.45f, noteRenderer.color.a);
        if (holdTrail != null)
        {
            holdTrail.startColor = Color.green;
            holdTrail.endColor = new Color(0.2f, 1f, 0.4f, 0.7f);
        }
        var p = transform.position;
        p.x = hitLineX;
        transform.position = p;
        return true;
    }

    public float HoldEndOffsetSeconds => !UsingSongClock
        ? -float.MaxValue
        : BeatManager.instance.songPositionInSeconds - (targetSongTime + holdDuration);
    public float HoldReleaseWindow => missAfter;
    public bool HoldKeyReleasedThisFrame => holdStarted && IsAssignedKeyUp();

    public bool IsOutsideTimingWindow => Mathf.Abs(SecondsUntilLine()) > missAfter;

    public bool IsHoldButtonStillDown
    {
        get
        {
            if (!holdStarted) return false;
            switch (assignedKey)
            {
                case KeyCode.W: return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
                case KeyCode.A: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
                case KeyCode.D: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
                case KeyCode.S: return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
                default: return Input.GetKey(assignedKey);
            }
        }
    }

    bool IsAssignedKeyUp()
    {
        switch (assignedKey)
        {
            case KeyCode.W: return Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
            case KeyCode.A: return Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow);
            case KeyCode.D: return Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow);
            case KeyCode.S: return Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow);
            default: return Input.GetKeyUp(assignedKey);
        }
    }

    float SecondsUntilLine()
    {
        if (UsingSongClock) return targetSongTime - BeatManager.instance.songPositionInSeconds;
        return (transform.position.x - hitLineX) / Mathf.Max(0.01f, MoveSpeed);
    }

    void FollowSongTime()
    {
        if (!UsingSongClock) return;
        var p = transform.position;
        p.x = hitLineX + (targetSongTime - BeatManager.instance.songPositionInSeconds) * MoveSpeed;
        transform.position = p;
    }

    public HitResult EvaluateAccuracy()
    {
        float secondsFromLine = Mathf.Abs(SecondsUntilLine());
        if (secondsFromLine <= perfectWindow) return HitResult.Perfect;
        if (secondsFromLine <= goodWindow) return HitResult.Good;
        return HitResult.Bad;
    }

    public static MiniLipid FindNextToHit()
    {
        MiniLipid next = null;
        float lowestX = float.MaxValue;
        foreach (var mini in active)
        {
            if (mini.isFinished) continue;
            float x = mini.transform.position.x;
            if (x < lowestX)
            {
                lowestX = x;
                next = mini;
            }
        }
        return next;
    }

    public void ConsumeMini(HitResult result)
    {
        if (isFinished) return;
        isFinished = true;

        GameObject particle = result == HitResult.Perfect ? perfectParticlePrefab
                            : result == HitResult.Good ? goodParticlePrefab
                            : badMissParticlePrefab;
        if (particle != null)
        {
            GameObject effect = Instantiate(particle, transform.position, Quaternion.identity);
            Destroy(effect, particleLifetime);
        }

        if (parentLipid != null) parentLipid.OnMiniFinished(result);
        Destroy(gameObject);
    }

    public bool IsLastInRow()
    {
        if (parentLipid != null) return parentLipid.IsLastPending;

        foreach (var mini in active)
        {
            if (mini != this && !mini.isFinished) return false;
        }
        return true;
    }
}