using System.Collections.Generic;
using UnityEngine;

public enum BeatGrade { Perfect, Good, Miss }

[DefaultExecutionOrder(-100)]
public class BeatManager : MonoBehaviour
{
    public static BeatManager instance;

    [Header("Tempo")]
    public float bpm = 120f;

    [Header("Music")]
    public AudioSource musicSource;
    public float songOffsetSeconds = 0f;

    [Header("Live Readout")]
    public float songPositionInSeconds;
    public float songPositionInBeats;

    [Header("Offset Calibration (Enter = tap, [ ] = nudge)")]
    public KeyCode testTapKey = KeyCode.Return;
    [SerializeField] private KeyCode offsetDownKey = KeyCode.LeftBracket;
    [SerializeField] private KeyCode offsetUpKey = KeyCode.RightBracket;
    [SerializeField] private int calibrationTaps = 8;
    public BeatGrade lastTestGrade;
    public float lastTestOffset;

    [Header("Calibration Tap Window (fraction of a beat)")]
    [SerializeField] private float perfectWindow = 0.2f;
    [SerializeField] private float goodWindow = 0.45f;

    private const float SmoothPull = 0.05f;
    private const float ResyncThreshold = 0.1f;

    private float virtualStartTime;
    private float lastMusicTime;
    private int musicLoopCount;
    private float smoothRawTime;
    private bool smoothClockStarted;
    private readonly List<float> tapErrors = new List<float>();

    public float BeatDuration => 60f / bpm;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartBeatClock();
    }

    public void StartBeatClock()
    {
        if (HasMusic) musicSource.Play();
        else virtualStartTime = Time.time;
    }

    bool HasMusic => musicSource != null && musicSource.clip != null;

    void Update()
    {
        songPositionInSeconds = HasMusic ? ReadMusicClock() : Time.time - virtualStartTime - songOffsetSeconds;
        songPositionInBeats = songPositionInSeconds / BeatDuration;

        HandleCalibrationKeys();
    }

    float ReadMusicClock()
    {
        float t = musicSource.timeSamples / (float)musicSource.clip.frequency;
        if (t + 1f < lastMusicTime) musicLoopCount++;
        lastMusicTime = t;

        if (musicSource.isPlaying)
        {
            float measured = t + musicLoopCount * musicSource.clip.length;
            if (!smoothClockStarted || Mathf.Abs(measured - smoothRawTime) > ResyncThreshold)
            {
                smoothRawTime = measured;
                smoothClockStarted = true;
            }
            else
            {
                smoothRawTime += Time.unscaledDeltaTime;
                smoothRawTime += (measured - smoothRawTime) * SmoothPull;
            }
        }
        else if (smoothClockStarted)
        {
            smoothRawTime += Time.unscaledDeltaTime;
        }
        else
        {
            return -songOffsetSeconds;
        }

        return smoothRawTime - songOffsetSeconds;
    }

    void HandleCalibrationKeys()
    {
        if (Input.GetKeyDown(testTapKey)) RecordCalibrationTap();

        bool down = Input.GetKeyDown(offsetDownKey);
        bool up = Input.GetKeyDown(offsetUpKey);
        if (down || up)
        {
            songOffsetSeconds += up ? 0.01f : -0.01f;
            tapErrors.Clear();
            Debug.Log($"[BeatManager] Song Offset Seconds = {songOffsetSeconds:F3}");
        }
    }

    void RecordCalibrationTap()
    {
        lastTestOffset = GetBeatOffset();
        lastTestGrade = GradeOffset(lastTestOffset);

        float errorSeconds = (songPositionInBeats - Mathf.Round(songPositionInBeats)) * BeatDuration;
        tapErrors.Add(errorSeconds);
        if (tapErrors.Count > calibrationTaps) tapErrors.RemoveAt(0);

        float avg = 0f;
        foreach (var e in tapErrors) avg += e;
        avg /= tapErrors.Count;

        Debug.Log($"[BeatManager] {lastTestGrade}  error {errorSeconds * 1000f:+0;-0} ms | avg of {tapErrors.Count} {avg * 1000f:+0;-0} ms " +
                  $"-> suggested Song Offset Seconds = {songOffsetSeconds + avg:F3} (current {songOffsetSeconds:F3})");
    }

    float GetBeatOffset()
    {
        float fractional = songPositionInBeats - Mathf.Floor(songPositionInBeats);
        return Mathf.Min(fractional, 1f - fractional);
    }

    BeatGrade GradeOffset(float offset)
    {
        if (offset <= perfectWindow) return BeatGrade.Perfect;
        if (offset <= goodWindow) return BeatGrade.Good;
        return BeatGrade.Miss;
    }
}
