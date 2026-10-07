using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LipidMovement : MonoBehaviour
{
    private static readonly List<LipidMovement> active = new List<LipidMovement>();
    public static IReadOnlyList<LipidMovement> Active => active;

    private static readonly KeyCode[] RandomKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    private static readonly float[] RandomRowsY = { -1.5f, 0f, 1.5f };

    private static float globalGameTimer = 0f;
    public static void ResetDifficultyTimer() => globalGameTimer = 0f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.0f;
    public float spawnPosX = 10f;
    public float stopPosX = 6f;
    public float stopPosY = 0f;

    [Header("Mini Lipid Settings")]
    public GameObject miniLipidPrefab;
    [SerializeField] private float spawnInterval = 0.4f;

    [Header("Beat Sync")]
    [SerializeField] private bool syncToBeat = true;
    [SerializeField] private float beatsBetweenNotes = 1f;
    [SerializeField] private int waveStartsOnBeatMultiple = 1;

    [Header("Progressive Difficulty")]
    [SerializeField] private int baseSequenceLength = 2;
    [SerializeField] private int maxSequenceLength = 6;
    private const float SecondsPerExtraNote = 20f;

    private bool hasStopped = false;
    private bool chartControlled = false;
    private bool snappedToStop = false;

    private int totalNotes = 0;
    private int spawnedNotes = 0;
    private int finishedNotes = 0;
    private bool hadMiss = false;
    private bool waveEnded = false;

    public float MoveSpeed => moveSpeed;
    public float WalkInDuration => Mathf.Abs(spawnPosX - stopPosX) / Mathf.Max(0.01f, moveSpeed);
    public bool AllNotesSpawned => totalNotes > 0 && spawnedNotes >= totalNotes;
    public bool IsLastPending => AllNotesSpawned && totalNotes - finishedNotes == 1;

    void OnEnable() => active.Add(this);
    void OnDisable() => active.Remove(this);

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

        Vector3 stopPos = new Vector3(stopPosX, stopPosY, 0f);
        transform.position = Vector3.MoveTowards(transform.position, stopPos, moveSpeed * Time.deltaTime);

        if (Mathf.Abs(transform.position.x - stopPosX) < 0.05f)
        {
            transform.position = stopPos;
            hasStopped = true;

            HideFinishedLipids();
            if (PlayerController.instance != null) PlayerController.instance.OnWaveStart();
            if (!chartControlled) StartCoroutine(ReleaseRandomNotes());
        }
    }

    public void BeginChartWave(int noteCount)
    {
        chartControlled = true;
        totalNotes = noteCount;
    }

    public void SnapToStop()
    {
        snappedToStop = true;
        transform.position = new Vector3(stopPosX, stopPosY, 0f);
    }

    public void RegisterSpawnedNote()
    {
        spawnedNotes++;
    }

    IEnumerator ReleaseRandomNotes()
    {
        if (miniLipidPrefab == null) yield break;

        int length = baseSequenceLength + Mathf.FloorToInt(globalGameTimer / SecondsPerExtraNote);
        totalNotes = Mathf.Clamp(length, 1, Mathf.Max(1, maxSequenceLength));

        var beat = BeatManager.instance;
        var template = miniLipidPrefab.GetComponent<MiniLipid>();

        if (syncToBeat && beat != null && template != null && template.MoveSpeed > 0f)
        {
            float travelTime = (stopPosX - template.hitLineX) / template.MoveSpeed;
            float step = beat.BeatDuration * Mathf.Max(0.125f, beatsBetweenNotes);
            float startGrid = beat.BeatDuration * Mathf.Max(1, waveStartsOnBeatMultiple);
            float firstArrival = Mathf.Ceil((beat.songPositionInSeconds + travelTime) / startGrid) * startGrid;

            for (int i = 0; i < totalNotes; i++)
            {
                float arriveAt = firstArrival + i * step;
                while (beat.songPositionInSeconds < arriveAt - travelTime)
                {
                    if (PlayerController.isGameOver) yield break;
                    yield return null;
                }

                var mini = SpawnRandomNote();
                if (mini != null) mini.SetTargetSongTime(arriveAt);
            }
        }
        else
        {
            for (int i = 0; i < totalNotes; i++)
            {
                SpawnRandomNote();
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    MiniLipid SpawnRandomNote()
    {
        KeyCode key = RandomKeys[Random.Range(0, RandomKeys.Length)];
        float y = RandomRowsY[Random.Range(0, RandomRowsY.Length)];

        GameObject obj = Instantiate(miniLipidPrefab, new Vector3(stopPosX, y, 0f), Quaternion.identity);
        RegisterSpawnedNote();

        MiniLipid mini = obj.GetComponent<MiniLipid>();
        if (mini != null) mini.InitializeMini(key, this);
        return mini;
    }

    public void OnMiniFinished(HitResult result)
    {
        finishedNotes++;
        if (result == HitResult.Miss) hadMiss = true;

        if (!waveEnded && AllNotesSpawned && finishedNotes >= totalNotes)
        {
            waveEnded = true;
            if (PlayerController.instance != null) PlayerController.instance.OnWaveEnd(hadMiss);
            Destroy(gameObject);
        }
    }

    void HideFinishedLipids()
    {
        foreach (var other in active)
        {
            if (other == this || !other.AllNotesSpawned) continue;
            foreach (var r in other.GetComponentsInChildren<Renderer>()) r.enabled = false;
        }
    }
}
