using UnityEngine;
using UnityEngine.SceneManagement; 

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    [Header("Hit Windows")]
    public float perfectWindow = 0.2f;
    public float goodWindow = 0.4f;

    [Header("Cooldown System")]
    public float cooldownTime = 0.2f;
    private float nextAllowedPressTime = 0f;

    [Header("Player Health (HP)")]
    public int maxHealth = 3;        // เลือดสูงสุด 3 ขีด
    private int currentHealth;       // เลือดปัจจุบัน

    [Header("Visual Feedback")]
    public SpriteRenderer playerSprite;
    public Color perfectColor = Color.green;
    public Color goodColor = Color.yellow;
    public Color badColor = Color.cyan;
    public Color damageColor = Color.white;

    private Color originalColor;
    private float resetColorTime = 0f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (playerSprite != null) originalColor = playerSprite.color;
        currentHealth = maxHealth; // เริ่มเกมเลือดเต็ม 3
    }

    void Update()
    {
        if (playerSprite != null && Time.time > resetColorTime) playerSprite.color = originalColor;
        if (Time.time < nextAllowedPressTime) return;

        if (Input.GetKeyDown(KeyCode.W)) TryHit(KeyCode.W);
        else if (Input.GetKeyDown(KeyCode.A)) TryHit(KeyCode.A);
        else if (Input.GetKeyDown(KeyCode.S)) TryHit(KeyCode.S);
        else if (Input.GetKeyDown(KeyCode.D)) TryHit(KeyCode.D);
    }

    void TryHit(KeyCode pressedKey)
    {
        nextAllowedPressTime = Time.time + cooldownTime;

        // อัปเดตมาใช้แบบใหม่ตามที่ Unity แนะนำ (แก้ Warning หายเกลี้ยง)
        LipidMovement[] allLipids = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);
        if (allLipids.Length == 0) return;

        LipidMovement targetLipid = null;
        float closestBeatDist = 100f;
        float currentBeat = HeartbeatManager.instance.heartPositionInBeats;

        foreach (LipidMovement lipid in allLipids)
        {
            float beatDist = Mathf.Abs(lipid.targetBeat - currentBeat);
            if (beatDist < closestBeatDist)
            {
                closestBeatDist = beatDist;
                targetLipid = lipid;
            }
        }

        // ตีได้ก็ต่อเมื่อก้อนไขมันวิ่งผ่าน X = 0 มาแล้ว
        if (targetLipid != null && targetLipid.transform.position.x <= 0f)
        {
            if (pressedKey == targetLipid.keyToPress)
            {
                if (closestBeatDist <= perfectWindow)
                {
                    Debug.Log("PERFECT!");
                    ChangeColorTemp(perfectColor, 0.2f);
                }
                else if (closestBeatDist <= goodWindow)
                {
                    Debug.Log("GOOD!");
                    ChangeColorTemp(goodColor, 0.2f);
                }
                else
                {
                    Debug.Log("BAD");
                    ChangeColorTemp(badColor, 0.2f);
                }

                Destroy(targetLipid.gameObject);
            }
            else
            {
                Debug.Log("MISS! กดผิดปุ่ม");
            }
        }
    }

    // ฟังก์ชันรับดาเมจเมื่อไขมันชน
    public void TakeDamage()
    {
        currentHealth--;
        Debug.Log($"<color=red>โดนชน!</color> เลือดเหลือ: {currentHealth}/{maxHealth}");

        ChangeColorTemp(damageColor, 3f);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // ฟังก์ชันตอนเลือดหมด
    void Die()
    {
        Debug.Log("<color=red>GAME OVER! เริ่มเกมใหม่</color>");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ChangeColorTemp(Color newColor, float duration)
    {
        if (playerSprite != null)
        {
            playerSprite.color = newColor;
            resetColorTime = Time.time + duration;
        }
    }
}