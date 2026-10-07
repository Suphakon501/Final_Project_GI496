using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum HitResult { Perfect, Good, Bad, Miss }

public class MiniLipid : MonoBehaviour
{
    private static readonly List<MiniLipid> active = new List<MiniLipid>();
    public static IReadOnlyList<MiniLipid> Active => active;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI letterText;

    [Header("Settings")]
    public KeyCode assignedKey;
    [SerializeField] private float moveSpeed = 3.0f;
    public float hitLineX = -3.0f;

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

    public float MoveSpeed => moveSpeed;

    bool UsingSongClock => hasTargetTime && BeatManager.instance != null;

    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);

    void Start()
    {
        UpdateVisual();
    }

    void Update()
    {
        if (PlayerController.isGameOver || isFinished) return;

        if (UsingSongClock) FollowSongTime();
        else transform.position += Vector3.left * moveSpeed * Time.deltaTime;

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
        UpdateVisual();
    }

    public void SetTargetSongTime(float songTime)
    {
        hasTargetTime = true;
        targetSongTime = songTime;
        FollowSongTime();
    }

    void UpdateVisual()
    {
        if (letterText != null) letterText.text = assignedKey.ToString();
    }

    float SecondsUntilLine()
    {
        if (UsingSongClock) return targetSongTime - BeatManager.instance.songPositionInSeconds;
        return (transform.position.x - hitLineX) / Mathf.Max(0.01f, moveSpeed);
    }

    void FollowSongTime()
    {
        if (!UsingSongClock) return;
        var p = transform.position;
        p.x = hitLineX + (targetSongTime - BeatManager.instance.songPositionInSeconds) * moveSpeed;
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
