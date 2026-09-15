using UnityEngine;

public class LipidMovement : MonoBehaviour
{
    [Header("Step Movement Settings")]
    [SerializeField] private float stepInterval = 1.0f;
    [SerializeField] private float stepDistance = 2.0f;

    [Header("Position & Height")]
    public float spawnPosX = 10f;
    public float hitPosX = -6f;
    public float spawnPosY = 0f;

    [Header("Audition Sequence System")]
    public KeyCode[] keySequence;
    public int sequenceLength = 2;
    public int currentKeyIndex = 0;

    [HideInInspector] public bool sequenceCompleted = false;

    private float stepTimer = 0f;
    private float currentX;

    void Start()
    {
        if (keySequence == null || keySequence.Length == 0)
        {
            InitializeSequence();
        }

        currentX = spawnPosX;
        transform.position = new Vector3(currentX, spawnPosY, 0f);
    }

    public void InitializeSequence()
    {
        KeyCode[] possibleKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        keySequence = new KeyCode[sequenceLength];

        for (int i = 0; i < sequenceLength; i++)
        {
            keySequence[i] = possibleKeys[Random.Range(0, possibleKeys.Length)];
        }
    }

    void Update()
    {
        if (PlayerController.isGameOver) return;

        stepTimer += Time.deltaTime;
        if (stepTimer >= stepInterval)
        {
            stepTimer = 0f;

            float nextX = currentX - stepDistance;
            if (nextX < hitPosX)
            {
                nextX = hitPosX;
            }

            bool isBlocked = false;
            LipidMovement[] allLipids = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);

            foreach (var lipid in allLipids)
            {
                if (lipid != this)
                {
                    if (lipid.transform.position.x > nextX && lipid.transform.position.x < currentX)
                    {
                        isBlocked = true;
                        break;
                    }
                }
            }

            if (!isBlocked)
            {
                currentX = nextX;
            }
        }

        transform.position = new Vector3(currentX, spawnPosY, 0f);

        if (transform.position.x <= hitPosX)
        {
            TriggerDamageAndDestroy();
        }

        LipidMovement[] allLipidsList = Object.FindObjectsByType<LipidMovement>(FindObjectsSortMode.None);
        float lowestX = float.MaxValue;
        LipidMovement frontLipid = null;

        foreach (LipidMovement lipid in allLipidsList)
        {
            if (lipid.transform.position.x < lowestX)
            {
                lowestX = lipid.transform.position.x;
                frontLipid = lipid;
            }
        }

        bool isFirst = (frontLipid == this);

        if (isFirst && GameUIManager.instance != null)
        {
            if (!sequenceCompleted)
            {
                GameUIManager.instance.ShowSequence(GetFormattedSequenceString());
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
        }
    }

    public void ResetSequence()
    {
        currentKeyIndex = 0;
    }

    void TriggerDamageAndDestroy()
    {
        if (HealthBarUI.instance != null)
        {
            HealthBarUI.instance.TakeDamage(34f);
        }

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