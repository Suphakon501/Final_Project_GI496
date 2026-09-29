using UnityEngine;

public enum BeatGrade { Perfect, Good, Miss }

// ตัวจับจังหวะเพลงกลาง ใช้ทดสอบระบบ rhythm ได้ทันทีแม้ยังไม่มีไฟล์เพลงจริง
// ถ้า musicSource ว่าง จะนับจังหวะเองจาก Time.time ตาม bpm ที่ตั้งไว้
// พอมีเพลงจริงแล้ว แค่ลาก AudioSource (ที่ใส่ AudioClip ไว้) มาใส่ musicSource ระบบจะ sync กับเพลงอัตโนมัติ
[DefaultExecutionOrder(-100)] // อัปเดตเวลาเพลงก่อนสคริปต์อื่น โน้ตจะได้อ่านเวลาเฟรมเดียวกัน
public class BeatManager : MonoBehaviour
{
    public static BeatManager instance;

    [Header("Tempo")]
    public float bpm = 120f;

    [Header("Music (ปล่อยว่างไว้ก่อนได้ถ้ายังไม่มีไฟล์เพลงจริง)")]
    public AudioSource musicSource;
    [Tooltip("เพลงเริ่มกี่วินาทีถึงจะเจอบีทแรก (ถ้าตัวอักษรเต้นก่อน/หลังเพลง ปรับตรงนี้ ทีละ 0.01-0.05)")]
    public float songOffsetSeconds = 0f;

    private float lastMusicTime;
    private int musicLoopCount;
    private float smoothRawTime;
    private bool smoothClockStarted;

    [Header("หา Offset: กด Enter ตามกลองหลายๆ ที แล้วดูค่าแนะนำใน Console / กด [ ] ขยับทีละ 0.01")]
    [SerializeField] private KeyCode offsetDownKey = KeyCode.LeftBracket;
    [SerializeField] private KeyCode offsetUpKey = KeyCode.RightBracket;
    [SerializeField] private int calibrationTaps = 8;
    private readonly System.Collections.Generic.List<float> tapErrors = new System.Collections.Generic.List<float>();

    [Header("Live Readout (ดูตอน Play เพื่อเช็คว่าจับจังหวะถูกไหม)")]
    public float songPositionInSeconds;
    public float songPositionInBeats;

    [Header("Test Tap (กด Enter เพื่อทดสอบจับเวลาการกดเทียบกับจังหวะ โดยไม่ไปยุ่งกับปุ่มอื่นในเกม)")]
    public KeyCode testTapKey = KeyCode.Return;
    public BeatGrade lastTestGrade;
    public float lastTestOffset;

    [Header("Grading Window - Test Tap (สัดส่วนของ 1 บีท เพดานสูงสุดคือ 0.5 เพราะเป็นเช็คแบบวนซ้ำทุกบีท)")]
    [SerializeField] private float perfectWindow = 0.2f;
    [SerializeField] private float goodWindow = 0.45f;

    [Header("Grading Window - Target Beat (หน่วยวินาทีตรงๆ ไม่มีเพดาน ใช้กับ RingSkillCheck)")]
    [SerializeField] private float targetPerfectWindowSeconds = 0.2f;
    [SerializeField] private float targetGoodWindowSeconds = 0.6f;

