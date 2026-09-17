using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("UI Panel")]
    public GameObject skillCheckPanel;
    public TextMeshProUGUI sequenceText;

    // index ตัวอักษรใน sequenceText ที่ต้องไฮไลต์/เด้งตามจังหวะ (-1 = ไม่มี)
    public int HighlightCharIndex { get; private set; } = -1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // ให้ตัวอักษรที่ต้องกดตอนนี้ เด้งตามจังหวะเพลงอัตโนมัติ (เฉพาะตัวนั้น ไม่ใช่ทั้งก้อนข้อความ) ไม่ต้องไปตั้งใน Editor เอง
        if (sequenceText != null && sequenceText.GetComponent<TMPCharacterBeatPulse>() == null)
        {
            sequenceText.gameObject.AddComponent<TMPCharacterBeatPulse>();
        }

        HideAllUI();
    }

    public void HideAllUI()
    {
        HighlightCharIndex = -1;
        if (sequenceText != null) sequenceText.text = "";
        if (skillCheckPanel != null && skillCheckPanel.activeSelf)
        {
            skillCheckPanel.SetActive(false);
        }
    }

    // แสดงเฉพาะชุดตัว W A S D ตามลำดับ, highlightCharIndex = ตำแหน่งตัวอักษรที่ต้องกดตอนนี้ (-1 = ไม่ไฮไลต์)
    public void ShowSequence(string sequenceStr, int highlightCharIndex = -1)
    {
        HighlightCharIndex = highlightCharIndex;

        if (skillCheckPanel != null && !skillCheckPanel.activeSelf)
        {
            skillCheckPanel.SetActive(true);
        }

        if (sequenceText != null)
        {
            if (!sequenceText.gameObject.activeSelf)
                sequenceText.gameObject.SetActive(true);

            sequenceText.text = sequenceStr;
        }
    }
}