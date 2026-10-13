using UnityEngine;

public class HitLineAnimation : MonoBehaviour
{
    public static HitLineAnimation instance;
    private Animator anim;

    void Awake()
    {
        instance = this;
        anim = GetComponent<Animator>();
    }

    public void PlayHitEffect()
    {
        if (anim != null)
        {
            // รีเซ็ต Trigger ก่อนเพื่อป้องกันค้าง แล้วค่อยสั่งยิงซ้ำใหม่ ทำให้กดรัวๆ แล้วเล่นอนิเมชั่นใหม่ได้ทันที
            anim.ResetTrigger("TriggerHit");
            anim.SetTrigger("TriggerHit");
        }
    }
}