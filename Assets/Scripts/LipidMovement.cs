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

    // ไขมันแต่ละตัวนับโน้ตของตัวเอง (ไม่นับทั้งจอ) เพราะโหมดชาร์ตอาจมีไขมัน 2 ตัวบนจอพร้อมกัน
    private int totalNotes = 0;
    private int spawnedNotes = 0;
    private int finishedNotes = 0;
    private bool hadMiss = false;
    private bool waveEnded = false;

    // โหมดชาร์ต: ChartPlayer เป็นคนปล่อยโน้ต ไขมันแค่เดินเข้ามายืน
    private bool chartControlled = false;
    private bool snappedToStop = false;

    public float MoveSpeed => moveSpeed;
    public float WalkInDuration => Mathf.Abs(spawnPosX - stopPosX) / Mathf.Max(0.01f, moveSpeed);
    public bool AllNotesSpawned => totalNotes > 0 && spawnedNotes >= totalNotes;
    public bool IsLastPending => AllNotesSpawned && totalNotes - finishedNotes == 1;

    void Start()
    {
        if (!snappedToStop) transform.position = new Vector3(spawnPosX, stopPosY, 0f);
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

            HideFinishedLipids();
            if (PlayerController.instance != null) PlayerController.instance.OnWaveStart();
            if (!chartControlled) StartCoroutine(ReleaseMiniLipidsRoutine());
        }
    }

    // ---------- โหมดชาร์ต (ChartPlayer เรียก) ----------

    public void BeginChartWave(int noteCount)
    {
        chartControlled = true;
        totalNotes = noteCount;
    }

    // มาช้าเกินกว่าจะเดินเข้าทัน (เช่นโน้ตชุดแรกต้นเพลง) ให้โผล่ที่จุดหยุดเลย
    public void SnapToStop()
    {
        snappedToStop = true;
        transform.position = new Vector3(stopPosX, stopPosY, 0f);
    }

    public void RegisterSpawnedNote()
    {
        spawnedNotes++;
    }

    // ---------- โหมดสุ่ม (แบบเดิม) ----------

    IEnumerator ReleaseMiniLipidsRoutine()
    {
        if (miniLipidPrefab == null)
        {
            yield break;
        }

        int calculatedLength = baseSequenceLength + Mathf.FloorToInt(globalGameTimer / 20f);
        int finalLength = Mathf.Clamp(calculatedLength, 1, Mathf.Max(1, maxSequenceLength));
        totalNotes = finalLength;

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
        RegisterSpawnedNote();

        MiniLipid miniScript = miniObj.GetComponent<MiniLipid>();
        if (miniScript != null)
        {
            miniScript.InitializeMini(assignedKey, this, index, total);
        }
        return miniScript;
    }

    // ---------- จบคลื่น ----------

    // MiniLipid เรียกทุกครั้งที่โน้ตของไขมันตัวนี้หายไป (กดโดน / กดผิด / ปล่อยผ่าน)
    public void OnMiniFinished(string result)
    {
        finishedNotes++;
        if (result == "MISS") hadMiss = true;

        if (!waveEnded && AllNotesSpawned && finishedNotes >= totalNotes)
        {
            waveEnded = true;
            if (PlayerController.instance != null) PlayerController.instance.OnWaveEnd(hadMiss);
            Destroy(gameObject);
        }
    }

    // ไขมันตัวใหม่มาถึงแล้ว: ซ่อนตัวเก่าที่ปล่อยโน้ตครบแล้ว (โน้ตของมันยังวิ่งต่อและนับผลได้ปกติ) จะได้ไม่ยืนซ้อนกัน
    void HideFinishedLipids()
    {
        foreach (var other in Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None))
        {
            if (other == this || !other.AllNotesSpawned) continue;
            foreach (var r in other.GetComponentsInChildren<Renderer>()) r.enabled = false;
        }
    }
}
