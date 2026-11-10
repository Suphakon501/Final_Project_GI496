using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-200)]
public class ChartPlayer : MonoBehaviour
{
    public static bool IsPostQTEGapActive { get; private set; }

    [Header("Chart (empty = random mode)")]
    public SongChart chart;
    public GameObject lipidPrefab;

    [Header("Row Heights (4 lanes, top to bottom)")]
    [Tooltip("ถ้าเปิด จะอ่าน Y ของลูกศรบน Hitline มาใช้เอง (ถ้าไม่เจอจะใช้ค่าด้านล่าง)")]
    [SerializeField] private bool useHitlineArrowY = true;
    [FormerlySerializedAs("topRowY")][SerializeField] private float rowYUp = 1.5f;      // W / Up
    [FormerlySerializedAs("middleRowY")][SerializeField] private float rowYLeft = 0f;      // A / Left
    [SerializeField] private float rowYRight = -0.75f;                                      // D / Right
    [FormerlySerializedAs("bottomRowY")][SerializeField] private float rowYDown = -1.5f;   // S / Down

    [Header("Enemy Lead-In (seconds)")]
    [SerializeField] private float enemyLeadIn = 0.3f;

    [Header("Recording")]
    public bool recordMode = false;
    [SerializeField] private float recordSnapBeats = 0.5f;
    [SerializeField, Min(0f)] private float recordHoldThresholdSeconds = 0.2f;
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
    private int nextQTE = 0;
    private float travelTime;
    private float walkTime;
    private GameObject miniPrefab;
    private float stopX;
    private bool finished = false;
    private bool musicStarted = false;
    private float lastPlayingTime;
    private float progressElapsed;
    private bool wasQTEActive;
    private float qteStartSongTime;
    private float chartTimelineOffset;
    private float postQTEGapEndSongTime;
    private float pendingNoteDelay;
    private bool postQTEGapActive;
    private NoteKey? recordingKey;
    private float recordingPressBeat;
    private ChartNote recordingNote;
    private const float SongEndTolerance = 1f;

    bool Active => chart != null;

