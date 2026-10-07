using UnityEngine;

public class LipidSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject lipidPrefab;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnIntervalSeconds = 5f;

    private float timer = 0f;

    void Update()
    {
        if (PlayerController.isGameOver || lipidPrefab == null) return;
        if (LipidMovement.Active.Count > 0) return;

        timer += Time.deltaTime;
        if (timer >= spawnIntervalSeconds)
        {
            timer = 0f;
            Instantiate(lipidPrefab);
        }
    }
}
