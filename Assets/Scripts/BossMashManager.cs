using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossMashManager : MonoBehaviour
{
    public static BossMashManager instance;

    [Header("Boss Prefab & Spawn Settings")]
    [SerializeField] private GameObject bossPrefab;          // ลาก Prefab ของบอสมาใส่ตรงนี้
    [SerializeField] private float spawnStartX = 30f;        // จุดเริ่มต้นทางขวาไกลๆ เพื่อให้พุ่งเข้ามา
    [SerializeField] private float targetX = 13.26f;         // พิกัด X เป้าหมายที่จะหยุด
    [SerializeField] private float fixedY = -0.25f;          // พิกัด Y คงที่ตลอดเวลา
    [SerializeField] private float moveSpeed = 50f;          // ความเร็วพุ่งตัวเท่ากับ 50

    [Header("Particle System (Mash Feedback)")]
    [SerializeField] private GameObject mashParticlePrefab;  // ลาก Prefab พาติเคิลที่จะให้แสดงตอนกด
    [SerializeField] private float particleLifetime = 0.5f;  // เวลาในการทำลายพาติเคิลทิ้ง

    [Header("Visual Feedback Settings")]
    [SerializeField] private Image keyFeedbackBackground;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hitColor = Color.yellow;

    [Header("Settings")]
    [SerializeField] private int scorePerPress = 50;

    private bool isMashActive = false;
    private bool isBossMovingIn = false;
    private int totalMashCount = 0;
    private float flashTimer = 0f;
    private GameObject currentBossInstance;

    void Awake()
    {
        instance = this;
    }

    public void StartMash(float duration)
    {
        isMashActive = true;
        totalMashCount = 0;
        isBossMovingIn = true;

        // 1. เสกบอสที่จุดเริ่มต้นทางขวาไกลๆ เพื่อเตรียมพุ่งเข้ามาด้วยความเร็ว 50
        if (bossPrefab != null)
        {
            Vector3 spawnPos = new Vector3(spawnStartX, fixedY, 0f);
            currentBossInstance = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        }

        // กำหนดเวลาช่วงรัวปุ่ม
        Invoke(nameof(EndMash), duration);
    }

    void Update()
    {
        // ให้บอสพุ่งเข้ามาด้วยความเร็ว 50 จนถึง X = 13.26, Y = -0.25
        if (isBossMovingIn && currentBossInstance != null)
        {
            Vector3 targetPos = new Vector3(targetX, fixedY, currentBossInstance.transform.position.z);
            currentBossInstance.transform.position = Vector3.MoveTowards(
                currentBossInstance.transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            // เมื่อถึงตำแหน่งเป้าหมายให้หยุดขยับ
            if (Mathf.Abs(currentBossInstance.transform.position.x - targetX) < 0.01f)
            {
                isBossMovingIn = false;
            }
        }

        if (!isMashActive || PlayerController.isGameOver) return;

        // กดปุ่มอะไรก็ได้บนคีย์บอร์ดรัวๆ
        if (Input.anyKeyDown)
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                totalMashCount++;
                TriggerVisualFeedback();
                SpawnMashParticleFromBoss(); // พาร์ติเคิลพุ่งออกจากตัวบอส
                AddScore();
            }
        }

        HandleVisualFlash();
    }

    void SpawnMashParticleFromBoss()
    {
        if (mashParticlePrefab == null || currentBossInstance == null) return;

        Vector3 spawnPos = currentBossInstance.transform.position;
        GameObject particleObj = Instantiate(mashParticlePrefab, spawnPos, Quaternion.identity);
        Destroy(particleObj, particleLifetime);
    }

    void AddScore()
    {
        if (PlayerController.instance != null)
        {
            PlayerController.instance.score += scorePerPress;
            // เรียกสั่งอัปเดตหน้าจอคะแนนทันที
            PlayerController.instance.UpdateScoreUI();
        }
    }

    void TriggerVisualFeedback()
    {
        flashTimer = 0.1f;
        if (keyFeedbackBackground != null) keyFeedbackBackground.color = hitColor;
    }

    void HandleVisualFlash()
    {
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0 && keyFeedbackBackground != null) keyFeedbackBackground.color = normalColor;
        }
    }

    void EndMash()
    {
        isMashActive = false;
        isBossMovingIn = false;

        // ลบ Prefab บอสทิ้งเมื่อหมดเวลา
        if (currentBossInstance != null)
        {
            Destroy(currentBossInstance);
        }

        if (LevelManager.instance != null)
        {
            LevelManager.instance.FinishBossPhase();
        }
    }
}