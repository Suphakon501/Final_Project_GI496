using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels (Menu scene only)")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelPanel;

    [Header("Scene Names")]
    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private string level1Scene = "Level1";
    [SerializeField] private string level2Scene = "Level2";
    [SerializeField] private string level3Scene = "Level3";

    private static bool openLevelSelectOnStart = false;

    private void Start()
    {
        if (mainPanel == null || levelPanel == null) return;

        if (openLevelSelectOnStart)
        {
            openLevelSelectOnStart = false;
            ShowLevelPanel();
        }
        else
        {
            ShowMainPanel();
        }
    }

    public void OnPlayClicked()
    {
        ShowLevelPanel();
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnLevel1Clicked() => LoadScene(level1Scene);
    public void OnLevel2Clicked() => LoadScene(level2Scene);
    public void OnLevel3Clicked() => LoadScene(level3Scene);

    public void OnBackClicked()
    {
        ShowMainPanel();
    }

    public void GoToMenu()
    {
        openLevelSelectOnStart = false;
        LoadScene(menuScene);
    }

    public void GoToLevelSelect()
    {
        openLevelSelectOnStart = true;
        LoadScene(menuScene);
    }

    private void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        levelPanel.SetActive(false);
    }

    private void ShowLevelPanel()
    {
        mainPanel.SetActive(false);
        levelPanel.SetActive(true);
    }

    private void LoadScene(string sceneName)
    {
        SceneTransition.Load(sceneName);
    }
}
