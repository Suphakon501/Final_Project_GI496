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

        [HideInInspector] public List<Transform> tiles = new List<Transform>();
        [HideInInspector] public SpriteRenderer template; // ตัว clone ในซีนที่ซ่อนไว้ ใช้สร้าง tile
        [HideInInspector] public Transform parent;
        [HideInInspector] public float width;
        [HideInInspector] public float spacing;
        [HideInInspector] public float y;
        [HideInInspector] public float z;
        [HideInInspector] public int spawnCount;
    }

    [Header("Speed")]
    public float baseSpeed = 2f;

    [Header("Layers (far to near)")]
    public Layer[] layers;

    [Header("Culling")]
    [Tooltip("ระยะเผื่อนอกขอบกล้อง ก่อนทำลาย/สร้าง tile (กันภาพกระตุกที่ขอบจอ)")]
    public float margin = 1f;
    [Tooltip("ปิดไว้ = tile ที่หลุดจอจะหายไปเฉย ๆ ไม่สร้างใหม่ (พื้นหลังจะหมด)")]
    public bool respawn = true;

    Camera cam;

    void Start()
    {
        cam = Camera.main;
        foreach (var layer in layers)
            BuildLayer(layer);
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
        foreach (var layer in layers)
        {
            if (layer.template == null) continue;

            float move = baseSpeed * layer.speedMultiplier * Time.deltaTime;

            foreach (var t in layer.tiles)
            {
                Vector3 p = t.position;
                p.x -= move;
                t.position = p;
            }

            CullLeft(layer);
            if (respawn) FillRight(layer);
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
        layer.tiles.Add(clone.transform);
    }
}