using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public static bool isGameOver = false;

    [Header("Score System")]
    public int score = 0;
    public TextMeshProUGUI scoreText;                 // ของเดิม (ปล่อยว่างได้ถ้าใช้ sprite แล้ว)
    [SerializeField] private SpriteNumber scoreNumber; // ตัวเลขคะแนนแบบ sprite

    [Header("Combo System (กดโดนติดกัน, MISS แล้วเริ่มนับใหม่)")]
    [SerializeField] private SpriteNumber comboNumber;
    [SerializeField] private GameObject comboRoot;     // กลุ่ม UI คอมโบทั้งก้อน (ตัวเลข + ป้าย COMBO) ซ่อนตอนคอมโบน้อย
    [SerializeField] private int minComboToShow = 2;
    public int combo { get; private set; }
    public int maxCombo { get; private set; }
    private bool warnedMissingCombo = false;

    [Header("Animation System")]
    public Animator playerAnimator;
    [SerializeField] private string idleAnimName = "Player_Idle";       // ยืนเฉยๆ ตอนไม่มีศัตรู (วนลูป)
    [SerializeField] private string drawAnimName = "Player_Draw";       // ศัตรูมาถึง: เลือดกลายเป็นมีด แล้วตั้งท่าเอง
    [SerializeField] private string readyAnimName = "Player_Ready";     // ตั้งท่ารอกด
    [SerializeField] private string[] throwAnimNames = { "Player_ThrowA", "Player_ThrowB" }; // สลับกันทุกครั้งที่กดโดน
    [SerializeField] private string hitAnimName = "Player_Hit";         // MISS (กดผิด / ปล่อยโน้ตผ่าน): ท่าโดนตี แล้วกลับท่ารอเอง
    [SerializeField] private string sheatheAnimName = "Player_Sheathe"; // คลื่นจบ: มีดละลายกลับ แล้วกลับไป Idle เอง
    [SerializeField] private float sheatheDelay = 0.5f;                  // ค้างท่าหลังโน้ตตัวสุดท้ายกี่วินาทีก่อนเก็บมีด

    private int throwStep = 0;

    [Header("HP Balance (เลือดขึ้น/ลงตามผลการกด)")]
    [SerializeField] private float hpPerfect = 2f;         // PERFECT ได้เลือดคืน
    [SerializeField] private float hpGood = 1f;            // GOOD ได้เลือดคืน
    [SerializeField] private float badDamage = 2f;         // BAD เสียเลือด
    [SerializeField] private float missDamageBase = 5f;    // MISS ครั้งแรก
    [SerializeField] private float missDamageStep = 3f;    // MISS ติดกัน หนักขึ้นครั้งละเท่านี้ (-5, -8, -11, ...)
    [SerializeField] private float missDamageMax = 20f;    // MISS หนักสุดไม่เกินนี้
    [SerializeField] private float cleanWaveBonus = 3f;    // เคลียร์โน้ตของไขมันตัวนั้นหมดโดยไม่ MISS เลย ได้โบนัส

    private int missStreak = 0;
    private int activeWaves = 0; // ไขมันที่ยังมีโน้ตค้างอยู่ (โหมดชาร์ตอาจซ้อนกัน 2 ตัว)

    [Header("Cooldown System")]
    public float cooldownTime = 0.08f;
    private float nextAllowedPressTime = 0f;

    [Header("UI Feedback Text (ใช้แค่ตอน GAME OVER)")]
    public TextMeshProUGUI feedbackText;

    [Header("UI Feedback Sprites (Perfect / Good / Bad / Miss)")]
    [SerializeField] private Image feedbackImage;
    [SerializeField] private Sprite perfectSprite;
    [SerializeField] private Sprite goodSprite;
    [SerializeField] private Sprite badSprite;
    [SerializeField] private Sprite missSprite;
    [SerializeField] private float feedbackDuration = 0.6f;
    [SerializeField] private float feedbackPopScale = 1.4f;   // ขนาดตอนเด้งขึ้นมา
    [SerializeField] private float feedbackShrinkSpeed = 15f; // ความเร็วหดกลับขนาดปกติ
    [SerializeField] private float feedbackMaxTilt = 6f;      // เอียงสุ่ม +- องศา ให้ดูไม่ซ้ำ

    [Header("Audio System (Sound Effects)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip goodSound;
    [SerializeField] private AudioClip badMissSound;

    [Header("Perfect Chain Sounds (Max 6 Steps)")]
    [SerializeField] private AudioClip[] perfectStepSounds = new AudioClip[5];
    [SerializeField] private AudioClip perfectEndSound;

    private int currentPerfectStreak = 0;

    void Awake()
    {
        instance = this;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        score = 0;
        currentPerfectStreak = 0;
        combo = 0;
        maxCombo = 0;
        missStreak = 0;
        activeWaves = 0;
        isGameOver = false;
        Time.timeScale = 1f;
        UpdateScoreUI();
        UpdateComboUI();

        PlayAnimationDirectly(idleAnimName);
        if (feedbackText != null) feedbackText.text = "";
        HideFeedbackSprite();
    }

    void Update()
    {
        // ให้ sprite feedback หดกลับขนาดปกติหลังเด้ง
        if (feedbackImage != null && feedbackImage.enabled)
        {
            feedbackImage.transform.localScale = Vector3.Lerp(
                feedbackImage.transform.localScale,
                Vector3.one,
                feedbackShrinkSpeed * Time.deltaTime
            );
        }

        if (isGameOver) return;
        if (Time.time < nextAllowedPressTime) return;

        if (Input.GetKeyDown(KeyCode.W)) TryHit(KeyCode.W);
        else if (Input.GetKeyDown(KeyCode.A)) TryHit(KeyCode.A);
        else if (Input.GetKeyDown(KeyCode.S)) TryHit(KeyCode.S);
        else if (Input.GetKeyDown(KeyCode.D)) TryHit(KeyCode.D);
    }

    void TryHit(KeyCode pressedKey)
    {
        nextAllowedPressTime = Time.time + cooldownTime;

        MiniLipid[] allMinis = Object.FindObjectsByType<MiniLipid>(FindObjectsSortMode.None);
        if (allMinis.Length == 0) return;

        MiniLipid targetMini = null;
        float lowestX = float.MaxValue;

        foreach (var mini in allMinis)
        {
            if (mini.transform.position.x < lowestX)
            {
                lowestX = mini.transform.position.x;
                targetMini = mini;
            }
        }

        if (targetMini == null) return;

        if (pressedKey == targetMini.assignedKey)
        {
            string accuracy = targetMini.EvaluateAccuracy();
            bool isLastOne = targetMini.IsLastInRow();

            PlayThrowAnimation();
            AddCombo(); // กดโดน (PERFECT / GOOD / BAD) นับคอมโบต่อ

            if (accuracy == "PERFECT")
            {
                score += 300;
                OnNoteHit(hpPerfect);

                currentPerfectStreak++;
                ShowFeedbackSprite(perfectSprite);

                PlayPerfectSound(isLastOne);

                // ส่งค่า "PERFECT" ให้ MiniLipid เล่นพาร์ทิเคิล Perfect
                targetMini.ConsumeMini("PERFECT");
            }
            else if (accuracy == "GOOD")
            {
                score += 100;
                OnNoteHit(hpGood);
                currentPerfectStreak = 0; // ตัดเชน Perfect ทันที
                ShowFeedbackSprite(goodSprite);

                PlaySound(goodSound);

                // ส่งค่า "GOOD" ให้ MiniLipid เล่นพาร์ทิเคิล Good
                targetMini.ConsumeMini("GOOD");
            }
            else
            {
                score += 25;
                OnNoteHit(-badDamage);
                currentPerfectStreak = 0; // ตัดเชน Perfect ทันที
                ShowFeedbackSprite(badSprite);

                PlaySound(badMissSound);

                // ส่งค่า "BAD" ให้ MiniLipid เล่นพาร์ทิเคิล Bad/Miss
                targetMini.ConsumeMini("BAD");
            }
        }
        else
        {
            currentPerfectStreak = 0;
            BreakCombo();
            ApplyMissDamage();
            PlayAnimationDirectly(hitAnimName); // กดผิดปุ่ม: ไม่ขว้าง เล่นท่าโดนตีแทน
            ShowFeedbackSprite(missSprite);

            PlaySound(badMissSound);

            // กดผิดปุ่ม ส่งค่า "MISS" ให้ MiniLipid เล่นพาร์ทิเคิล Bad/Miss
            targetMini.ConsumeMini("MISS");
        }

        UpdateScoreUI();
        // ไม่ต้องสั่งกลับท่าเอง: ท่าขว้างเล่นจบแล้ว Animator พากลับ Player_Ready ให้
    }

    void PlayThrowAnimation()
    {
        if (throwAnimNames == null || throwAnimNames.Length == 0) return;
        PlayAnimationDirectly(throwAnimNames[throwStep % throwAnimNames.Length]);
        throwStep++;
    }

    // LipidMovement เรียกตอนศัตรูหยุดและเริ่มปล่อยโน้ต
    public void OnWaveStart()
    {
        if (isGameOver) return;
        CancelInvoke("PlaySheathe");
        activeWaves++;
        if (activeWaves > 1) return; // ยังสู้ไขมันตัวก่อนอยู่ ไม่ต้องควักมีดใหม่

        throwStep = 0;
        if (HealthBarUI.instance != null) HealthBarUI.instance.SetWaveActive(true);
        PlayAnimationDirectly(drawAnimName);
    }

    // LipidMovement เรียกตอนโน้ตของคลื่นนี้หมดแล้ว
    // หน่วงไว้ก่อน ไม่งั้นท่าขว้างของโน้ตตัวสุดท้ายจะโดนทับทันทีจนมองไม่เห็น
    public void OnWaveEnd(bool hadMiss)
    {
        if (isGameOver) return;

        if (!hadMiss && cleanWaveBonus > 0f && HealthBarUI.instance != null)
            HealthBarUI.instance.AddHealth(cleanWaveBonus);

        activeWaves = Mathf.Max(0, activeWaves - 1);
        if (activeWaves > 0) return; // ไขมันตัวถัดไปมาแล้ว ยังไม่เก็บมีด

        if (HealthBarUI.instance != null) HealthBarUI.instance.SetWaveActive(false);
        CancelInvoke("PlaySheathe");
        Invoke("PlaySheathe", sheatheDelay);
    }

    // กดโดนโน้ต (PERFECT / GOOD / BAD): ตัดสาย MISS ติดกัน แล้วเพิ่ม/ลดเลือดตามผล
    void OnNoteHit(float hpChange)
    {
        missStreak = 0;
        if (HealthBarUI.instance == null) return;
        if (hpChange > 0f) HealthBarUI.instance.AddHealth(hpChange);
        else if (hpChange < 0f) HealthBarUI.instance.TakeDamage(-hpChange);
    }

    // MISS: ครั้งแรกเบา ถ้าพลาดติดกันจะหนักขึ้นเรื่อยๆ (พลาดครั้งเดียวยังเอาคืนได้ พังติดกันถึงจะเจ็บ)
    void ApplyMissDamage()
    {
        float damage = Mathf.Min(missDamageBase + missDamageStep * missStreak, missDamageMax);
        missStreak++;
        if (HealthBarUI.instance != null) HealthBarUI.instance.TakeDamage(damage);
    }

    void PlaySheathe()
    {
        if (!isGameOver) PlayAnimationDirectly(sheatheAnimName);
    }

    void PlayPerfectSound(bool isLastOfSequence)
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = null;

        // ถ้าเป็นตัวสุดท้าย หรือครบ 6 ตัวพอดี เล่นเสียงปิดทันที
        if (isLastOfSequence || currentPerfectStreak >= 6)
        {
            clipToPlay = perfectEndSound;
            currentPerfectStreak = 0;
        }
        else
        {
            int soundIndex = Mathf.Clamp(currentPerfectStreak - 1, 0, perfectStepSounds.Length - 1);
            clipToPlay = perfectStepSounds[soundIndex];
        }

        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void PlayAnimationDirectly(string animName)
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(animName))
        {
            if (!playerAnimator.HasState(0, Animator.StringToHash(animName)))
            {
                Debug.LogWarning($"[PlayerController] ไม่พบท่า '{animName}' ใน Animator ({playerAnimator.runtimeAnimatorController?.name}) " +
                                 "→ ลองกด Tools > Build Player Animations ใหม่ และเช็คว่า Animator ใช้ PlayerMain.controller");
                return;
            }
            playerAnimator.Play(animName, 0, 0f);
        }
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
            Debug.LogWarning("[PlayerController] คอมโบนับได้แล้ว (" + combo + ") แต่ช่อง Combo Number ยังว่าง → ลาก Object ที่มี SpriteNumber ของคอมโบมาใส่ที่ตัว Player");
        }
        GameObject root = comboRoot != null ? comboRoot : (comboNumber != null ? comboNumber.gameObject : null);
        if (root != null && root.activeSelf != show) root.SetActive(show);
        if (show && comboNumber != null) comboNumber.SetValue(combo);
    }

    void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            if (!isGameOver)
            {
                CancelInvoke("ClearFeedback");
                Invoke("ClearFeedback", 0.6f);
            }
        }
    }

    void ClearFeedback()
    {
        if (feedbackText != null && !isGameOver) feedbackText.text = "";
    }

    void ShowFeedbackSprite(Sprite sprite)
    {
        if (feedbackImage == null || sprite == null) return;

        feedbackImage.sprite = sprite;
        feedbackImage.enabled = true;
        feedbackImage.transform.localScale = Vector3.one * feedbackPopScale;
        feedbackImage.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-feedbackMaxTilt, feedbackMaxTilt));

        CancelInvoke("HideFeedbackSprite");
        Invoke("HideFeedbackSprite", feedbackDuration);
    }

    void HideFeedbackSprite()
    {
        if (feedbackImage != null) feedbackImage.enabled = false;
    }

    // MiniLipid เรียกตอนผู้เล่นปล่อยให้โน้ตวิ่งเลยเส้นไปโดยไม่กด
    public void RegisterMissedNote()
    {
        if (isGameOver) return;

        currentPerfectStreak = 0;
        BreakCombo();
        ApplyMissDamage();
        PlayAnimationDirectly(hitAnimName);
        ShowFeedbackSprite(missSprite);
        PlaySound(badMissSound);
    }

    public void CheckGameOver()
    {
        if (isGameOver) return;

        if (HealthBarUI.instance != null && HealthBarUI.instance.IsEmpty)
        {
            Die();
        }
    }

    void Die()
    {
        isGameOver = true;
        HideFeedbackSprite();
        ShowFeedback("GAME OVER!");
        Debug.Log("GAME OVER! คะแนนสุทธิ: " + score);

        PlayAnimationDirectly(idleAnimName);

        MiniLipid[] remainingMinis = Object.FindObjectsByType<MiniLipid>(FindObjectsSortMode.None);
        foreach (var mini in remainingMinis) Destroy(mini.gameObject);

        LipidMovement[] remainingLipids = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);
        foreach (var lipid in remainingLipids) Destroy(lipid.gameObject);

        // มี LevelManager = ไปหน้าจบด่าน (รีสตาร์ท / กลับเมนู) แทนการปิดเกม
        if (LevelManager.instance != null)
        {
            LevelManager.instance.OnPlayerDied();
            return;
        }

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}