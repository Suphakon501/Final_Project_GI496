using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Panels (assign these in the Inspector)")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Image settingsBackdrop;
    [SerializeField] private SettingsMenuController settingsNavigation;
    [SerializeField, Range(0f, 1f)] private float settingsBackdropAlpha = 0.58f;

    [Header("Scene Names")]
    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private string level1Scene = "Level1";
    [SerializeField] private string level2Scene = "Level2";
    [SerializeField] private string level3Scene = "Level3";

    private static bool openLevelSelectOnStart;

    private void Awake()
    {
        if (settingsPanel == null)
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.gameObject.name == "Canvas_Settingpanel")
                {
                    settingsPanel = canvas.gameObject;
                    break;
                }
            }
        }

        if (settingsBackdrop == null && settingsPanel != null)
        {
            var background = settingsPanel.transform.Find("Image");
            if (background != null) settingsBackdrop = background.GetComponent<Image>();
        }
        if (settingsBackdrop != null)
        {
            Color color = settingsBackdrop.color;
            color.a = settingsBackdropAlpha;
            settingsBackdrop.color = color;
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (levelPanel != null) levelPanel.SetActive(false);
        if (settingsNavigation == null) settingsNavigation = GetComponent<SettingsMenuController>();
        if (settingsNavigation == null && settingsPanel != null)
            settingsNavigation = settingsPanel.GetComponentInChildren<SettingsMenuController>(true);
        if (settingsNavigation == null) settingsNavigation = FindObjectOfType<SettingsMenuController>();
    }

    private void Start()
    {
        if (mainPanel == null || levelPanel == null) return;
        if (openLevelSelectOnStart)
        {
            openLevelSelectOnStart = false;
            ShowLevelPanel();
        }
        else ShowMainPanel();
    }

    public void OnPlayClicked() => ShowLevelPanel();

    public void OnSettingsClicked()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("MainMenu: Assign Canvas_Settingpanel to Settings Panel.");
            return;
        }
        if (levelPanel != null) levelPanel.SetActive(false);
        settingsPanel.SetActive(true);
        if (settingsNavigation != null) settingsNavigation.OpenCategories();
    }

    public void OnSettingsBackClicked()
    {
        if (settingsNavigation != null) settingsNavigation.CancelPendingChanges();
        ShowMainPanel();
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
    public void OnBackClicked() => ShowMainPanel();

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
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        if (levelPanel != null) levelPanel.SetActive(false);
    }

    private void ShowLevelPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelPanel != null) levelPanel.SetActive(true);
    }

    private void LoadScene(string sceneName) => SceneTransition.Load(sceneName);
}
