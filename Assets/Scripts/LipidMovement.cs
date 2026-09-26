using System.Collections;
using UnityEngine;

public class LipidMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.0f;
    public float spawnPosX = 10f;
    public float stopPosX = 6f;
    public float stopPosY = 0f;

    [Header("Mini Lipid Settings")]
    public GameObject miniLipidPrefab;
    [SerializeField] private float spawnInterval = 0.4f; // ระยะเวลาในการหน่วงเวลาก่อนปล่อยตัวถัดไป (วินาที)

    [Header("Progressive Difficulty (Difficulty Scaling)")]
    private static float globalGameTimer = 0f;
    [SerializeField] private int baseSequenceLength = 2;

    private bool hasStopped = false;

    void Start()
    {
        transform.position = new Vector3(spawnPosX, stopPosY, 0f);
        hasStopped = false;
    }

    void Update()
    {
        if (PlayerController.isGameOver) return;

        globalGameTimer += Time.deltaTime;

        if (hasStopped) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            new Vector3(stopPosX, stopPosY, 0f),
            moveSpeed * Time.deltaTime
        );

        if (Mathf.Abs(transform.position.x - stopPosX) < 0.05f)
        {
            transform.position = new Vector3(stopPosX, stopPosY, 0f);
            hasStopped = true;

            StartCoroutine(ReleaseMiniLipidsRoutine());
        }
    }

    IEnumerator ReleaseMiniLipidsRoutine()
    {
        if (miniLipidPrefab == null)
        {
            yield break;
        }

        int calculatedLength = baseSequenceLength + Mathf.FloorToInt(globalGameTimer / 20f);
        int finalLength = Mathf.Clamp(calculatedLength, 2, 6); // จำกัดสูงสุดไม่เกิน 6 ตัว

        KeyCode[] possibleKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        float[] allowedYPositions = { -1.5f, 0f, 1.5f };

        for (int i = 0; i < finalLength; i++)
        {
            KeyCode assignedKey = possibleKeys[Random.Range(0, possibleKeys.Length)];
            float randomY = allowedYPositions[Random.Range(0, allowedYPositions.Length)];

            Vector3 spawnPos = new Vector3(stopPosX, randomY, 0f);
            GameObject miniObj = Instantiate(miniLipidPrefab, spawnPos, Quaternion.identity);

            MiniLipid miniScript = miniObj.GetComponent<MiniLipid>();
            if (miniScript != null)
            {
                miniScript.InitializeMini(assignedKey, this, i, finalLength);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void OnAllMinisCleared()
    {
        Destroy(gameObject);
    }
}