using UnityEngine;

/// <summary>
/// เด้งตัวตามจังหวะแบบ Friday Night Funkin: ยืด-ย่อสั้น ๆ ทุกบีตแล้วค่อย ๆ กลับรูป
/// ทำงานเฉพาะ scale (ไม่แตะตำแหน่ง/การหันหน้า จึงไม่ชนกับสคริปต์อื่น)
/// </summary>
[DisallowMultipleComponent]
public class BeatBop : MonoBehaviour
{
    [Tooltip("ความแรงของการยืด-ย่อ (0.09 = 9%)")]
    [SerializeField, Range(0f, 0.5f)] private float squashAmount = 0.09f;

    [Tooltip("สัดส่วนการย่อแนวนอนเทียบค่าแรง (0 = ไม่ย่อแนวนอน)")]
    [SerializeField, Range(0f, 1f)] private float horizontalAmount = 0.35f;

    [Tooltip("ระยะเวลาของการเด้งหนึ่งครั้ง (วินาที)")]
    [SerializeField, Min(0.05f)] private float bopDuration = 0.22f;

    [Tooltip("อ่านจังหวะจาก BeatManager อัตโนมัติ")]
    [SerializeField] private bool useSongBeat = true;

    private float timer;
    private int lastBeat = int.MinValue;
    private float appliedX = 1f;
    private float appliedY = 1f;

    void OnDisable()
    {
        ClearApplied();
    }

    void LateUpdate()
    {
        // คืนสเกลเดิมก่อน เพื่อไม่ให้ค่าทบกันข้ามเฟรม (รองรับกรณีสคริปต์อื่นสลับด้าน/ปรับสเกล)
        ClearApplied();

        if (useSongBeat)
        {
            BeatManager beat = BeatManager.instance;
            if (beat != null)
            {
                int current = Mathf.FloorToInt(beat.songPositionInBeats);
                if (current != lastBeat)
                {
                    lastBeat = current;
                    if (current >= 0) timer = bopDuration;
                }
            }
        }

        if (timer > 0f)
            timer = Mathf.Max(0f, timer - Time.deltaTime);

        float curve = bopDuration > 0f ? timer / bopDuration : 0f;
        curve *= curve;   // กระตุกทันทีตอนบีตแล้วค่อย ๆ คืนตัว

        if (curve <= 0.0001f) return;

        appliedY = 1f + squashAmount * curve;
        appliedX = 1f - squashAmount * horizontalAmount * curve;

        Vector3 s = transform.localScale;
        s.x *= appliedX;
        s.y *= appliedY;
        transform.localScale = s;
    }

    /// <summary>ลบผลของการเด้งที่ค้างจากเฟรมก่อน</summary>
    void ClearApplied()
    {
        if (Mathf.Approximately(appliedX, 1f) && Mathf.Approximately(appliedY, 1f)) return;

        Vector3 s = transform.localScale;
        s.x /= appliedX;
        s.y /= appliedY;
        transform.localScale = s;

        appliedX = 1f;
        appliedY = 1f;
    }
}
