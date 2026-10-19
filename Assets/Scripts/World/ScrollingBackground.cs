using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "Layer";
        public SpriteRenderer sprite;
        [Range(0f, 3f)] public float speedMultiplier = 1f;
        public Vector2 positionOffset = Vector2.zero;
        [Range(0f, 0.2f)] public float overlap = 0.02f;

        [HideInInspector] public Transform[] tiles;
        [HideInInspector] public float spacing;
        [HideInInspector] public float originX;
        [HideInInspector] public float y;
        [HideInInspector] public float z;
        [HideInInspector] public float offset;
    }

    [Header("Speed")]
    public float baseSpeed = 2f;

    [Header("Layers (far to near)")]
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
