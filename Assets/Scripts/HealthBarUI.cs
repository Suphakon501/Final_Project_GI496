using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public static HealthBarUI instance;

    [SerializeField] private Image fillImage;

    [Header("HP Settings")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float startingHP = 100f;

    [Header("Passive Drain")]
    [SerializeField] private float hpDrainPerSecond = 0f;
    [SerializeField] private bool drainOnlyDuringWaves = true;

    private bool waveActive = false;

    [Header("Debug")]
    [SerializeField] private bool invincible = false;

    [Header("Visual Speed")]
    [SerializeField] private float visualDrainSpeed = 20f;
    [SerializeField] private float visualHealSpeed = 5f;

    private float currentHP;
    private float displayedHP;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsEmpty => currentHP <= 0f;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        ResetHealth();
    }

    private void Update()
    {
        if (PlayerController.isGameOver) return;

        if (!invincible && hpDrainPerSecond > 0f && (!drainOnlyDuringWaves || waveActive))
        {
            currentHP = Mathf.Max(0f, currentHP - (hpDrainPerSecond * Time.deltaTime));
        }

        if (currentHP <= 0f && !PlayerController.isGameOver)
        {
            currentHP = 0f;

            if (PlayerController.instance != null)
            {
                PlayerController.instance.CheckGameOver();
            }
        }

        float visualSpeed = displayedHP < currentHP ? visualHealSpeed : visualDrainSpeed;
        displayedHP = Mathf.Lerp(
            displayedHP,
            currentHP,
            visualSpeed * Time.deltaTime
        );

        if (fillImage != null)
            fillImage.fillAmount = displayedHP / maxHP;
    }

    public void ResetHealth()
    {
        currentHP = Mathf.Clamp(startingHP, 0f, maxHP);
        displayedHP = currentHP;

        if (fillImage != null)
            fillImage.fillAmount = displayedHP / maxHP;
    }

    public void SetWaveActive(bool active)
    {
        waveActive = active;
    }

    public void AddHealth(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
    }

    public void TakeDamage(float amount)
    {
        if (invincible) return;

        currentHP = Mathf.Clamp(currentHP - amount, 0f, maxHP);

        displayedHP = currentHP;

        if (fillImage != null)
            fillImage.fillAmount = displayedHP / maxHP;
    }
}
