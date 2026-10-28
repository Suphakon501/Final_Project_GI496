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

    [Header("4-Lane Y Positions (Top to Bottom)")]
    [SerializeField] private float yPosTop = 2.25f;    // แถวที่ 1 (บนสุด: ขึ้น / Q) -> พิกัด 3
    [SerializeField] private float yPosMid1 = 0.75f;   // แถวที่ 2 (ถัดลงมา: ซ้าย / A) -> พิกัด 1
    [SerializeField] private float yPosMid2 = -0.75f;  // แถวที่ 3 (ถัดลงมา: ขวา / D) -> พิกัด -1
    [SerializeField] private float yPosBottom = -2.25f;// แถวที่ 4 (ล่างสุด: ลง / S) -> พิกัด -3

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
        else transform.position += Vector3.left * MoveSpeed * Time.deltaTime;

        if (SecondsUntilLine() < -missAfter)
        {
            if (PlayerController.instance != null) PlayerController.instance.RegisterMissedNote();
            ConsumeMini(HitResult.Miss);
        }
    }

    public void InitializeMini(KeyCode key, LipidMovement owner)
    {
        assignedKey = key;
        parentLipid = owner;

        // จัดตำแหน่ง Y ตามพิกัด (3, 1, -1, -3)
        Vector3 pos = transform.position;
        if (assignedKey == KeyCode.W) pos.y = yPosTop;       // บนสุด (ขึ้น / W)
        else if (assignedKey == KeyCode.A) pos.y = yPosMid1;  // ถัดลงมา (ซ้าย / A)
        else if (assignedKey == KeyCode.D) pos.y = yPosMid2;  // ถัดลงมา (ขวา / D)
        else if (assignedKey == KeyCode.S) pos.y = yPosBottom;// ล่างสุด (ลง / S)
        transform.position = pos;
    }

    public void SetTargetSongTime(float songTime)
    {
        hasTargetTime = true;
        targetSongTime = songTime;
        FollowSongTime();
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