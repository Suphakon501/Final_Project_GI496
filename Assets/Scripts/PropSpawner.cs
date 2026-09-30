using UnityEngine;

/// <summary>
/// ข้อมูลพรอบ 1 แถวใน Inspector: ตัวพรอบ + เวลาสปอว์นของตัวนั้น
/// </summary>
[System.Serializable]
public class PropEntry
{
    [Tooltip("Prefab หรือ Sprite ที่วางในซีน (ลากจาก Hierarchy ได้)")]
    public GameObject prop;

    [Tooltip("สปอว์นพรอบตัวนี้ทุกกี่วินาที สุ่มระหว่างค่าน้อย-มาก (เช่น 2, 4) ใช้ในโหมดปกติ ไม่ใช้ในโหมด Chain")]
    public Vector2 spawnInterval = new Vector2(1f, 3f);
}

/// <summary>
/// สปอว์นพรอบแล้วให้เลื่อนไปทางซ้าย เมื่อพ้นจอจะลบทิ้งตามเวลาที่ตั้งไว้ (ตั้งเวลาสปอว์นแยกรายตัวได้)
/// มีโหมด Seamless Chain สำหรับพรอบที่ต้องต่อกันเป็นแถว
/// </summary>
public class PropSpawner : MonoBehaviour
{
    [Header("รายการพรอบ (ตั้งเวลาสปอว์นแยกแต่ละตัวได้)")]
    public PropEntry[] props;

    [Header("การลบพรอบ")]
    [Tooltip("พรอบทุกตัวที่เลยหน้าจอไปจะถูกลบ หน่วงกี่วินาทีหลังพ้นขอบซ้ายของจอ (0 = ลบทันที)")]
    public float destroyDelay = 1f;

    [Header("โหมดต่อกันเป็นแถว (ไม่มีช่องว่าง)")]
    [Tooltip("เปิด = สปอว์นชิ้นใหม่ต่อท้ายชิ้นก่อนหน้าพอดี (ไม่ใช้ตัวจับเวลา)")]
    public bool seamlessChain = false;
    [Tooltip("ให้ชิ้นซ้อนกันกี่หน่วย กลบรอยต่อ (เช่น 0.02)")]
    [Range(0f, 0.2f)] public float chainOverlap = 0.02f;
    [Tooltip("ช่องว่างระหว่างชิ้น สุ่มระหว่างค่าน้อย-มาก (0, 0 = ต่อกันติด, เช่น 1, 3 = เว้น 1-3 หน่วย)")]
    public Vector2 chainGap = Vector2.zero;
    [Tooltip("เริ่มเกมให้มีพรอบเต็มจอเลย ไม่ต้องรอให้เลื่อนเข้ามา")]
    public bool prefillScreen = true;

    [Header("ยึดตำแหน่งตามที่วางไว้")]
    [Tooltip("ใช้ค่า Y (และ Z) ตามที่วางไว้ในซีน แทนการสุ่ม")]
    public bool keepPlacedY = true;
    [Tooltip("ใช้ค่า X ตามที่วางไว้ด้วย (ถ้าปิด จะสปอว์นที่ขอบขวาของจอ) ไม่มีผลในโหมด Chain")]
    public bool keepPlacedX = false;
    [Tooltip("ตัวที่วางในซีนให้เลื่อนตั้งแต่เริ่มเกมเลย ณ ตำแหน่งที่วางไว้")]
    public bool moveSceneObjectsAtStart = true;

    [Header("ความเร็ว")]
    [Tooltip("ถ้าใส่ไว้ พรอบจะใช้ความเร็วเดียวกับพื้นหลัง (baseSpeed x ตัวคูณด้านล่าง)")]
    public ScrollingBackground background;
    public float speedMultiplier = 1f;
    [Tooltip("ใช้เมื่อไม่ได้ใส่ background")]
    public float manualSpeed = 2f;

    [Header("การสปอว์น")]
    [Tooltip("ใช้เมื่อปิด Keep Placed Y")]
    public float minY = -3f;
    public float maxY = 3f;
    [Tooltip("ระยะจากขอบขวาของจอ ถึงขอบซ้ายของพรอบ ตอนสปอว์น (ค่ามากขึ้น = โผล่ไกลจากจอขึ้น)")]
    public float spawnOffsetX = 2f;
    public Transform parentForProps;

    [Header("เลเยอร์การแสดงผล")]
    [Tooltip("ชื่อ Sorting Layer (เว้นว่าง = ใช้ตามที่ตั้งไว้ในตัวพรอบ)")]
    public string sortingLayerName = "";
    public int sortingOrder = 0;

    Camera cam;
    GameObject[] templates;
    GameObject lastProp;
    bool chainStarted;
    float[] timers;

    void Start()
    {
        cam = Camera.main;
        if (cam == null || !cam.orthographic)
            Debug.LogWarning("PropSpawner: ต้องมีกล้องที่ tag เป็น MainCamera และตั้ง Projection = Orthographic ไม่งั้นตำแหน่งสปอว์นจะผิด");

        BuildTemplates();
        timers = new float[props.Length];
        for (int i = 0; i < timers.Length; i++) ResetTimer(i);
    }

