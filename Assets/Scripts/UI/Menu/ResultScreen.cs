using TMPro;
using UnityEngine;

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

        if (scoreNumber != null) scoreNumber.SetValue(r.score, false);
        if (comboNumber != null) comboNumber.SetValue(r.maxCombo, false);
        if (bestNumber != null) bestNumber.SetValue(r.bestScore, false);

        if (scoreText != null) scoreText.text = r.score.ToString("N0");
        if (comboText != null) comboText.text = r.maxCombo.ToString();
        if (bestText != null) bestText.text = r.bestScore.ToString("N0");

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
