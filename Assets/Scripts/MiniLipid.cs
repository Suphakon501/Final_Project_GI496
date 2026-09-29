using UnityEngine;
using TMPro;

public class MiniLipid : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI letterText;

    [Header("Settings")]
    public KeyCode assignedKey;
    [SerializeField] private float moveSpeed = 3.0f;
    public float hitLineX = -3.0f;

    [Header("Timing Window (วินาที) - วัดจากเวลา ไม่ใช่ระยะทาง เลยยุติธรรมเท่ากันทุกความเร็ว")]
    [SerializeField] private float perfectWindow = 0.09f; // กดห่างจากจังหวะไม่เกินนี้ = PERFECT
    [SerializeField] private float goodWindow = 0.2f;     // ไม่เกินนี้ = GOOD, เกินกว่านี้ = BAD
    [SerializeField] private float missAfter = 0.23f;     // วิ่งเลยเส้นไปนานเท่านี้แล้วยังไม่กด = MISS

    public float MoveSpeed => moveSpeed;

    [Header("Particle Effects")]
    [SerializeField] private GameObject goodParticlePrefab;      // �������ŵ͹�� Good (��Ш�»ҹ��ҧ)
    [SerializeField] private GameObject perfectParticlePrefab; // �������ŵ͹�� Perfect (��Ш�¡��ҧ�ҡ�)
    [SerializeField] private GameObject badMissParticlePrefab;   // �������ŵ͹�� Bad / Miss

    private LipidMovement parentLipid;
    private bool isFinished = false;
    private int miniIndex = 0;
    private int totalInRow = 0;

    // โน้ตที่ผูกกับเพลง: รู้ว่าต้องถึงเส้นตอนเวลาเพลงเท่าไหร่ แล้วคำนวณตำแหน่งจากเวลาเพลงทุกเฟรม (แบบ Taiko / Muse Dash)
    private bool hasTargetTime = false;
    private float targetSongTime;

    public void SetTargetSongTime(float songTime)
    {
        hasTargetTime = true;
        targetSongTime = songTime;
        FollowSongTime();
    }

    bool UsingSongClock => hasTargetTime && BeatManager.instance != null;

    // + = ยังไม่ถึงเส้น, - = เลยเส้นไปแล้ว (วินาที)
    float SecondsUntilLine()
    {
        if (UsingSongClock) return targetSongTime - BeatManager.instance.songPositionInSeconds;
        return (transform.position.x - hitLineX) / Mathf.Max(0.01f, moveSpeed);
    }

    void FollowSongTime()
    {
        if (!UsingSongClock) return;
        var p = transform.position;
        p.x = hitLineX + (targetSongTime - BeatManager.instance.songPositionInSeconds) * moveSpeed;
        transform.position = p;
    }

    void Start()
    {
        UpdateVisual();
    }

    public void InitializeMini(KeyCode key, LipidMovement lipidOwner, int index, int total)
    {
        assignedKey = key;
        parentLipid = lipidOwner;
        miniIndex = index;
        totalInRow = total;
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (letterText != null)
        {
            letterText.text = assignedKey.ToString();
        }
    }

    void Update()
    {
        if (PlayerController.isGameOver || isFinished) return;

        if (UsingSongClock) FollowSongTime();
        else transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (SecondsUntilLine() < -missAfter)
        {
            if (PlayerController.instance != null) PlayerController.instance.RegisterMissedNote();
            ConsumeMini("MISS");
        }
    }

    public string EvaluateAccuracy()
    {
        // ห่างจากจังหวะกี่วินาที (โน้ตที่ผูกเพลง = เทียบกับเวลาเพลงตรงๆ)
        float secondsFromLine = Mathf.Abs(SecondsUntilLine());

        if (secondsFromLine <= perfectWindow) return "PERFECT";
        else if (secondsFromLine <= goodWindow) return "GOOD";
        else return "BAD";
    }

    public void ConsumeMini(string accuracyResult)
    {
        if (isFinished) return;
        isFinished = true;

        GameObject selectedParticle = null;

        if (accuracyResult == "PERFECT")
        {
            selectedParticle = perfectParticlePrefab;
        }
        else if (accuracyResult == "GOOD")
        {
            selectedParticle = goodParticlePrefab;
        }
        else
        {
            selectedParticle = badMissParticlePrefab;
        }

        if (selectedParticle != null)
        {
            GameObject effect = Instantiate(selectedParticle, transform.position, Quaternion.identity);
            Destroy(effect, 0.6f);
        }

        CheckAndNotifyParent();
        Destroy(gameObject);
    }

    public bool IsLastInRow()
    {
        MiniLipid[] remainingMinis = Object.FindObjectsByType<MiniLipid>(FindObjectsSortMode.None);
        int activeCount = 0;

        foreach (var m in remainingMinis)
        {
            if (m != this && !m.isFinished)
            {
                activeCount++;
            }
        }
        return (activeCount == 0);
    }

    void CheckAndNotifyParent()
    {
        if (IsLastInRow() && parentLipid != null)
        {
            parentLipid.OnAllMinisCleared();
        }
    }
}