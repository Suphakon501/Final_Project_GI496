using UnityEngine;

public class LipidMovement : MonoBehaviour
{
    [Header("Timing")]
    public float targetBeat;
    public float beatsToReachTarget = 4f;

    [Header("Position & Height")]
    public float spawnPosX = 10f;
    public float hitPosX = -6f;
    public float spawnPosY = 0f;

    [Header("Audition Sequence System")]
    public KeyCode[] keySequence;
    public int sequenceLength = 3;
    public int currentKeyIndex = 0;

    [HideInInspector] public bool sequenceCompleted = false;

    private float skillCheckStartBeat = 0f;
    private const float skillCheckDuration = 2f;
    private bool skillCheckExpired = false;

    void Start()
    {
        KeyCode[] possibleKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        keySequence = new KeyCode[sequenceLength];

        for (int i = 0; i < sequenceLength; i++)
        {
            keySequence[i] = possibleKeys[Random.Range(0, possibleKeys.Length)];
        }

        transform.position = new Vector3(spawnPosX, spawnPosY, 0);
    }

    void Update()
    {
        if (PlayerController.isGameOver) return;
        if (HeartbeatManager.instance == null) return;

        float currentBeat = HeartbeatManager.instance.heartPositionInBeats;

        if (currentBeat < (targetBeat - beatsToReachTarget))
        {
            transform.position = new Vector3(spawnPosX, spawnPosY, 0);
            return;
        }

        float beatInteger = Mathf.Floor(currentBeat);
        float beatFraction = currentBeat - beatInteger;
        float slideDuration = 0.3f;
        float stepProgress = Mathf.Clamp01(beatFraction / slideDuration);
        float steppedCurrentBeat = beatInteger + stepProgress;

        float beatsUntilHit = targetBeat - steppedCurrentBeat;
        float t = beatsUntilHit / beatsToReachTarget;

        float currentX = Mathf.Lerp(hitPosX, spawnPosX, t);
        transform.position = new Vector3(currentX, spawnPosY, 0);

        if (transform.position.x <= hitPosX)
        {
            TriggerDamageAndDestroy();
        }


        LipidMovement[] allLipids = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);
        float lowestTargetBeat = float.MaxValue;

        foreach (LipidMovement lipid in allLipids)
        {
            if (lipid.targetBeat < lowestTargetBeat)
            {
                lowestTargetBeat = lipid.targetBeat;
            }
        }

        bool isFirst = (this.targetBeat <= lowestTargetBeat);

        if (sequenceCompleted && !skillCheckExpired)
        {
            float elapsedSkillBeat = currentBeat - skillCheckStartBeat;
            if (elapsedSkillBeat >= skillCheckDuration)
            {
                skillCheckExpired = true;
            }
        }

        // ส่งข้อมูลให้ UI กลางจอแสดงผล (เฉพาะตัวแรกสุด)
        if (isFirst && GameUIManager.instance != null)
        {
            if (!sequenceCompleted)
            {
                // ยังพิมพ์ไม่ครบ -> แสดงชุดปุ่ม W A S D กลางจอแบบไม่มีสี
                GameUIManager.instance.ShowSequence(GetFormattedSequenceString());
            }
            else if (!skillCheckExpired)
            {
                // พิมพ์ครบแล้ว -> แสดงหลอด Skill Check กลางจอ
                float elapsedSkillBeat = currentBeat - skillCheckStartBeat;
                float progress = Mathf.Clamp01(elapsedSkillBeat / skillCheckDuration);
                float pingPongValue = Mathf.PingPong(progress * 4f, 1f);

                GameUIManager.instance.ShowSkillCheck(pingPongValue);
            }
            else
            {
                GameUIManager.instance.HideAllUI();
            }
        }
    }

    public string GetFormattedSequenceString()
    {
        string displayStr = "";
        for (int i = 0; i < keySequence.Length; i++)
        {
            if (i == currentKeyIndex)
            {
                displayStr += "[" + keySequence[i] + "] ";
            }
            else
            {
                displayStr += keySequence[i] + " ";
            }
        }
        return displayStr;
    }

    public void CorrectKeyInput()
    {
        currentKeyIndex++;

        if (currentKeyIndex >= keySequence.Length)
        {
            sequenceCompleted = true;
            skillCheckStartBeat = HeartbeatManager.instance.heartPositionInBeats;
        }
    }

    public void ResetSequence()
    {
        currentKeyIndex = 0;
    }

    void TriggerDamageAndDestroy()
    {
        if (PlayerController.instance != null)
        {
            PlayerController.instance.TakeDamage();
        }
        if (GameUIManager.instance != null)
        {
            GameUIManager.instance.HideAllUI();
        }
        Destroy(gameObject);
    }
}