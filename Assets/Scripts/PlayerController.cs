using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Hit Windows (หน่วยเป็นจังหวะ)")]
    [Tooltip("ระยะ Perfect (แม่นเป๊ะ)")]
    public float perfectWindow = 0.1f;

    [Tooltip("ระยะ Good (คลาดเคลื่อนนิดหน่อย)")]
    public float goodWindow = 0.25f;

    [Tooltip("ระยะ Bad (เกือบจะวืด)")]
    public float badWindow = 0.4f;

    void Update()
    {
        // เช็คการกดปุ่ม
        if (Input.GetKeyDown(KeyCode.W)) TryHit(KeyCode.W);
        if (Input.GetKeyDown(KeyCode.A)) TryHit(KeyCode.A);
        if (Input.GetKeyDown(KeyCode.S)) TryHit(KeyCode.S);
        if (Input.GetKeyDown(KeyCode.D)) TryHit(KeyCode.D);
    }

    void TryHit(KeyCode pressedKey)
    {
        LipidMovement[] allLipids = FindObjectsOfType<LipidMovement>();
        LipidMovement targetLipid = null;
        float closestBeatDist = 100f;

        // 1. หาว่าไขมันตัวไหนอยู่ใกล้จังหวะที่สุด
        foreach (LipidMovement lipid in allLipids)
        {
            float currentBeat = HeartbeatManager.instance.heartPositionInBeats;
            float beatDist = Mathf.Abs(lipid.targetBeat - currentBeat);

            if (beatDist < closestBeatDist)
            {
                closestBeatDist = beatDist;
                targetLipid = lipid;
            }
        }

        // 2. ถ้ามีไขมันอยู่ในระยะที่ไกลที่สุดที่ยังพอยอมรับได้ (Bad Window)
        if (targetLipid != null && closestBeatDist <= badWindow)
        {
            // 3. เช็คว่ากดถูกปุ่มไหม?
            if (pressedKey == targetLipid.keyToPress)
            {
                // ถ้ากดถูกปุ่ม มาวัดความแม่นยำกัน!
                if (closestBeatDist <= perfectWindow)
                {
                    Debug.Log("<color=yellow>PERFECT!!!</color> แม่นมาก!");
                }
                else if (closestBeatDist <= goodWindow)
                {
                    Debug.Log("<color=green>GOOD!</color> นอกจังหวะนิดนึง");
                }
                else
                {
                    Debug.Log("<color=orange>BAD.</color> เกือบวืดแล้ว!");
                }

                Destroy(targetLipid.gameObject); // ตีโดนแล้วไขมันแตก
            }
            else
            {
                Debug.Log($"<color=red>MISS!</color> กดผิดปุ่ม! (ต้องกด {targetLipid.keyToPress})");
                Destroy(targetLipid.gameObject); // ทำโทษกดผิด ให้ไขมันแตกไปเลยแต่ไม่ได้คะแนน
            }
        }
        else
        {
            Debug.Log("วืดดดดดดดดด! ไม่มีไขมันอยู่ในระยะเลย");
        }
    }
}