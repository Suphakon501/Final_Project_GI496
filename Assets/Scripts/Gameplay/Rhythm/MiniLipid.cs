using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitResult { Perfect, Good, Bad, Miss }

public class MiniLipid : MonoBehaviour
{
    private static readonly List<MiniLipid> active = new List<MiniLipid>();
    public static IReadOnlyList<MiniLipid> Active => active;
    private static Transform cachedHitline;

    [Header("Settings")]
    public KeyCode assignedKey;
    [SerializeField] private float moveSpeed = 3.0f;
    public float hitLineX = -3.0f;

[Header("4-Lane Y Positions (Top to Bottom: ซ้าย -> ขึ้น -> ลง -> ขวา)")]
    [SerializeField] private float yPosTop = 3.0f;     // แถวที่ 1 (บนสุด: ซ้าย / A) -> พิกัด 3.0
    [SerializeField] private float yPosMid1 = 1.0f;    // แถวที่ 2 (ถัดลงมา: ขึ้น / W) -> พิกัด 1.0
    [SerializeField] private float yPosMid2 = -1.0f;   // แถวที่ 3 (ถัดลงมา: ล่าง / S) -> พิกัด -1.0
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

    [Header("Hit Knock-Away (เด้งขึ้นแล้วร่วงตกจากจอ)")]
    [Tooltip("กดโดนโน้ตแล้วให้โน้ตเด้งขึ้นและร่วงหลุดกล้อง แทนการหายทันที")]
    [SerializeField] private bool knockAwayOnHit = true;
    [SerializeField] private float knockUpSpeed = 9f;
    [Tooltip("ความเร็วที่เด้งไปทางซ้าย (เข้าหาตัวละคร)")]
    [SerializeField] private float knockLeftSpeed = 3.5f;
    [Tooltip("สุ่มเบี่ยงซ้าย-ขวาเพิ่มเล็กน้อย (±)")]
    [SerializeField] private float knockSideJitter = 1f;
    [SerializeField] private float knockGravity = 26f;
    [Tooltip("ความเร็วหมุน (องศา/วินาที)")]
    [SerializeField] private float knockSpinSpeed = 540f;
    [SerializeField] private float knockLifetime = 1.2f;
    [Tooltip("ขนาดตอนเด้งสุด (1 = เท่าเดิม)")]
    [SerializeField] private float knockPopScale = 1.35f;
    [SerializeField, Range(0.01f, 0.5f)] private float knockPopPhase = 0.12f;

    [Header("Fly To Score UI")]
    [Tooltip("กดโดนแล้วให้โน้ตบินโค้งไปที่ตัวเลขคะแนน แล้วให้ UI คะแนนเด้ง")]
    [SerializeField] private bool flyToScore = true;
    [SerializeField] private float flyDuration = 0.35f;
    [SerializeField] private float flyArcHeight = 1.2f;
    [SerializeField] private float flySpinSpeed = 260f;
    [SerializeField, Range(0.05f, 1f)] private float flyEndScale = 0.3f;

    [Header("Long Note Visuals (LDL)")]
    [SerializeField] private Sprite holdHeadSprite;
    [SerializeField] private Sprite holdBodySprite;
    [SerializeField] private Sprite holdTailSprite;

    private LipidMovement parentLipid;
    private bool isFinished = false;
    private bool hasTargetTime = false;
    private float targetSongTime;
    private float holdDuration;
    private bool holdStarted;
    private LineRenderer holdTrail;
    private SpriteRenderer noteRenderer;
    private SpriteRenderer holdHeadRenderer;
    private SpriteRenderer holdBodyRenderer;
    private SpriteRenderer holdTailRenderer;
    private Bounds holdMainBounds;

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

        if (!holdStarted)
        {
            if (UsingSongClock) FollowSongTime();
            else transform.position += Vector3.left * MoveSpeed * Time.deltaTime;
        }

        if (holdDuration > 0f) UpdateHoldVisual();

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

        // ใช้ตำแหน่งลูกศรจริงในซีนเป็นจุดตัดสิน: โน้ตวิ่งมาหยุดที่ลูกศรของปุ่มนั้น
        if (TryGetLaneAnchor(assignedKey, out Vector3 anchor))
        {
            hitLineX = anchor.x;

            Vector3 anchored = transform.position;
            anchored.y = anchor.y;
            transform.position = anchored;
            return;
        }

