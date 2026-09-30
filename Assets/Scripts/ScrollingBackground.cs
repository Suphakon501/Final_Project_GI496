using UnityEngine;

/// <summary>
/// พื้นหลังเลื่อนจากขวาไปซ้ายแบบวนลูป (Parallax) แบบไม่มีรอยต่อ
/// - ปรับความเร็วรวม (baseSpeed) และความเร็วแต่ละเลเยอร์ (speedMultiplier) ได้
/// - ตำแหน่งทุกแผ่นคำนวณจากค่า offset ค่าเดียว จึงไม่คลาดสะสม
/// - มี Overlap ให้แผ่นภาพซ้อนกันเล็กน้อย กลบรอยต่อ
/// </summary>
public class ScrollingBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "Layer";
        [Tooltip("SpriteRenderer ของภาพพื้นหลังเลเยอร์นี้ (pivot ควรอยู่กึ่งกลาง)")]
        public SpriteRenderer sprite;
        [Tooltip("ตัวคูณความเร็ว: ไกล = น้อย (เช่น 0.3), ใกล้ = มาก (เช่น 1)")]
        [Range(0f, 3f)] public float speedMultiplier = 1f;
        [Tooltip("ขยับตำแหน่งเลเยอร์ตอนเริ่มเกม (X, Y)")]
        public Vector2 positionOffset = Vector2.zero;
        [Tooltip("ให้แผ่นภาพซ้อนกันกี่หน่วย (กลบรอยต่อ) ถ้ายังเห็นรอย ให้เพิ่มทีละนิด เช่น 0.02")]
        [Range(0f, 0.2f)] public float overlap = 0.02f;

        [HideInInspector] public Transform[] tiles;
        [HideInInspector] public float spacing;
        [HideInInspector] public float originX;
        [HideInInspector] public float y;
        [HideInInspector] public float z;
        [HideInInspector] public float offset;
    }

    [Header("ความเร็วรวม")]
    public float baseSpeed = 2f;

    [Header("เลเยอร์ (เรียงจากไกล -> ใกล้)")]
    public Layer[] layers;

    Camera cam;

    void Start()
    {
        cam = Camera.main;
        foreach (var layer in layers)
            BuildLayer(layer);
    }

    void BuildLayer(Layer layer)
    {
        if (layer.sprite == null) return;

        Transform st = layer.sprite.transform;
        st.position += (Vector3)layer.positionOffset;

        float width = layer.sprite.bounds.size.x;
        layer.spacing = Mathf.Max(0.01f, width - layer.overlap);
        layer.y = st.position.y;
        layer.z = st.position.z;

        float camHalfW = cam.orthographicSize * cam.aspect;
        float camLeft = cam.transform.position.x - camHalfW;
        float camRight = cam.transform.position.x + camHalfW;

        // เลื่อนจุดเริ่มไปทางซ้ายทีละ 1 แผ่น จนขอบซ้ายพ้นจอ (คงตำแหน่งที่วางไว้)
        float startX = st.position.x;
        int shift = Mathf.Max(0, Mathf.CeilToInt((startX - width * 0.5f - camLeft) / layer.spacing));
        layer.originX = startX - shift * layer.spacing;

        int count = Mathf.CeilToInt((camRight - (layer.originX - width * 0.5f)) / layer.spacing) + 2;

        layer.tiles = new Transform[count];
        layer.tiles[0] = st;
        for (int i = 1; i < count; i++)
        {
            SpriteRenderer clone = Instantiate(layer.sprite, st.position, st.rotation, st.parent);
            clone.name = layer.sprite.name + "_tile" + i;
            layer.tiles[i] = clone.transform;
        }

        PositionTiles(layer);
    }

    void Update()
    {
        foreach (var layer in layers)
        {
            if (layer.tiles == null) continue;

            float move = baseSpeed * layer.speedMultiplier * Time.deltaTime;
            layer.offset = Mathf.Repeat(layer.offset + move, layer.spacing);
            PositionTiles(layer);
        }
    }

    void PositionTiles(Layer layer)
    {
        for (int i = 0; i < layer.tiles.Length; i++)
        {
            layer.tiles[i].position = new Vector3(
                layer.originX + i * layer.spacing - layer.offset,
                layer.y,
                layer.z);
        }
    }
}