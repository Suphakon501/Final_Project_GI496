using UnityEngine;

// ติดกับ prefab ของเอฟเฟกต์ เล่นจบแล้วทำลายตัวเอง
public class HitVFX : MonoBehaviour
{
    [Tooltip("0 = ใช้ความยาวของ Animation clip ใน Animator อัตโนมัติ")]
    public float lifetime = 0f;

    void Start()
    {
        float t = lifetime;

        if (t <= 0f)
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                foreach (var clip in anim.runtimeAnimatorController.animationClips)
                    t = Mathf.Max(t, clip.length);
            }
        }

        if (t <= 0f) t = 0.6f; // ค่าสำรอง
        Destroy(gameObject, t);
    }
}