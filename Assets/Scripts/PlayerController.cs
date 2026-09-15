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

    void Awake() { instance = this; }

    void Start()
    {
        score = 0;
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

        LipidMovement[] allLipids = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);
        if (allLipids.Length == 0) return;

        LipidMovement targetLipid = null;
        float closestX = float.MaxValue;

        foreach (LipidMovement lipid in allLipids)
        {
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

                //  Skill Check  
                if (RingSkillCheck.instance != null)
                {
                    RingSkillCheck.instance.StartRingCheck();
                }

                Destroy(targetLipid.gameObject);

                Invoke("ResetToIdle", 0.4f);
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

    void PlayAnimationDirectly(string animName)
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(animName))
        {
            playerAnimator.Play(animName, 0, 0f);
        }
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

    void Die()
    {
        isGameOver = true;
        ShowFeedback("GAME OVER!");
        Debug.Log("GAME OVER! ¤Ðá¹¹ÊØ·¸Ô: " + score);

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