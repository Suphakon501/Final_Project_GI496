using UnityEngine;

/// <summary>วงแหวน procedural ที่ขยายและจางหายเอง (สร้างโดย ProcParticles.Shockwave)</summary>
public class ProcShockwave : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color baseColor = Color.white;
    private float startSize = 0.3f;
    private float endSize = 2f;
    private float lifetime = 0.3f;
    private float elapsed;

    public void Play(float start, float end, float life)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startSize = Mathf.Max(0.01f, start);
        endSize = Mathf.Max(startSize, end);
        lifetime = Mathf.Max(0.01f, life);
        elapsed = 0f;

        if (spriteRenderer != null) baseColor = spriteRenderer.color;
        transform.localScale = Vector3.one * startSize;
    }

    void Update()
    {
        if (spriteRenderer == null || PauseMenu.IsPaused) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);
        float eased = 1f - (1f - t) * (1f - t);

        transform.localScale = Vector3.one * Mathf.Lerp(startSize, endSize, eased);

        Color color = baseColor;
        color.a = baseColor.a * (1f - t);
        spriteRenderer.color = color;

        if (t >= 1f) Destroy(gameObject);
    }
}
