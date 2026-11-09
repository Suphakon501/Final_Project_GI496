using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BossHitEffect : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float framesPerSecond = 30f;
    [SerializeField] private float rotationSpeed = 40f;

    private SpriteRenderer effectRenderer;
    private int currentFrame;
    private float elapsed;

    void Awake()
    {
        effectRenderer = GetComponent<SpriteRenderer>();
        if (frames == null || frames.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        effectRenderer.sprite = frames[0];
        Vector3 center = transform.TransformPoint(effectRenderer.sprite.bounds.center);
        transform.RotateAround(center, Vector3.forward, Random.Range(-8f, 8f));
        rotationSpeed *= Random.value < 0.5f ? -1f : 1f;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        elapsed += Time.unscaledDeltaTime;
        Vector3 center = transform.TransformPoint(effectRenderer.sprite.bounds.center);
        transform.RotateAround(center, Vector3.forward, rotationSpeed * Time.unscaledDeltaTime);
        int frame = Mathf.FloorToInt(elapsed * framesPerSecond);
        if (frame >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }

        if (frame == currentFrame) return;
        currentFrame = frame;
        effectRenderer.sprite = frames[currentFrame];
    }
}
