using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ปุ่มเมนูที่เปลี่ยนรูปตอนเอาเมาส์ไปวาง: ปุ่มดำ (ตัวอักษรหมอง) -> ปุ่มแดง (ตัวอักษรสว่าง)
/// วางสคริปต์นี้บน GameObject ของปุ่มที่มี Image + Button อยู่แล้ว
/// รองรับทั้งเมาส์ และการเลือกด้วยคีย์บอร์ด/จอย (ผ่าน EventSystem)
/// </summary>
[RequireComponent(typeof(Image))]
public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("รูปของปุ่ม")]
    [SerializeField] private Sprite normalSprite;   // เช่น Btn_Play_Normal
    [SerializeField] private Sprite hoverSprite;    // เช่น Btn_Play_Hover

    [Header("เอฟเฟกต์ตอน Hover")]
    [SerializeField] private float hoverScale = 1.06f;   // ขยายขึ้นเล็กน้อย (1 = ไม่ขยาย)
    [SerializeField] private float animSpeed = 14f;      // ความไวของการขยาย/หด
    [SerializeField] private Vector2 hoverOffset = new Vector2(-12f, 0f); // เลื่อนซ้ายนิดๆ ให้ดูพุ่งออกมา

    [Header("เสียง (ไม่ใส่ก็ได้)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverSound;

    private Image image;
    private Button button;
    private RectTransform rect;
    private Vector2 basePosition;
    private bool hovered;

    private void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();
        rect = (RectTransform)transform;
        basePosition = rect.anchoredPosition;

        if (normalSprite == null) normalSprite = image.sprite; // ถ้าไม่ได้ใส่ ใช้รูปที่ตั้งไว้ใน Image
        image.preserveAspect = true;
        image.sprite = normalSprite;
    }

    private void OnEnable()
    {
        SetHover(false, instant: true);
    }

    private void OnDisable()
    {
        SetHover(false, instant: true);
    }

    private void Update()
    {
        // ค่อยๆ ขยาย/หด และเลื่อนตำแหน่ง ใช้ unscaled เผื่อเกมถูก pause
        float targetScale = hovered ? hoverScale : 1f;
        Vector2 targetPos = hovered ? basePosition + hoverOffset : basePosition;
        float t = 1f - Mathf.Exp(-animSpeed * Time.unscaledDeltaTime);

        float s = Mathf.Lerp(rect.localScale.x, targetScale, t);
        rect.localScale = new Vector3(s, s, 1f);
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, t);
    }

    // ---------- เมาส์ ----------
    public void OnPointerEnter(PointerEventData e) => SetHover(true);
    public void OnPointerExit(PointerEventData e) => SetHover(false);

    // ---------- คีย์บอร์ด / จอย ----------
    public void OnSelect(BaseEventData e) => SetHover(true);
    public void OnDeselect(BaseEventData e) => SetHover(false);

    private void SetHover(bool value, bool instant = false)
    {
        if (image == null) return;
        if (value && button != null && !button.interactable) return; // ปุ่มกดไม่ได้ ไม่ต้องเปลี่ยน

        bool wasHovered = hovered;
        hovered = value;

        image.sprite = (value && hoverSprite != null) ? hoverSprite : normalSprite;

        if (value && !wasHovered && audioSource != null && hoverSound != null)
            audioSource.PlayOneShot(hoverSound);

        if (instant)
        {
            rect.localScale = Vector3.one;
            rect.anchoredPosition = basePosition;
        }
    }
}