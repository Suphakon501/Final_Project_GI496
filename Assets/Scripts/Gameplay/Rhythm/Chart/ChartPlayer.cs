using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-200)]
public class ChartPlayer : MonoBehaviour
{
    [Header("Chart (empty = random mode)")]
    public SongChart chart;
    public GameObject lipidPrefab;

    [Header("Row Heights")]
    [SerializeField] private float topRowY = 1.5f;
    [SerializeField] private float middleRowY = 0f;
    [SerializeField] private float bottomRowY = -1.5f;

    [Header("Enemy Lead-In (seconds)")]
    [SerializeField] private float enemyLeadIn = 0.3f;

    [Header("Recording")]
    public bool recordMode = false;
    [SerializeField] private float recordSnapBeats = 0.5f;
    [SerializeField] private bool clearChartBeforeRecording = true;

    [Header("Progress Bar")]
    [SerializeField] private ProgressTracker progressTracker;

    [Header("Song finished")]
    public UnityEvent onChartFinished;

    private struct Wave { public int first; public int count; public float enemyTime; public LipidMovement enemy; }

    private BeatManager beat;
    private readonly List<ChartNote> notes = new List<ChartNote>();
    private readonly List<Wave> waves = new List<Wave>();
    private int noteWaveIndex = 0;
    private int nextNote = 0;
    private int nextWave = 0;
    private float travelTime;
    private float walkTime;
    private GameObject miniPrefab;
    private float stopX;
    private bool finished = false;
    private bool musicStarted = false;
    private float lastPlayingTime;
    private const float SongEndTolerance = 1f;

    bool Active => chart != null;

