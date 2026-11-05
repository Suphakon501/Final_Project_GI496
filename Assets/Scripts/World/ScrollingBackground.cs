using System.Collections.Generic;
using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "Layer";
        [Tooltip("Sprite ต้นแบบ (ลากจาก Project หรือจาก Hierarchy ก็ได้)")]
        public SpriteRenderer sprite;
        [Range(0f, 3f)] public float speedMultiplier = 1f;
        public Vector2 positionOffset = Vector2.zero;
        [Range(0f, 0.2f)] public float overlap = 0.02f;
        [Tooltip("ให้เลเยอร์นี้เด้งตามบีตหรือไม่ (เปิดเฉพาะเลเยอร์ตึก/เมือง)")]
        public bool pulseOnBeat = false;

        [HideInInspector] public List<Transform> tiles = new List<Transform>();
        [HideInInspector] public SpriteRenderer template; // ตัว clone ในซีนที่ซ่อนไว้ ใช้สร้าง tile
        [HideInInspector] public Transform parent;
        [HideInInspector] public float width;
        [HideInInspector] public float spacing;
        [HideInInspector] public float y;
        [HideInInspector] public float z;
        [HideInInspector] public int spawnCount;
        [HideInInspector] public Vector3 baseScale = Vector3.one;
        [HideInInspector] public float beatJitter = 1f;
        [HideInInspector] public Color baseColor = Color.white;
        [HideInInspector] public List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    }

    [Header("Speed")]
    public float baseSpeed = 2f;

    [Header("Beat Pulse (เด้งตาม BPM)")]
    [SerializeField] private bool beatPulse = true;
    [Tooltip("ขนาดการเด้ง (0.03 = 3%)")]
    [SerializeField, Min(0f)] private float beatScaleAmount = 0.035f;
    [Tooltip("ระยะที่พื้นหลังกระตุกขึ้น (หน่วย)")]
    [SerializeField, Min(0f)] private float beatBounceHeight = 0.08f;
    [Tooltip("ระยะเวลาทั้งหมดของการเด้งหนึ่งครั้ง")]
    [SerializeField, Min(0.05f)] private float beatPulseDuration = 0.34f;
    [Tooltip("เวลาที่ใช้พุ่งขึ้นถึงจุดสูงสุด (สั้น = กระตุกแรง)")]
    [SerializeField, Min(0.01f)] private float beatAttackTime = 0.05f;
    [Tooltip("ความหน่วงต่อชั้น ให้เลเยอร์หลังตามหลังเลเยอร์หน้าเล็กน้อย")]
    [SerializeField, Min(0f)] private float beatLayerStagger = 0.04f;
    [Tooltip("สุ่มแรงเด้งแต่ละบีตให้ต่างกัน (± สัดส่วน) กันดูซ้ำเดิม")]
    [SerializeField, Range(0f, 0.4f)] private float beatRandomVariance = 0.12f;
    [Tooltip("ทุกกี่บีตถึงจะแรงเป็นพิเศษ (1 = ทุกบีตเท่ากัน)")]
    [SerializeField, Min(1)] private int strongBeatEvery = 4;
    [SerializeField, Min(1f)] private float strongBeatMultiplier = 1.8f;

    [Header("Layers (far to near)")]
    public Layer[] layers;

    [Header("Culling")]
    [Tooltip("ระยะเผื่อนอกขอบกล้อง ก่อนทำลาย/สร้าง tile (กันภาพกระตุกที่ขอบจอ)")]
    public float margin = 1f;
    [Tooltip("ปิดไว้ = tile ที่หลุดจอจะหายไปเฉย ๆ ไม่สร้างใหม่ (พื้นหลังจะหมด)")]
    public bool respawn = true;

    [Header("Backlight Glow (แสงเรืองหลังเมือง)")]
    [SerializeField] private bool backlightGlow = true;
    [Tooltip("สีของแสงที่สาดขึ้นมาจากด้านหลังตึก")]
    [SerializeField] private Color backlightColor = new Color(0.62f, 0.3f, 1f);
    [Tooltip("สีที่สลับสุ่ม (ม่วง / ชมพู / น้ำเงิน / เขียว / เหลือง) — ว่าง = ใช้สีเดี่ยวด้านบน")]
    [SerializeField] private Color[] backlightColors =
    {
        new Color(0.62f, 0.3f, 1f),
        new Color(1f, 0.35f, 0.75f),
        new Color(0.2f, 0.45f, 1f),
        new Color(0.25f, 1f, 0.55f),
        new Color(1f, 0.85f, 0.2f)
    };
    [Tooltip("ระยะเวลาที่สีหนึ่งค้างอยู่ก่อนเปลี่ยน (วินาที)")]
    [SerializeField, Min(0.2f)] private float backlightHoldMin = 2f;
    [SerializeField, Min(0.2f)] private float backlightHoldMax = 4.5f;
    [Tooltip("ความนุ่มของการเปลี่ยนสี")]
    [SerializeField, Min(0.05f)] private float backlightFade = 0.8f;
    [Tooltip("ความสว่างของแสง")]
    [SerializeField, Range(0f, 1f)] private float backlightAlpha = 0.65f;
    [Tooltip("ขนาดของแสง (กว้าง x สูง เป็นหน่วยของฉาก)")]
    [SerializeField, Min(1f)] private float backlightWidth = 30f;
    [SerializeField, Min(1f)] private float backlightHeight = 18f;
    [Tooltip("ตำแหน่งกึ่งกลางแสงในแนวตั้ง (0 = กลางจอ)")]
    [SerializeField] private float backlightY = 1.2f;
    [Tooltip("แสงวูบวาบเพิ่มตอนบีต (0 = นิ่ง)")]
    [SerializeField, Range(0f, 0.5f)] private float backlightBeatPulse = 0.12f;
    [Tooltip("ลำดับการวาดของแสง (ต้องอยู่หลังเลเยอร์ตึก order 1)")]
    [SerializeField] private int backlightOrder = 0;

    [Header("Night Ambience (บรรยากาศกลางคืน)")]
    [Tooltip("ความมืดของ BG (1 = ปกติ, 0.55 = กลางคืน) — ไม่เปลี่ยนสี สีเดิมยังอยู่")]
    [SerializeField, Range(0.2f, 1f)] private float sceneDarkness = 0.55f;
    [Tooltip("สีของม่านกลางคืน (โทนน้ำเงินเข้มมาก เกือบดำ)")]
    [SerializeField] private Color nightVeilColor = new Color(0.02f, 0.03f, 0.09f);
    [Tooltip("ความทึบของม่าน (ยิ่งมากยิ่งมืด)")]
    [SerializeField, Range(0f, 0.7f)] private float nightVeilAlpha = 0.38f;
    [Tooltip("ลำดับการวาดของม่าน (ให้อยู่ใต้ตัวผู้เล่น/โน้ต เพื่อไม่ให้ตัวละครมืด)")]
    [SerializeField] private int nightVeilOrder = 2;
    [Tooltip("ขนาดม่านเกินขอบจอเท่าไร (กันขอบโผล่)")]
    [SerializeField, Min(1f)] private float veilMargin = 1.2f;

    Camera cam;
    private int lastBeatIndex = int.MinValue;
    private float beatTimer;
    private float beatStrength = 1f;
    private SpriteRenderer nightVeil;
    private SpriteRenderer backlight;
    private Layer skyLayer;
    private Color backlightNow = Color.white;
    private Color backlightTarget = Color.white;
    private float backlightTimer;
    private int lastBacklightIndex = -1;

    void Start()
    {
        cam = Camera.main;

        // เลเยอร์แรก (หลังสุด) คือฟ้องฟ้า — ต้องอยู่หลังแผ่นแสงเรือง
        if (layers != null && layers.Length > 0) skyLayer = layers[0];

        foreach (var layer in layers)
            BuildLayer(layer);

        BuildBacklight();
        BuildNightVeil();
    }

    void BuildLayer(Layer layer)
    {
        if (layer.sprite == null || layer.sprite.sprite == null) return;

        Transform src = layer.sprite.transform;
        bool isSceneObject = layer.sprite.gameObject.scene.IsValid();

        // ไม่แตะต้อง/ไม่ทำลายตัวต้นฉบับ (อาจเป็น asset ใน Project)
        Vector3 basePos = src.position + (Vector3)layer.positionOffset;
        layer.parent = isSceneObject ? src.parent : transform;

        // ใช้ขนาดจากตัว Sprite เอง เพราะ bounds ของ asset ไม่น่าเชื่อถือ
        layer.width = layer.sprite.sprite.bounds.size.x * Mathf.Abs(src.lossyScale.x);
        layer.spacing = Mathf.Max(0.01f, layer.width - layer.overlap);
        layer.y = basePos.y;
        layer.z = basePos.z;

        // ต้นแบบในซีน (ซ่อนไว้) ใช้ clone tile ทั้งหมด
        layer.template = Instantiate(layer.sprite, basePos, src.rotation, layer.parent);
        layer.template.name = layer.sprite.name + "_template";
        layer.template.gameObject.SetActive(false);
        layer.baseScale = layer.template.transform.localScale;
        layer.baseColor = layer.template.color;

        // ถ้าลากมาจากซีน ให้ซ่อนตัวเดิมไม่ให้ซ้อนกับ tile ที่สร้างใหม่
        if (isSceneObject)
            layer.sprite.gameObject.SetActive(false);

        // tile แรกเริ่มจากซ้ายสุดของกล้อง แล้วเติมไปทางขวาจนเต็มจอ
        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        int shift = Mathf.Max(0, Mathf.CeilToInt((basePos.x - layer.width * 0.5f - camLeft) / layer.spacing));
        SpawnTile(layer, basePos.x - shift * layer.spacing);
        FillRight(layer);
    }

    void Update()
    {
        UpdateBeatPulse(Time.deltaTime);
        UpdateBacklight();
        UpdateNightVeil();

        int pulseIndex = 0;
        foreach (var layer in layers)
        {
            float curve = 0f;
            if (layer.pulseOnBeat)
            {
                curve = LayerBeatCurve(pulseIndex, layer);
                pulseIndex++;
            }

            if (layer.template == null) continue;

            float bob = beatBounceHeight * curve;
            float scaleMul = 1f + beatScaleAmount * curve;
            float move = baseSpeed * layer.speedMultiplier * Time.deltaTime;

            for (int i = 0; i < layer.tiles.Count; i++)
            {
                Transform t = layer.tiles[i];
                Vector3 p = t.position;
                p.x -= move;
                p.y = layer.y + bob;
                t.position = p;
                t.localScale = layer.baseScale * scaleMul;

                // หรี่สีเดิมลง (คูณความมืด) — สี/โทนภาพยังเป็นของเดิม แค่เข้มขึ้น
                if (sceneDarkness < 0.999f && i < layer.renderers.Count)
                {
                    Color c = layer.baseColor * sceneDarkness;
                    c.a = layer.baseColor.a;
                    layer.renderers[i].color = c;
                }
            }

            CullLeft(layer);
            if (respawn) FillRight(layer);
        }
    }

    /// <summary>เส้นโค้งการเด้งของแต่ละชั้น: กระตุกขึ้นเร็ว แล้วค้างตัวลงนุ่ม ๆ แบบสปริง</summary>
    float LayerBeatCurve(int index, Layer layer)
    {
        if (!layer.pulseOnBeat || beatTimer <= 0f) return 0f;

        float elapsed = (Mathf.Max(0.05f, beatPulseDuration) - beatTimer) - index * beatLayerStagger;
        if (elapsed <= 0f) return 0f;

        // ช่วงพุ่งขึ้น: smoothstep สั้น ๆ ขึ้นเร็วแต่ไม่กระชาก
        float attackT = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, beatAttackTime));
        float attack = attackT * attackT * (3f - 2f * attackT);

        // ช่วงค้างตัว: สปริงหน่วง มีเด้งกลับลงต่ำกว่าเดิมนิดหน่อยแล้วนิ่ง
        float decayT = Mathf.Max(0f, elapsed - beatAttackTime);
        float settle = Mathf.Exp(-8.5f * decayT) * Mathf.Cos(decayT * 15f);

        return attack * settle * beatStrength * layer.beatJitter;
    }

    /// <summary>คำนวณการเด้งของพื้นหลังตามbeatเพลง (อ่านจาก BeatManager)</summary>
    void UpdateBeatPulse(float deltaTime)
    {
        if (beatTimer > 0f) beatTimer = Mathf.Max(0f, beatTimer - deltaTime);

        BeatManager beat = BeatManager.instance;
        if (!beatPulse || beat == null) return;

        int beatIndex = Mathf.FloorToInt(beat.songPositionInBeats);
        if (beatIndex == lastBeatIndex) return;
        lastBeatIndex = beatIndex;
        if (beatIndex < 0) return;

        bool strong = strongBeatEvery > 1 && beatIndex % strongBeatEvery == 0;
        beatStrength = strong ? strongBeatMultiplier : 1f;
        beatTimer = Mathf.Max(0.05f, beatPulseDuration);

        // สุ่มแรงของแต่ละบีต/แต่ละชั้นให้ไม่ซ้ำเดิม ดูเป็นธรรมชาติ
        foreach (var layer in layers)
        {
            if (!layer.pulseOnBeat) continue;
            layer.beatJitter = 1f + Random.Range(-beatRandomVariance, beatRandomVariance);
        }
    }

    // ทำลาย tile ที่ขอบขวาพ้นขอบซ้ายของกล้องไปแล้ว (ทุกตัวเป็น clone ในซีน จึง Destroy ได้)
    void CullLeft(Layer layer)
    {
        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;

        for (int i = layer.tiles.Count - 1; i >= 0; i--)
        {
            Transform t = layer.tiles[i];
            if (t.position.x + layer.width * 0.5f < camLeft - margin)
            {
                layer.tiles.RemoveAt(i);
                if (i < layer.renderers.Count) layer.renderers.RemoveAt(i);
                Destroy(t.gameObject);
            }
        }
    }

    // สร้าง tile ใหม่ต่อท้ายทางขวา จนกว่าจะพ้นขอบขวาของกล้อง
    void FillRight(Layer layer)
    {
        float camRight = cam.transform.position.x + cam.orthographicSize * cam.aspect;

        while (true)
        {
            float nextX;
            if (layer.tiles.Count > 0)
            {
                float lastX = layer.tiles[layer.tiles.Count - 1].position.x;
                if (lastX + layer.width * 0.5f >= camRight + margin) break;
                nextX = lastX + layer.spacing;
            }
            else
            {
                nextX = camRight; // ไม่เหลือ tile เลย
            }

            SpawnTile(layer, nextX);
        }
    }

    void SpawnTile(Layer layer, float x)
    {
        SpriteRenderer clone = Instantiate(layer.template, layer.parent);
        clone.gameObject.SetActive(true);
        clone.name = layer.sprite.name + "_tile" + (++layer.spawnCount);
        clone.transform.position = new Vector3(x, layer.y, layer.z);
        if (layer == skyLayer) clone.sortingOrder = -1; // ท้องฟ้าอยู่หลังแสงเรือง
        layer.tiles.Add(clone.transform);
        layer.renderers.Add(clone);
    }

    // ---------- Backlight glow ----------

    /// <summary>แผ่นแสงเรืองนุ่ม ๆ ที่สาดขึ้นมาจากด้านหลังตึก (ไม่แตะสีสไปรท์BG)</summary>
    void BuildBacklight()
    {
        if (!backlightGlow || cam == null || backlightAlpha <= 0.001f) return;

        GameObject go = new GameObject("BacklightGlow");
        go.transform.SetParent(transform, false);

        backlight = go.AddComponent<SpriteRenderer>();
        backlight.sprite = CreateGlowSprite(128);
        Material unlit = CreateUnlitSpriteMaterial();
        if (unlit != null) backlight.sharedMaterial = unlit;
        backlight.sortingOrder = backlightOrder;

        backlightTarget = PickBacklightColor();
        backlightNow = backlightTarget;
        backlightTimer = Random.Range(backlightHoldMin, Mathf.Max(backlightHoldMin, backlightHoldMax));

        UpdateBacklight();
    }

    void UpdateBacklight()
    {
        if (backlight == null || cam == null) return;

        float dt = Time.deltaTime;

        // สุ่มสีใหม่เมื่อหมดเวลา แล้วค่อย ๆ เปลี่ยนไปหาสีนั้น
        backlightTimer -= dt;
        if (backlightTimer <= 0f)
        {
            backlightTarget = PickBacklightColor();
            backlightTimer = Random.Range(backlightHoldMin, Mathf.Max(backlightHoldMin, backlightHoldMax));
        }

        backlightNow = Color.Lerp(backlightNow, backlightTarget, Mathf.Clamp01(dt / Mathf.Max(0.05f, backlightFade)));

        float pulse = 1f;
        if (beatTimer > 0f && backlightBeatPulse > 0f)
            pulse += backlightBeatPulse * (beatTimer / Mathf.Max(0.05f, beatPulseDuration));

        Color c = backlightNow;
        c.a = Mathf.Clamp01(backlightAlpha * pulse);
        backlight.color = c;

        backlight.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y + backlightY, 0f);
        backlight.transform.localScale = new Vector3(backlightWidth, backlightHeight, 1f);
    }

    /// <summary>สุ่มสีถัดไป (ไม่ซ้ำสีเดิมติดกัน) ถ้าไม่ได้ใส่ palette ไว้จะใช้ backlightColor เดี่ยว ๆ</summary>
    Color PickBacklightColor()
    {
        if (backlightColors == null || backlightColors.Length == 0) return backlightColor;
        if (backlightColors.Length == 1) return backlightColors[0];

        int index = Random.Range(0, backlightColors.Length);
        if (index == lastBacklightIndex) index = (index + 1) % backlightColors.Length;
        lastBacklightIndex = index;
        return backlightColors[index];
    }

    /// <summary>สไปร์ทวงแสงนุ่ม ๆ (radial gradient) ที่เจนเองตอนรันไทม์</summary>
    Sprite CreateGlowSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    // ---------- Night ambience ----------

    /// <summary>ม่านกลางคืนเต็มจอ (สีเข้มจาง ๆ) ให้ภาพรวมดูละมุนเหมือนกลางคืน</summary>
    void BuildNightVeil()
    {
        if (cam == null || nightVeilAlpha <= 0.001f) return;

        GameObject go = new GameObject("NightVeil");
        go.transform.SetParent(transform, false);

        nightVeil = go.AddComponent<SpriteRenderer>();
        nightVeil.sprite = CreateSolidSprite();
        Material unlit = CreateUnlitSpriteMaterial();
        if (unlit != null) nightVeil.sharedMaterial = unlit;
        nightVeil.sortingOrder = nightVeilOrder;

        Color c = nightVeilColor;
        c.a = nightVeilAlpha;
        nightVeil.color = c;

        UpdateNightVeil();
    }

    void UpdateNightVeil()
    {
        if (nightVeil == null || cam == null) return;

        nightVeil.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        nightVeil.transform.localScale = new Vector3(VeilWidth(), VeilHeight(), 1f);
    }

    float VeilHeight() => cam.orthographicSize * 2f * veilMargin;
    float VeilWidth() => VeilHeight() * Mathf.Max(0.1f, cam.aspect);

    /// <summary>material สไปรท์แบบ unlit ของ URP 2D (กันกรณี default material เรนเดอร์ไม่ขึ้น)</summary>
    Material CreateUnlitSpriteMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        return shader != null ? new Material(shader) : null;
    }

    /// <summary>สไปรท์ 1x1 สีขาว ใช้ย้อมเป็นม่านเต็มจอ</summary>
    Sprite CreateSolidSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
