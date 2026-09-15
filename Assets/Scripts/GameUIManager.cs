using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("UI Panel")]
    public GameObject skillCheckPanel;
    public TextMeshProUGUI sequenceText;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // บังคับซ่อน UI ปุ่มทั้งหมดทันทีตั้งแต่เริ่มเกม (รวมช่วงหน่วง 3 วิแรกด้วย)
        HideAllUI();
    }

    public void HideAllUI()
    {
        if (sequenceText != null) sequenceText.text = "";
        if (skillCheckPanel != null && skillCheckPanel.activeSelf)
        {
            skillCheckPanel.SetActive(false);
        }
    }

    // แสดงเฉพาะชุดปุ่ม W A S D กลางจอ
    public void ShowSequence(string sequenceStr)
    {
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