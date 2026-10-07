using UnityEngine;

public class BeatPulseUI : MonoBehaviour
{
    [Header("Pulse Scale")]
    [SerializeField] private float restScale = 1f;
    [SerializeField] private float peakScale = 1.3f;

    [Header("Pulse Shape (0 = on beat, 1 = between beats)")]
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
        float distFromBeat = Mathf.Min(beatFraction, 1f - beatFraction) * 2f;

        float curveValue = pulseCurve.Evaluate(distFromBeat);
        float scale = Mathf.Lerp(restScale, peakScale, curveValue);

        transform.localScale = baseScale * scale;
    }
}
