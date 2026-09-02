using UnityEngine;
using TMPro;

public class LipidMovement : MonoBehaviour
{
    [Header("Timing")]
    public float targetBeat;
    public float beatsToReachTarget = 4f;

    [Header("Position")]
    public float spawnPosX = 10f;
    public float hitPosX = -6f;

    [Header("Hit System")]
    public KeyCode keyToPress;
    public TextMeshPro textDisplay;

    [Header("Skill Check UI")]
    public RectTransform skillCheckBG;
    public RectTransform redLineCursor;

    private float barTravelDistance;
    private bool wasActiveLastFrame = false;

    void Start()
    {
        KeyCode[] possibleKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        keyToPress = possibleKeys[Random.Range(0, possibleKeys.Length)];

        if (textDisplay != null)
        {
            textDisplay.text = keyToPress.ToString();
        }

        if (skillCheckBG != null)
        {
            barTravelDistance = skillCheckBG.rect.width / 2f;
            skillCheckBG.gameObject.SetActive(false);
        }

        if (textDisplay != null)
        {
            textDisplay.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (HeartbeatManager.instance == null) return;

        float currentBeat = HeartbeatManager.instance.heartPositionInBeats;

        // 1. ขยับตำแหน่งตัวก้อนไขมัน
        float beatInteger = Mathf.Floor(currentBeat);
        float beatFraction = currentBeat - beatInteger;
        float slideDuration = 0.3f;
        float stepProgress = Mathf.Clamp01(beatFraction / slideDuration);
        float steppedCurrentBeat = beatInteger + stepProgress;

        float beatsUntilHit = targetBeat - steppedCurrentBeat;
        float t = beatsUntilHit / beatsToReachTarget;

        float currentX = Mathf.Lerp(hitPosX, spawnPosX, t);
        transform.position = new Vector3(currentX, transform.position.y, 0);

        // 2. เช็คคิว (อัปเดตมาใช้แบบใหม่ตามที่ Unity แนะนำ)
        LipidMovement[] allLipids = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);
        float lowestTargetBeat = float.MaxValue;

        foreach (LipidMovement lipid in allLipids)
        {
            if (lipid.targetBeat < lowestTargetBeat)
            {
                lowestTargetBeat = lipid.targetBeat;
            }
        }

        // 3. เงื่อนไขการแสดงผล (ต้องเป็นคิวแรก และ ผ่าน X = 0 มาแล้ว)
        bool shouldShowUI = (this.targetBeat <= lowestTargetBeat && transform.position.x <= 0f);

        if (shouldShowUI != wasActiveLastFrame)
        {
            if (skillCheckBG != null) skillCheckBG.gameObject.SetActive(shouldShowUI);
            if (textDisplay != null) textDisplay.gameObject.SetActive(shouldShowUI);
            wasActiveLastFrame = shouldShowUI;
        }

        // 4. ระบบแกว่งเส้นแดง
        float realBeatsUntilHit = targetBeat - currentBeat;

        if (redLineCursor != null && shouldShowUI)
        {
            float pingPongValue = Mathf.PingPong(currentBeat + 0.5f, 1f);
            float cursorX = Mathf.Lerp(-barTravelDistance, barTravelDistance, pingPongValue);
            redLineCursor.localPosition = new Vector3(cursorX, 0, 0);
        }

        // 5. ชนแล้วหักเลือดทันที
        if (transform.position.x <= hitPosX)
        {
            if (PlayerController.instance != null)
            {
                PlayerController.instance.TakeDamage();
            }
            Destroy(gameObject);
        }
    }
}