using UnityEngine;
using TMPro; // อย่าลืมบรรทัดนี้ สำหรับระบบตัวหนังสือ

public class LipidMovement : MonoBehaviour
{
    [Header("Timing")]
    public float targetBeat;
    public float beatsToReachTarget = 4f;

    [Header("Position")]
    public float spawnPosX = 10f;
    public float hitPosX = -6f;

    [Header("Hit System")]
    public KeyCode keyToPress; // ปุ่มที่ถูกสุ่มให้ผู้เล่นกด
    public TextMeshPro textDisplay; // ช่องใส่ TextMeshPro

    void Start()
    {
        // 1. สร้างคลังปุ่มที่เป็นไปได้ (W A S D)
        KeyCode[] possibleKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };

        // 2. สุ่มหยิบมา 1 ปุ่มตอนที่ก้อนไขมันเกิด
        keyToPress = possibleKeys[Random.Range(0, possibleKeys.Length)];

        // 3. เปลี่ยนตัวหนังสือบนก้อนไขมันให้ตรงกับปุ่มที่สุ่มได้
        if (textDisplay != null)
        {
            textDisplay.text = keyToPress.ToString();
        }
    }

    void Update()
    {
        // ป้องกัน Error ถ้าไม่มี GameManager อยู่ในฉาก
        if (HeartbeatManager.instance == null) return;

        // ดึงเวลาของจริง (แบบไหลลื่นปกติ)
        float currentBeat = HeartbeatManager.instance.heartPositionInBeats;

        // ==========================================
        // ระบบเลื่อนแบบ "พรึ่บ...แล้วหยุด" (Step-and-Stop)
        // ==========================================
        float beatInteger = Mathf.Floor(currentBeat); // ปัดเศษทิ้ง เอาแค่เลขจำนวนเต็ม
        float beatFraction = currentBeat - beatInteger; // เอาแค่เศษทศนิยมด้านหลัง

        // ตั้งค่าความไวในการกระตุก (0.3 คือใช้เวลาแค่ 30% ของ 1 จังหวะในการพุ่ง)
        float slideDuration = 0.3f;

        // บีบให้ค่าเปอร์เซ็นต์วิ่งไปตันที่ 100% (1.0) อย่างรวดเร็ว
        float stepProgress = Mathf.Clamp01(beatFraction / slideDuration);

        // ได้ค่าเวลาใหม่ที่จะเดินเป็นขั้นบันได เอาไปใช้ขยับตัว
        float steppedCurrentBeat = beatInteger + stepProgress;
        // ==========================================

        // คำนวณระยะห่างด้วยเวลาแบบขั้นบันได
        float beatsUntilHit = targetBeat - steppedCurrentBeat;
        float t = beatsUntilHit / beatsToReachTarget;

        // ขยับตำแหน่งตามเวลาที่คำนวณได้
        float currentX = Mathf.Lerp(hitPosX, spawnPosX, t);
        transform.position = new Vector3(currentX, transform.position.y, 0);

        // เวลาเช็คหลุดจอ (Miss) ให้ใช้เวลาของจริง เพื่อไม่ให้ไขมันค้างตอนจังหวะหยุด
        float realBeatsUntilHit = targetBeat - currentBeat;
        if (realBeatsUntilHit < -1.5f)
        {
            Debug.Log("Miss! ปล่อยไขมันหลุดไปได้ไง!");
            Destroy(gameObject);
        }
    }
}