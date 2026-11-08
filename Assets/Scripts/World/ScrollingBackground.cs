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
        [Tooltip("ให้เลเยอร์นี้เด้งตามบีตหรือไม่ (เปิดเฉพาะเลเยอร์ตึก/เมือง/ฝูงคน)")]
        public bool pulseOnBeat = false;
        [Tooltip("แรงเด้งของเลเยอร์นี้เทียบกับค่าหลัก (0.5 = ครึ่งเดียว = นุ่มกว่า)")]
        [Range(0f, 2f)] public float pulseAmount = 1f;
        [Tooltip("การยืด/หดตัวของเลเยอร์นี้ (0 = ไม่เปลี่ยนขนาดเลย — เหมาะกับภาพยาวที่มีรอยต่อ tile)")]
        [Range(0f, 2f)] public float pulseScale = 1f;
        [Tooltip("ความหน่วงก่อนเริ่มเด้ง (วินาที) — ใส่ค่าต่างกันให้แต่ละชั้นไม่ขยับพร้อมกัน")]
        [Min(0f)] public float pulseDelay = 0f;
        [Tooltip("ความนุ่ม: 0 = กระตุกแบบสปริงเดิม, 1 = ยกขึ้นแล้วผ่อนลงเรียบ ไม่สะบัดกลับ")]
        [Range(0f, 1f)] public float pulseSoftness = 0f;
        [Tooltip("ความยาวการเด้งหนึ่งครั้งของชั้นนี้ (0 = ใช้ค่าหลัก)")]
        [Min(0f)] public float pulseDuration = 0f;

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
        [System.NonSerialized] public float pulseElapsed;  // เวลาที่ผ่านไปของการเด้งรอบล่าสุด (ติดลบ = ยังอยู่ในช่วงหน่วง)
        [System.NonSerialized] public float pulseStrength; // แรงของบีตที่สั่งให้ชั้นนี้เด้งรอบล่าสุด
        [HideInInspector] public Color baseColor = Color.white;
        [HideInInspector] public List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    }

    [Header("Speed")]
    public float baseSpeed = 2f;

    [Header("Beat Pulse (เด้งตาม BPM)")]
    [SerializeField] private bool beatPulse = true;
    [Tooltip("พิมพ์สรุปการตั้งค่า pulse ลง Console ครั้งเดียวตอนเริ่ม (ใช้ตรวจว่าฝูงคนถูกนับเป็นเลเยอร์เด้งจริงหรือไม่)")]
    [SerializeField] private bool logPulseDiagnostics = true;
    [Tooltip("ถ้าพบ SpriteRenderer ชื่อขึ้นต้นด้วย Crowd ที่ยังไม่อยู่ใน layers ให้ลงทะเบียนเป็นเลเยอร์เด้งให้อัตโนมัติ (กันลิสต์หลุด)")]
    [SerializeField] private bool autoRegisterCrowdLayers = true;
    [Tooltip("BPM ที่ใช้เด้งแทน เมื่อไม่พบ BeatManager ในฉาก")]
    [SerializeField, Min(1f)] private float fallbackBpm = 120f;
    [Tooltip("ขนาดการเด้ง (0.03 = 3%)")]
    [SerializeField, Min(0f)] private float beatScaleAmount = 0.035f;
    [Tooltip("ระยะที่พื้นหลังกระตุกขึ้น (หน่วย)")]
    [SerializeField, Min(0f)] private float beatBounceHeight = 0.08f;
    [Tooltip("ระยะเวลาทั้งหมดของการเด้งหนึ่งครั้ง")]
    [SerializeField, Min(0.05f)] private float beatPulseDuration = 0.34f;
    [Tooltip("เวลาที่ใช้พุ่งขึ้นถึงจุดสูงสุด (สั้น = กระตุกแรง)")]
    [SerializeField, Min(0.01f)] private float beatAttackTime = 0.05f;
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
    [Header("Glow Core (จุดสว่างจ้าเพิ่มความเรืองแสง)")]
    [Tooltip("เพิ่มจุดแสงจ้าเล็ก ๆ ซ้อนกลางแสงหลัก ให้ดูเรืองแสงขึ้น")]
    [SerializeField] private bool addBacklightCore = true;
    [Tooltip("ความสว่างของจุดแสงจ้าเทียบกับแสงหลัก")]
    [SerializeField, Range(0f, 1f)] private float backlightCoreAlpha = 0.85f;
    [Tooltip("ขนาดจุดแสงจ้าเทียบกับแสงหลัก (1 = เท่ากัน)")]
    [SerializeField, Range(0.1f, 1.5f)] private float backlightCoreScale = 0.45f;
    [Tooltip("ผสมสีขาวเข้าจุดแสงจ้า (ยิ่งมากยิ่งจ้า)")]
    [SerializeField, Range(0f, 1f)] private float backlightCoreWhiten = 0.55f;

    [Header("Night Ambience (บรรยากาศกลางคืน)")]
    [Tooltip("ความมืดของ BG (1 = ปกติ, 0.55 = กลางคืน) — ไม่เปลี่ยนสี สีเดิมยังอยู่")]
    [SerializeField, Range(0.2f, 1f)] private float sceneDarkness = 0.55f;
    [Tooltip("สีของม่านกลางคืน (โทนน้ำเงินเข้มมาก เกือบดำ)")]
    [SerializeField] private Color nightVeilColor = new Color(0.02f, 0.03f, 0.09f);
    [Tooltip("ความทึบของม่าน (ยิ่งมากยิ่งมืด)")]
    [SerializeField, Range(0f, 0.7f)] private float nightVeilAlpha = 0.38f;
    [Tooltip("ขนาดม่านเกินขอบจอเท่าไร (กันขอบโผล่)")]
    [SerializeField, Min(1f)] private float veilMargin = 1.2f;

    Camera cam;
    private int lastBeatIndex = int.MinValue;
    private bool warnedNoBeatManager;
    private float beatTimer;
    private float beatStrength = 1f;
    private SpriteRenderer nightVeil;
    private SpriteRenderer backlight;
    private SpriteRenderer backlightCore;
    private int skyOrder = -3;
    private int veilOrder = -2;
    private int glowOrder = -1;
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

        layers = AutoAppendCrowdLayers(layers);

        // ต้องรู้ลำดับก่อนสร้าง tile จะได้วางฟ้าไว้หลังสุด
        ResolveSortOrders();

        foreach (var layer in layers)
            BuildLayer(layer);

        LogPulseSetup();

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

        foreach (var layer in layers)
        {
            float curve = 0f;
            if (layer.pulseOnBeat)
            {
                layer.pulseElapsed += Time.deltaTime;
                curve = LayerBeatCurve(layer);
            }

            if (layer.template == null) continue;

            float bob = beatBounceHeight * curve;
            float scaleMul = 1f + beatScaleAmount * curve * layer.pulseScale;
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

    /// <summary>
    /// เส้นโค้งการเด้งของเลเยอร์: ผสมระหว่างสปริงกระตุก (ตึก) กับโค้งนุ่มขึ้น-ลงเรียบ (ฝูงคน)
    /// แต่ละชั้นมีเวลาเด้งของตัวเอง จึงหน่วง/นุ่มได้ไม่เท่ากัน
    /// </summary>
    float LayerBeatCurve(Layer layer)
    {
        if (!layer.pulseOnBeat) return 0f;

        float duration = layer.pulseDuration > 0.001f ? layer.pulseDuration : Mathf.Max(0.05f, beatPulseDuration);
        float elapsed = layer.pulseElapsed;
        if (elapsed <= 0f || elapsed >= duration) return 0f;

        // โค้งกระตุกแบบเดิม: smoothstep สั้น ๆ ขึ้นเร็ว แล้วสปริงหน่วง (เด้งกลับต่ำกว่าเดิมนิดหน่อย)
        float attackT = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, beatAttackTime));
        float attack = attackT * attackT * (3f - 2f * attackT);
        float decayT = Mathf.Max(0f, elapsed - beatAttackTime);
        float settle = Mathf.Exp(-8.5f * decayT) * Mathf.Cos(decayT * 15f);
        float snap = attack * settle;

        // โค้งนุ่ม: ยกขึ้นเร็ว (peak ราว 40% ของช่วง) แล้วผ่อนลงจบที่เส้นเดิมพอดี ไม่สะบัดกลับ
        float u = Mathf.Clamp01(elapsed / duration);
        float soft = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.8f));

        float curve = Mathf.Lerp(snap, soft, Mathf.Clamp01(layer.pulseSoftness));
        return curve * layer.pulseStrength * Mathf.Max(0f, layer.pulseAmount);
    }

    /// <summary>คำนวณการเด้งของพื้นหลังตามbeatเพลง (อ่านจาก BeatManager)</summary>
    void UpdateBeatPulse(float deltaTime)
    {
        if (beatTimer > 0f) beatTimer = Mathf.Max(0f, beatTimer - deltaTime);

        if (!beatPulse) return;

        BeatManager beat = BeatManager.instance;
        if (beat == null && !warnedNoBeatManager)
        {
            warnedNoBeatManager = true;
            Debug.LogWarning($"[ScrollingBackground] ไม่พบ BeatManager — ใช้ BPM สำรอง {fallbackBpm} ในการเด้ง");
        }

        // ถ้าไม่มี BeatManager ให้เดินนาฬิกาเองด้วย BPM สำรอง (BG จะได้ไม่่นิ่ง)
        float beats = beat != null ? beat.songPositionInBeats : Time.time / (60f / Mathf.Max(1f, fallbackBpm));
        int beatIndex = Mathf.FloorToInt(beats);
        if (beatIndex == lastBeatIndex) return;
        lastBeatIndex = beatIndex;
        if (beatIndex < 0) return;

        bool strong = strongBeatEvery > 1 && beatIndex % strongBeatEvery == 0;
        beatStrength = strong ? strongBeatMultiplier : 1f;
        beatTimer = Mathf.Max(0.05f, beatPulseDuration);

        // สุ่มแรงของแต่ละบีต/แต่ละชั้นให้ไม่ซ้ำเดิม ดูเป็นธรรมชาติ
        // แต่ละชั้นเริ่มเด้งไม่พร้อมกันด้วย pulseDelay ของตัวเอง (ชั้นไกลตามหลังชั้นหน้าเล็กน้อย)
        foreach (var layer in layers)
        {
            if (!layer.pulseOnBeat) continue;

            layer.beatJitter = 1f + Random.Range(-beatRandomVariance, beatRandomVariance);
            layer.pulseStrength = beatStrength * layer.beatJitter;
            layer.pulseElapsed = -Mathf.Max(0f, layer.pulseDelay);
        }
    }

    /// <summary>
    /// ถ้าในซีนมี SpriteRenderer ชื่อขึ้นต้นด้วย "Crowd" ที่ยังไม่ถูกใช้เป็นเลเยอร์ ให้ลงทะเบียนเป็นเลเยอร์ BG อัตโนมัติ
    /// ตำแหน่ง/สเกลใช้ค่าของ transform เดิมทุกอย่าง (ไม่แก้) — กันกรณีลิสต์ layers ในซีนหลุด/ยังไม่โหลดใหม่
    /// </summary>
    Layer[] AutoAppendCrowdLayers(Layer[] src)
    {
        if (!autoRegisterCrowdLayers || cam == null) return src;

        List<Layer> list = new List<Layer>();
        HashSet<SpriteRenderer> used = new HashSet<SpriteRenderer>();

        if (src != null)
        {
            foreach (var l in src)
            {
                if (l == null) continue;
                list.Add(l);
                if (l.sprite != null) used.Add(l.sprite);
            }
        }

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.sprite == null || used.Contains(sr)) continue;
            if (!sr.name.StartsWith("Crowd", System.StringComparison.OrdinalIgnoreCase)) continue;

            bool front = sr.name.IndexOf("Front", System.StringComparison.OrdinalIgnoreCase) >= 0;
            Layer layer = new Layer
            {
                name = sr.name,
                sprite = sr,
                speedMultiplier = 0.72f,
                overlap = front ? 1.5f : 0.6f,
                pulseOnBeat = true,
                pulseAmount = front ? 2.6f : 2f,
                pulseScale = 0f,
                pulseDelay = front ? 0f : 0.04f,
                pulseSoftness = front ? 0.7f : 0.55f,
                pulseDuration = front ? 0.22f : 0.16f
            };

            list.Add(layer);
            used.Add(sr);
            Debug.Log($"[ScrollingBackground] auto-register '{sr.name}' (y {sr.transform.position.y:0.##}, scale {sr.transform.lossyScale.x:0.###})");
        }

        return list.ToArray();
    }

    /// <summary>สรุปการตั้งค่า pulse ลง Console ครั้งเดียว — ช่วยรู้ทันทีว่าฝูงคนถูกนับเป็นเลเยอร์เด้งหรือไม่</summary>
    void LogPulseSetup()
    {
        if (!logPulseDiagnostics) return;

        var sb = new System.Text.StringBuilder("[ScrollingBackground] ");
        sb.Append(BeatManager.instance == null ? "BeatManager = NOT FOUND (BG จะไม่เด้งเลย)" : "BeatManager = ok");
        if (layers != null)
        {
            foreach (var l in layers)
            {
                if (l == null || !l.pulseOnBeat) continue;
                float dur = l.pulseDuration > 0.001f ? l.pulseDuration : beatPulseDuration;
                sb.Append($" | {l.name}: tiles={l.tiles.Count} amount={l.pulseAmount:0.##} dur={dur:0.##}s");
            }
        }
        Debug.Log(sb.ToString());
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
        if (layer == skyLayer) clone.sortingOrder = skyOrder; // ท้องฟ้าอยู่หลังแสงเรือง
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
        backlight.sortingOrder = glowOrder;

        // จุดแสงจ้าซ้อนกลางแสงหลัก (อยู่บนแสงหลัก แต่ยังใต้ตึก)
        if (addBacklightCore)
        {
            GameObject coreGo = new GameObject("BacklightCore");
            coreGo.transform.SetParent(transform, false);

            backlightCore = coreGo.AddComponent<SpriteRenderer>();
            backlightCore.sprite = backlight.sprite;
            if (unlit != null) backlightCore.sharedMaterial = unlit;
            backlightCore.sortingOrder = glowOrder;
        }

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

        Vector3 center = new Vector3(cam.transform.position.x, cam.transform.position.y + backlightY, 0f);

        Color c = backlightNow;
        c.a = Mathf.Clamp01(backlightAlpha * pulse);
        backlight.color = c;
        backlight.transform.position = center;
        backlight.transform.localScale = new Vector3(backlightWidth, backlightHeight, 1f);

        if (backlightCore == null) return;

        // จุดสว่างจ้า: เล็กกว่า ขาวกว่า และเต้นตามบีตแรงกว่าแสงหลัก
        float corePulse = 1f;
        if (beatTimer > 0f)
            corePulse += (backlightBeatPulse + 0.12f) * (beatTimer / Mathf.Max(0.05f, beatPulseDuration));

        Color coreColor = Color.Lerp(backlightNow, Color.white, backlightCoreWhiten);
        coreColor.a = Mathf.Clamp01(backlightAlpha * backlightCoreAlpha * corePulse);
        backlightCore.color = coreColor;

        backlightCore.transform.position = center + new Vector3(0f, -backlightHeight * 0.05f, -0.01f);
        backlightCore.transform.localScale = new Vector3(
            backlightWidth * backlightCoreScale,
            backlightHeight * backlightCoreScale * Mathf.Lerp(1f, 1.2f, pulse - 1f),
            1f);
    }

    /// <summary>
    /// วางลำดับการวาดให้อัตโนมัติจากหลังไปหน้า: ท้องฟ้า → ม่านกลางคืน → แสงเรือง → ตึก
    /// ทำให้แสงเรืองไม่ถูกม่านกลางคืนคลุมจนมืด
    /// </summary>
    void ResolveSortOrders()
    {
        int lowest = int.MaxValue;

        if (layers != null)
        {
            foreach (var layer in layers)
            {
                if (layer == null || layer == skyLayer || layer.sprite == null) continue;
                lowest = Mathf.Min(lowest, layer.sprite.sortingOrder);
            }
        }

        if (lowest == int.MaxValue) lowest = 10;

        skyOrder = lowest - 3;
        veilOrder = lowest - 2;
        glowOrder = lowest - 1;
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
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(a, 1.7f)));
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
        nightVeil.sortingOrder = veilOrder;

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
