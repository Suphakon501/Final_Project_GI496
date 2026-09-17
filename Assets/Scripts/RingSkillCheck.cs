using UnityEngine;
using UnityEngine.UI;

public enum SkillCheckGrade { Perfect, Good, Miss }

public class RingSkillCheck : MonoBehaviour
{
    public static RingSkillCheck instance;

    [Header("UI Elements")]
    public RectTransform shrinkingRing; // ǧ��ǹǧ�͡����˴�����
    public RectTransform targetRing;    // ǧ������µç��ҧ

    [Header("Color Settings (����¹�յ���ѧ���)")]
    private Image shrinkingRingImage;
    public Color outOfRangeColor = Color.red;
    public Color inRangeColor = Color.green;
    public Color goodRangeColor = Color.yellow;

    [Header("Settings")]
    public float shrinkDuration = 1.5f; // ���ҷ��ǧ��ǹ��˴����Ҩ��ش (�Թҷ�)
    public float startScale = 3.0f;     // ��Ҵ������鹢ͧǧ��ǹ��ҹ�͡
    public float targetScale = 1.0f;

    [Header("Heal Amount")]
    [SerializeField] private float perfectHealAmount = 12f; // เลือดที่ได้คืนตอนกด Perfect (เดิม hardcode 10)
    [SerializeField] private float goodHealAmount = 5f; // เลือดที่ได้คืนตอนกด Good (ใกล้ Perfect แต่ไม่ทันเป๊ะ)

    [Header("Good Zone (ก่อนถึงช่วง Perfect)")]
    [SerializeField] private float goodZoneStart = 0.5f; // progress ตั้งแต่ค่านี้ถึงก่อน 0.75 นับเป็น Good

    private float currentTimer = 0f;
    private bool isChecking = false;
    private GameObject pendingTarget; // lipid ที่รอถูกกำจัดจริงตอนวงบีบปิด
    private float targetBeat; // บีทเป้าหมายที่ล็อกไว้ตอนวงเปิด (กันไม่ให้ผลวนตามจังหวะเพลงซ้ำๆ)

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

            // ������¹�� ���� 0.85 �繨ش��ҧ (��ǧ�������Ѻ��� 0.75 �֧ 0.95)
            if (shrinkingRingImage != null)
            {
                SkillCheckGrade liveGrade = GetCurrentGrade(progress);
                shrinkingRingImage.color = liveGrade == SkillCheckGrade.Perfect ? inRangeColor // เขียว = Perfect
                    : liveGrade == SkillCheckGrade.Good ? goodRangeColor // เหลือง = Good
                    : outOfRangeColor; // แดง = Miss
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

    public void StartRingCheck(GameObject target = null)
    {
        pendingTarget = target;
        isChecking = true;
        currentTimer = 0f;

        if (BeatManager.instance != null)
        {
            // เล็งบีทเป้าหมายให้ตรงกับจังหวะที่วงจะหดสุดพอดี (ตามระยะเวลา shrinkDuration)
            float beatsAhead = shrinkDuration / BeatManager.instance.BeatDuration;
            targetBeat = Mathf.Round(BeatManager.instance.songPositionInBeats + beatsAhead);
        }

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

        // ��ͤ������¤�������Ӫ�ǧ 0.75 �֧ 0.95 ���ըش�դ�ç 0.85
        SkillCheckGrade grade = GetCurrentGrade(progress);

        if (PlayerController.instance != null)
        {
            PlayerController.instance.RegisterSkillCheckResult(grade);
        }

        if (grade == SkillCheckGrade.Perfect)
        {
            Debug.Log("PERFECT! ���Ѻ���ʹ�׹ +10 HP");

            if (HealthBarUI.instance != null)
            {
                HealthBarUI.instance.AddHealth(perfectHealAmount);
            }
        }
        else if (grade == SkillCheckGrade.Good)
        {
            Debug.Log("GOOD! กดเร็วไปนิด แต่ยังพอได้เลือดคืน");

            if (HealthBarUI.instance != null)
            {
                HealthBarUI.instance.AddHealth(goodHealAmount);
            }
        }
        else
        {
            Debug.Log("MISS! ���Դ�ѧ��� ��������ʹ");
        }

        CloseSkillCheck();
    }

    void FailSkillCheck()
    {
        if (PlayerController.instance != null)
        {
            PlayerController.instance.RegisterSkillCheckResult(SkillCheckGrade.Miss);
        }

        Debug.Log("MISS! �������");
        CloseSkillCheck();
    }

    void CloseSkillCheck()
    {
        isChecking = false;
        SetVisible(false);

        // ตอนนี้ถือว่ากำจัด lipid จริงๆ แล้ว (ไม่ว่าผล skill check จะ Perfect หรือ Miss)
        if (pendingTarget != null)
        {
            Destroy(pendingTarget);
            pendingTarget = null;
        }
    }

    // ถ้ามี BeatManager (ใส่เพลงแล้ว) ตัดสินจากความใกล้จังหวะเพลงจริงแทน progress ของวงแหวน
    SkillCheckGrade GetCurrentGrade(float progress)
    {
        if (BeatManager.instance != null)
        {
            BeatGrade beatGrade = BeatManager.instance.GradeAgainstTarget(targetBeat);
            return beatGrade == BeatGrade.Perfect ? SkillCheckGrade.Perfect
                : beatGrade == BeatGrade.Good ? SkillCheckGrade.Good
                : SkillCheckGrade.Miss;
        }

        bool isPerfect = progress >= 0.75f && progress <= 0.95f;
        bool isGood = !isPerfect && progress >= goodZoneStart && progress < 0.75f;
        return isPerfect ? SkillCheckGrade.Perfect : (isGood ? SkillCheckGrade.Good : SkillCheckGrade.Miss);
    }

    void SetVisible(bool isVisible)
    {
        if (shrinkingRing != null) shrinkingRing.gameObject.SetActive(isVisible);
        if (targetRing != null) targetRing.gameObject.SetActive(isVisible);
    }
}