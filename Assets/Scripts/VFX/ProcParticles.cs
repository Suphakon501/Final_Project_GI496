using UnityEngine;

/// <summary>
/// โรงงาน particle ที่สร้างเองทั้งหมดจากโค้ด (ไม่ต้องมี asset ในโปรเจกต์)
/// สร้าง texture วงกลม/วงแหวนตอนรัน แล้วยิง burst หรือ shockwave ตามจุดที่ต้องการ
/// </summary>
public static class ProcParticles
{
    private static Sprite dotSprite;
    private static Sprite ringSprite;
    private static Material dotMaterial;
    private static Material ringMaterial;

    public static Sprite Dot
    {
        get
        {
            if (dotSprite == null) dotSprite = CreateSoftCircle(64, 2.1f);
            return dotSprite;
        }
    }

    public static Sprite Ring
    {
        get
        {
            if (ringSprite == null) ringSprite = CreateRing(128, 0.74f, 0.1f);
            return ringSprite;
        }
    }

    static Material DotMaterial
    {
        get
        {
            if (dotMaterial == null) dotMaterial = CreateSpriteMaterial(Dot);
            return dotMaterial;
        }
    }

    static Material RingMaterial
    {
        get
        {
            if (ringMaterial == null) ringMaterial = CreateSpriteMaterial(Ring);
            return ringMaterial;
        }
    }

    /// <summary>ยิงประกายออกจากจุดหนึ่ง (ใช้ตอนกดโดนโน้ต/ลงพื้น/เก็บคอมโบ)</summary>
    public static void Burst(Vector3 position, Color color, int count = 14, float speed = 6f, float lifetime = 0.45f, float size = 0.4f)
    {
        if (count <= 0) return;

        var go = new GameObject("ProcBurst");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();

        // ParticleSystem ที่เพิ่ง AddComponent จะเริ่มเล่นเองทันที
        // ต้องหยุดให้สนิทก่อน ไม่งั้น Unity ไม่ยอมให้แก้ duration (ขึ้น error แดง)
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.45f, size);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.gravityModifier = 0.35f;
        main.maxParticles = Mathf.Max(64, count * 4);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 500)) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.06f;
        shape.radiusThickness = 1f;
        shape.arc = 360f;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.3f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.35f), new GradientAlphaKey(0f, 1f) });

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sharedMaterial = DotMaterial;
        particleRenderer.sortingOrder = 9;

        ps.Play(true);
        Object.Destroy(go, lifetime + 0.35f);
    }

    /// <summary>วงแหวนขยายตัวแล้วจางหาย (ใช้เน้นจังหวะ impact)</summary>
    public static void Shockwave(Vector3 position, Color color, float startSize = 0.35f, float endSize = 2.2f, float lifetime = 0.3f)
    {
        var go = new GameObject("ProcShockwave");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Ring;
        sr.color = color;
        sr.sharedMaterial = RingMaterial;
        sr.sortingOrder = 9;

        var ring = go.AddComponent<ProcShockwave>();
        ring.Play(startSize, endSize, lifetime);
    }

    static Material CreateSpriteMaterial(Sprite sprite)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        var material = new Material(shader);
        if (sprite != null) material.mainTexture = sprite.texture;
        return material;
    }

    /// <summary>สร้างสไปรท์วงกลมขอบนุ่ม (PPU = size → ขนาด 1 หน่วย world ที่ scale 1)</summary>
    public static Sprite CreateSoftCircle(int size, float hardness)
    {
        size = Mathf.Max(8, size);
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = Mathf.Pow(alpha, Mathf.Max(0.01f, hardness));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    /// <summary>สร้างสไปรท์วงแหวน (ไว้ทำ shockwave)</summary>
    public static Sprite CreateRing(int size, float radiusRatio, float thicknessRatio)
    {
        size = Mathf.Max(16, size);
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                float band = Mathf.Abs(distance - Mathf.Clamp01(radiusRatio));
                float alpha = Mathf.Clamp01(1f - band / Mathf.Max(0.01f, thicknessRatio));
                alpha *= alpha;
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
