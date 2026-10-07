using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// เล่นเพลงตามชาร์ต (SongChart): โน้ตออกมาที่บีท / ปุ่ม / แถวเดิมทุกครั้ง
// ใส่ชาร์ตแล้ว LipidSpawner (โหมดสุ่ม) จะถูกปิดเอง / ไม่ใส่ชาร์ต = เกมสุ่มแบบเดิม
//
// โหมดอัด (Record Mode): ติ๊กแล้วกด Play → เพลงเล่น → กด W/A/S/D ตามจังหวะที่อยากให้มีโน้ต
// → กดหยุด Play โน้ตจะถูกบันทึกลงไฟล์ชาร์ต แล้วค่อยแก้ทีละตัวใน Inspector
[DefaultExecutionOrder(-200)] // ตั้งเพลง/BPM ให้ BeatManager ก่อนมันเริ่มเล่นเพลง
public class ChartPlayer : MonoBehaviour
{
    [Header("ชาร์ต (ปล่อยว่าง = โหมดสุ่มแบบเดิม)")]
    public SongChart chart;
    [Tooltip("prefab ไขมันตัวใหญ่ (LipidEnemy)")]
    public GameObject lipidPrefab;

    [Header("ความสูงของแถว (บน / กลาง / ล่าง)")]
    [SerializeField] private float topRowY = 1.5f;
    [SerializeField] private float middleRowY = 0f;
    [SerializeField] private float bottomRowY = -1.5f;

    [Header("ไขมันเดินเข้ามาก่อนโน้ตแรกของชุดกี่วินาที (เผื่อท่าควักมีด)")]
    [SerializeField] private float enemyLeadIn = 0.3f;

    [Header("โหมดอัดชาร์ต")]
    public bool recordMode = false;
    [Tooltip("จับโน้ตที่กดให้ลงบีทที่ใกล้สุด: 1 = ทุกบีท, 0.5 = ครึ่งบีท, 0.25 = เศษ 1/4 บีท")]
    [SerializeField] private float recordSnapBeats = 0.5f;
    [Tooltip("ติ๊ก = ล้างโน้ตเดิมทั้งหมดก่อนอัด / ไม่ติ๊ก = อัดเพิ่มต่อจากของเดิม")]
    [SerializeField] private bool clearChartBeforeRecording = true;

    [Header("หลอด progress (ถ้าใส่ จะวิ่งตามความยาวเพลง ต้องปิด Use Internal Timer ที่หลอดด้วย)")]
    [SerializeField] private ProgressTracker progressTracker;

    [Header("เพลงจบ")]
    public UnityEvent onChartFinished;

    private struct Wave { public int first; public int count; public float enemyTime; public LipidMovement enemy; }

    private BeatManager beat;
    private readonly List<ChartNote> notes = new List<ChartNote>();
    private readonly List<Wave> waves = new List<Wave>();
    private int noteWaveIndex = 0; // ไขมันของโน้ตตัวถัดไป
    private int nextNote = 0;
    private int nextWave = 0;
    private float travelTime;
    private float walkTime;
    private GameObject miniPrefab;
    private float stopX;
    private bool finished = false;
    private bool musicStarted = false;

    bool Active => chart != null;

