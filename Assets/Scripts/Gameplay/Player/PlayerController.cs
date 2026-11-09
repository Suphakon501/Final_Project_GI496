using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public static bool isGameOver = false;

    // รองรับปุ่มตามลำดับเลนใหม่: ซ้าย (A), ล่าง (S), ขึ้น (W), ขวา (D)
    private static readonly KeyCode[] InputKeys = {
        KeyCode.A, KeyCode.LeftArrow,
        KeyCode.S, KeyCode.DownArrow,
        KeyCode.W, KeyCode.UpArrow,
        KeyCode.D, KeyCode.RightArrow
    };

    private const int PerfectScore = 300;
    private const int GoodScore = 100;
    private const int BadScore = 25;
    private const int HoldTickScore = 10;
    private const int HoldTailBonus = 25;
    private const int PerfectChainLength = 6;

    [Header("Score System")]
    public int score = 0;
    public TextMeshProUGUI scoreText;
    [SerializeField] private SpriteNumber scoreNumber;
    [Header("Score Pop (ตอนโน้ตบินมาถึง)")]
    [SerializeField] private float scorePopScale = 1.45f;
    [SerializeField] private float scorePopDuration = 0.28f;

    [Header("Combo System")]
    [SerializeField] private SpriteNumber comboNumber;
    [SerializeField] private GameObject comboRoot;
    [SerializeField] private int minComboToShow = 2;
    [SerializeField, Min(0f)] private float comboIdleDelay = 2f;
    [SerializeField, Min(0.01f)] private float comboFadeDuration = 0.6f;
    public int combo { get; private set; }
    public int maxCombo { get; private set; }

    public int perfectCount { get; private set; }
    public int goodCount { get; private set; }
    public int badCount { get; private set; }
    public int missCount { get; private set; }

    [Header("Animation System (Animator state names)")]
    public Animator playerAnimator;
    [SerializeField] private string idleAnimName = "Player_Idle";
    [SerializeField] private string drawAnimName = "Player_Draw";
    [SerializeField] private string[] throwAnimNames = { "Player_ThrowA", "Player_ThrowB" };
    [SerializeField] private string hitAnimName = "Player_Hit";
    [SerializeField] private string kickDownAnimName = "Player_KickDown";
    [SerializeField] private string shootingAnimName = "Player_Shooting";
    [SerializeField] private string jumpAttackAnimName = "Player_JumpAttack";
    [SerializeField] private string sheatheAnimName = "Player_Sheathe";
    [SerializeField] private float sheatheDelay = 0.5f;
    [SerializeField] private float attackStopBeforeHitLine = 0.75f;
    [SerializeField] private float jumpAttackLeftOffset = 0.25f;
    [SerializeField] private float jumpAttackBelowNote = 0.5f;
    [SerializeField] private float jumpAttackDuration = 0.35f;

    [Header("HP Balance")]
    [SerializeField] private float hpPerfect = 2f;
    [SerializeField] private float hpGood = 1f;
    [SerializeField] private float badDamage = 2f;
    [SerializeField] private float missDamageBase = 5f;
    [SerializeField] private float missDamageStep = 3f;
    [SerializeField] private float missDamageMax = 20f;
    [SerializeField] private float cleanWaveBonus = 3f;

    [Header("Cooldown System")]
    public float cooldownTime = 0.08f;

    [Header("Game Over Text")]
    public TextMeshProUGUI feedbackText;

    [Header("UI Feedback Sprites (Perfect / Good / Bad / Miss)")]
    [SerializeField] private Image feedbackImage;
    [SerializeField] private Sprite perfectSprite;
    [SerializeField] private Sprite goodSprite;
    [SerializeField] private Sprite badSprite;
    [SerializeField] private Sprite missSprite;
    [SerializeField] private float feedbackDuration = 0.6f;
    [SerializeField] private float feedbackPopScale = 1.4f;
    [SerializeField] private float feedbackShrinkSpeed = 15f;
    [SerializeField] private float feedbackMaxTilt = 6f;

    [Header("Judgement Popup (โผล่ที่ปุ่มกดแต่ละปุ่ม)")]
    [Tooltip("ระยะลอยเหนือปุ่มกด (พิกเซลจอ)")]
    [SerializeField] private Vector2 gradePopupOffset = new Vector2(0f, 34f);
    [Tooltip("ความกว้างข้อความเกรด เท่าของความกว้างปุ่ม (2 = กว้างเป็น 2 เท่าของปุ่ม)")]
    [SerializeField, Min(0.5f)] private float gradePopupWidth = 2.2f;
    [SerializeField, Min(0.01f)] private float gradePopupPop = 0.12f;
    [SerializeField, Min(0f)] private float gradePopupHold = 0.1f;
    [SerializeField, Min(0.01f)] private float gradePopupFade = 0.24f;
    [SerializeField] private float gradePopupRise = 26f;
    [SerializeField, Min(1f)] private float gradePopupStartScale = 1.45f;
    [SerializeField] private float gradePopupTilt = 5f;

    [Header("Audio System (Sound Effects)")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("เสียงตอนกดโน้ต — ใช้เสียงเดียวกันทุกเกรด (Perfect / Good / Bad / Miss)")]
    [SerializeField] private AudioClip hitSound;
    [Tooltip("ความดังของเสียงเอฟเฟกต์กดโน้ต (เทียบกับระดับเสียงของ AudioSource) — ลดไว้ไม่ให้กลบเพลง")]
    [SerializeField, Range(0f, 1f)] private float soundEffectVolume = 0.45f;

    [Header("Sound Events")]
    [SerializeField] private AudioClip perfectSoundEvent;
    [SerializeField] private AudioClip fullComboSoundEvent;
    [SerializeField] private AudioClip goodJobSoundEvent;
    [SerializeField] private AudioClip tryAgainSoundEvent;
    [SerializeField, Range(0f, 1f)] private float soundEventMusicVolume = 0.25f;
    [SerializeField, Min(0f)] private float soundEventDuckFade = 0.15f;
    [SerializeField, Min(0f)] private float soundEventRestoreFade = 0.25f;

    [Header("Directional Arrow Press Feedback")]
    [SerializeField] private float arrowPressScale = 1.18f;
    [SerializeField] private float arrowFlashBrightness = 1.8f;
    [SerializeField] private float arrowFlashDuration = 0.18f;

    [Header("Press VFX")]
    [SerializeField] private GameObject pressVfxPrefab;
    [SerializeField] private Vector3 pressVfxOffset = Vector3.zero;

    [Header("Jump VFX")]
    [Tooltip("เว้นว่างได้ ระบบจะโหลด Resources/Vfx/JumpWind ให้เอง")]
    [SerializeField] private GameObject jumpVfxPrefab;
    [SerializeField] private Vector3 jumpVfxOffset = new Vector3(0f, -1.2f, 0f);
    [SerializeField] private float jumpVfxScale = 3.4f;
    [SerializeField] private float landVfxScale = 4.2f;

    [Header("Combo Screen Tint")]
    [SerializeField] private Color comboTintColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private float tintAlphaPer50Combo = 0.1f;
    [SerializeField] private float maxComboTintAlpha = 0.3f;
    [SerializeField] private float comboTintFadeSpeed = 1.8f;
    [SerializeField, Range(0f, 0.2f)] private float comboMilestoneFlash = 0.045f;
    [SerializeField, Min(0f)] private float comboFlashFadeSpeed = 3f;
    [SerializeField, Range(0f, 0.1f)] private float comboTintFlicker = 0.015f;
    [SerializeField, Min(0f)] private float comboTintFlickerSpeed = 5f;

    private float nextAllowedPressTime = 0f;
    private int throwStep = 0;
    private int missStreak = 0;
    private int activeWaves = 0;
    private int currentPerfectStreak = 0;
    private int soundEventPerfectStreak;
    private readonly Queue<AudioClip> pendingSoundEvents = new Queue<AudioClip>();
    private Coroutine soundEventRoutine;
    private float currentSoundEventMusicDuck = 1f;
    private float preQTEAnimatorSpeed = 1f;
    private bool qteReadyPoseActive;
    private bool warnedMissingCombo = false;
    private CanvasGroup comboCanvasGroup;
    private float comboIdleTime;
    private class ActiveHoldNote
    {
        public MiniLipid note;
        public int ticksAwarded;
    }

    private readonly List<ActiveHoldNote> activeHolds = new List<ActiveHoldNote>();
    private Vector3 homePosition;
    private bool isAwayFromHome;
    private readonly SpriteRenderer[] directionArrows = new SpriteRenderer[4]; // A/Left, S/Down, W/Up, D/Right
    private readonly Vector3[] arrowBaseScales = new Vector3[4];
    private readonly Color[] arrowBaseColors = new Color[4];
    private readonly float[] arrowFlashTimers = new float[4];
    private Image comboTintOverlay;
    private readonly Image[] directionArrowScreenImages = new Image[4];
    private Canvas comboTintCanvas;
    private RectTransform comboTintCanvasRect;
    private int lastComboTintMilestone;
    private float comboMilestoneFlashAmount;
    private bool jumpVfxPending;
    private Transform scorePopTarget;
    private Vector3 scorePopBaseScale = Vector3.one;
    private Coroutine scorePopRoutine;

    private Canvas gradeCanvas;
    private readonly Image[] gradeLaneImages = new Image[4];
    private readonly RectTransform[] gradeLaneRects = new RectTransform[4];
    private readonly Vector3[] gradeLaneAnchors = new Vector3[4];
    private readonly float[] gradeLaneAges = new float[4];

    void Awake()
    {
        instance = this;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        GameObject comboUI = comboRoot != null ? comboRoot : comboNumber != null ? comboNumber.gameObject : null;
        if (comboUI != null)
        {
            comboCanvasGroup = comboUI.GetComponent<CanvasGroup>();
            if (comboCanvasGroup == null) comboCanvasGroup = comboUI.AddComponent<CanvasGroup>();
        }
        if (jumpVfxPrefab == null) jumpVfxPrefab = Resources.Load<GameObject>("Vfx/JumpWind");
        CacheDirectionArrows();
        CreateComboTintOverlay();
        CreateGradeOverlay();
    }

    void OnDestroy()
    {
        EndQTEReadyPose(false);
        AudioSettingsRuntime.SetMusicDuck(1f);
    }

    void Start()
    {
        homePosition = transform.position;
        isGameOver = false;
        Time.timeScale = 1f;
        score = 0;
        combo = 0;
        maxCombo = 0;
        perfectCount = 0;
        goodCount = 0;
        badCount = 0;
        missCount = 0;
        soundEventPerfectStreak = 0;
        currentSoundEventMusicDuck = 1f;
        AudioSettingsRuntime.SetMusicDuck(1f);

        // ถ้าใช้ SpriteNumber ให้มันเด้งด้วยระบบ pop ของมันเอง (ไม่ให้แย่งกันเขียน scale)
        if (scoreNumber == null)
        {
            scorePopTarget = ScorePopTransform();
            if (scorePopTarget != null) scorePopBaseScale = scorePopTarget.localScale;
        }

        UpdateScoreUI();
        UpdateComboUI();
        PlayAnimation(idleAnimName);
        if (feedbackText != null) feedbackText.text = "";
        HideFeedbackSprite();
    }

    void Update()
    {
        UpdateGradeOverlay();
        UpdateDirectionArrowFeedback();
        UpdateComboScreenTint();
        UpdateComboVisibility();

        if (isGameOver || BossMashManager.IsMashActive) return;

        UpdateHoldNote();

        foreach (var key in InputKeys)
        {
            if (Input.GetKeyDown(key))
            {
                KeyCode normalizedKey = NormalizeKey(key);
                RevealComboUI();
                FlashDirectionArrow(normalizedKey);
                CancelInvoke(nameof(ReturnHome));
                if (isAwayFromHome) ReturnHome();
                if (Time.time >= nextAllowedPressTime) TryHit(normalizedKey);
                break;
            }
        }
    }

    public void PlayRandomQTEAttack()
    {
        if (playerAnimator == null) return;

        var attacks = new List<string>();
        if (playerAnimator.HasState(0, Animator.StringToHash(shootingAnimName))) attacks.Add(shootingAnimName);
        if (throwAnimNames != null)
            foreach (string attack in throwAnimNames)
                if (!string.IsNullOrEmpty(attack) && playerAnimator.HasState(0, Animator.StringToHash(attack))) attacks.Add(attack);
        if (playerAnimator.HasState(0, Animator.StringToHash(kickDownAnimName))) attacks.Add(kickDownAnimName);
        if (playerAnimator.HasState(0, Animator.StringToHash(jumpAttackAnimName))) attacks.Add(jumpAttackAnimName);

        if (attacks.Count > 0) PlayAnimation(attacks[Random.Range(0, attacks.Count)]);
    }

    public void PlayQTEHitSound() => PlaySound(hitSound);
    public void PlayQTEStartSound(AudioClip clip) => PlaySound(clip);

    public void BeginQTEReadyPose(float playbackSpeed)
    {
        if (playerAnimator == null) return;
        preQTEAnimatorSpeed = playerAnimator.speed;
        qteReadyPoseActive = true;
        playerAnimator.speed = Mathf.Clamp(playbackSpeed, 0.05f, 1f);
        PlayAnimation(drawAnimName);
    }

    public void EndQTEReadyPose(bool holdReadyPose = true)
    {
        if (!qteReadyPoseActive) return;
        qteReadyPoseActive = false;
        if (playerAnimator == null) return;
        playerAnimator.speed = preQTEAnimatorSpeed;
        if (holdReadyPose) PlayAnimation("Player_Ready");
    }

    public bool IsQTEPersistentUI(Graphic graphic)
    {
        if (graphic == null) return false;
        Transform target = graphic.transform;
        return IsUnder(target, scoreText != null ? scoreText.transform : null)
            || IsUnder(target, scoreNumber != null ? scoreNumber.transform.parent : null)
            || IsUnder(target, comboRoot != null ? comboRoot.transform : comboNumber != null ? comboNumber.transform : null);
    }

    public void KeepQTEHudVisible()
    {
        if (scoreNumber != null)
        {
            scoreNumber.gameObject.SetActive(true);
            scoreNumber.SetValue(score, false);
            EnableGraphics(scoreNumber.transform.parent != null ? scoreNumber.transform.parent : scoreNumber.transform);
        }
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.text = "Score: " + score;
            EnableGraphics(scoreText.transform);
        }

        SetQTEComboVisible(true);
        Transform comboVisualRoot = comboRoot != null ? comboRoot.transform : comboNumber != null ? comboNumber.transform : null;
        if (comboVisualRoot != null) EnableGraphics(comboVisualRoot);
    }

    public void SetQTEComboVisible(bool visible)
    {
        GameObject root = comboRoot != null ? comboRoot : comboNumber != null ? comboNumber.gameObject : null;
        if (root == null) return;

        root.SetActive(visible);
        if (comboCanvasGroup != null) comboCanvasGroup.alpha = visible ? 1f : 0f;
        if (visible)
        {
            comboIdleTime = 0f;
            if (comboNumber != null) comboNumber.SetValue(combo);
        }
    }

    public void RefreshComboUI() => UpdateComboUI();

    static void EnableGraphics(Transform root)
    {
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            graphic.enabled = true;
    }

    static bool IsUnder(Transform target, Transform root)
    {
        return root != null && (target == root || target.IsChildOf(root));
    }

    private void CacheDirectionArrows()
    {
        Transform hitline = GameObject.Find("Hitline")?.transform;
        if (hitline == null) return;

        // Keep array order aligned with FlashDirectionArrow: A, S, W, D.
        string[] names = { "Left2_1", "Down2_1", "Up2_1", "Right2_0" };
        for (int i = 0; i < names.Length; i++)
        {
            Transform arrow = hitline.Find(names[i]);
            if (arrow == null) continue;
            directionArrows[i] = arrow.GetComponent<SpriteRenderer>();
            if (directionArrows[i] == null) continue;
            arrowBaseScales[i] = arrow.localScale;
            arrowBaseColors[i] = directionArrows[i].color;
        }
    }

    private void FlashDirectionArrow(KeyCode key)
    {
        int index = key == KeyCode.A ? 0 : key == KeyCode.S ? 1 : key == KeyCode.W ? 2 : key == KeyCode.D ? 3 : -1;
        if (index >= 0 && directionArrows[index] != null)
        {
            arrowFlashTimers[index] = arrowFlashDuration;
            SpawnPressVfx(directionArrows[index].transform.position);
        }
    }

    private void SpawnPressVfx(Vector3 position)
    {
        if (pressVfxPrefab == null) return;
        Instantiate(pressVfxPrefab, position + pressVfxOffset, Quaternion.identity);
    }

    private void UpdateDirectionArrowFeedback()
    {
        for (int i = 0; i < directionArrows.Length; i++)
        {
            SpriteRenderer arrow = directionArrows[i];
            if (arrow == null) continue;

            arrowFlashTimers[i] = Mathf.Max(0f, arrowFlashTimers[i] - Time.unscaledDeltaTime);
            float flash = arrowFlashDuration > 0f ? arrowFlashTimers[i] / arrowFlashDuration : 0f;
            arrow.transform.localScale = arrowBaseScales[i] * Mathf.Lerp(1f, arrowPressScale, flash);
            Color brightColor = arrowBaseColors[i] * arrowFlashBrightness;
            brightColor.a = arrowBaseColors[i].a;
            arrow.color = Color.Lerp(arrowBaseColors[i], brightColor, flash);
        }

        UpdateDirectionArrowScreenImages();
    }

    private void UpdateComboScreenTint()
    {
        if (comboTintOverlay == null) return;

        int milestones = Mathf.Max(0, combo) / 50;
        if (milestones > lastComboTintMilestone)
            comboMilestoneFlashAmount = 1f;
        lastComboTintMilestone = milestones;
        comboMilestoneFlashAmount = Mathf.MoveTowards(
            comboMilestoneFlashAmount, 0f, comboFlashFadeSpeed * Time.unscaledDeltaTime);

        float targetTint = Mathf.Min(milestones * tintAlphaPer50Combo, maxComboTintAlpha);
        float pulse = comboMilestoneFlash * comboMilestoneFlashAmount;
        float flicker = targetTint > 0f
            ? Mathf.Sin(Time.unscaledTime * comboTintFlickerSpeed * Mathf.PI * 2f) * comboTintFlicker
            : 0f;
        targetTint = Mathf.Clamp(targetTint + pulse + flicker, 0f, maxComboTintAlpha + comboMilestoneFlash);
        Color targetColor = new Color(comboTintColor.r, comboTintColor.g, comboTintColor.b, targetTint);
        comboTintOverlay.color = Color.Lerp(comboTintOverlay.color, targetColor,
            1f - Mathf.Exp(-comboTintFadeSpeed * Time.unscaledDeltaTime));
    }

    private void CreateComboTintOverlay()
    {
        comboTintCanvas = FindFirstObjectByType<Canvas>();
        if (comboTintCanvas == null) return;

        var overlayObject = new GameObject("ComboScreenTint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.layer = comboTintCanvas.gameObject.layer;
        RectTransform rect = (RectTransform)overlayObject.transform;
        rect.SetParent(comboTintCanvas.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();

        foreach (Button button in comboTintCanvas.GetComponentsInChildren<Button>(true))
        {
            Transform branch = button.transform;
            while (branch.parent != null && branch.parent != comboTintCanvas.transform)
                branch = branch.parent;
            if (branch.parent == comboTintCanvas.transform)
                branch.SetAsLastSibling();
        }

        comboTintOverlay = overlayObject.GetComponent<Image>();
        comboTintOverlay.color = new Color(comboTintColor.r, comboTintColor.g, comboTintColor.b, 0f);
        comboTintOverlay.raycastTarget = false;
        comboTintCanvasRect = comboTintCanvas.transform as RectTransform;

        for (int i = 0; i < directionArrows.Length; i++)
        {
            SpriteRenderer source = directionArrows[i];
            if (source == null || source.sprite == null) continue;

            var arrowObject = new GameObject(source.name + "_Untinted", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            arrowObject.layer = comboTintCanvas.gameObject.layer;
            RectTransform arrowRect = (RectTransform)arrowObject.transform;
            arrowRect.SetParent(comboTintCanvas.transform, false);
            arrowRect.anchorMin = Vector2.zero;
            arrowRect.anchorMax = Vector2.zero;
            arrowRect.pivot = new Vector2(0.5f, 0.5f);

            Image image = arrowObject.GetComponent<Image>();
            image.sprite = source.sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            directionArrowScreenImages[i] = image;
        }
    }

    private void UpdateDirectionArrowScreenImages()
    {
        if (comboTintCanvasRect == null) return;

        Camera camera = Camera.main;
        if (camera == null) return;

        for (int i = 0; i < directionArrows.Length; i++)
        {
            SpriteRenderer source = directionArrows[i];
            Image image = directionArrowScreenImages[i];
            if (source == null || image == null) continue;

            image.enabled = source.enabled && source.gameObject.activeInHierarchy;
            image.color = source.color;
            Vector3 screenCenter = camera.WorldToScreenPoint(source.bounds.center);
            Vector3 screenMin = camera.WorldToScreenPoint(source.bounds.min);
            Vector3 screenMax = camera.WorldToScreenPoint(source.bounds.max);
            Camera uiCamera = comboTintCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : comboTintCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    comboTintCanvasRect, screenCenter, uiCamera, out Vector2 localPosition))
                continue;

            RectTransform imageRect = image.rectTransform;
            imageRect.anchoredPosition = localPosition;
            imageRect.sizeDelta = new Vector2(
                Mathf.Abs(screenMax.x - screenMin.x), Mathf.Abs(screenMax.y - screenMin.y)) / comboTintCanvas.scaleFactor;
            imageRect.localRotation = Quaternion.Euler(0f, 0f, source.transform.eulerAngles.z);
        }
    }

    // แปลงปุ่มกดให้ตรงกับเลนใหม่ (A=ซ้าย, S=ล่าง, W=ขึ้น, D=ขวา)
    KeyCode NormalizeKey(KeyCode key)
    {
        if (key == KeyCode.A || key == KeyCode.LeftArrow) return KeyCode.A;     // บนสุด: ซ้าย
        if (key == KeyCode.S || key == KeyCode.DownArrow) return KeyCode.S;     // ถัดมา: ล่าง
        if (key == KeyCode.W || key == KeyCode.UpArrow) return KeyCode.W;       // ถัดมา: ขึ้น
        if (key == KeyCode.D || key == KeyCode.RightArrow) return KeyCode.D;    // ล่างสุด: ขวา
        return key;
    }

    void TryHit(KeyCode pressedKey)
    {
        MiniLipid target = MiniLipid.FindNextForKey(pressedKey);
        if (target == null) return;

        nextAllowedPressTime = Time.time + cooldownTime;

        if (target.IsHold)
        {
            if (target.TryStartHold())
            {
                HitResult headResult = target.EvaluateAccuracy();
                OnNoteHit(headResult, false, pressedKey, target.hitLineX, target.transform.position.y);
                activeHolds.Add(new ActiveHoldNote { note = target });
                UpdateScoreUI();
            }
            return;
        }

        HitResult result = target.EvaluateAccuracy();
        OnNoteHit(result, target.IsLastInRow(), pressedKey, target.hitLineX, target.transform.position.y);
        target.ConsumeMini(result);
        UpdateScoreUI();
    }

    void UpdateHoldNote()
    {
        for (int i = activeHolds.Count - 1; i >= 0; i--)
        {
            ActiveHoldNote hold = activeHolds[i];
            MiniLipid note = hold.note;

            if (note == null || !note.gameObject.activeInHierarchy)
            {
                activeHolds.RemoveAt(i);
                continue;
            }

            if (note.IsHoldButtonStillDown) comboIdleTime = 0f;

            int holdTicksNow = note.HoldTickCount;
            if (holdTicksNow > hold.ticksAwarded)
            {
                score += (holdTicksNow - hold.ticksAwarded) * HoldTickScore;
                hold.ticksAwarded = holdTicksNow;
                UpdateScoreUI();
            }

            if (note.HoldKeyReleasedThisFrame)
            {
                if (Mathf.Abs(note.HoldEndOffsetSeconds) <= note.HoldReleaseWindow)
                    CompleteHoldNote(hold);
                else
                    MissActiveHold(hold);
                continue;
            }

            if (note.HoldEndOffsetSeconds > note.HoldReleaseWindow || !note.IsHoldButtonStillDown)
                MissActiveHold(hold);
        }
    }

    void CompleteHoldNote(ActiveHoldNote hold)
    {
        activeHolds.Remove(hold);
        score += HoldTailBonus;
        hold.note.ConsumeMini(HitResult.Perfect);
        UpdateScoreUI();
    }

    void MissActiveHold(ActiveHoldNote hold)
    {
        activeHolds.Remove(hold);
        OnNoteMissed(hold.note.assignedKey);
        hold.note.ConsumeMini(HitResult.Miss);
        UpdateScoreUI();
    }

    void OnNoteHit(HitResult result, bool isLastOfWave, KeyCode pressedKey, float hitLineX, float noteY)
    {
        if (result == HitResult.Bad) CancelDirectionalMove();
        else PlayDirectionalAnimation(pressedKey, hitLineX, noteY);
        AddCombo();
        missStreak = 0;

        switch (result)
        {
            case HitResult.Perfect:
                perfectCount++;
                soundEventPerfectStreak++;
                if (soundEventPerfectStreak == 15) QueueSoundEvent(perfectSoundEvent);
                score += PerfectScore;
                ChangeHealth(hpPerfect);
                currentPerfectStreak++;
                ShowGrade(perfectSprite, pressedKey);
                PlayPerfectSound(isLastOfWave);
                break;

            case HitResult.Good:
                goodCount++;
                soundEventPerfectStreak = 0;
                if (goodCount == 10) QueueSoundEvent(goodJobSoundEvent);
                score += GoodScore;
                ChangeHealth(hpGood);
                currentPerfectStreak = 0;
                ShowGrade(goodSprite, pressedKey);
                PlaySound(hitSound);
                break;

            default:
                badCount++;
                soundEventPerfectStreak = 0;
                if (badCount == 6) QueueSoundEvent(tryAgainSoundEvent);
                score += BadScore;
                ChangeHealth(-badDamage);
                PlayAnimation(hitAnimName);
                currentPerfectStreak = 0;
                ShowGrade(badSprite, pressedKey);
                PlaySound(hitSound);
                break;
        }

        CameraImpact.Hit(result, combo);
    }

    void OnNoteMissed(KeyCode missedKey = KeyCode.None)
    {
        missCount++;
        soundEventPerfectStreak = 0;
        CancelDirectionalMove();
        currentPerfectStreak = 0;
        BreakCombo();
        ApplyMissDamage();
        PlayAnimation(hitAnimName);
        ShowGrade(missSprite, missedKey);
        PlaySound(hitSound);
        CameraImpact.Hit(HitResult.Miss);
    }

    public void RegisterMissedNote() => RegisterMissedNote(KeyCode.None);

    public void RegisterMissedNote(KeyCode missedKey)
    {
        if (!isGameOver) OnNoteMissed(missedKey);
    }

    public void OnWaveStart()
    {
        if (isGameOver) return;
        CancelInvoke(nameof(PlaySheathe));
        activeWaves++;
        if (activeWaves > 1) return;

        throwStep = 0;
        if (HealthBarUI.instance != null) HealthBarUI.instance.SetWaveActive(true);
        PlayAnimation(drawAnimName);
    }

    public void OnWaveEnd(bool hadMiss)
    {
        if (isGameOver) return;

        if (!hadMiss && cleanWaveBonus > 0f) ChangeHealth(cleanWaveBonus);

        activeWaves = Mathf.Max(0, activeWaves - 1);
        if (activeWaves > 0) return;

        if (HealthBarUI.instance != null) HealthBarUI.instance.SetWaveActive(false);
        CancelInvoke(nameof(PlaySheathe));
        Invoke(nameof(PlaySheathe), sheatheDelay);
    }

    void PlaySheathe()
    {
        if (!isGameOver) PlayAnimation(sheatheAnimName);
    }

    void ChangeHealth(float amount)
    {
        if (HealthBarUI.instance == null) return;
        if (amount > 0f) HealthBarUI.instance.AddHealth(amount);
        else if (amount < 0f) HealthBarUI.instance.TakeDamage(-amount);
    }

    void ApplyMissDamage()
    {
        float damage = Mathf.Min(missDamageBase + missDamageStep * missStreak, missDamageMax);
        missStreak++;
        ChangeHealth(-damage);
    }

    public void CheckGameOver()
    {
        if (isGameOver) return;
        if (HealthBarUI.instance != null && HealthBarUI.instance.IsEmpty) Die();
    }

    void Die()
    {
        isGameOver = true;
        CancelDirectionalMove();
        HideFeedbackSprite();
        if (feedbackText != null) feedbackText.text = "GAME OVER!";
        PlayAnimation("Player_Faint");
        CameraImpact.GameOver();
        Debug.Log("GAME OVER! Score: " + score);

        for (int i = MiniLipid.Active.Count - 1; i >= 0; i--) Destroy(MiniLipid.Active[i].gameObject);
        for (int i = LipidMovement.Active.Count - 1; i >= 0; i--) Destroy(LipidMovement.Active[i].gameObject);

        if (LevelManager.instance != null)
        {
            LevelManager.instance.OnPlayerDied();
            return;
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (scoreNumber != null) scoreNumber.SetValue(score);
    }

    public void RegisterQTEHit(int scoreValue)
    {
        if (!BossMashManager.IsMashActive) return;

        score += scoreValue;
        UpdateScoreUI();
        AddCombo();
    }

    Transform ScorePopTransform()
    {
        if (scoreNumber != null) return scoreNumber.transform;
        if (scoreText != null) return scoreText.rectTransform;
        return null;
    }

    /// <summary>ตำแหน่ง world ของตัวเลขคะแนน (ใช้เป็นจุดหมายให้โน้ตบินมา)</summary>
    public static bool TryGetScoreWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (instance == null) return false;

        Transform target = instance.ScorePopTransform();
        if (target == null) return false;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        Camera cam = Camera.main;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && cam != null)
        {
            Vector3 screenPoint = target.position;
            float depth = Mathf.Abs(cam.transform.position.z);
            worldPosition = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        }
        else
        {
            worldPosition = target.position;
        }

        worldPosition.z = 0f;
        return true;
    }

    /// <summary>ให้ UI คะแนนเด้งขึ้นแล้วกลับมา (เรียกตอนโน้ตบินมาถึง)</summary>
    public static void PopScoreUI()
    {
        if (instance != null) instance.PlayScorePop();
    }

    void PlayScorePop()
    {
        if (scoreNumber != null)
        {
            scoreNumber.PlayPop(scorePopScale, scorePopDuration);
            return;
        }

        if (scorePopTarget == null)
        {
            scorePopTarget = ScorePopTransform();
            if (scorePopTarget != null) scorePopBaseScale = scorePopTarget.localScale;
        }
        if (scorePopTarget == null) return;

        if (scorePopRoutine != null) StopCoroutine(scorePopRoutine);
        scorePopRoutine = StartCoroutine(ScorePopRoutine());
    }

    IEnumerator ScorePopRoutine()
    {
        float duration = Mathf.Max(0.05f, scorePopDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curve = Mathf.Sin(t * Mathf.PI);
            scorePopTarget.localScale = scorePopBaseScale * (1f + (scorePopScale - 1f) * curve);
            yield return null;
        }

        scorePopTarget.localScale = scorePopBaseScale;
        scorePopRoutine = null;
    }

    void AddCombo()
    {
        combo++;
        if (combo > maxCombo) maxCombo = combo;
        comboIdleTime = 0f;
        if (combo == 50) QueueSoundEvent(fullComboSoundEvent);
        UpdateComboUI();
    }

    void QueueSoundEvent(AudioClip clip)
    {
        if (clip == null) return;
        pendingSoundEvents.Enqueue(clip);
        if (soundEventRoutine == null) soundEventRoutine = StartCoroutine(PlaySoundEventQueue());
    }

    IEnumerator PlaySoundEventQueue()
    {
        while (pendingSoundEvents.Count > 0)
        {
            AudioClip clip = pendingSoundEvents.Dequeue();
            yield return FadeSoundEventMusic(soundEventMusicVolume, soundEventDuckFade);
            AudioSettingsRuntime.PlayEffect(clip);
            yield return new WaitForSecondsRealtime(clip.length);
        }

        yield return FadeSoundEventMusic(1f, soundEventRestoreFade);
        soundEventRoutine = null;
    }

    IEnumerator FadeSoundEventMusic(float target, float duration)
    {
        float start = currentSoundEventMusicDuck;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentSoundEventMusicDuck = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            AudioSettingsRuntime.SetMusicDuck(currentSoundEventMusicDuck);
            yield return null;
        }

        currentSoundEventMusicDuck = target;
        AudioSettingsRuntime.SetMusicDuck(target);
    }

    void BreakCombo()
    {
        combo = 0;
        UpdateComboUI();
    }

    void UpdateComboUI()
    {
        bool show = combo >= minComboToShow;
        if (show && comboNumber == null && !warnedMissingCombo)
        {
            warnedMissingCombo = true;
            Debug.LogWarning("[PlayerController] Combo Number is not assigned on Player");
        }

        GameObject root = comboRoot != null ? comboRoot : (comboNumber != null ? comboNumber.gameObject : null);
        if (root != null && root.activeSelf != show) root.SetActive(show);
        if (comboCanvasGroup != null) comboCanvasGroup.alpha = show ? 1f : 0f;
        if (show && comboNumber != null) comboNumber.SetValue(combo);
    }

    void UpdateComboVisibility()
    {
        if (combo < minComboToShow || comboCanvasGroup == null || BossMashManager.IsMashActive) return;

        comboIdleTime += Time.deltaTime;
        float fadeProgress = Mathf.Clamp01((comboIdleTime - comboIdleDelay) / comboFadeDuration);
        comboCanvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, fadeProgress);
        if (fadeProgress >= 1f)
        {
            GameObject root = comboRoot != null ? comboRoot : comboNumber.gameObject;
            root.SetActive(false);
        }
    }

    void RevealComboUI()
    {
        if (combo < minComboToShow) return;

        comboIdleTime = 0f;
        GameObject root = comboRoot != null ? comboRoot : comboNumber.gameObject;
        root.SetActive(true);
        if (comboCanvasGroup != null) comboCanvasGroup.alpha = 1f;
    }

    void ShowFeedbackSprite(Sprite sprite)
    {
        if (feedbackImage == null || sprite == null) return;

        feedbackImage.sprite = sprite;
        feedbackImage.enabled = true;
        feedbackImage.transform.localScale = Vector3.one * feedbackPopScale;
        feedbackImage.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-feedbackMaxTilt, feedbackMaxTilt));

        CancelInvoke(nameof(HideFeedbackSprite));
        Invoke(nameof(HideFeedbackSprite), feedbackDuration);
    }

    void HideFeedbackSprite()
    {
        if (feedbackImage != null) feedbackImage.enabled = false;
    }

    void ShrinkFeedbackSprite()
    {
        if (feedbackImage == null || !feedbackImage.enabled) return;
        feedbackImage.transform.localScale = Vector3.Lerp(
            feedbackImage.transform.localScale, Vector3.one, feedbackShrinkSpeed * Time.deltaTime);
    }

    // เกรดโผล่ที่ปุ่มกดของเลนนั้น (Perfect / Good / Bad / Miss) แล้วลอยขึ้น + จางหาย
    void CreateGradeOverlay()
    {
        if (gradeCanvas != null) return;

        var canvasObject = new GameObject("GradeOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        int uiLayer = LayerMask.NameToLayer("UI");
        canvasObject.layer = uiLayer >= 0 ? uiLayer : comboTintCanvas != null ? comboTintCanvas.gameObject.layer : 5;

        gradeCanvas = canvasObject.GetComponent<Canvas>();
        gradeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gradeCanvas.sortingOrder = 500; // อยู่บนสุด กัน UI อื่นบัง

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    Image GradeLaneImage(int index)
    {
        if (gradeLaneImages[index] != null) return gradeLaneImages[index];

        if (gradeCanvas == null) CreateGradeOverlay();
        if (gradeCanvas == null) return null;

        var popupObject = new GameObject("Grade_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        popupObject.layer = gradeCanvas.gameObject.layer;

        RectTransform rect = (RectTransform)popupObject.transform;
        rect.SetParent(gradeCanvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = popupObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;

        gradeLaneRects[index] = rect;
        gradeLaneImages[index] = image;
        return image;
    }

    void ShowGrade(Sprite sprite, KeyCode key)
    {
        int index = KeyGradeIndex(key);
        if (sprite == null || index < 0) return;

        Image image = GradeLaneImage(index);
        if (image == null) return;

        gradeLaneAges[index] = 0f;
        gradeLaneAnchors[index] = GradeAnchor(index);

        image.sprite = sprite;
        image.color = Color.white;
        image.enabled = true;

        RectTransform rect = gradeLaneRects[index];
        rect.localScale = Vector3.one * gradePopupStartScale;
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-gradePopupTilt, gradePopupTilt));
        rect.SetAsLastSibling();

        // ขนาดอิงจากขนาดปุ่มบนจอ แต่คุมไม่ให้ใหญ่เกิน
        Camera cam = Camera.main;
        float screenWidth = Screen.height * 0.07f;
        SpriteRenderer arrow = directionArrows[index];
        if (cam != null && arrow != null)
        {
            Vector3 min = cam.WorldToScreenPoint(arrow.bounds.min);
            Vector3 max = cam.WorldToScreenPoint(arrow.bounds.max);
            float width = Mathf.Abs(max.x - min.x);
            if (width > 1f) screenWidth = width;
        }

        float scaleFactor = gradeCanvas.scaleFactor > 0f ? gradeCanvas.scaleFactor : 1f;
        float boxWidth = Mathf.Clamp(screenWidth * gradePopupWidth / scaleFactor, 100f, 420f);
        rect.sizeDelta = new Vector2(boxWidth, boxWidth * 0.55f);
    }

    Vector3 GradeAnchor(int index)
    {
        SpriteRenderer arrow = directionArrows[index];
        if (arrow != null) return arrow.transform.position;

        // ไม่เจอปุ่ม: อิงตำแหน่งผู้เล่น + เลน (index เรียง A, S, W, D)
        float[] laneY = { 3f, -1f, 1f, -3f };
        return transform.position + new Vector3(-3.2f, laneY[Mathf.Clamp(index, 0, 3)], 0f);
    }

    void UpdateGradeOverlay()
    {
        if (gradeCanvas == null) return;

        Camera cam = Camera.main;
        float total = gradePopupPop + gradePopupHold + gradePopupFade;

        for (int i = 0; i < gradeLaneImages.Length; i++)
        {
            Image image = gradeLaneImages[i];
            if (image == null || !image.enabled) continue;

            gradeLaneAges[i] += Time.unscaledDeltaTime;
            float age = gradeLaneAges[i];

            float popT = Mathf.Clamp01(age / gradePopupPop);
            float fadeT = Mathf.Clamp01((age - gradePopupPop - gradePopupHold) / gradePopupFade);

            RectTransform rect = gradeLaneRects[i];
            float scale = Mathf.Lerp(gradePopupStartScale, 1f, 1f - Mathf.Pow(1f - popT, 3f)) * (1f + 0.05f * fadeT);
            rect.localScale = Vector3.one * scale;

            Color color = image.color;
            color.a = 1f - fadeT * fadeT;
            image.color = color;

            if (age >= total || cam == null)
            {
                image.enabled = false;
                image.sprite = null;
                continue;
            }

            // ตำแหน่งปุ่มบนจอ (ขยับตามกล้อง/ซูม)
            Vector3 screenPoint = cam.WorldToScreenPoint(gradeLaneAnchors[i]);
            if (screenPoint.z <= 0f) continue;

            Vector2 point = (Vector2)screenPoint + gradePopupOffset;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)gradeCanvas.transform, point, null, out Vector2 local)) continue;

            rect.anchoredPosition = local + new Vector2(0f, gradePopupRise * Mathf.Clamp01(age / total));
        }
    }

    static int KeyGradeIndex(KeyCode key)
    {
        if (key == KeyCode.A || key == KeyCode.LeftArrow) return 0;
        if (key == KeyCode.S || key == KeyCode.DownArrow) return 1;
        if (key == KeyCode.W || key == KeyCode.UpArrow) return 2;
        if (key == KeyCode.D || key == KeyCode.RightArrow) return 3;
        return -1;
    }

    void PlayDirectionalAnimation(KeyCode pressedKey, float hitLineX, float noteY)
    {
        // Current lanes top-to-bottom: A/W/S/D = jump, shoot, throw, kick-down.
        switch (pressedKey)
        {
            case KeyCode.A:
                StartDirectionalMove(new Vector3(
                    hitLineX - attackStopBeforeHitLine - jumpAttackLeftOffset,
                    noteY - jumpAttackBelowNote,
                    homePosition.z));
                PlayAnimation(jumpAttackAnimName);
                Invoke(nameof(ReturnHome), jumpAttackDuration);
                jumpVfxPending = true;
                SpawnJumpVfx(homePosition, jumpVfxScale);
                ProcParticles.Burst(homePosition + jumpVfxOffset, new Color(0.9f, 0.95f, 1f), 8, 3.5f, 0.3f, 0.28f);
                break;
            case KeyCode.W:
                PlayAnimation(shootingAnimName);
                break;
            case KeyCode.S:
                if (throwAnimNames != null && throwAnimNames.Length > 0)
                {
                    PlayAnimation(throwAnimNames[throwStep % throwAnimNames.Length]);
                    throwStep++;
                }
                break;
            default:
                StartDirectionalMove(new Vector3(
                    hitLineX - attackStopBeforeHitLine,
                    homePosition.y,
                    homePosition.z));
                PlayAnimation(kickDownAnimName);
                break;
        }
    }

    void StartDirectionalMove(Vector3 attackPosition)
    {
        transform.position = attackPosition;
        isAwayFromHome = true;
    }

    void CancelDirectionalMove()
    {
        ReturnHome();
    }

    void ReturnHome()
    {
        transform.position = homePosition;
        isAwayFromHome = false;

        if (jumpVfxPending)
        {
            jumpVfxPending = false;
            SpawnJumpVfx(homePosition, landVfxScale);

            Vector3 feet = homePosition + jumpVfxOffset;
            ProcParticles.Shockwave(feet, new Color(1f, 1f, 1f, 0.75f), 0.4f, 2.6f, 0.3f);
            ProcParticles.Burst(feet, new Color(0.95f, 0.95f, 1f), 10, 4.5f, 0.35f, 0.3f);
        }
    }

    void SpawnJumpVfx(Vector3 position, float scale)
    {
        if (jumpVfxPrefab == null) return;
        GameObject vfx = Instantiate(jumpVfxPrefab, position + jumpVfxOffset, Quaternion.identity);
        vfx.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    void PlayAnimation(string animName)
    {
        if (playerAnimator == null || string.IsNullOrEmpty(animName)) return;

        if (!playerAnimator.HasState(0, Animator.StringToHash(animName)))
        {
            Debug.LogWarning($"[PlayerController] Animator state '{animName}' not found in ({playerAnimator.runtimeAnimatorController?.name}) " +
                             "-> run Tools > Build Player Animations and check the Animator uses PlayerMain.controller");
            return;
        }
        playerAnimator.Play(animName, 0, 0f);
    }

    void PlayPerfectSound(bool isLastOfWave)
    {
        PlaySound(hitSound);

        // จบช่วง perfect ต่อเนื่อง (จบเวฟหรือครบเชน) ให้เริ่มนับใหม่
        if (isLastOfWave || currentPerfectStreak >= PerfectChainLength) currentPerfectStreak = 0;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;

        // ความดังสุทธิ = ค่าใน Inspector x ตัวปรับ NOTE HIT ในหน้า Settings
        float volume = Mathf.Clamp01(soundEffectVolume * AudioSettingsRuntime.NoteHitVolume);
        audioSource.PlayOneShot(clip, volume);
    }
}
