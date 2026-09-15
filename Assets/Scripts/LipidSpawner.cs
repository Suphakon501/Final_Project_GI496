using UnityEngine;

public class LipidSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject lipidPrefab;

    [Header("Spawn Position")]
    [SerializeField] private float customSpawnPosX = 10f;
    [SerializeField] private float customSpawnPosY = 0f;

    [Header("Spawn Timing (วินาที)")]
    [SerializeField] private float startDelaySeconds = 3.0f; 
    [SerializeField] private float spawnIntervalSeconds = 2.0f; 

    [Header("Difficulty Progression (ตามจำนวนตัวที่เกิด)")]
    [SerializeField] private int countFor4Keys = 5;  
    [SerializeField] private int countFor6Keys = 15; 

    private float timer = 0f;
    private bool hasStarted = false;
    private int totalSpawnedCount = 0;

    void Update()
    {
        if (PlayerController.isGameOver) return;
        if (lipidPrefab == null) return;

        if (!hasStarted)
        {
            timer += Time.deltaTime;
            if (timer >= startDelaySeconds)
            {
                hasStarted = true;
                timer = spawnIntervalSeconds; 
            }
            return;
        }

        // 2. จับเวลาเกิดตัวถัดไปเรื่อยๆ
        timer += Time.deltaTime;
        if (timer >= spawnIntervalSeconds)
        {
            timer = 0f;
            SpawnLipid();
        }
    }

    void SpawnLipid()
    {
        Vector3 spawnPos = new Vector3(customSpawnPosX, customSpawnPosY, 0f);

        GameObject newLipid = Instantiate(lipidPrefab, spawnPos, Quaternion.identity);
        LipidMovement lipidMovement = newLipid.GetComponent<LipidMovement>();

        if (lipidMovement != null)
        {
            lipidMovement.spawnPosX = customSpawnPosX;
            lipidMovement.spawnPosY = customSpawnPosY;

            totalSpawnedCount++;

            if (totalSpawnedCount >= countFor6Keys)
            {
                lipidMovement.sequenceLength = 6;
            }
            else if (totalSpawnedCount >= countFor4Keys)
            {
                lipidMovement.sequenceLength = 4;
            }
            else
            {
                lipidMovement.sequenceLength = 2;
            }

            lipidMovement.InitializeSequence();
        }
    }
}