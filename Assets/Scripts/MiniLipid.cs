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

    [Header("Particle Effects")]
    [SerializeField] private GameObject goodParticlePrefab;      // พาร์ทิเคิลตอนได้ Good (กระจายปานกลาง)
    [SerializeField] private GameObject perfectParticlePrefab; // พาร์ทิเคิลตอนได้ Perfect (กระจายกว้างมากๆ)
    [SerializeField] private GameObject badMissParticlePrefab;   // พาร์ทิเคิลตอนได้ Bad / Miss

    private LipidMovement parentLipid;
    private bool isFinished = false;
    private int miniIndex = 0;
    private int totalInRow = 0;

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

        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        if (transform.position.x <= hitLineX - 1.5f)
        {
            ConsumeMini("MISS");
        }
    }

    public string EvaluateAccuracy()
    {
        float distance = Mathf.Abs(transform.position.x - hitLineX);

        if (distance <= 0.6f) return "PERFECT";
        else if (distance <= 1.3f) return "GOOD";
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