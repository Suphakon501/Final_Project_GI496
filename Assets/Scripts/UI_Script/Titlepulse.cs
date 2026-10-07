using UnityEngine;

public class TitlePulse : MonoBehaviour
{
    [Header("Pulse")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float maxScale = 1.1f;

    [Header("Tilt")]
    [SerializeField] private float tiltAngle = 0f;
    [SerializeField] private float swayAmount = 0f;

    [Header("Other")]
    [SerializeField] private bool useUnscaledTime = true;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        float wave = (Mathf.Sin(t * speed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minScale, maxScale, wave);
        transform.localScale = baseScale * scale;

        float sway = Mathf.Sin(t * speed * 0.5f) * swayAmount;
        transform.localRotation = Quaternion.Euler(0f, 0f, tiltAngle + sway);
    }
}
