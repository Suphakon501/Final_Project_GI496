using UnityEngine;

/// <summary>
/// ระบบ "impact" ของกล้อง: สั่น + ซูมกระชาก + เอียง ตามผลการกดโน้ต
/// ใช้ค่า trauma (0-1) สะสมแล้วลดลงเรื่อยๆ แรงสั่นจะคิดจาก trauma^2 เพื่อให้กระแทกที่จุดแรกแล้วค่อยๆ สงบ
/// </summary>
[DefaultExecutionOrder(-50)]
public class CameraImpact : MonoBehaviour
{
    public static CameraImpact instance;

    [Header("Trauma ต่อผลการกด (0 - 1)")]
    [SerializeField, Range(0f, 1f)] private float perfectTrauma = 0.34f;
    [SerializeField, Range(0f, 1f)] private float goodTrauma = 0.2f;
    [SerializeField, Range(0f, 1f)] private float badTrauma = 0.16f;
    [SerializeField, Range(0f, 1f)] private float missTrauma = 0.5f;
    [SerializeField, Range(0f, 1f)] private float gameOverTrauma = 1f;

    [Header("Combo Boost")]
    [Tooltip("คอมโบยิ่งสูงยิ่งสั่นแรงขึ้นนิดหน่อย")]
    [SerializeField] private float traumaPerCombo = 0.01f;
    [SerializeField, Range(0f, 0.5f)] private float maxComboBonus = 0.2f;

    [Header("Shake Feel")]
    [SerializeField] private float maxShake = 0.3f;
    [SerializeField] private float shakeFrequency = 22f;
    [SerializeField] private float traumaDecay = 2.2f;

    [Header("Zoom Punch (orthographic size ที่หักออก)")]
    [SerializeField] private float perfectZoom = 0.16f;
    [SerializeField] private float goodZoom = 0.09f;
    [SerializeField] private float badZoom = 0.07f;
    [SerializeField] private float missZoom = 0.24f;
    [SerializeField] private float gameOverZoom = 0.5f;
    [SerializeField] private float zoomRecoverSpeed = 5.5f;

    [Header("Roll (เอียงกล้อง)")]
    [SerializeField] private float maxRoll = 1.6f;

    private Camera cam;
    private LowHealthCameraShake lowHealth;
    private Vector3 baseLocalPosition;
    private Quaternion baseRotation;
    private float baseOrthoSize;
    private float trauma;
    private float zoomPunch;
    private float noiseSeed;

    // ใส่คอมโพเนนต์ให้กล้องหลักอัตโนมัติ ทุกซีน ไม่ต้องแก้ไฟล์ซีน
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || mainCamera.GetComponent<CameraImpact>() != null) return;
        mainCamera.gameObject.AddComponent<CameraImpact>();
    }

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
        lowHealth = GetComponent<LowHealthCameraShake>();
        baseLocalPosition = transform.localPosition;
        baseRotation = transform.localRotation;
        baseOrthoSize = cam != null ? cam.orthographicSize : 5f;
        noiseSeed = Random.Range(0f, 1000f);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseRotation;
        if (cam != null) cam.orthographicSize = baseOrthoSize;
    }

    public static void Hit(HitResult result) => Hit(result, 0);

    public static void Hit(HitResult result, int combo)
    {
        if (instance == null) return;

        float shake;
        float zoom;
        switch (result)
        {
            case HitResult.Perfect: shake = instance.perfectTrauma; zoom = instance.perfectZoom; break;
            case HitResult.Good: shake = instance.goodTrauma; zoom = instance.goodZoom; break;
            case HitResult.Bad: shake = instance.badTrauma; zoom = instance.badZoom; break;
            default: shake = instance.missTrauma; zoom = instance.missZoom; break;
        }

        if (result != HitResult.Miss)
            shake += Mathf.Min(Mathf.Max(0, combo) * instance.traumaPerCombo, instance.maxComboBonus);

        instance.AddImpact(shake, zoom);
    }

    public static void GameOver()
    {
        if (instance != null) instance.AddImpact(instance.gameOverTrauma, instance.gameOverZoom);
    }

    void AddImpact(float shakeAmount, float zoomAmount)
    {
        trauma = Mathf.Clamp01(trauma + shakeAmount);
        zoomPunch = Mathf.Max(zoomPunch, zoomAmount);
    }

    void LateUpdate()
    {
        if (PauseMenu.IsPaused) return;

        float dt = Time.unscaledDeltaTime;
        float strength = trauma * trauma;

        trauma = Mathf.Max(0f, trauma - traumaDecay * dt);
        zoomPunch = Mathf.MoveTowards(zoomPunch, 0f, zoomRecoverSpeed * dt);

        float time = Time.unscaledTime * shakeFrequency;
        float x = (Mathf.PerlinNoise(noiseSeed, time) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(time, noiseSeed) - 0.5f) * 2f;
        float roll = (Mathf.PerlinNoise(noiseSeed + 17.31f, time) - 0.5f) * 2f;

        Vector3 offset = new Vector3(x, y, 0f) * (maxShake * strength);
        Vector3 healthOffset = lowHealth != null ? lowHealth.CurrentOffset : Vector3.zero;

        transform.localPosition = baseLocalPosition + healthOffset + offset;
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, roll * maxRoll * strength);

        if (cam != null)
            cam.orthographicSize = baseOrthoSize - zoomPunch;
    }
}
