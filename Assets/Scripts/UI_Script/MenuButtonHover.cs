using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;

    [Header("Hover Effect")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float animSpeed = 14f;
    [SerializeField] private Vector2 hoverOffset = new Vector2(-12f, 0f);

    [Header("Sound (optional)")]
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

        if (normalSprite == null) normalSprite = image.sprite;
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
        float targetScale = hovered ? hoverScale : 1f;
        Vector2 targetPos = hovered ? basePosition + hoverOffset : basePosition;
        float t = 1f - Mathf.Exp(-animSpeed * Time.unscaledDeltaTime);

        float s = Mathf.Lerp(rect.localScale.x, targetScale, t);
        rect.localScale = new Vector3(s, s, 1f);
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, t);
    }

    public void OnPointerEnter(PointerEventData e) => SetHover(true);
    public void OnPointerExit(PointerEventData e) => SetHover(false);

    public void OnSelect(BaseEventData e) => SetHover(true);
    public void OnDeselect(BaseEventData e) => SetHover(false);

    private void SetHover(bool value, bool instant = false)
    {
        if (image == null) return;
        if (value && button != null && !button.interactable) return;

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
