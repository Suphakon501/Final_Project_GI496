using UnityEngine;

public class LipidDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private float lifetime = 2.0f; // อยู่บนจอให้เก็บ 2 วินาที ก่อนจะหายไปเอง
    private float timer = 0f;
    private bool isCollected = false;

    void Update()
    {
        if (PlayerController.isGameOver || isCollected) return;

        timer += Time.deltaTime;

        // ถ้าผู้เล่นกด Space bar ในช่วงเวลาที่ของยังตกอยู่
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CollectDrop();
        }

        // ถ้าหมดเวลา 2 วินาทีแล้วยังไม่เก็บ ของจะหายไปเอง
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void CollectDrop()
    {
        isCollected = true;

        // เพิ่มเลือดให้ 10 ผ่าน HealthBarUI
        if (HealthBarUI.instance != null)
        {
            HealthBarUI.instance.AddHealth(15f);
            Debug.Log("เก็บไขมันสำเร็จ! +15 HP");
        }

        // ทำลายไอเทต์ทิ้งหลังเก็บ
        Destroy(gameObject);
    }
}