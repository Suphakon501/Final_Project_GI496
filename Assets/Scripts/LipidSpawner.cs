using UnityEngine;

public class LipidSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject lipidPrefab; 

    [Header("Spawn Position")]
    [SerializeField] private float customSpawnPosX = 10f;
    [SerializeField] private float customSpawnPosY = 0f;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnIntervalSeconds = 5f; 

    private float timer = 0f;

    void Update()
    {
        if (PlayerController.isGameOver) return;
        if (lipidPrefab == null) return;

        bool lipidAlive = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None).Length > 0;
        if (lipidAlive) return;

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
        Instantiate(lipidPrefab, spawnPos, Quaternion.identity);
    }
}