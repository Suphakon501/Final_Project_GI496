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

    [Header("Combo System (เชื่อมกับ RingSkillCheck)")]
    public int comboCount = 0;
    public TextMeshProUGUI comboText; // ถ้าไม่ผูก Text ไว้ใน Inspector จะแค่ไม่โชว์ UI แต่คะแนน/คอมโบยังนับปกติ
    [SerializeField] private int comboBonusPerStack = 50; // คอมโบยิ่งสูง ยิ่งได้โบนัสคะแนนต่อครั้งมากขึ้น

    [Header("Animation System")]
    public Animator playerAnimator;
    [SerializeField] private string idleAnimName = "Player_Idle";
    [SerializeField] private string prepAnimName = "Player_Prep";
    [SerializeField] private string throwAnimName = "Player_Throw";
    [SerializeField] private float throwHoldDuration = 0.4f; // ระยะเวลาที่ค้างท่า Throw ก่อนกลับ Idle
    [SerializeField] private float minPoseVisibleTime = 0.15f; // แต่ละท่าต้องค้างจออย่างน้อยเท่านี้ก่อนโดนท่าถัดไปทับ

    [Header("Cooldown System")]
    public float cooldownTime = 0.08f;
    private float nextAllowedPressTime = 0f;

    [Header("UI Feedback Text")]
    public TextMeshProUGUI feedbackText;

    void Awake() { instance = this; }

    void Start()
    {
        score = 0;
        comboCount = 0;
        isGameOver = false;
        Time.timeScale = 1f;
        UpdateScoreUI();
        UpdateComboUI();

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

        LipidMovement[] allLipids = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);
        if (allLipids.Length == 0) return;

        LipidMovement targetLipid = null;
        float closestX = float.MaxValue;

        foreach (LipidMovement lipid in allLipids)
        {
            if (lipid.sequenceCompleted) continue; // ตัวนี้กำลังรอผล skill check อยู่ ยังไม่ใช่เป้าใหม่

            if (lipid.transform.position.x < closestX)
            {
                closestX = lipid.transform.position.x;
                targetLipid = lipid;
            }
        }

        if (targetLipid == null) return;

        KeyCode expectedKey = targetLipid.keySequence[targetLipid.currentKeyIndex];

        if (pressedKey == expectedKey)
        {
            // กดถูกคีย์อย่างเดียวไม่พอ ถ้ามี BeatManager ต้องกดให้ตรงจังหวะเพลงด้วย ไม่งั้นนับเป็นพลาดเหมือนกดผิดคีย์
            if (BeatManager.instance != null && BeatManager.instance.GradeCurrentPress() == BeatGrade.Miss)
            {
                targetLipid.ResetSequence();
                PlayAnimationDirectly(idleAnimName);
                ShowFeedback("OFF-BEAT!");
                return;
            }

            targetLipid.CorrectKeyInput();

            int currentIndex = targetLipid.currentKeyIndex;

            if (targetLipid.sequenceCompleted)
            {
                PlayAnimationDirectly(throwAnimName);

                score += 300;
                ShowFeedback("GREAT! +300");
                UpdateScoreUI();

                if (GameUIManager.instance != null)
                {
                    GameUIManager.instance.HideAllUI();
                }

                targetLipid.enabled = false; // หยุดไม่ให้เดินต่อ/ทำดาเมจซ้ำ ระหว่างรอผล skill check

                //  Skill Check — RingSkillCheck จะเป็นคนทำลาย targetLipid เองตอนกดจบ (ไม่ว่า Perfect หรือ Miss)
                if (RingSkillCheck.instance != null)
                {
                    RingSkillCheck.instance.StartRingCheck(targetLipid.gameObject);
                }
                else
                {
                    Destroy(targetLipid.gameObject);
                }

                Invoke("ResetToIdle", throwHoldDuration);
            }
            else
            {
                if (currentIndex % 2 == 1)
                {
                    PlayAnimationDirectly(prepAnimName);
                }
                else
                {
                    PlayAnimationDirectly(throwAnimName);
                }
            }
        }
        else
        {
            targetLipid.ResetSequence();
            PlayAnimationDirectly(idleAnimName);
            ShowFeedback("MISS!");
        }
    }

    private float lastAnimStartTime = -999f;
    private string pendingAnimName;

    // การันตีว่าแต่ละท่าจะค้างบนจอนานอย่างน้อย minPoseVisibleTime
    // ไม่งั้นถ้ากดรัวเร็วกว่านี้ ท่าก่อนหน้าจะโดนสลับทับจนมองไม่ทันว่าขยับ
    void PlayAnimationDirectly(string animName)
    {
        if (playerAnimator == null || string.IsNullOrEmpty(animName)) return;

        float elapsed = Time.time - lastAnimStartTime;
        if (elapsed >= minPoseVisibleTime)
        {
            CancelInvoke(nameof(PlayPendingAnim));
            DoPlayAnimation(animName);
        }
        else
        {
            pendingAnimName = animName;
            CancelInvoke(nameof(PlayPendingAnim));
            Invoke(nameof(PlayPendingAnim), minPoseVisibleTime - elapsed);
        }
    }

    void PlayPendingAnim()
    {
        DoPlayAnimation(pendingAnimName);
    }

    void DoPlayAnimation(string animName)
    {
        playerAnimator.Play(animName, 0, 0f);
        lastAnimStartTime = Time.time;
    }

    void ResetToIdle()
    {
        if (!isGameOver)
        {
            PlayAnimationDirectly(idleAnimName);
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    // เรียกจาก RingSkillCheck หลังบีบวงจบทุกครั้ง (Perfect / Good / Miss)
    public void RegisterSkillCheckResult(SkillCheckGrade grade)
    {
        switch (grade)
        {
            case SkillCheckGrade.Perfect:
                comboCount++;
                int bonus = comboCount * comboBonusPerStack;
                score += bonus;
                ShowFeedback($"COMBO x{comboCount}! +{bonus}");
                break;

            case SkillCheckGrade.Good:
                // กดใกล้ๆ แต่ไม่ทัน Perfect: ไม่เพิ่ม ไม่หักคอมโบ แค่ได้เลือดคืนบางส่วน (ฮีลจัดการฝั่ง RingSkillCheck)
                ShowFeedback("GOOD!");
                break;

            case SkillCheckGrade.Miss:
                if (comboCount > 0) ShowFeedback("COMBO BREAK!");
                comboCount = 0;
                break;
        }

        UpdateScoreUI();
        UpdateComboUI();
    }

    void UpdateComboUI()
    {
        if (comboText != null)
        {
            comboText.text = comboCount > 0 ? "Combo x" + comboCount : "";
        }
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

    public void TakeDamage()
    {
        if (isGameOver) return;

        ShowFeedback("DAMAGE!");
        PlayAnimationDirectly(idleAnimName);

        if (HealthBarUI.instance != null && HealthBarUI.instance.IsEmpty)
        {
            Die();
        }
    }

    // เรียกจาก HealthBarUI ตอนเลือดหมดจากสาเหตุอื่นที่ไม่ใช่การโดนชน (เช่น เลือดไหลลดเองต่อวินาที)
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
        Debug.Log("GAME OVER! ��ṹ�ط��: " + score);

        PlayAnimationDirectly(idleAnimName);

        LipidSpawner[] spawners = Object.FindObjectsByType<LipidSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            spawner.enabled = false;
        }

        LipidMovement[] remainingLipids = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);
        foreach (var lipid in remainingLipids)
        {
            Destroy(lipid.gameObject);
        }

        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.HideAllUI();
        }

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}