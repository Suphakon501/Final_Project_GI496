using UnityEngine;
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
    public TextMeshProUGUI scoreText;

    [Header("Animation System")]
    public Animator playerAnimator;
    [SerializeField] private string idleAnimName = "Player_Idle";
    [SerializeField] private string prepAnimName = "Player_Prep";
    [SerializeField] private string throwAnimName = "Player_Throw";

    [Header("Cooldown System")]
    public float cooldownTime = 0.08f;
    private float nextAllowedPressTime = 0f;

    [Header("UI Feedback Text")]
    public TextMeshProUGUI feedbackText;

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
        isGameOver = false;
        Time.timeScale = 1f;
        UpdateScoreUI();

        PlayAnimationDirectly(idleAnimName);
        if (feedbackText != null) feedbackText.text = "";
    }

    void Update()
    {
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

            PlayAnimationDirectly(throwAnimName);

            if (accuracy == "PERFECT")
            {
                score += 300;
                if (HealthBarUI.instance != null)
                {
                    HealthBarUI.instance.AddHealth(5f); // ได้เลือด +5 เฉพาะ Perfect
                }

                currentPerfectStreak++;
                ShowFeedback("PERFECT! +300 (+5 HP)");

                PlayPerfectSound(isLastOne);

                // ส่งค่า "PERFECT" ให้ MiniLipid เล่นพาร์ทิเคิล Perfect
                targetMini.ConsumeMini("PERFECT");
            }
            else if (accuracy == "GOOD")
            {
                score += 100;
                currentPerfectStreak = 0; // ตัดเชน Perfect ทันที
                ShowFeedback("GOOD! +100");

                PlaySound(goodSound);

                // ส่งค่า "GOOD" ให้ MiniLipid เล่นพาร์ทิเคิล Good
                targetMini.ConsumeMini("GOOD");
            }
            else
            {
                score += 25;
                currentPerfectStreak = 0; // ตัดเชน Perfect ทันที
                ShowFeedback("BAD!");

                PlaySound(badMissSound);

                // ส่งค่า "BAD" ให้ MiniLipid เล่นพาร์ทิเคิล Bad/Miss
                targetMini.ConsumeMini("BAD");
            }
        }
        else
        {
            currentPerfectStreak = 0;
            PlayAnimationDirectly(idleAnimName);
            ShowFeedback("MISS!");

            PlaySound(badMissSound);

            // กดผิดปุ่ม ส่งค่า "MISS" ให้ MiniLipid เล่นพาร์ทิเคิล Bad/Miss
            targetMini.ConsumeMini("MISS");
        }

        UpdateScoreUI();
        Invoke("ResetToIdle", 0.3f);
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
            playerAnimator.Play(animName, 0, 0f);
        }
    }

    void ResetToIdle()
    {
        if (!isGameOver) PlayAnimationDirectly(idleAnimName);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
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
        ShowFeedback("GAME OVER!");
        Debug.Log("GAME OVER! คะแนนสุทธิ: " + score);

        PlayAnimationDirectly(idleAnimName);

        MiniLipid[] remainingMinis = Object.FindObjectsByType<MiniLipid>(FindObjectsSortMode.None);
        foreach (var mini in remainingMinis) Destroy(mini.gameObject);

        LipidMovement[] remainingLipids = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);
        foreach (var lipid in remainingLipids) Destroy(lipid.gameObject);

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}