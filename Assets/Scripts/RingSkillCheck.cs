using UnityEngine;
using UnityEngine.UI;

public class RingSkillCheck : MonoBehaviour
{
    public static RingSkillCheck instance;

    [Header("UI Elements")]
    public RectTransform shrinkingRing; // วงแหวนวงนอกที่จะหดเข้ามา
    public RectTransform targetRing;    // วงเป้าหมายตรงกลาง

    [Header("Color Settings (เปลี่ยนสีตามจังหวะ)")]
    private Image shrinkingRingImage;
    public Color outOfRangeColor = Color.red;
    public Color inRangeColor = Color.green;

    [Header("Settings")]
    public float shrinkDuration = 1.5f; // เวลาที่วงแหวนใช้หดเข้ามาจนสุด (วินาที)
    public float startScale = 3.0f;     // ขนาดเริ่มต้นของวงแหวนด้านนอก
    public float targetScale = 1.0f;    // ขนาดเป้าหมายตอนมาบรรจบพอดี

    private float currentTimer = 0f;
    private bool isChecking = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (shrinkingRing != null)
        {
            shrinkingRingImage = shrinkingRing.GetComponent<Image>();
        }
    }

    void Start()
    {
        SetVisible(false);
    }

    void Update()
    {
        if (!isChecking) return;

        currentTimer += Time.deltaTime;
        float progress = currentTimer / shrinkDuration;

        if (shrinkingRing != null)
        {
            float currentScale = Mathf.Lerp(startScale, targetScale, progress);
            shrinkingRing.localScale = new Vector3(currentScale, currentScale, 1f);

            // เช็คเปลี่ยนสี โดยใช้ 0.85 เป็นจุดกลาง (ช่วงที่ยอมรับคือ 0.75 ถึง 0.95)
            if (shrinkingRingImage != null)
            {
                if (progress >= 0.75f && progress <= 0.95f)
                {
                    shrinkingRingImage.color = inRangeColor;  // สีเขียว
                }
                else
                {
                    shrinkingRingImage.color = outOfRangeColor; // สีแดง
                }
            }
        }

        if (currentTimer >= shrinkDuration + 0.15f)
        {
            FailSkillCheck();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            EvaluateSkillCheck();
        }
    }

    public void StartRingCheck()
    {
        isChecking = true;
        currentTimer = 0f;

        SetVisible(true);
        if (shrinkingRing != null)
        {
            shrinkingRing.localScale = new Vector3(startScale, startScale, 1f);
            if (shrinkingRingImage != null) shrinkingRingImage.color = outOfRangeColor;
        }
    }

    void EvaluateSkillCheck()
    {
        float progress = currentTimer / shrinkDuration;

        // ล็อคเป้าหมายความแม่นยำช่วง 0.75 ถึง 0.95 โดยมีจุดพีคตรง 0.85
        if (progress >= 0.75f && progress <= 0.95f)
        {
            Debug.Log("PERFECT! ได้รับเลือดคืน +10 HP");

            if (HealthBarUI.instance != null)
            {
                HealthBarUI.instance.AddHealth(10f);
            }
        }
        else
        {
            Debug.Log("MISS! กดผิดจังหวะ ไม่ได้เลือด");
        }

        CloseSkillCheck();
    }

    void FailSkillCheck()
    {
        Debug.Log("MISS! หมดเวลา");
        CloseSkillCheck();
    }

    void CloseSkillCheck()
    {
        isChecking = false;
        SetVisible(false);
    }

    void SetVisible(bool isVisible)
    {
        if (shrinkingRing != null) shrinkingRing.gameObject.SetActive(isVisible);
        if (targetRing != null) targetRing.gameObject.SetActive(isVisible);
    }
}