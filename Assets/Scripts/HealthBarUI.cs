using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public static HealthBarUI instance;

    [SerializeField] private Image fillImage;

    [Header("HP Settings")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float startingHP = 100f;

    [Header("เลือดลดอัตโนมัติตามเวลา (ถ้าไม่ต้องการให้ลดเอง ปรับเป็น 0 ได้ครับ)")]
    [SerializeField] private float hpDrainPerSecond = 0f; // ตั้งเป็น 0 ไปก่อน จะได้ให้เลือดลดเฉพาะตอนชนหรือโดนโจมตีครับ

    [Header("Visual Speed (ความเร็วภาพหลอดเลือดวิ่งตาม)")]
    [SerializeField] private float visualDrainSpeed = 20f; // ปรับให้สูงขึ้นมากๆ (เช่น 20 หรือ 50) เลือดจะกระชากลดลงทันทีแบบแทบไม่สโลว์
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

        // 1. เลือดลดอัตโนมัติตามเวลา (ถ้า hpDrainPerSecond เป็น 0 จะไม่ลดเอง)
        if (hpDrainPerSecond > 0f)
        {
            currentHP = Mathf.Max(0f, currentHP - (hpDrainPerSecond * Time.deltaTime));
        }

        // 2. เช็คว่าถ้าเลือดหมด ให้สั่งจบเกม
        if (currentHP <= 0f && !PlayerController.isGameOver)
        {
            currentHP = 0f;

            if (PlayerController.instance != null)
            {
                PlayerController.instance.CheckGameOver();
            }
        }

        // 3. ทำให้ภาพหลอดเลือดบนจอวิ่งตามเลือดจริงแบบรวดเร็วทันใจ
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

    public void AddHealth(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
    }

    public void TakeDamage(float amount)
    {
        // หักเลือดจริงทันที
        currentHP = Mathf.Clamp(currentHP - amount, 0f, maxHP);

        // บังคับให้ displayedHP กระชากตามไปทันที ไม่ต้องรอไหลสโลว์ (เลือดจะหายวูบลงทันทีตามคาด)
        displayedHP = currentHP;

        if (fillImage != null)
            fillImage.fillAmount = displayedHP / maxHP;
    }
}