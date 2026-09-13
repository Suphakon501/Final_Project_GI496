using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    [Header("HP")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float startingHP = 80f;

    [Header("เลือดลดต่อวินาที")]
    [SerializeField] private float hpDrainPerSecond = 0f;

    [Header("ความเร็วที่ภาพหลอดเลือดตามค่า HP")]
    [SerializeField] private float visualDrainSpeed = 0.15f;
    [SerializeField] private float visualHealSpeed = 5f;

    private float currentHP;
    private float displayedHP;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsEmpty => currentHP <= 0f;

    private void Start()
    {
        ResetHealth();
    }

    private void Update()
    {
        // ตั้งค่าเป็น 0 ได้หากต้องการให้ HP เปลี่ยนเฉพาะตอน Perfect/Good/Bad/Miss
        if (hpDrainPerSecond > 0f)
            currentHP = Mathf.Max(0f, currentHP - hpDrainPerSecond * Time.deltaTime);

        // ตอนเลือดลดให้ค่อย ๆ ลดแบบ osu! แต่ตอนฮีลให้พุ่งขึ้นมองเห็นชัด
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
        currentHP = Mathf.Clamp(currentHP - amount, 0f, maxHP);
    }
}