        // Fallback: ตำแหน่ง Y จากพรีแฟ็บ (ซีนที่ไม่มี Hitline/ลูกศร)
        Vector3 pos = transform.position;
        if (assignedKey == KeyCode.A) pos.y = yPosTop;
        else if (assignedKey == KeyCode.W) pos.y = yPosMid1;
        else if (assignedKey == KeyCode.S) pos.y = yPosMid2;
        else if (assignedKey == KeyCode.D) pos.y = yPosBottom;
        transform.position = pos;
    }

    static bool TryGetLaneAnchor(KeyCode key, out Vector3 anchor)
    {
        anchor = Vector3.zero;

        if (cachedHitline == null)
        {
            GameObject hitline = GameObject.Find("Hitline");
            if (hitline != null) cachedHitline = hitline.transform;
        }
        if (cachedHitline == null) return false;

        string arrowName;
        switch (key)
        {
            case KeyCode.A: arrowName = "Left2_1"; break;
            case KeyCode.W: arrowName = "Up2_1"; break;
            case KeyCode.S: arrowName = "Down2_1"; break;
            case KeyCode.D: arrowName = "Right2_0"; break;
            default: return false;
        }

        Transform arrow = cachedHitline.Find(arrowName);
        if (arrow == null) return false;

        anchor = arrow.position;
        return true;
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

        if (holdHeadSprite != null && holdBodySprite != null && holdTailSprite != null)
        {
            BuildHoldVisual();
            return;
        }

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

    void BuildHoldVisual()
    {
        if (holdHeadRenderer != null)
        {
            UpdateHoldVisual();
            return;
        }

        noteRenderer = GetComponent<SpriteRenderer>();
        if (noteRenderer == null) return;

        holdMainBounds = noteRenderer.sprite != null
            ? noteRenderer.sprite.bounds
            : new Bounds(Vector3.zero, Vector3.one);

        holdHeadRenderer = CreateHoldPart("HoldHead", holdHeadSprite);
        holdBodyRenderer = CreateHoldPart("HoldBody", holdBodySprite);
        holdTailRenderer = CreateHoldPart("HoldTail", holdTailSprite);

        noteRenderer.enabled = false;
        UpdateHoldVisual();
    }

    SpriteRenderer CreateHoldPart(string partName, Sprite sprite)
    {
        var partObject = new GameObject(partName);
        partObject.transform.SetParent(transform, false);

        var partRenderer = partObject.AddComponent<SpriteRenderer>();
        partRenderer.sprite = sprite;
        partRenderer.color = noteRenderer != null ? noteRenderer.color : Color.white;
        partRenderer.sortingOrder = noteRenderer != null ? noteRenderer.sortingOrder : 5;
        return partRenderer;
    }

    void UpdateHoldVisual()
    {
        if (holdHeadRenderer == null || holdBodyRenderer == null || holdTailRenderer == null) return;

        float visualDuration = holdDuration;
        if (holdStarted && UsingSongClock)
            visualDuration = Mathf.Clamp(
                targetSongTime + holdDuration - BeatManager.instance.songPositionInSeconds,
                0f, holdDuration);

        float noteScale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float localLength = (visualDuration * MoveSpeed) / noteScale;

        Bounds head = holdHeadRenderer.sprite.bounds;
        Bounds body = holdBodyRenderer.sprite.bounds;
        Bounds tail = holdTailRenderer.sprite.bounds;

        float left = holdMainBounds.min.x;
        float centerY = holdMainBounds.center.y;

        holdHeadRenderer.transform.localPosition = new Vector3(
            left - head.min.x,
            centerY - head.center.y,
            0f);

        float headRight = left + head.size.x;
        float tailRight = left + localLength;
        float tailLeft = tailRight - tail.size.x;
        bool showBody = tailLeft > headRight;
        holdBodyRenderer.enabled = showBody;
        holdTailRenderer.enabled = showBody;

        float bodyWidth = Mathf.Max(0.001f, tailLeft - headRight);
        float bodyScaleX = bodyWidth / Mathf.Max(0.001f, body.size.x);

        holdBodyRenderer.transform.localScale = new Vector3(bodyScaleX, 1f, 1f);
        holdBodyRenderer.transform.localPosition = new Vector3(
            headRight - bodyScaleX * body.min.x,
            centerY - body.center.y,
            0f);

        holdTailRenderer.transform.localPosition = new Vector3(
            tailRight - tail.max.x,
            centerY - tail.center.y,
            0f);
    }

    void SetHoldColor(Color color)
    {
        if (noteRenderer != null) noteRenderer.color = color;
        if (holdHeadRenderer != null) holdHeadRenderer.color = color;
        if (holdBodyRenderer != null) holdBodyRenderer.color = color;
        if (holdTailRenderer != null) holdTailRenderer.color = color;
    }

    public bool TryStartHold()
    {
        if (!IsHold || holdStarted || Mathf.Abs(SecondsUntilLine()) > missAfter) return false;
        holdStarted = true;
        if (noteRenderer == null) noteRenderer = GetComponent<SpriteRenderer>();
        SetHoldColor(new Color(0.45f, 1f, 0.45f, noteRenderer != null ? noteRenderer.color.a : 1f));
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

    // เลือกโน้ตของ "เลน/ปุ่ม" ที่กดเท่านั้น (ไม่สนโน้ตเลนอื่น) แล้วเอาตัวที่ถึงคิวก่อน
    public static MiniLipid FindNextForKey(KeyCode key)
    {
        MiniLipid next = null;
        float earliest = float.MaxValue;
        foreach (var mini in active)
        {
            if (mini.isFinished || mini.HoldStarted || mini.assignedKey != key) continue;

            float untilLine = mini.SecondsUntilLine();
            if (Mathf.Abs(untilLine) > mini.missAfter) continue;

            if (untilLine < earliest)
            {
                earliest = untilLine;
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

        SpawnImpactBurst(result);

        if (parentLipid != null) parentLipid.OnMiniFinished(result);

        if (result == HitResult.Miss || !knockAwayOnHit)
        {
            Destroy(gameObject);
            return;
        }

        // กดโดนแล้วโน้ตจะบินโค้งไปที่ตัวเลขคะแนน (ถ้าหาจุดหมายไม่เจอจะเด้งร่วงแทน)
        active.Remove(this);

        if (flyToScore && PlayerController.TryGetScoreWorldPosition(out Vector3 scorePosition))
            StartCoroutine(FlyToScoreRoutine(scorePosition));
        else
            StartCoroutine(KnockAwayRoutine());
    }

    void SpawnImpactBurst(HitResult result)
    {
        Color sparkColor;
        int count;
        switch (result)
        {
            case HitResult.Perfect: sparkColor = new Color(1f, 0.88f, 0.45f); count = 18; break;
            case HitResult.Good: sparkColor = new Color(0.45f, 0.95f, 1f); count = 12; break;
            case HitResult.Bad: sparkColor = new Color(1f, 0.45f, 0.4f); count = 9; break;
            default: sparkColor = new Color(0.8f, 0.25f, 0.35f); count = 8; break;
        }

        bool perfect = result == HitResult.Perfect;
        ProcParticles.Burst(transform.position, sparkColor, count, perfect ? 8f : 5.5f, 0.45f, 0.42f);

        if (perfect)
            ProcParticles.Shockwave(transform.position, new Color(1f, 0.92f, 0.6f, 0.85f), 0.3f, 2f, 0.3f);
        else if (result == HitResult.Miss)
            ProcParticles.Shockwave(transform.position, new Color(0.9f, 0.25f, 0.3f, 0.7f), 0.25f, 1.3f, 0.35f);
    }

    IEnumerator FlyToScoreRoutine(Vector3 target)
    {
        if (holdTrail != null) holdTrail.enabled = false;
        foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;

        Vector3 start = transform.position;
        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.rotation;
        float duration = Mathf.Max(0.12f, flyDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (PauseMenu.IsPaused)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            Vector3 position = Vector3.Lerp(start, target, eased);
            position.y += flyArcHeight * Mathf.Sin(t * Mathf.PI);
            transform.position = position;
            transform.rotation = startRotation * Quaternion.Euler(0f, 0f, flySpinSpeed * t);
            transform.localScale = startScale * Mathf.Lerp(1f, flyEndScale, eased);

            yield return null;
        }

        PlayerController.PopScoreUI();
        ProcParticles.Shockwave(target, new Color(1f, 0.95f, 0.7f, 0.9f), 0.15f, 0.9f, 0.2f);
        Destroy(gameObject);
    }

    IEnumerator KnockAwayRoutine()
    {
        if (holdTrail != null) holdTrail.enabled = false;
        foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;

        // เด้งไปทางซ้ายเข้าหาตัวละครเป็นหลัก แล้วเบี่ยงซ้าย-ขวานิดหน่อยให้ดูเป็นธรรมชาติ
        float drift = -Mathf.Abs(knockLeftSpeed) + Random.Range(-knockSideJitter, knockSideJitter);
        Vector3 velocity = new Vector3(drift, knockUpSpeed, 0f);
        Vector3 startScale = transform.localScale;
        float spin = knockSpinSpeed * (Random.value < 0.5f ? -1f : 1f);
        float popPhase = Mathf.Max(0.0001f, knockPopPhase);
        float elapsed = 0f;

        while (elapsed < knockLifetime)
        {
            if (PauseMenu.IsPaused)
            {
                yield return null;
                continue;
            }

            float dt = Time.deltaTime;
            elapsed += dt;

            velocity.y -= knockGravity * dt;
            transform.position += velocity * dt;
            transform.Rotate(0f, 0f, spin * dt);

            float t = Mathf.Clamp01(elapsed / knockLifetime);
            float scale = t < popPhase
                ? Mathf.Lerp(1f, knockPopScale, t / popPhase)
                : Mathf.Lerp(knockPopScale, 0.2f, (t - popPhase) / (1f - popPhase));
            transform.localScale = startScale * Mathf.Max(0.01f, scale);

            yield return null;
        }

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
