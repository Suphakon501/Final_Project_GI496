using UnityEngine;

[System.Serializable]
public class PropEntry
{
    public GameObject prop;

    public Vector2 spawnInterval = new Vector2(1f, 3f);
}

public class PropSpawner : MonoBehaviour
{
    [Header("Props")]
    public PropEntry[] props;

    [Header("Cleanup")]
    public float destroyDelay = 1f;

    [Header("Chain Mode")]
    public bool seamlessChain = false;
    [Range(0f, 0.2f)] public float chainOverlap = 0.02f;
    public Vector2 chainGap = Vector2.zero;
    public bool prefillScreen = true;

    [Header("Placement")]
    public bool keepPlacedY = true;
    public bool keepPlacedX = false;
    public bool moveSceneObjectsAtStart = true;

    [Header("Speed")]
    public ScrollingBackground background;
    public float speedMultiplier = 1f;
    public float manualSpeed = 2f;

    [Header("Spawning")]
    public float minY = -3f;
    public float maxY = 3f;
    public float spawnOffsetX = 2f;
    public Transform parentForProps;

    [Header("Sorting")]
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
            Debug.LogWarning("PropSpawner: requires an orthographic camera tagged MainCamera");

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
