using UnityEngine;
using UnityEngine.UI;

/// <summary>Gently shakes only the gameplay camera while player health is low.</summary>
public class LowHealthCameraShake : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.25f;

    [Header("Shake")]
    [SerializeField, Min(0f)] private float shakeAmplitude = 0.06f;
    [SerializeField, Min(0f)] private float shakeFrequency = 16f;
    [SerializeField, Min(0f)] private float intensityChangeSpeed = 4f;
    [SerializeField] private bool shakeHorizontally = true;
    [SerializeField] private bool shakeVertically = true;

    [Header("Low Health Screen Darkening")]
    [SerializeField, Range(0f, 1f)] private float maximumDarkness = 0.18f;
    [SerializeField, Min(0f)] private float darknessPulseSpeed = 2.5f;
    [SerializeField, Min(0f)] private float darknessChangeSpeed = 3f;

    private Vector3 initialLocalPosition;
    private float currentIntensity;
    private float noiseSeed;
    private Image darknessOverlay;
    private float currentDarkness;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        noiseSeed = Random.Range(0f, 1000f);
        CreateDarknessOverlay();
    }

    private void LateUpdate()
    {
        float targetIntensity = GetLowHealthIntensity();
        currentIntensity = Mathf.MoveTowards(
            currentIntensity,
            targetIntensity,
            intensityChangeSpeed * Time.unscaledDeltaTime);

        UpdateDarknessOverlay();

        float time = Time.unscaledTime * shakeFrequency;
        float x = shakeHorizontally ? (Mathf.PerlinNoise(noiseSeed, time) - 0.5f) * 2f : 0f;
        float y = shakeVertically ? (Mathf.PerlinNoise(time, noiseSeed) - 0.5f) * 2f : 0f;
        Vector3 offset = new Vector3(x, y, 0f) * (shakeAmplitude * currentIntensity);
        transform.localPosition = initialLocalPosition + offset;
    }

    private void CreateDarknessOverlay()
    {
        // Render below the game's normal overlay UI so HUD elements stay clear.
        var canvasObject = new GameObject("LowHealthDarknessOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var imageObject = new GameObject("Darkness", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(canvasObject.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        darknessOverlay = imageObject.GetComponent<Image>();
        darknessOverlay.color = Color.black;
        darknessOverlay.raycastTarget = false;
    }

    private void UpdateDarknessOverlay()
    {
        if (darknessOverlay == null) return;

        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * darknessPulseSpeed * Mathf.PI * 2f);
        float targetDarkness = GetLowHealthIntensity() * maximumDarkness * pulse;
        currentDarkness = Mathf.MoveTowards(currentDarkness, targetDarkness, darknessChangeSpeed * Time.unscaledDeltaTime);

        Color color = darknessOverlay.color;
        color.a = currentDarkness;
        darknessOverlay.color = color;
    }

    private float GetLowHealthIntensity()
    {
        if (HealthBarUI.instance == null || PlayerController.isGameOver || lowHealthThreshold <= 0f)
            return 0f;

        float healthRatio = HealthBarUI.instance.MaxHP > 0f
            ? HealthBarUI.instance.CurrentHP / HealthBarUI.instance.MaxHP
            : 1f;
        if (healthRatio >= lowHealthThreshold) return 0f;

        float lowHealthProgress = 1f - Mathf.Clamp01(healthRatio / lowHealthThreshold);
        return lowHealthProgress * lowHealthProgress * (3f - 2f * lowHealthProgress);
    }

    private void OnDisable()
    {
        transform.localPosition = initialLocalPosition;
        if (darknessOverlay != null)
            darknessOverlay.color = new Color(0f, 0f, 0f, 0f);
    }

    private void OnDestroy()
    {
        if (darknessOverlay != null)
            Destroy(darknessOverlay.canvas.gameObject);
    }
}
