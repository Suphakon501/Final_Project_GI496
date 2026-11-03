using TMPro;
using UnityEngine;

using UnityEngine.UI;

public class ResultScreen : MonoBehaviour
{
    [Header("Result Display (optional)")]
    [SerializeField] private SpriteNumber scoreNumber;
    [SerializeField] private SpriteNumber comboNumber;
    [SerializeField] private SpriteNumber bestNumber;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private GameObject newBestObject;

    [Header("Hit Counts")]
    [SerializeField] private TMP_Text perfectCountText;
    [SerializeField] private TMP_Text goodCountText;
    [SerializeField] private TMP_Text badCountText;
    [SerializeField] private TMP_Text missCountText;
    [SerializeField] private TMP_Text maxComboCountText;

    [Header("Selected Song Info")]
    [SerializeField] private TMP_Text songNameText;
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private Image modeImage;
    [SerializeField] private Sprite normalModeSprite;
    [SerializeField] private Sprite easyModeSprite;
    [SerializeField] private Sprite hardModeSprite;

    [Header("Calories Estimate")]
    [SerializeField] private TMP_Text kcalText;
    [SerializeField, Min(1)] private int scorePointsPerKcal = 3000;

    [Header("Scene")]
    [SerializeField] private string menuScene = "Menu";
    [SerializeField] private string fallbackLevelScene = "Level1";

    [Header("Shortcuts")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private KeyCode menuKey = KeyCode.Escape;

    void Start()
    {
        Time.timeScale = 1f;
        var r = LevelManager.LastResult;

        if (songNameText == null || modeText == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                if (songNameText == null)
                {
                    Transform songObject = canvas.transform.Find("SongName");
                    if (songObject != null) songNameText = songObject.GetComponent<TMP_Text>();
                }
                if (modeText == null)
                {
                    Transform modeObject = canvas.transform.Find("Mode");
                    if (modeObject != null)
                    {
                        modeText = modeObject.GetComponent<TMP_Text>();
                        if (modeImage == null) modeImage = modeObject.GetComponent<Image>();
                    }
                }
            }
        }

        if (songNameText != null && !string.IsNullOrWhiteSpace(MainMenu.SelectedSongName))
            songNameText.text = MainMenu.SelectedSongName;
        if (modeText != null && !string.IsNullOrWhiteSpace(MainMenu.SelectedModeName))
            modeText.text = MainMenu.SelectedModeName;
        if (modeImage != null && !string.IsNullOrWhiteSpace(MainMenu.SelectedModeName))
        {
            switch (MainMenu.SelectedModeName.Trim().ToUpperInvariant())
            {
                case "NORMAL": if (normalModeSprite != null) modeImage.sprite = normalModeSprite; break;
                case "EASY": if (easyModeSprite != null) modeImage.sprite = easyModeSprite; break;
                case "HARD": if (hardModeSprite != null) modeImage.sprite = hardModeSprite; break;
            }
        }

        if (scoreNumber != null) scoreNumber.SetValue(r.score, false);
        if (comboNumber != null) comboNumber.SetValue(r.maxCombo, false);
        if (bestNumber != null) bestNumber.SetValue(r.bestScore, false);

        if (scoreText != null) scoreText.text = r.score.ToString("N0");
        if (comboText != null) comboText.text = r.maxCombo.ToString();
        if (bestText != null) bestText.text = r.bestScore.ToString("N0");

        if (perfectCountText != null) perfectCountText.text = r.perfectCount.ToString();
        if (goodCountText != null) goodCountText.text = r.goodCount.ToString();
        if (badCountText != null) badCountText.text = r.badCount.ToString();
        if (missCountText != null) missCountText.text = r.missCount.ToString();
        if (maxComboCountText != null) maxComboCountText.text = r.maxCombo.ToString();
        if (kcalText != null)
        {
            int estimatedKcal = Mathf.RoundToInt(Mathf.Max(0, r.score) / (float)Mathf.Max(1, scorePointsPerKcal));
            kcalText.text = estimatedKcal.ToString();
        }

        if (newBestObject != null) newBestObject.SetActive(r.newBest);
    }

    void Update()
    {
        if (Input.GetKeyDown(restartKey)) Restart();
        else if (Input.GetKeyDown(menuKey)) BackToMenu();
    }

    public void Restart()
    {
        string level = LevelManager.HasResult && !string.IsNullOrEmpty(LevelManager.LastResult.levelScene)
            ? LevelManager.LastResult.levelScene
            : fallbackLevelScene;
        SceneTransition.Load(level);
    }

    public void BackToMenu()
    {
        SceneTransition.Load(menuScene);
    }
}
