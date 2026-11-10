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

    [Header("Death Burst")]
    [SerializeField, Min(4)] private int burstPieceCount = 10;
    [SerializeField, Min(0.05f)] private float burstLifetime = 0.8f;
    [SerializeField, Min(0f)] private float burstSpeed = 6f;

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
    private bool hadQTESkip = false;
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

    public void RegisterSkippedChartNote()
    {
        spawnedNotes++;
        finishedNotes++;
        FinishWaveIfComplete();
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

        FinishWaveIfComplete();
    }

    public void OnMiniDiscardedForQTE()
    {
        finishedNotes++;
        hadQTESkip = true;
        FinishWaveIfComplete();
    }

    void FinishWaveIfComplete()
    {
        if (!waveEnded && AllNotesSpawned && finishedNotes >= totalNotes)
        {
            waveEnded = true;
            if (PlayerController.instance != null) PlayerController.instance.OnWaveEnd(hadMiss || hadQTESkip);
            StartCoroutine(DeathBurst());
        }
    }

    IEnumerator DeathBurst()
    {
        // Briefly swell, then collapse the parent before releasing visual-only pieces.
        Vector3 originalScale = transform.localScale;
        float pulseDuration = 0.16f;
        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pulseDuration);
            float swell = t < 0.45f ? Mathf.SmoothStep(1f, 1.18f, t / 0.45f) : Mathf.SmoothStep(1.18f, 0.05f, (t - 0.45f) / 0.55f);
            transform.localScale = originalScale * swell;
            yield return null;
        }

        transform.localScale = Vector3.zero;
        SpawnBurstPieces();
        // Keep this coroutine host alive until the fragment animations finish.
        // Destroying it here would stop every AnimateBurstPiece coroutine.
        yield return new WaitForSeconds(burstLifetime);
        Destroy(gameObject);
    }

    void SpawnBurstPieces()
    {
        if (miniLipidPrefab == null) return;

        for (int i = 0; i < burstPieceCount; i++)
        {
            // Spread fragments over the full circle with a little randomness, like a pop.
            float angle = (360f / burstPieceCount) * i + Random.Range(-22f, 22f);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            Vector3 origin = transform.position + (Vector3)(direction * Random.Range(0.02f, 0.12f));
            GameObject piece = Instantiate(miniLipidPrefab, origin, Quaternion.identity);

            // Keep only the existing lipid sprite. Burst pieces must not register or behave as notes.
            MiniLipid note = piece.GetComponent<MiniLipid>();
            if (note != null)
            {
                note.enabled = false;
                Destroy(note);
            }
            foreach (var canvas in piece.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
            foreach (var collider in piece.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;

            piece.transform.localScale *= Random.Range(1.4f, 1.9f);
            var sprite = piece.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                sprite.sortingOrder = 20;
                sprite.color = new Color(1f, 0.82f, 0.12f, 1f);
            }

            StartCoroutine(AnimateBurstPiece(piece.transform, sprite, direction));
        }
    }

    IEnumerator AnimateBurstPiece(Transform piece, SpriteRenderer sprite, Vector2 direction)
    {
        Vector3 startScale = piece.localScale;
        Vector3 velocity = (Vector3)(direction * burstSpeed * Random.Range(0.8f, 1.35f));
        float spin = Random.Range(-720f, 720f);
        float elapsed = 0f;

        while (elapsed < burstLifetime && piece != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / burstLifetime);
            piece.position += velocity * Time.deltaTime;
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 0.9f);
            piece.Rotate(0f, 0f, spin * Time.deltaTime);
            piece.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (sprite != null)
            {
                Color color = sprite.color;
                color.a = 1f - t;
                sprite.color = color;
            }
            yield return null;
        }

        if (piece != null) Destroy(piece.gameObject);
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
