using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossMashManager : MonoBehaviour
{
    public static BossMashManager instance;

    [Header("UI Reference")]
    [SerializeField] private GameObject mashUIPanel; // พาเนลปุ่มกดรัว
    [SerializeField] private TextMeshProUGUI infoText; // Text แสดงจำนวนครั้งที่กด

    [Header("Boss Animation UI Reference")]
    [SerializeField] private GameObject bossUIObject; // ลาก GameObject "BossUI" ที่ทำแอนิเมชันมาใส่ช่องนี้

    [Header("Score Display Reference")]
    [SerializeField] private SpriteNumber scoreSpriteNumber; // คอมโพเนนต์ SpriteNumber ของคะแนน
    [SerializeField] private RectTransform scoreRectTransform; // ลาก GameObject "Score" มาใส่
    [SerializeField] private Vector2 bossPositionCenter = new Vector2(0f, 0f); // พิกัด Score ตรงกลางจอตอนสู้บอส

    private Vector2 originalScorePosition;
    private bool hasSavedPosition = false;

    [Header("Visual Feedback Settings")]
    [SerializeField] private Image keyABackground; // พื้นหลังปุ่ม A
    [SerializeField] private Image keyDBackground; // พื้นหลังปุ่ม D
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hitColor = Color.yellow;

    [Header("Settings")]
    [SerializeField] private int scorePerPress = 50; // คะแนนโบนัสต่อการกด 1 ครั้ง

    private bool isMashActive = false;
    private int totalMashCount = 0;
    private float flashTimerA = 0f;
    private float flashTimerD = 0f;

    void Awake()
    {
        instance = this;
        if (mashUIPanel != null) mashUIPanel.SetActive(false);
        if (bossUIObject != null) bossUIObject.SetActive(false); // ซ่อนแอนิเมชันบอสไว้ตอนเริ่มเกม

        if (scoreRectTransform != null)
        {
            originalScorePosition = scoreRectTransform.anchoredPosition;
            hasSavedPosition = true;
        }
    }

    public void StartMash(float duration)
    {
        isMashActive = true;
        totalMashCount = 0;

        // สั่งเปิดพาเนลปุ่มกดรัวและแอนิเมชันบอส
        if (mashUIPanel != null) mashUIPanel.SetActive(true);
        if (bossUIObject != null) bossUIObject.SetActive(true);

        // ย้าย Score มาตรงกลางจอตอนเข้าสู่ช่วงบอส
        if (scoreRectTransform != null)
        {
            scoreRectTransform.anchoredPosition = bossPositionCenter;
        }

        UpdateUI();

        // กำหนดเวลาช่วงรัวปุ่ม (เช่น 6 วินาที)
        Invoke(nameof(EndMash), duration);
    }

    void Update()
    {
        if (!isMashActive || PlayerController.isGameOver) return;

        bool pressed = false;

        // เมื่อกดปุ่ม A หรือ D
        if (Input.GetKeyDown(KeyCode.A))
        {
            totalMashCount++;
            pressed = true;
            TriggerVisualA();
            AddScore();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            totalMashCount++;
            pressed = true;
            TriggerVisualD();
            AddScore();
        }

        if (pressed)
        {
            UpdateUI();
        }

        HandleVisualFlash();
    }

    void AddScore()
    {
        if (PlayerController.instance != null)
        {
            PlayerController.instance.score += scorePerPress;

            if (scoreSpriteNumber != null)
            {
                scoreSpriteNumber.SetValue(PlayerController.instance.score);
            }
        }
    }

    void TriggerVisualA()
    {
        flashTimerA = 0.1f;
        if (keyABackground != null) keyABackground.color = hitColor;
    }

    void TriggerVisualD()
    {
        flashTimerD = 0.1f;
        if (keyDBackground != null) keyDBackground.color = hitColor;
    }

    void HandleVisualFlash()
    {
        if (flashTimerA > 0)
        {
            flashTimerA -= Time.deltaTime;
            if (flashTimerA <= 0 && keyABackground != null) keyABackground.color = normalColor;
        }

        if (flashTimerD > 0)
        {
            flashTimerD -= Time.deltaTime;
            if (flashTimerD <= 0 && keyDBackground != null) keyDBackground.color = normalColor;
        }
    }

    void UpdateUI()
    {
        if (infoText != null)
        {
            infoText.text = $"MASH A & D!\nHits: {totalMashCount}";
        }
    }

    void EndMash()
    {
        isMashActive = false;

        // ปิดพาเนลปุ่มและแอนิเมชันบอสเมื่อหมดเวลาช่วงบอส
        if (mashUIPanel != null) mashUIPanel.SetActive(false);
        if (bossUIObject != null) bossUIObject.SetActive(false);

        // คืนตำแหน่ง Score กลับสู่ที่เดิม
        if (scoreRectTransform != null && hasSavedPosition)
        {
            scoreRectTransform.anchoredPosition = originalScorePosition;
        }

        if (LevelManager.instance != null)
        {
            LevelManager.instance.FinishBossPhase();
        }
    }
}