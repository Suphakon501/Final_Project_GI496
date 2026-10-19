using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SpriteNumber : MonoBehaviour
{
    public enum Align { Left, Center, Right }

    [Header("Sprites")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [SerializeField] private Sprite commaSprite;

    [Header("Layout")]
    [SerializeField] private float digitHeight = 60f;
    [SerializeField] private float spacing = -6f;
    [SerializeField] private Align alignment = Align.Right;
    [SerializeField] private bool useThousandsSeparator = true;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private Sprite suffixSprite;
    [SerializeField] private float suffixHeight = 44f;
    [SerializeField] private float suffixSpacing = 10f;

    [Header("Pop On Change")]
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float popShrinkSpeed = 12f;

    [Header("Editor Preview")]
    [SerializeField] private int previewValue = 12345;

    const string DigitObjectName = "Digit";
    const string SuffixObjectName = "Suffix";

    private readonly List<Image> pool = new List<Image>();
    private Image suffixImage;
    private int currentValue = int.MinValue;
    private bool dirty = true;

    public int Value => currentValue;

    void OnEnable()
    {
        if (suffixSprite == null && gameObject.name == "ComboNumber")
            suffixSprite = Resources.Load<Sprite>("ComboX");
        CollectPool();
        dirty = true;
    }

    void OnValidate()
    {
        dirty = true;
    }

    void OnRectTransformDimensionsChange()
    {
        dirty = true;
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

        var sprites = new List<Sprite>(text.Length);
        foreach (char c in text)
        {
            Sprite s = null;
            if (c >= '0' && c <= '9' && digitSprites != null && digitSprites.Length > c - '0') s = digitSprites[c - '0'];
            else if (c == ',') s = commaSprite;
            if (s != null) sprites.Add(s);
        }

        float totalWidth = 0f;
        var widths = new float[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            widths[i] = digitHeight * sprites[i].rect.width / sprites[i].rect.height;
            totalWidth += widths[i];
        }
        if (sprites.Count > 1) totalWidth += spacing * (sprites.Count - 1);
        float suffixWidth = suffixSprite != null ? suffixHeight * suffixSprite.rect.width / suffixSprite.rect.height : 0f;
        if (sprites.Count > 0 && suffixSprite != null) totalWidth += suffixSpacing + suffixWidth;

        var rt = (RectTransform)transform;
        Rect r = rt.rect;
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
            irt.anchorMin = irt.anchorMax = rt.pivot;
            irt.pivot = new Vector2(0f, 0.5f);
            irt.sizeDelta = new Vector2(widths[i], digitHeight);
            irt.anchoredPosition = new Vector2(x, r.center.y);
            x += widths[i] + spacing;
        }

        if (suffixSprite != null)
        {
            if (suffixImage == null) suffixImage = CreateSuffixImage();
            bool showSuffix = sprites.Count > 0;
            if (suffixImage.gameObject.activeSelf != showSuffix) suffixImage.gameObject.SetActive(showSuffix);
            if (showSuffix)
            {
                suffixImage.sprite = suffixSprite;
                RectTransform suffixRt = suffixImage.rectTransform;
                suffixRt.anchorMin = suffixRt.anchorMax = rt.pivot;
                suffixRt.pivot = new Vector2(0f, 0.5f);
                suffixRt.sizeDelta = new Vector2(suffixWidth, suffixHeight);
                suffixRt.anchoredPosition = new Vector2(x - spacing + suffixSpacing, r.center.y);
            }
        }
    }

    void CollectPool()
    {
        pool.Clear();
        foreach (Transform child in transform)
        {
            var img = child.GetComponent<Image>();
            if (img == null) continue;
            if (child.name == DigitObjectName) pool.Add(img);
            else if (child.name == SuffixObjectName) suffixImage = img;
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

    Image CreateSuffixImage()
    {
        var go = new GameObject(SuffixObjectName, typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }
}