    void OnDisable()
    {
        IsPostQTEGapActive = false;
        postQTEGapActive = false;
        pendingNoteDelay = 0f;
#if UNITY_EDITOR
        if (!Active || !recordMode) return;
        if (recordingKey.HasValue && beat != null)
            FinishRecordedHold(beat.songPositionInBeats);
        chart.SortNotes();
        UnityEditor.EditorUtility.SetDirty(chart);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"[ChartPlayer] Saved {chart.notes.Count} notes to '{chart.name}'");
#endif
    }

    public void BeginPostQTEGap(float duration)
    {
        if (beat == null) return;

        float qteDuration = wasQTEActive
            ? Mathf.Max(0f, beat.songPositionInSeconds - qteStartSongTime)
            : 0f;
        float totalDelay = qteDuration + Mathf.Max(0f, duration);
        chartTimelineOffset += totalDelay;
        wasQTEActive = false;

        if (duration > 0f)
        {
            postQTEGapEndSongTime = beat.songPositionInSeconds + duration;
            postQTEGapActive = true;
            IsPostQTEGapActive = true;
            pendingNoteDelay += totalDelay;
        }
        else
            DelayActiveNotes(totalDelay);
    }

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
            Debug.Log("[ChartPlayer] Recording: tap W/A/S/D for taps; hold the key and release to record hold length. Stop Play to save.");
        }
    }

    void Start()
    {
        if (!Active || recordMode || !enabled) return;

        progressElapsed = 0f;
        ReadRowYFromHitline();

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
        travelTime = (lipid.stopPosX - mini.hitLineX)
                   / Mathf.Max(0.01f, mini.MoveSpeed * chart.noteSpeedMultiplier);
        walkTime = lipid.WalkInDuration;

        notes.AddRange(chart.notes);
        notes.Sort((a, b) => a.beat.CompareTo(b.beat));
        if (chart.qteEvents != null) chart.qteEvents.Sort((a, b) => a.beat.CompareTo(b.beat));
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
        if (BossMashManager.IsMashActive)
        {
            if (!wasQTEActive)
            {
                wasQTEActive = true;
                qteStartSongTime = beat.songPositionInSeconds;
            }
            return;
        }

        if (beat.musicSource != null && beat.musicSource.isPlaying)
        {
            musicStarted = true;
            lastPlayingTime = beat.musicSource.time;
        }
        float songTime = beat.songPositionInSeconds;

        if (progressTracker != null)
        {
            // Drive the marker from active gameplay time, then catch up if the
            // actual clip clock is further ahead. Both clocks pause with timeScale.
            AudioSource source = beat.musicSource;
            if (source != null && source.clip != null && source.clip.length > 0f)
            {
                progressElapsed += Time.deltaTime;

                float sampleTime = source.timeSamples / (float)source.clip.frequency;
                progressElapsed = Mathf.Max(progressElapsed, sampleTime);
                progressTracker.SetProgress(Mathf.Clamp01(progressElapsed / source.clip.length));
            }
            else if (chart.music != null && chart.music.length > 0f)
            {
                progressTracker.SetProgress(Mathf.Clamp01((songTime + chart.offsetSeconds) / chart.music.length));
            }
        }

        if (postQTEGapActive)
        {
            if (songTime < postQTEGapEndSongTime) return;
            postQTEGapActive = false;
            IsPostQTEGapActive = false;
            DelayActiveNotes(pendingNoteDelay);
            pendingNoteDelay = 0f;
        }
        float t = songTime - chartTimelineOffset;
        if (recordMode)
        {
            RecordInput();
            CheckSongEnded(true);
            return;
        }

        while (chart.qteEvents != null && nextQTE < chart.qteEvents.Count && t >= chart.qteEvents[nextQTE].beat * chart.BeatDuration)
        {
            ChartQTE qte = chart.qteEvents[nextQTE++];
            if (BossMashManager.instance != null)
            {
                wasQTEActive = true;
                qteStartSongTime = songTime;
                DiscardActiveNotesForQTE();
                BossMashManager.instance.StartChartQTE(qte.durationSeconds);
                return;
            }
            Debug.LogWarning("[ChartPlayer] QTE event skipped because BossMashManager is missing");
        }

        float beatDur = chart.BeatDuration;
        while (nextWave < waves.Count && t >= waves[nextWave].enemyTime)
        {
            var w = waves[nextWave];
            bool hasUpcomingNote = false;
            for (int i = w.first; i < w.first + w.count; i++)
            {
                if (i >= nextNote && notes[i].beat * beatDur > t)
                {
                    hasUpcomingNote = true;
                    break;
                }
            }
            if (!hasUpcomingNote)
            {
                nextWave++;
                continue;
            }

            var go = Instantiate(lipidPrefab);
            w.enemy = go.GetComponent<LipidMovement>();
            w.enemy.BeginChartWave(w.count);
            if (t > w.enemyTime + 0.05f) w.enemy.SnapToStop();
            waves[nextWave] = w;
            nextWave++;
        }

        while (nextNote < notes.Count)
        {
            var n = notes[nextNote];
            float arriveAt = n.beat * beatDur;
            if (arriveAt <= t)
            {
                while (noteWaveIndex < waves.Count - 1 && nextNote >= waves[noteWaveIndex].first + waves[noteWaveIndex].count)
                    noteWaveIndex++;
                if (waves[noteWaveIndex].enemy != null)
                    waves[noteWaveIndex].enemy.RegisterSkippedChartNote();
                nextNote++;
                continue;
            }
            if (t < arriveAt - travelTime) break;

            while (noteWaveIndex < waves.Count - 1 && nextNote >= waves[noteWaveIndex].first + waves[noteWaveIndex].count) noteWaveIndex++;
            var w = waves[noteWaveIndex];
            if (w.enemy == null) break;

            SpawnNote(n, w.enemy, arriveAt + chartTimelineOffset);
            nextNote++;
        }

        CheckSongEnded(false);
    }

    void DelayActiveNotes(float delay)
    {
        if (delay <= 0f) return;
        foreach (MiniLipid mini in MiniLipid.Active)
            if (mini != null) mini.DelayTargetSongTime(delay);
    }

    void DiscardActiveNotesForQTE()
    {
        MiniLipid[] activeNotes = new MiniLipid[MiniLipid.Active.Count];
        for (int i = 0; i < activeNotes.Length; i++) activeNotes[i] = MiniLipid.Active[i];
        foreach (MiniLipid mini in activeNotes)
            if (mini != null) mini.DiscardForQTE();
    }

    // แต่ละปุ่มใช้ตำแหน่งลูกศรทิศของตัวเอง; W/S ย้ายเลนได้จากตำแหน่งลูกศรในฉาก
    float RowYFor(NoteKey k)
    {
        switch (k)
        {
            case NoteKey.W: return rowYUp;
            case NoteKey.A: return rowYLeft;
            case NoteKey.D: return rowYRight;
            default: return rowYDown;
        }
    }

    // อ่านตำแหน่ง Y ของลูกศรบน Hitline (ชื่อเดียวกับที่ PlayerController ใช้) ให้โน้ตวิ่งตรงแถวลูกศร
    void ReadRowYFromHitline()
    {
        if (!useHitlineArrowY) return;

        GameObject hitline = GameObject.Find("Hitline");
        if (hitline == null) return;

        if (!TryArrowY(hitline.transform, "Up2_1", out float up)) return;
        if (!TryArrowY(hitline.transform, "Left2_1", out float left)) return;
        if (!TryArrowY(hitline.transform, "Right2_0", out float right)) return;
        if (!TryArrowY(hitline.transform, "Down2_1", out float down)) return;

        rowYUp = up; rowYLeft = left; rowYRight = right; rowYDown = down;
        Debug.Log($"[ChartPlayer] Row Y from Hitline: W={up:0.##} A={left:0.##} D={right:0.##} S={down:0.##}");
    }

    static bool TryArrowY(Transform hitline, string childName, out float y)
    {
        Transform t = hitline.Find(childName);
        y = t != null ? t.position.y : 0f;
        return t != null;
    }

    void SpawnNote(ChartNote n, LipidMovement owner, float arriveAt)
    {
        float y = RowYFor(n.key);
        var go = Instantiate(miniPrefab, new Vector3(stopX, y, 0f), Quaternion.identity);
        owner.RegisterSpawnedNote();

        var mini = go.GetComponent<MiniLipid>();
        if (mini == null) return;
        mini.SetMoveSpeedMultiplier(chart.noteSpeedMultiplier);
        mini.InitializeMini(n.KeyCode, owner);
        mini.SetHoldDuration(Mathf.Max(0f, n.holdBeats) * chart.BeatDuration);
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
        if (recordingKey.HasValue && WasKeyReleased(recordingKey.Value))
        {
            FinishRecordedHold(beat.songPositionInBeats);
            recordingKey = null;
            recordingNote = null;
        }

        if (recordingKey.HasValue) return;

        NoteKey? pressed = null;
        if (Input.GetKeyDown(KeyCode.D)) pressed = NoteKey.W;
        else if (Input.GetKeyDown(KeyCode.F)) pressed = NoteKey.A;
        else if (Input.GetKeyDown(KeyCode.K)) pressed = NoteKey.S;
        else if (Input.GetKeyDown(KeyCode.J)) pressed = NoteKey.D;
        if (pressed == null) return;

        float snap = Mathf.Max(0.0625f, recordSnapBeats);
        float snapped = Mathf.Round(beat.songPositionInBeats / snap) * snap;
        if (snapped < 0f) return;

        chart.notes.RemoveAll(x => Mathf.Approximately(x.beat, snapped));
        recordingNote = new ChartNote { beat = snapped, key = pressed.Value, row = ChartNote.DefaultRowFor(pressed.Value) };
        chart.notes.Add(recordingNote);
        recordingKey = pressed.Value;
        recordingPressBeat = beat.songPositionInBeats;
        Debug.Log($"[ChartPlayer] Started beat {snapped:0.##} {pressed.Value}; release key to finish note");
    }

    bool WasKeyReleased(NoteKey key)
    {
        switch (key)
        {
            case NoteKey.W: return Input.GetKeyUp(KeyCode.D);
            case NoteKey.A: return Input.GetKeyUp(KeyCode.F);
            case NoteKey.S: return Input.GetKeyUp(KeyCode.K);
            default: return Input.GetKeyUp(KeyCode.J);
        }
    }

    void FinishRecordedHold(float songBeat)
    {
        if (recordingNote == null) return;

        float snap = Mathf.Max(0.0625f, recordSnapBeats);
        float pressedDurationBeats = Mathf.Max(0f, songBeat - recordingPressBeat);
        float pressedDurationSeconds = pressedDurationBeats * chart.BeatDuration;
        float snappedReleaseBeat = Mathf.Round(songBeat / snap) * snap;
        float durationBeats = Mathf.Max(0f, snappedReleaseBeat - recordingNote.beat);
        recordingNote.holdBeats = pressedDurationSeconds < recordHoldThresholdSeconds
            ? 0f
            : Mathf.Round(durationBeats / snap) * snap;
        Debug.Log($"[ChartPlayer] Recorded {recordingNote.beat:0.##} {recordingNote.key} hold {recordingNote.holdBeats:0.##} beats ({chart.notes.Count} notes)");
    }

}