    float ResolveDelay(int index)
    {
        return destroyDelay;
    }

    void BuildTemplates()
    {
        templates = new GameObject[props.Length];

        for (int i = 0; i < props.Length; i++)
        {
            GameObject src = props[i] != null ? props[i].prop : null;
            if (src == null) continue;

            // ตัวที่วางอยู่ในซีน (ไม่ใช่ Prefab ในโฟลเดอร์ Project)
            if (src.scene.IsValid())
            {
                GameObject t = Instantiate(src, src.transform.position, src.transform.rotation);
                t.name = src.name + "_template";
                t.SetActive(false);
                templates[i] = t;

                if (moveSceneObjectsAtStart)
                {
                    ApplySorting(src);
                    AttachMover(src, ResolveDelay(i));
                    if (seamlessChain) lastProp = src;
                }
                else
                {
                    src.SetActive(false);
                }
            }
            else
            {
                templates[i] = src;
            }
        }
    }

    void Update()
    {
        if (templates == null || templates.Length == 0) return;

        if (seamlessChain)
        {
            UpdateChain();
            return;
        }

        // แต่ละพรอบนับเวลาสปอว์นของตัวเอง
        for (int i = 0; i < templates.Length; i++)
        {
            if (templates[i] == null) continue;

            timers[i] -= Time.deltaTime;
            if (timers[i] <= 0f)
            {
                Spawn(false, i);
                ResetTimer(i);
            }
        }
    }

    void UpdateChain()
    {
        float camRight = cam.transform.position.x + cam.orthographicSize * cam.aspect;
        float limit = camRight + spawnOffsetX;

        for (int guard = 0; guard < 100; guard++)
        {
            if (lastProp != null && GetBounds(lastProp).max.x > limit) break;

            GameObject p = Spawn(true, Random.Range(0, templates.Length));
            if (p == null) break;
            lastProp = p;
        }
    }

    void ResetTimer(int index)
    {
        Vector2 iv = props[index].spawnInterval;
        timers[index] = Random.Range(Mathf.Min(iv.x, iv.y), Mathf.Max(iv.x, iv.y));
    }

    GameObject Spawn(bool chain, int index)
    {
        GameObject template = templates[index];
        if (template == null) return null;

        Vector3 placed = template.transform.position;
        float camRight = cam.transform.position.x + cam.orthographicSize * cam.aspect;

        float x = keepPlacedX ? placed.x : camRight + spawnOffsetX;
        float y = keepPlacedY ? placed.y : Random.Range(minY, maxY);

        GameObject prop = Instantiate(template, new Vector3(x, y, placed.z),
                                      template.transform.rotation, parentForProps);
        prop.name = template.name.Replace("_template", "");
        prop.SetActive(true);

        if (chain)
        {
            float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
            float targetLeft;

            if (lastProp != null)
                targetLeft = GetBounds(lastProp).max.x - chainOverlap
                             + Random.Range(chainGap.x, chainGap.y);
            else if (prefillScreen && !chainStarted)
                targetLeft = camLeft;
            else
                targetLeft = camRight + spawnOffsetX;

            chainStarted = true;
            float shift = targetLeft - GetBounds(prop).min.x;
            prop.transform.position += Vector3.right * shift;
        }
        else if (!keepPlacedX)
        {
            float shift = (camRight + spawnOffsetX) - GetBounds(prop).min.x;
            prop.transform.position += Vector3.right * shift;
        }

        ApplySorting(prop);
        AttachMover(prop, ResolveDelay(index));
        return prop;
    }

    void ApplySorting(GameObject prop)
    {
        if (string.IsNullOrEmpty(sortingLayerName)) return;

        foreach (var sr in prop.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;
        }
    }

    void AttachMover(GameObject prop, float delay)
    {
        PropMover mover = prop.AddComponent<PropMover>();
        mover.destroyDelay = delay;
        mover.getSpeed = GetSpeed;
    }

    float GetSpeed()
    {
        return background != null ? background.baseSpeed * speedMultiplier : manualSpeed;
    }

    public static Bounds GetBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.01f);

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}

/// <summary>
/// เลื่อนพรอบไปทางซ้าย และลบทิ้งเมื่อพ้นขอบซ้ายของจอไปครบเวลาที่ตั้ง
/// (ถูกเพิ่มให้พรอบอัตโนมัติโดย PropSpawner)
/// </summary>
public class PropMover : MonoBehaviour
{
    [HideInInspector] public float destroyDelay = 1f;
    public System.Func<float> getSpeed;

    Camera cam;
    float offscreenTimer;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        float speed = getSpeed != null ? getSpeed() : 0f;
        transform.position += Vector3.left * speed * Time.deltaTime;

        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        float propRight = PropSpawner.GetBounds(gameObject).max.x;

        if (propRight < camLeft)
        {
            offscreenTimer += Time.deltaTime;
            if (offscreenTimer >= destroyDelay)
                Destroy(gameObject);
        }
    }
}