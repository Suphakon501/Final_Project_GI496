using TMPro;
using System.Collections;
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

    [Header("Combo System")]
    [SerializeField] private SpriteNumber comboNumber;
    [SerializeField] private GameObject comboRoot;
    [SerializeField] private int minComboToShow = 2;
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

    [Header("Audio System (Sound Effects)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip goodSound;
    [SerializeField] private AudioClip badMissSound;

    [Header("Directional Arrow Press Feedback")]
    [SerializeField] private float arrowPressScale = 1.18f;
    [SerializeField] private float arrowFlashBrightness = 1.8f;
    [SerializeField] private float arrowFlashDuration = 0.18f;

    [Header("Press VFX")]
    [SerializeField] private GameObject pressVfxPrefab;
    [SerializeField] private Vector3 pressVfxOffset = Vector3.zero;

    [Header("Combo Screen Tint")]
    [SerializeField] private Color comboTintColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private float tintAlphaPer50Combo = 0.1f;
    [SerializeField] private float maxComboTintAlpha = 0.3f;
    [SerializeField] private float comboTintFadeSpeed = 1.8f;
    [SerializeField, Range(0f, 0.2f)] private float comboMilestoneFlash = 0.045f;
    [SerializeField, Min(0f)] private float comboFlashFadeSpeed = 3f;
    [SerializeField, Range(0f, 0.1f)] private float comboTintFlicker = 0.015f;
    [SerializeField, Min(0f)] private float comboTintFlickerSpeed = 5f;

    [Header("Perfect Chain Sounds")]
    [SerializeField] private AudioClip[] perfectStepSounds = new AudioClip[5];
    [SerializeField] private AudioClip perfectEndSound;

    private float nextAllowedPressTime = 0f;
    private int throwStep = 0;
    private int missStreak = 0;
    private int activeWaves = 0;
    private int currentPerfectStreak = 0;
    private bool warnedMissingCombo = false;
    private MiniLipid activeHold;
    private int holdTicksAwarded;
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

    void Awake()
    {
        instance = this;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        CacheDirectionArrows();
        CreateComboTintOverlay();
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

        UpdateScoreUI();
        UpdateComboUI();
        PlayAnimation(idleAnimName);
        if (feedbackText != null) feedbackText.text = "";
        HideFeedbackSprite();
    }

    void Update()
    {
        ShrinkFeedbackSprite();
        UpdateDirectionArrowFeedback();
        UpdateComboScreenTint();

        if (isGameOver) return;

        UpdateHoldNote();

        foreach (var key in InputKeys)
        {
            if (Input.GetKeyDown(key))
            {
                KeyCode normalizedKey = NormalizeKey(key);
                FlashDirectionArrow(normalizedKey);
                CancelInvoke(nameof(ReturnHome));
                if (isAwayFromHome) ReturnHome();
                if (Time.time >= nextAllowedPressTime) TryHit(normalizedKey);
                break;
            }
        }
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
        nextAllowedPressTime = Time.time + cooldownTime;

        MiniLipid target = MiniLipid.FindNextToHit();
        if (target == null) return;

        if (pressedKey == target.assignedKey)
        {
            if (target.IsHold)
            {
                if (target.TryStartHold())
                {
                    HitResult headResult = target.EvaluateAccuracy();
                    OnNoteHit(headResult, false, pressedKey, target.hitLineX, target.transform.position.y);
                    activeHold = target;
                    holdTicksAwarded = 0;
                    UpdateScoreUI();
                }
                else if (target.IsOutsideTimingWindow)
                {
                    OnNoteMissed();
                    target.ConsumeMini(HitResult.Miss);
                    UpdateScoreUI();
                }
                return;
            }

            HitResult result = target.EvaluateAccuracy();
            OnNoteHit(result, target.IsLastInRow(), pressedKey, target.hitLineX, target.transform.position.y);
            target.ConsumeMini(result);
        }
        else
        {
            OnNoteMissed();
            target.ConsumeMini(HitResult.Miss);
        }

        UpdateScoreUI();
    }

    void UpdateHoldNote()
    {
        if (activeHold == null) return;
        if (!activeHold.gameObject.activeInHierarchy)
        {
            activeHold = null;
            return;
        }

        float endOffset = activeHold.HoldEndOffsetSeconds;
        int holdTicksNow = activeHold.HoldTickCount;
        if (holdTicksNow > holdTicksAwarded)
        {
            score += (holdTicksNow - holdTicksAwarded) * HoldTickScore;
            holdTicksAwarded = holdTicksNow;
            UpdateScoreUI();
        }

        if (activeHold.HoldKeyReleasedThisFrame)
        {
            if (Mathf.Abs(endOffset) <= activeHold.HoldReleaseWindow)
                CompleteHoldNote();
            else
                MissActiveHold();
            return;
        }

        if (endOffset > activeHold.HoldReleaseWindow || !activeHold.IsHoldButtonStillDown)
        {
            MissActiveHold();
            return;
        }
    }

    void CompleteHoldNote()
    {
        MiniLipid completed = activeHold;
        activeHold = null;
        score += HoldTailBonus;
        completed.ConsumeMini(HitResult.Perfect);
        UpdateScoreUI();
    }

    void MissActiveHold()
    {
        MiniLipid missed = activeHold;
        activeHold = null;
        OnNoteMissed();
        missed.ConsumeMini(HitResult.Miss);
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
                score += PerfectScore;
                ChangeHealth(hpPerfect);
                currentPerfectStreak++;
                ShowFeedbackSprite(perfectSprite);
                PlayPerfectSound(isLastOfWave);
                break;

            case HitResult.Good:
                goodCount++;
                score += GoodScore;
                ChangeHealth(hpGood);
                currentPerfectStreak = 0;
                ShowFeedbackSprite(goodSprite);
                PlaySound(goodSound);
                break;

            default:
                badCount++;
                score += BadScore;
                ChangeHealth(-badDamage);
                PlayAnimation(hitAnimName);
                currentPerfectStreak = 0;
                ShowFeedbackSprite(badSprite);
                PlaySound(badMissSound);
                break;
        }
    }

    void OnNoteMissed()
    {
        missCount++;
        CancelDirectionalMove();
        currentPerfectStreak = 0;
        BreakCombo();
        ApplyMissDamage();
        PlayAnimation(hitAnimName);
        ShowFeedbackSprite(missSprite);
        PlaySound(badMissSound);
    }

    public void RegisterMissedNote()
    {
        if (!isGameOver) OnNoteMissed();
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

    void AddCombo()
    {
        combo++;
        if (combo > maxCombo) maxCombo = combo;
        UpdateComboUI();
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
        if (show && comboNumber != null) comboNumber.SetValue(combo);
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
        AudioClip clip;
        if (isLastOfWave || currentPerfectStreak >= PerfectChainLength)
        {
            clip = perfectEndSound;
            currentPerfectStreak = 0;
        }
        else
        {
            if (perfectStepSounds == null || perfectStepSounds.Length == 0) return;
            int index = Mathf.Clamp(currentPerfectStreak - 1, 0, perfectStepSounds.Length - 1);
            clip = perfectStepSounds[index];
        }
        PlaySound(clip);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
