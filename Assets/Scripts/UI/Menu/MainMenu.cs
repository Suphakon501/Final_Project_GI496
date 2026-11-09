using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public static string SelectedSongName { get; private set; }
    public static string SelectedModeName { get; private set; }

    [Header("Panels (assign these in the Inspector)")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject mangaPanel;
    [SerializeField] private Image mangaPageImage;
    [SerializeField] private Sprite[] mangaPages;
    [SerializeField] private Button mangaBackButton;
    [SerializeField] private Button mangaNextButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Image settingsBackdrop;
    [SerializeField] private SettingsMenuController settingsNavigation;
    [SerializeField, Range(0f, 1f)] private float settingsBackdropAlpha = 0.58f;

    [Header("Scene Names")]
    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private string level1Scene = "Cradle";

    private static bool openLevelSelectOnStart;
    private Animator levelPanelAnimator;
    private readonly Button[] mainMenuOptions = new Button[3];
    private int mangaPageIndex;

    private void Awake()
    {
        ConfigureMainMenuKeyboardNavigation();

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
        if (mangaPanel != null) mangaPanel.SetActive(false);
        if (levelPanel != null)
        {
            levelPanelAnimator = levelPanel.GetComponent<Animator>();
            if (levelPanelAnimator != null) levelPanelAnimator.enabled = false;
        }
        if (levelPanel != null)
        {
            var carousel = levelPanel.GetComponent<SongSelectCarousel>();
            if (carousel == null) carousel = levelPanel.AddComponent<SongSelectCarousel>();
            carousel.Initialize(this);
        }
        if (levelPanel != null) levelPanel.SetActive(false);
        if (settingsNavigation == null) settingsNavigation = GetComponent<SettingsMenuController>();
        if (settingsNavigation == null && settingsPanel != null)
            settingsNavigation = settingsPanel.GetComponentInChildren<SettingsMenuController>(true);
        if (settingsNavigation == null) settingsNavigation = FindFirstObjectByType<SettingsMenuController>(FindObjectsInactive.Include);
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

    public void OnPlayClicked()
    {
        if (mangaPanel == null || mangaPageImage == null || mangaPages == null || mangaPages.Length == 0)
        {
            Debug.LogWarning("MainMenu: assign Manga Panel, Manga Page Image, and Manga Pages in the Inspector.");
            ShowLevelPanel();
            return;
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelPanel != null) levelPanel.SetActive(false);

        mangaPageIndex = 0;
        ShowMangaPage();
        mangaPanel.SetActive(true);
        ConfigureMangaButtonNavigation();
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
        StartCoroutine(FocusMangaButtonNextFrame(mangaNextButton != null ? mangaNextButton : mangaBackButton));
    }

    public void OnMangaNextClicked()
    {
        if (!HasMangaPages()) return;

        if (mangaPageIndex < mangaPages.Length - 1)
        {
            mangaPageIndex++;
            ShowMangaPage();
        }
        else
        {
            if (mangaPanel != null) mangaPanel.SetActive(false);
            ShowLevelPanel();
        }
    }

    public void OnMangaBackClicked()
    {
        if (!HasMangaPages()) return;

        if (mangaPageIndex > 0)
        {
            mangaPageIndex--;
            ShowMangaPage();
        }
        else
        {
            if (mangaPanel != null) mangaPanel.SetActive(false);
            ShowMainPanel();
        }
    }

    private bool HasMangaPages()
    {
        if (mangaPanel != null && mangaPageImage != null && mangaPages != null && mangaPages.Length > 0)
            return true;

        Debug.LogWarning("MainMenu: assign Manga Panel, Manga Page Image, and Manga Pages in the Inspector.");
        return false;
    }

    private void ShowMangaPage()
    {
        mangaPageImage.sprite = mangaPages[mangaPageIndex];
    }

    private void ConfigureMangaButtonNavigation()
    {
        if (mangaBackButton == null || mangaNextButton == null) return;

        Navigation backNavigation = mangaBackButton.navigation;
        backNavigation.mode = Navigation.Mode.Explicit;
        backNavigation.selectOnLeft = mangaNextButton;
        backNavigation.selectOnRight = mangaNextButton;
        backNavigation.selectOnUp = mangaNextButton;
        backNavigation.selectOnDown = mangaNextButton;
        mangaBackButton.navigation = backNavigation;

        Navigation nextNavigation = mangaNextButton.navigation;
        nextNavigation.mode = Navigation.Mode.Explicit;
        nextNavigation.selectOnLeft = mangaBackButton;
        nextNavigation.selectOnRight = mangaBackButton;
        nextNavigation.selectOnUp = mangaBackButton;
        nextNavigation.selectOnDown = mangaBackButton;
        mangaNextButton.navigation = nextNavigation;
    }

    private static void FocusMangaButton(Button button)
    {
        if (EventSystem.current == null || button == null || !button.interactable) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private IEnumerator FocusMangaButtonNextFrame(Button button)
    {
        yield return null;
        if (mangaPanel != null && mangaPanel.activeInHierarchy)
            FocusMangaButton(button);
    }

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

    public void OnLevel1Clicked() => LoadSongScene(level1Scene, "Sub Urban - Cradles", "NORMAL");
    // The second available song is stored in the Level1.1 scene in this project.
    public void OnLevel2Clicked() => LoadSongScene("Level1.1", "Red VS White", "EASY");
    // Level 3 is currently a Coming Soon slot in the song selector.
    public void OnLevel3Clicked() { }
    public void OnBackClicked() => ShowMainPanel();
    public void LoadSongScene(string sceneName)
    {
        if (sceneName == level1Scene || sceneName == "Level1" || sceneName == "Cradle")
            LoadSongScene(sceneName, "Sub Urban - Cradles", "NORMAL");
        else if (sceneName == "Level1.1")
            LoadSongScene(sceneName, "Red VS White", "EASY");
        else
            LoadSongScene(sceneName, sceneName, "NORMAL");
    }

    public void LoadSongScene(string sceneName, string songName, string modeName)
    {
        SelectedSongName = songName;
        SelectedModeName = modeName;
        LoadScene(sceneName);
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
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mangaPanel != null) mangaPanel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
        if (mainPanel != null) mainPanel.SetActive(true);
        if (levelPanel != null) levelPanel.SetActive(false);
        FocusFirstMainMenuOption();
    }

    private void ConfigureMainMenuKeyboardNavigation()
    {
        if (mainPanel == null) return;

        string[] buttonNames = { "PlayBT", "SettingBT", "ExitBT" };
        for (int i = 0; i < buttonNames.Length; i++)
        {
            Transform buttonTransform = mainPanel.transform.Find(buttonNames[i]);
            if (buttonTransform != null) mainMenuOptions[i] = buttonTransform.GetComponent<Button>();
        }

        for (int i = 0; i < mainMenuOptions.Length; i++)
        {
            Button button = mainMenuOptions[i];
            if (button == null) continue;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = mainMenuOptions[(i + mainMenuOptions.Length - 1) % mainMenuOptions.Length];
            navigation.selectOnDown = mainMenuOptions[(i + 1) % mainMenuOptions.Length];
            button.navigation = navigation;
        }
    }

    private void FocusFirstMainMenuOption()
    {
        if (EventSystem.current == null || mainMenuOptions[0] == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(mainMenuOptions[0].gameObject);
    }

    private void ShowLevelPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mangaPanel != null) mangaPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (levelPanel != null)
        {
            levelPanel.SetActive(true);
            if (levelPanelAnimator != null)
            {
                levelPanelAnimator.enabled = true;
                levelPanelAnimator.Play("LevelSelectUI", 0, 0f);
                levelPanelAnimator.Update(0f);
            }
        }
    }

    private void LoadScene(string sceneName) => SceneTransition.Load(sceneName);
}
