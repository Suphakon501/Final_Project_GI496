using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private string fallbackLevelScene = "Cradle";

    [Header("Keyboard Navigation")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    void Start()
    {
        TVPowerTransition.PlayOpen();
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

        FindResultButtons();
        ConfigureButtonNavigation();
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
        StartCoroutine(FocusRestartButtonNextFrame());
    }

    void FindResultButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.gameObject.name == "Restart") restartButton = button;
            else if (button.gameObject.name == "BackToMenu") menuButton = button;
        }
    }

    void ConfigureButtonNavigation()
    {
        if (restartButton == null || menuButton == null) return;

        Navigation restartNavigation = restartButton.navigation;
        restartNavigation.mode = Navigation.Mode.Explicit;
        restartNavigation.selectOnUp = menuButton;
        restartNavigation.selectOnDown = menuButton;
        restartNavigation.selectOnLeft = menuButton;
        restartNavigation.selectOnRight = menuButton;
        restartButton.navigation = restartNavigation;

        Navigation menuNavigation = menuButton.navigation;
        menuNavigation.mode = Navigation.Mode.Explicit;
        menuNavigation.selectOnUp = restartButton;
        menuNavigation.selectOnDown = restartButton;
        menuNavigation.selectOnLeft = restartButton;
        menuNavigation.selectOnRight = restartButton;
        menuButton.navigation = menuNavigation;
    }

    IEnumerator FocusRestartButtonNextFrame()
    {
        yield return null;
        if (EventSystem.current == null || restartButton == null || !restartButton.interactable) yield break;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
    }

    void ApplySongInfo(LevelManager.Result result)
    {
        if (songNameText == null) songNameText = FindTextByName("SongName");
        if (modeText == null) modeText = FindTextByName("Mode");
        if (modeImage == null) modeImage = FindImageByName("Mode");

        string levelScene = !string.IsNullOrEmpty(result.levelScene)
            ? result.levelScene
            : SceneManager.GetActiveScene().name;

        // ใช้ซีนที่เล่นจริงเป็นหลัก ป้องกันค่า static จากเพลงก่อนหน้าค้างข้ามซีน
        string songName = SongNameForScene(levelScene);
        if (string.IsNullOrWhiteSpace(songName)) songName = MainMenu.SelectedSongName;
        string modeName = ModeForScene(levelScene);
        if (string.IsNullOrWhiteSpace(modeName)) modeName = MainMenu.SelectedModeName;

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
        switch (sceneName.Trim().ToUpperInvariant())
        {
            case "CRADLE":
            case "LEVEL1":
                return "Sub Urban - Cradles";
            case "BOMFUNK":
                return "Bomfunk MC's - Uprocking Beats";
            case "MOVES LIKE JAGGER":
                return "Moves Like Jagger - Maroon 5";
            default:
                return null;
        }
    }

    static string ModeForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        switch (sceneName.Trim().ToUpperInvariant())
        {
            case "CRADLE":
            case "LEVEL1":
                return "EASY";
            case "BOMFUNK":
                return "NORMAL";
            case "MOVES LIKE JAGGER":
                return "HARD";
            default:
                return null;
        }
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
