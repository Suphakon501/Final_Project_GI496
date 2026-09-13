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

    [Header("Cooldown System")]
    public float cooldownTime = 0.08f;
    private float nextAllowedPressTime = 0f;

    [Header("Player Health (HP)")]
    [SerializeField] private HealthBarUI healthBar;
    [SerializeField] private float perfectHealthGain = 12f;
    [SerializeField] private float goodHealthGain = 6f;
    [SerializeField] private float badHealthGain = 2f;
    [SerializeField] private float missHealthDamage = 20f;

    [Header("UI Feedback Text")]
    public TextMeshProUGUI feedbackText;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private static readonly int PreThrow = Animator.StringToHash("PreThrow");
    private static readonly int ThrowCollect = Animator.StringToHash("ThrowCollect");

    void Awake()
    { 
        instance = this;
      
        if (animator == null)
            animator = GetComponent<Animator>();


    }

    void Start()
    {
        if (healthBar == null)
            healthBar = Object.FindFirstObjectByType<HealthBarUI>();

        if (healthBar != null)
            healthBar.ResetHealth();
        score = 0;
        isGameOver = false;
        Time.timeScale = 1f;
        UpdateScoreUI();

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

        if (Input.GetKeyDown(KeyCode.Space)) TryHit(KeyCode.Space);
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

        // ‡ø ∑’Ë 1: æ‘¡æÏ™ÿ¥ªÿË¡ W, A, S, D
        if (!targetLipid.sequenceCompleted)
        {
            if (pressedKey == KeyCode.Space) return;

            KeyCode expectedKey = targetLipid.keySequence[targetLipid.currentKeyIndex];

            if (pressedKey == expectedKey)
            {
                animator?.SetTrigger(PreThrow);
                targetLipid.CorrectKeyInput();
            }
            else
            {
                targetLipid.ResetSequence();
            }
        }
        else
        {
           // °¥ Spacebar ‡™Á§‚´π ’°≈“ß®Õ
            if (pressedKey == KeyCode.Space)
            {
                animator?.SetTrigger(ThrowCollect);
                string hitResult = GameUIManager.instance.CheckHitZone();

                if (hitResult == "Perfect")
                {
                    score += 300;
                    ChangeHealth(perfectHealthGain);
                    ShowFeedback("PERFECT! +300");
                    UpdateScoreUI();
                    GameUIManager.instance.HideAllUI(); 
                    Destroy(targetLipid.gameObject);
                }
                else if (hitResult == "Good")
                {
                    score += 150;
                    ChangeHealth(goodHealthGain);
                    ShowFeedback("GOOD! +150");
                    UpdateScoreUI();
                    GameUIManager.instance.HideAllUI();
                    Destroy(targetLipid.gameObject);
                }
                else
                {
                    score += 50;
                    ChangeHealth(badHealthGain);
                    ShowFeedback("BAD +50");
                    UpdateScoreUI();
                    GameUIManager.instance.HideAllUI();
                    Destroy(targetLipid.gameObject);
                }
            }
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "" + score;
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

        ChangeHealth(-missHealthDamage);
        ShowFeedback("DAMAGE!");

        if (healthBar != null && healthBar.IsEmpty)
        {
            Die();
        }
    }

    private void ChangeHealth(float amount)
    {
        if (healthBar == null) return;

        if (amount >= 0f)
            healthBar.AddHealth(amount);
        else
            healthBar.TakeDamage(-amount);
    }

    void Die()
    {
        isGameOver = true;
        ShowFeedback("GAME OVER!");
        Debug.Log("GAME OVER! §–·ππ ÿ∑∏‘: " + score);

       
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
