using UnityEngine;

// ทำให้ GameObject ที่แปะ component นี้ไว้ เต้น/ขยายตามจังหวะเพลงอัตโนมัติ
// ช่วยให้คนเล่นเห็นจังหวะได้จากภาพ ไม่ต้องนับเองหรือมีสกิล rhythm มาก่อน
// ถ้าไม่มี BeatManager ในซีน จะไม่ทำอะไรเลย (ไม่พังของเดิม)
public class BeatPulseUI : MonoBehaviour
{
    [Header("Pulse Scale")]
    [SerializeField] private float restScale = 1f;
    [SerializeField] private float peakScale = 1.3f;

    [Header("Pulse Shape (0 = ตรงบีทพอดี ใหญ่สุด, 1 = กลางบีท ปกติ)")]
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (BeatManager.instance == null) return;

        float beatFraction = BeatManager.instance.songPositionInBeats - Mathf.Floor(BeatManager.instance.songPositionInBeats);
        float distFromBeat = Mathf.Min(beatFraction, 1f - beatFraction) * 2f; // 0 = ตรงบีท, 1 = กลางบีท

        float curveValue = pulseCurve.Evaluate(distFromBeat);
        float scale = Mathf.Lerp(restScale, peakScale, curveValue);

        transform.localScale = baseScale * scale;
    }
}
