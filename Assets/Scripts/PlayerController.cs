using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public static bool isGameOver = false;

    private static readonly KeyCode[] InputKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    private const int PerfectScore = 300;
    private const int GoodScore = 100;
    private const int BadScore = 25;
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

    [Header("Animation System (Animator state names)")]
    public Animator playerAnimator;
    [SerializeField] private string idleAnimName = "Player_Idle";
    [SerializeField] private string drawAnimName = "Player_Draw";
    [SerializeField] private string[] throwAnimNames = { "Player_ThrowA", "Player_ThrowB" };
    [SerializeField] private string hitAnimName = "Player_Hit";
    [SerializeField] private string sheatheAnimName = "Player_Sheathe";
    [SerializeField] private float sheatheDelay = 0.5f;

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

    [Header("Perfect Chain Sounds")]
    [SerializeField] private AudioClip[] perfectStepSounds = new AudioClip[5];
    [SerializeField] private AudioClip perfectEndSound;

    private float nextAllowedPressTime = 0f;
    private int throwStep = 0;
    private int missStreak = 0;
    private int activeWaves = 0;
    private int currentPerfectStreak = 0;
    private bool warnedMissingCombo = false;

    void Awake()
    {
        instance = this;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        isGameOver = false;
        Time.timeScale = 1f;
        score = 0;
        combo = 0;
        maxCombo = 0;

        UpdateScoreUI();
        UpdateComboUI();
        PlayAnimation(idleAnimName);
        if (feedbackText != null) feedbackText.text = "";
        HideFeedbackSprite();
    }

    void Update()
    {
        ShrinkFeedbackSprite();

        if (isGameOver || Time.time < nextAllowedPressTime) return;

        foreach (var key in InputKeys)
        {
            if (Input.GetKeyDown(key))
            {
                TryHit(key);
                break;
            }
        }
    }

    void TryHit(KeyCode pressedKey)
    {
        nextAllowedPressTime = Time.time + cooldownTime;

        MiniLipid target = MiniLipid.FindNextToHit();
        if (target == null) return;

        if (pressedKey == target.assignedKey)
        {
            HitResult result = target.EvaluateAccuracy();
            OnNoteHit(result, target.IsLastInRow());
            target.ConsumeMini(result);
        }
        else
        {
            OnNoteMissed();
            target.ConsumeMini(HitResult.Miss);
        }

        UpdateScoreUI();
    }

    void OnNoteHit(HitResult result, bool isLastOfWave)
    {
        PlayThrowAnimation();
        AddCombo();
        missStreak = 0;

        switch (result)
        {
            case HitResult.Perfect:
                score += PerfectScore;
                ChangeHealth(hpPerfect);
                currentPerfectStreak++;
                ShowFeedbackSprite(perfectSprite);
                PlayPerfectSound(isLastOfWave);
                break;

            case HitResult.Good:
                score += GoodScore;
                ChangeHealth(hpGood);
                currentPerfectStreak = 0;
                ShowFeedbackSprite(goodSprite);
                PlaySound(goodSound);
                break;

            default:
                score += BadScore;
                ChangeHealth(-badDamage);
                currentPerfectStreak = 0;
                ShowFeedbackSprite(badSprite);
                PlaySound(badMissSound);
                break;
        }
    }

    void OnNoteMissed()
    {
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
        HideFeedbackSprite();
        if (feedbackText != null) feedbackText.text = "GAME OVER!";
        PlayAnimation(idleAnimName);
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

    void UpdateScoreUI()
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

    void PlayThrowAnimation()
    {
        if (throwAnimNames == null || throwAnimNames.Length == 0) return;
        PlayAnimation(throwAnimNames[throwStep % throwAnimNames.Length]);
        throwStep++;
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
