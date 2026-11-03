using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        ApplySongInfo(r);

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

    void ApplySongInfo(LevelManager.Result result)
    {
        if (songNameText == null) songNameText = FindTextByName("SongName");
        if (modeText == null) modeText = FindTextByName("Mode");
        if (modeImage == null) modeImage = FindImageByName("Mode");

        string levelScene = !string.IsNullOrEmpty(result.levelScene)
            ? result.levelScene
            : SceneManager.GetActiveScene().name;

        string songName = MainMenu.SelectedSongName;
        string modeName = MainMenu.SelectedModeName;

        // ถ้าเริ่มเล่นจาก Editor หรือโหลดซีนตรงๆ ค่าที่เมนูส่งมาจะว่าง -> เดาจากซีนที่เล่น
        if (string.IsNullOrWhiteSpace(songName)) songName = SongNameForScene(levelScene);
        if (string.IsNullOrWhiteSpace(modeName)) modeName = ModeForScene(levelScene);

        if (songNameText != null && !string.IsNullOrWhiteSpace(songName)) songNameText.text = songName;
        if (modeText != null && !string.IsNullOrWhiteSpace(modeName)) modeText.text = modeName;

        if (modeImage != null && !string.IsNullOrWhiteSpace(modeName))
        {
            Sprite sprite = SpriteForMode(modeName);
            if (sprite != null)
            {
                modeImage.sprite = sprite;
                modeImage.enabled = true;
            }
        }
    }

    static TMP_Text FindTextByName(string objectName)
    {
        foreach (var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (text.gameObject.name == objectName) return text;
        return null;
    }

    static Image FindImageByName(string objectName)
    {
        foreach (var image in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (image.gameObject.name == objectName) return image;
        return null;
    }

    Sprite SpriteForMode(string modeName)
    {
        switch (modeName.Trim().ToUpperInvariant())
        {
            case "NORMAL": return normalModeSprite;
            case "EASY": return easyModeSprite;
            case "HARD": return hardModeSprite;
            default: return null;
        }
    }

    static string SongNameForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        if (sceneName.Contains("1.1") || sceneName.ToUpperInvariant().Contains("LEVEL2")) return "Red VS White";
        return "Sub Urban - Cradles";
    }

    static string ModeForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        return sceneName.Contains("1.1") || sceneName.ToUpperInvariant().Contains("LEVEL2") ? "EASY" : "NORMAL";
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
