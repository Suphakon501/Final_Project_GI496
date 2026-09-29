using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// แสดงตัวเลขด้วย sprite แทน Text: เรียก SetValue(1234) แล้วมันสร้าง/เรียงรูปตัวเลขให้เอง ไม่ต้องจัดมือ
// แปะบน GameObject เปล่าใน Canvas แล้วลาก sprite 0-9 ใส่ตามลำดับ
// รูปทุกตัวต้องสูงเท่ากัน (ครอปมาให้แล้วใน Assets/Sprites/Ui/Number) เส้นฐานจะได้ตรงกัน
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SpriteNumber : MonoBehaviour
{
    public enum Align { Left, Center, Right }

    [Header("Sprites")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10]; // ช่อง 0-9 ตามลำดับ
    [SerializeField] private Sprite commaSprite;

    [Header("Layout")]
    [SerializeField] private float digitHeight = 60f;        // ความสูงตัวเลข (หน่วยเดียวกับ UI)
    [SerializeField] private float spacing = -6f;            // ระยะห่างระหว่างตัว (ติดลบ = ชิดกันขึ้น)
    [SerializeField] private Align alignment = Align.Right;  // ชิดขอบไหนของกรอบ RectTransform
    [SerializeField] private bool useThousandsSeparator = true; // 12,345
    [SerializeField] private Color color = Color.white;      // ขาว = สีเดิมของรูป

    [Header("เด้งตอนค่าเปลี่ยน")]
    [SerializeField] private float popScale = 1.15f;         // 1 = ไม่เด้ง
    [SerializeField] private float popShrinkSpeed = 12f;

    [Header("ค่าที่โชว์ใน Editor ตอนยังไม่กด Play")]
    [SerializeField] private int previewValue = 12345;

    const string DigitObjectName = "Digit";

    private readonly List<Image> pool = new List<Image>();
    private int currentValue = int.MinValue;
    private bool dirty = true;

    public int Value => currentValue;

    void OnEnable()
    {
        CollectPool();
        dirty = true;
    }

    void OnValidate()
    {
        dirty = true; // ห้ามสร้าง GameObject ใน OnValidate เลยไปทำใน Update แทน
    }

    void OnRectTransformDimensionsChange()
    {
        dirty = true; // ย่อ/ขยายกรอบแล้วจัดตำแหน่งใหม่
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            if (dirty) Render(previewValue);
            return;
        }

        if (dirty && currentValue != int.MinValue) Render(currentValue);

        if (transform.localScale != Vector3.one)
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, popShrinkSpeed * Time.deltaTime);
    }

    public void SetValue(int value, bool pop = true)
    {
        if (value == currentValue) return;
        bool firstTime = currentValue == int.MinValue;
        currentValue = value;
        Render(value);

        if (pop && !firstTime && Application.isPlaying && popScale != 1f)
            transform.localScale = Vector3.one * popScale;
    }

    void Render(int value)
    {
        dirty = false;
        string text = useThousandsSeparator
            ? value.ToString("N0", CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);

        // เลือก sprite ของแต่ละตัวอักษร (ข้ามตัวที่ไม่มีรูป)
        var sprites = new List<Sprite>(text.Length);
        foreach (char c in text)
        {
            Sprite s = null;
            if (c >= '0' && c <= '9' && digitSprites != null && digitSprites.Length > c - '0') s = digitSprites[c - '0'];
            else if (c == ',') s = commaSprite;
            if (s != null) sprites.Add(s);
        }

        // ความกว้างแต่ละตัวตามสัดส่วนรูป (เลข 1 แคบกว่าเลข 0)
        float totalWidth = 0f;
        var widths = new float[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            widths[i] = digitHeight * sprites[i].rect.width / sprites[i].rect.height;
            totalWidth += widths[i];
        }
        if (sprites.Count > 1) totalWidth += spacing * (sprites.Count - 1);

        var rt = (RectTransform)transform;
        Rect r = rt.rect; // พิกัดเทียบกับ pivot ของกรอบ
        float x = alignment == Align.Left ? r.xMin
                : alignment == Align.Right ? r.xMax - totalWidth
                : r.center.x - totalWidth * 0.5f;

        while (pool.Count < sprites.Count) pool.Add(CreateDigitImage());

        for (int i = 0; i < pool.Count; i++)
        {
            var img = pool[i];
            bool used = i < sprites.Count;
            if (img.gameObject.activeSelf != used) img.gameObject.SetActive(used);
            if (!used) continue;

            img.sprite = sprites[i];
            img.color = color;

            var irt = img.rectTransform;
            irt.anchorMin = irt.anchorMax = rt.pivot; // ให้ anchoredPosition ใช้พิกัดเดียวกับ rt.rect
            irt.pivot = new Vector2(0f, 0.5f);
            irt.sizeDelta = new Vector2(widths[i], digitHeight);
            irt.anchoredPosition = new Vector2(x, r.center.y);
            x += widths[i] + spacing;
        }
    }

    void CollectPool()
    {
        pool.Clear();
        foreach (Transform child in transform)
        {
            if (child.name != DigitObjectName) continue;
            var img = child.GetComponent<Image>();
            if (img != null) pool.Add(img);
        }
    }

    Image CreateDigitImage()
    {
        var go = new GameObject(DigitObjectName, typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }
}
