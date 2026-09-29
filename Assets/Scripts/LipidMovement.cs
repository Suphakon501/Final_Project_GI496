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
    [SerializeField] private float spawnInterval = 0.4f; // ระยะเวลาในการหน่วงเวลาก่อนปล่อยตัวถัดไป (วินาที) - ใช้ตอนปิด Sync To Beat

    [Header("Beat Sync (โน้ตวิ่งถึงเส้นตรงบีทเพลงพอดี)")]
    [SerializeField] private bool syncToBeat = true;
    [SerializeField] private float beatsBetweenNotes = 1f; // 1 = ทุกบีท, 0.5 = ครึ่งบีท (โหด), 2 = ทุก 2 บีท
    [SerializeField] private int waveStartsOnBeatMultiple = 1; // 1 = โน้ตแรกลงบีทไหนก็ได้, 4 = ลงต้นห้องเพลงเสมอ (ฟังเป็นเพลงขึ้น แต่รอนานขึ้นนิด)

    [Header("Progressive Difficulty (Difficulty Scaling)")]
    private static float globalGameTimer = 0f;
    [SerializeField] private int baseSequenceLength = 2;  // จำนวนโน้ตต่อไขมัน 1 ตัวตอนเริ่มเกม (เพิ่มขึ้น 1 ทุก 20 วิ)
    [SerializeField] private int maxSequenceLength = 6;   // จำนวนโน้ตสูงสุดต่อไขมัน 1 ตัว

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

            if (PlayerController.instance != null) PlayerController.instance.OnWaveStart();
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
        int finalLength = Mathf.Clamp(calculatedLength, 1, Mathf.Max(1, maxSequenceLength));

        var beat = BeatManager.instance;
        var miniTemplate = miniLipidPrefab.GetComponent<MiniLipid>();

        if (syncToBeat && beat != null && miniTemplate != null && miniTemplate.MoveSpeed > 0f)
        {
            // วางตารางให้โน้ตแต่ละตัว "ถึงเส้น" ตรงบีท แล้วถอยเวลากลับมาว่าต้องปล่อยตอนไหน
            float speed = miniTemplate.MoveSpeed;
            float travelTime = (stopPosX - miniTemplate.hitLineX) / speed;
            float step = beat.BeatDuration * Mathf.Max(0.125f, beatsBetweenNotes);

            float earliestArrival = beat.songPositionInSeconds + travelTime;
            float startGrid = beat.BeatDuration * Mathf.Max(1, waveStartsOnBeatMultiple);
            float firstArrival = Mathf.Ceil(earliestArrival / startGrid) * startGrid; // บีท (หรือต้นห้อง) แรกที่ยังทันไปถึง

            for (int i = 0; i < finalLength; i++)
            {
                float arriveAt = firstArrival + i * step;
                float spawnAt = arriveAt - travelTime;
                while (beat.songPositionInSeconds < spawnAt)
                {
                    if (PlayerController.isGameOver) yield break;
                    yield return null;
                }

                // โน้ตคำนวณตำแหน่งจากเวลาเพลงเองทุกเฟรม เลยถึงเส้นตรงบีทเป๊ะไม่ว่าเฟรมจะกระตุกแค่ไหน
                var mini = SpawnMini(i, finalLength, stopPosX);
                if (mini != null) mini.SetTargetSongTime(arriveAt);
            }
            yield break;
        }

        // ไม่มี BeatManager หรือปิด Sync To Beat: ปล่อยตามเวลาแบบเดิม
        for (int i = 0; i < finalLength; i++)
        {
            SpawnMini(i, finalLength, stopPosX);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    MiniLipid SpawnMini(int index, int total, float x)
    {
        KeyCode[] possibleKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        float[] allowedYPositions = { -1.5f, 0f, 1.5f };

        KeyCode assignedKey = possibleKeys[Random.Range(0, possibleKeys.Length)];
        float randomY = allowedYPositions[Random.Range(0, allowedYPositions.Length)];

        GameObject miniObj = Instantiate(miniLipidPrefab, new Vector3(x, randomY, 0f), Quaternion.identity);

        MiniLipid miniScript = miniObj.GetComponent<MiniLipid>();
        if (miniScript != null)
        {
            miniScript.InitializeMini(assignedKey, this, index, total);
        }
        return miniScript;
    }

    public void OnAllMinisCleared()
    {
        if (PlayerController.instance != null) PlayerController.instance.OnWaveEnd();
        Destroy(gameObject);
    }
}