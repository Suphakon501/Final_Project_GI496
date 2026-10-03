using UnityEngine;

/// <summary>
/// ทำให้ข้อความ/รูปชื่อเกมขยับเข้าๆ ออกๆ (ย่อ-ขยาย) แบบ splash text ของ Minecraft
/// วางสคริปต์นี้บน GameObject ของชื่อเกม (Text / TextMeshPro / Image ก็ได้)
/// </summary>
public class TitlePulse : MonoBehaviour
{
    [Header("การย่อ-ขยาย")]
    [SerializeField] private float speed = 4f;          // ยิ่งมากยิ่งเต้นเร็ว
    [SerializeField] private float minScale = 0.9f;     // เล็กสุด
    [SerializeField] private float maxScale = 1.1f;     // ใหญ่สุด

    [Header("เอียงข้อความ (แบบ Minecraft)")]
    [SerializeField] private float tiltAngle = 0f;      // เช่น 15 หรือ -15 ให้ข้อความเอียงคงที่
    [SerializeField] private float swayAmount = 0f;     // แกว่งซ้ายขวาเล็กน้อย เช่น 3

    [Header("ตั้งค่าอื่นๆ")]
    [SerializeField] private bool useUnscaledTime = true; // ให้ขยับต่อแม้เกม pause

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        // Sin ให้ค่า -1..1 -> แปลงเป็น 0..1 แล้วผสมระหว่าง min กับ max
        float wave = (Mathf.Sin(t * speed) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minScale, maxScale, wave);
        transform.localScale = baseScale * scale;

        // เอียง + แกว่งเล็กน้อย
        float sway = Mathf.Sin(t * speed * 0.5f) * swayAmount;
        transform.localRotation = Quaternion.Euler(0f, 0f, tiltAngle + sway);
    }
}