    void Awake()
    {
        if (!Active) return;

        beat = FindFirstObjectByType<BeatManager>();
        if (beat == null)
        {
            Debug.LogError("[ChartPlayer] ไม่พบ BeatManager ใน scene");
            enabled = false;
            return;
        }

        // ชาร์ตเป็นเจ้าของเพลง / BPM / offset
        beat.bpm = chart.bpm;
        beat.songOffsetSeconds = chart.offsetSeconds;
        if (chart.music != null && beat.musicSource != null)
        {
            beat.musicSource.clip = chart.music;
            beat.musicSource.loop = false; // เพลงจบ = ด่านจบ
        }

        if (progressTracker != null) progressTracker.SetExternalControl();

        // ปิดการสุ่มแบบเดิม
        foreach (var spawner in FindObjectsByType<LipidSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;

        if (recordMode)
        {
            if (clearChartBeforeRecording) chart.notes.Clear();
            Debug.Log("[ChartPlayer] โหมดอัด: กด W/A/S/D ตามจังหวะเพลง แล้วกดหยุด Play เพื่อบันทึก");
        }
    }

    void Start()
    {
        if (!Active || recordMode || !enabled) return;

        var lipid = lipidPrefab != null ? lipidPrefab.GetComponent<LipidMovement>() : null;
        if (lipid == null || lipid.miniLipidPrefab == null)
        {
            Debug.LogError("[ChartPlayer] ช่อง Lipid Prefab ต้องเป็น LipidEnemy ที่มี Mini Lipid Prefab");
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

        Debug.Log($"[ChartPlayer] เล่นชาร์ต '{chart.name}': {notes.Count} โน้ต / ไขมัน {waves.Count} ตัว");
    }

    // โน้ตที่อยู่ติดกันเป็นไขมันตัวเดียว เว้นเกิน waveGapBeats = ไขมันตัวใหม่
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

        if (beat.musicSource != null && beat.musicSource.isPlaying) musicStarted = true;
        float t = beat.songPositionInSeconds;

        if (progressTracker != null && chart.music != null)
            progressTracker.SetProgress(Mathf.Clamp01((t + chart.offsetSeconds) / chart.music.length));

        if (recordMode)
        {
            RecordInput();
            CheckSongEnded(true);
            return;
        }

        // ไขมันเดินเข้ามาก่อนโน้ตชุดของมัน
        while (nextWave < waves.Count && t >= waves[nextWave].enemyTime)
        {
            var w = waves[nextWave];
            var go = Instantiate(lipidPrefab);
            w.enemy = go.GetComponent<LipidMovement>();
            w.enemy.BeginChartWave(w.count);
            if (t > w.enemyTime + 0.05f) w.enemy.SnapToStop(); // ต้นเพลง/มาช้า: โผล่ที่จุดหยุดเลย
            waves[nextWave] = w;
            nextWave++;
        }

        // ปล่อยโน้ตให้ถึงเส้นตรงบีทในชาร์ต
        float beatDur = chart.BeatDuration;
        while (nextNote < notes.Count)
        {
            var n = notes[nextNote];
            float arriveAt = n.beat * beatDur;
            if (t < arriveAt - travelTime) break;

            while (noteWaveIndex < waves.Count - 1 && nextNote >= waves[noteWaveIndex].first + waves[noteWaveIndex].count) noteWaveIndex++;
            var w = waves[noteWaveIndex];
            if (w.enemy == null) break; // ไขมันยังไม่ถูกสร้าง (ไม่ควรเกิด) รอเฟรมถัดไป

            SpawnNote(n, w.enemy, nextNote - w.first, w.count, arriveAt);
            nextNote++;
        }

        CheckSongEnded(false);
    }

    void SpawnNote(ChartNote n, LipidMovement owner, int index, int total, float arriveAt)
    {
        float y = n.row == NoteRow.Top ? topRowY : n.row == NoteRow.Bottom ? bottomRowY : middleRowY;
        var go = Instantiate(miniPrefab, new Vector3(stopX, y, 0f), Quaternion.identity);
        owner.RegisterSpawnedNote();

        var mini = go.GetComponent<MiniLipid>();
        if (mini == null) return;
        mini.InitializeMini(n.KeyCode, owner, index, total);
        mini.SetTargetSongTime(arriveAt);
    }

    void CheckSongEnded(bool recording)
    {
        bool musicDone = musicStarted && beat.musicSource != null && !beat.musicSource.isPlaying;
        bool notesDone = recording || (nextNote >= notes.Count && FindObjectsByType<MiniLipid>(FindObjectsSortMode.None).Length == 0);
        if (!musicDone || !notesDone) return;

        finished = true;
        Debug.Log("[ChartPlayer] เพลงจบ");
        onChartFinished?.Invoke();
    }

    // ---------- อัดชาร์ต ----------

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

        // กดซ้ำบีทเดิม = แทนที่ปุ่มเดิม (ไม่เกิดโน้ตซ้อน)
        chart.notes.RemoveAll(x => Mathf.Approximately(x.beat, snapped));
        chart.notes.Add(new ChartNote { beat = snapped, key = pressed.Value, row = ChartNote.DefaultRowFor(pressed.Value) });
        Debug.Log($"[ChartPlayer] อัด: บีท {snapped:0.##}  {pressed.Value}  (รวม {chart.notes.Count} โน้ต)");
    }

#if UNITY_EDITOR
    void OnDisable()
    {
        if (!Active || !recordMode) return;
        chart.SortNotes();
        UnityEditor.EditorUtility.SetDirty(chart);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[ChartPlayer] บันทึก {chart.notes.Count} โน้ตลง '{chart.name}' แล้ว → ปิด Record Mode แล้วกด Play เพื่อเล่น");
    }
#endif
}