    private float virtualStartTime;

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
        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.Play();
        }
        else
        {
            virtualStartTime = Time.time;
        }
    }

    void Update()
    {
        if (musicSource != null && musicSource.clip != null)
        {
            // เวลาเพลงนับต่อเนื่องแม้เพลงวนลูป (ไม่งั้นพอเพลงวนกลับ 0 การรอบีทของโน้ตจะค้าง)
            // timeSamples แม่นกว่า .time แต่ Unity อัปเดตเป็นก้อนๆ (~20-40ms) เลยต้องทำให้ลื่นอีกชั้น
            float t = musicSource.timeSamples / (float)musicSource.clip.frequency;
            if (t + 1f < lastMusicTime) musicLoopCount++;
            lastMusicTime = t;

            if (musicSource.isPlaying)
            {
                float measured = t + musicLoopCount * musicSource.clip.length;
                if (!smoothClockStarted || Mathf.Abs(measured - smoothRawTime) > 0.1f)
                {
                    smoothRawTime = measured; // เริ่มใหม่ / คลาดเยอะ (เช่นเฟรมกระตุก) ให้กระโดดไปตรงเลย
                    smoothClockStarted = true;
                }
                else
                {
                    smoothRawTime += Time.unscaledDeltaTime;               // เดินลื่นๆ ตามเวลาจริง
                    smoothRawTime += (measured - smoothRawTime) * 0.05f;   // ดึงเข้าหาเวลาเพลงทีละนิด ไม่ให้หลุด
                }
                songPositionInSeconds = smoothRawTime - songOffsetSeconds;
            }
            else
            {
                songPositionInSeconds = -songOffsetSeconds;
            }
        }
        else
        {
            songPositionInSeconds = Time.time - virtualStartTime - songOffsetSeconds;
        }

        songPositionInBeats = songPositionInSeconds / BeatDuration;

        if (Input.GetKeyDown(testTapKey))
        {
            lastTestOffset = GetBeatOffset();
            lastTestGrade = GradeOffset(lastTestOffset);

            // + = กดหลังบีทของเกม (บีทเกมมาก่อนเพลง), - = กดก่อน
            float errorSeconds = (songPositionInBeats - Mathf.Round(songPositionInBeats)) * BeatDuration;
            tapErrors.Add(errorSeconds);
            if (tapErrors.Count > calibrationTaps) tapErrors.RemoveAt(0);

            float avg = 0f;
            foreach (var e in tapErrors) avg += e;
            avg /= tapErrors.Count;

            Debug.Log($"[BeatManager] {lastTestGrade}  กดคลาด {errorSeconds * 1000f:+0;-0} ms | เฉลี่ย {tapErrors.Count} ครั้ง {avg * 1000f:+0;-0} ms " +
                      $"→ แนะนำ Song Offset Seconds = {songOffsetSeconds + avg:F3} (ตอนนี้ {songOffsetSeconds:F3})");
        }

        if (Input.GetKeyDown(offsetDownKey) || Input.GetKeyDown(offsetUpKey))
        {
            songOffsetSeconds += Input.GetKeyDown(offsetUpKey) ? 0.01f : -0.01f;
            tapErrors.Clear(); // เปลี่ยน offset แล้ว ผลกดเก่าใช้ไม่ได้
            Debug.Log($"[BeatManager] Song Offset Seconds = {songOffsetSeconds:F3}  (จดค่านี้ไว้ ค่าที่แก้ตอน Play จะหายตอนหยุด)");
        }
    }

    // ระยะห่างจากบีทที่ใกล้ที่สุด หน่วยเป็นสัดส่วนของ 1 บีท (0 = ตรงเป๊ะ, 0.5 = ไกลสุด)
    public float GetBeatOffset()
    {
        float fractional = songPositionInBeats - Mathf.Floor(songPositionInBeats);
        return Mathf.Min(fractional, 1f - fractional);
    }

    public BeatGrade GradeOffset(float offset)
    {
        if (offset <= perfectWindow) return BeatGrade.Perfect;
        if (offset <= goodWindow) return BeatGrade.Good;
        return BeatGrade.Miss;
    }

    // ให้ระบบอื่น (เช่น RingSkillCheck, LipidSpawner) เรียกใช้ตอนอยากเช็คว่ากด/เกิด ตรงจังหวะแค่ไหน
    public BeatGrade GradeCurrentPress()
    {
        return GradeOffset(GetBeatOffset());
    }

    // สำหรับเช็คที่ต้องล็อก "บีทเป้าหมาย" ไว้ล่วงหน้า (เช่น RingSkillCheck) แทนการเทียบกับบีทที่ใกล้สุด ณ ขณะนั้น
    // ไม่งั้นถ้าเปิดค้างไว้หลายบีท ผลจะวน Perfect/Good/Miss ซ้ำไปเรื่อยๆ ตามจังหวะเพลง
    // คืนค่าเป็นวินาที (ไม่ใช่สัดส่วนบีท) เพราะเช็คแบบนี้ไม่ใช่การวนซ้ำ เลยไม่ต้องติดเพดานครึ่งบีท ขยายกว้างได้อิสระ
    public float GetOffsetFromTargetBeat(float targetBeat)
    {
        return Mathf.Abs(songPositionInBeats - targetBeat) * BeatDuration;
    }

    public BeatGrade GradeAgainstTarget(float targetBeat)
    {
        float offsetSeconds = GetOffsetFromTargetBeat(targetBeat);
        if (offsetSeconds <= targetPerfectWindowSeconds) return BeatGrade.Perfect;
        if (offsetSeconds <= targetGoodWindowSeconds) return BeatGrade.Good;
        return BeatGrade.Miss;
    }
}
