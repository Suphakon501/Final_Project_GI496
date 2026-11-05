using UnityEngine;

/// <summary>
/// ทำให้ตัวละครสว่างเต็มที่แบบไม่มีแสงฟุ้ง:
/// เปลี่ยนสไปรท์เป็น material แบบ unlit เพื่อไม่ให้แสงฉากมาหรี่ความสว่างลง
/// (ไม่มีการวาดวงแสงเพิ่มใด ๆ รอบตัว)
/// </summary>
[DisallowMultipleComponent]
public class CharacterLight : MonoBehaviour
{
    [Tooltip("ใช้ material unlit ให้ตัวละครสว่างเต็มที่ ไม่ถูกแสงฉากหรี่")]
    [SerializeField] private bool unlit = true;

    [Tooltip("โทนสีที่คลุมตัวละคร (ขาว = สีเดิมของภาพ)")]
    [SerializeField] private Color tint = Color.white;

    private SpriteRenderer body;
    private Material runtimeMaterial;

    void Awake()
    {
        body = GetComponent<SpriteRenderer>();
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (body == null) return;

        if (unlit)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                runtimeMaterial = new Material(shader);
                body.sharedMaterial = runtimeMaterial;
            }
        }

        ApplyTint();
    }

    void ApplyTint()
    {
        if (body == null) return;

        Color c = tint;
        c.a = body.color.a;
        body.color = c;
    }

    void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
