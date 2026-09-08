using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("UI Panel")]
    public GameObject skillCheckPanel;     
    public TextMeshProUGUI sequenceText;     
    public RectTransform redLineCursor;      
    public RectTransform skillCheckBG;       

    [Header("Color Zones")]
    public RectTransform greenZone;
    public RectTransform yellowZone;

    private float barTravelDistance;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (skillCheckBG != null)
        {
            barTravelDistance = skillCheckBG.rect.width / 2f;
        }
        HideAllUI();
    }

    public void HideAllUI()
    {
        if (skillCheckPanel != null) skillCheckPanel.SetActive(false);
    }

    public void ShowSequence(string sequenceStr)
    {
        if (skillCheckPanel != null && !skillCheckPanel.activeSelf)
            skillCheckPanel.SetActive(true);

        if (sequenceText != null)
        {
            sequenceText.gameObject.SetActive(true);
            sequenceText.text = sequenceStr;
        }

        if (redLineCursor != null)
        {
            redLineCursor.gameObject.SetActive(false); 
        }
    }

    public void ShowSkillCheck(float progress)
    {
        if (skillCheckPanel != null && !skillCheckPanel.activeSelf)
            skillCheckPanel.SetActive(true);

        if (sequenceText != null)
        {
            sequenceText.gameObject.SetActive(false); 
        }

        if (redLineCursor != null)
        {
            redLineCursor.gameObject.SetActive(true);
            float cursorX = Mathf.Lerp(-barTravelDistance, barTravelDistance, progress);
            redLineCursor.localPosition = new Vector3(cursorX, redLineCursor.localPosition.y, 0);
        }
    }
    public string CheckHitZone()
    {
        if (redLineCursor == null) return "Bad";

        if (greenZone != null && RectTransformUtility.RectangleContainsScreenPoint(greenZone, redLineCursor.position, null))
        {
            return "Perfect";
        }
        if (yellowZone != null && RectTransformUtility.RectangleContainsScreenPoint(yellowZone, redLineCursor.position, null))
        {
            return "Good";
        }
        return "Bad";
    }
}