    void Awake()
    {
        if (!Active) return;

        beat = FindFirstObjectByType<BeatManager>();
        if (beat == null)
        {
            Debug.LogError("[ChartPlayer] BeatManager not found in scene");
            enabled = false;
            return;
        }

        beat.bpm = chart.bpm;
        beat.songOffsetSeconds = chart.offsetSeconds;
        if (chart.music != null && beat.musicSource != null)
        {
            beat.musicSource.clip = chart.music;
            beat.musicSource.loop = false;
        }

        if (progressTracker != null) progressTracker.SetExternalControl();

        foreach (var spawner in FindObjectsByType<LipidSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;

        if (recordMode)
        {
            if (clearChartBeforeRecording) chart.notes.Clear();
            Debug.Log("[ChartPlayer] Recording: press W/A/S/D with the music, stop Play to save");
        }
    }

    void Start()
    {
        if (!Active || recordMode || !enabled) return;

        var lipid = lipidPrefab != null ? lipidPrefab.GetComponent<LipidMovement>() : null;
        if (lipid == null || lipid.miniLipidPrefab == null)
        {
            Debug.LogError("[ChartPlayer] Lipid Prefab must be a LipidEnemy with a Mini Lipid Prefab");
            enabled = false;
            return;
        }

        var mini = lipid.miniLipidPrefab.GetComponent<MiniLipid>();
        miniPrefab = lipid.miniLipidPrefab;
        stopX = lipid.stopPosX;
        travelTime = (lipid.stopPosX - mini.hitLineX) / Mathf.Max(0.01f, mini.MoveSpeed);
        walkTime = lipid.WalkInDuration;

        notes.AddRange(chart.notes);
        notes.Sort((a, b) => a.beat.CompareTo(b.beat));
        BuildWaves();

        Debug.Log($"[ChartPlayer] Playing '{chart.name}': {notes.Count} notes, {waves.Count} waves");
    }

    void BuildWaves()
    {
        float beatDur = chart.BeatDuration;
        int start = 0;
        for (int i = 1; i <= notes.Count; i++)
        {
            bool split = i == notes.Count || notes[i].beat - notes[i - 1].beat > chart.waveGapBeats;
            if (!split) continue;

            float firstSpawn = notes[start].beat * beatDur - travelTime;
            waves.Add(new Wave
            {
                first = start,
                count = i - start,
                enemyTime = firstSpawn - walkTime - enemyLeadIn,
            });
            start = i;
        }
    }

    void Update()
    {
        if (!Active || PlayerController.isGameOver || finished) return;

        if (beat.musicSource != null && beat.musicSource.isPlaying)
        {
            musicStarted = true;
            lastPlayingTime = beat.musicSource.time;
        }
        float t = beat.songPositionInSeconds;

        if (progressTracker != null && chart.music != null)
            progressTracker.SetProgress(Mathf.Clamp01((t + chart.offsetSeconds) / chart.music.length));

        if (recordMode)
        {
            RecordInput();
            CheckSongEnded(true);
            return;
        }

        while (nextWave < waves.Count && t >= waves[nextWave].enemyTime)
        {
            var w = waves[nextWave];
            var go = Instantiate(lipidPrefab);
            w.enemy = go.GetComponent<LipidMovement>();
            w.enemy.BeginChartWave(w.count);
            if (t > w.enemyTime + 0.05f) w.enemy.SnapToStop();
            waves[nextWave] = w;
            nextWave++;
        }

        float beatDur = chart.BeatDuration;
        while (nextNote < notes.Count)
        {
            var n = notes[nextNote];
            float arriveAt = n.beat * beatDur;
            if (t < arriveAt - travelTime) break;

            while (noteWaveIndex < waves.Count - 1 && nextNote >= waves[noteWaveIndex].first + waves[noteWaveIndex].count) noteWaveIndex++;
            var w = waves[noteWaveIndex];
            if (w.enemy == null) break;

            SpawnNote(n, w.enemy, arriveAt);
            nextNote++;
        }

        CheckSongEnded(false);
    }

    void SpawnNote(ChartNote n, LipidMovement owner, float arriveAt)
    {
        float y = n.row == NoteRow.Top ? topRowY : n.row == NoteRow.Bottom ? bottomRowY : middleRowY;
        var go = Instantiate(miniPrefab, new Vector3(stopX, y, 0f), Quaternion.identity);
        owner.RegisterSpawnedNote();

        var mini = go.GetComponent<MiniLipid>();
        if (mini == null) return;
        mini.InitializeMini(n.KeyCode, owner);
        mini.SetTargetSongTime(arriveAt);
    }

    void CheckSongEnded(bool recording)
    {
        bool musicDone = musicStarted && beat.musicSource != null && !beat.musicSource.isPlaying
                      && beat.musicSource.clip != null && lastPlayingTime >= beat.musicSource.clip.length - SongEndTolerance;
        bool notesDone = recording || (nextNote >= notes.Count && MiniLipid.Active.Count == 0);
        if (!musicDone || !notesDone) return;

        finished = true;
        Debug.Log("[ChartPlayer] Song finished");
        onChartFinished?.Invoke();
    }

    void RecordInput()
    {
        NoteKey? pressed = null;
        if (Input.GetKeyDown(KeyCode.W)) pressed = NoteKey.W;
        else if (Input.GetKeyDown(KeyCode.A)) pressed = NoteKey.A;
        else if (Input.GetKeyDown(KeyCode.S)) pressed = NoteKey.S;
        else if (Input.GetKeyDown(KeyCode.D)) pressed = NoteKey.D;
        if (pressed == null) return;

        float snap = Mathf.Max(0.0625f, recordSnapBeats);
        float snapped = Mathf.Round(beat.songPositionInBeats / snap) * snap;
        if (snapped < 0f) return;

        chart.notes.RemoveAll(x => Mathf.Approximately(x.beat, snapped));
        chart.notes.Add(new ChartNote { beat = snapped, key = pressed.Value, row = ChartNote.DefaultRowFor(pressed.Value) });
        Debug.Log($"[ChartPlayer] Recorded beat {snapped:0.##} {pressed.Value} ({chart.notes.Count} notes)");
    }

#if UNITY_EDITOR
    void OnDisable()
    {
        if (!Active || !recordMode) return;
        chart.SortNotes();
        UnityEditor.EditorUtility.SetDirty(chart);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[ChartPlayer] Saved {chart.notes.Count} notes to '{chart.name}'");
    }
#endif
}
