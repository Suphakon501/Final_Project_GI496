using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Hover selection and page navigation for user-authored settings UI. Creates no UI.</summary>
public class SettingsMenuController : MonoBehaviour
{
    private enum Page { Categories, Audio, Gameplay }
    private enum Category { Audio, Gameplay }

    [Header("Existing UI panels")]
    [SerializeField] private GameObject categoriesPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject gameplayPanel;

    [Header("Existing category buttons and labels")]
    [SerializeField] private Button audioButton;
    [SerializeField] private Button gameplayButton;
    [SerializeField] private TMP_Text audioLabel;
    [SerializeField] private TMP_Text gameplayLabel;
    [SerializeField] private GameObject audioArrow;
    [SerializeField] private GameObject gameplayArrow;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Main menu")]
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private AudioSettingsPanel audioSettings;

    private Page currentPage = Page.Categories;
    private Category selectedCategory = Category.Audio;
    private string audioLabelText;
    private string gameplayLabelText;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Awake()
    {
        if (mainMenu == null) mainMenu = FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (audioSettings == null) audioSettings = FindObjectOfType<AudioSettingsPanel>();
        FindOptionReferences();
        OpenCategories();
    }

    private void Update()
    {
        if (currentPage != Page.Categories || EventSystem.current == null)
        {
            HideHover();
            return;
        }

        Vector2 pointerPosition;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) { HideHover(); return; }
        pointerPosition = Mouse.current.position.ReadValue();
#else
        pointerPosition = Input.mousePosition;
#endif

        var pointer = new PointerEventData(EventSystem.current) { position = pointerPosition };
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointer, raycastResults);
        foreach (var hit in raycastResults)
        {
            var button = hit.gameObject.GetComponentInParent<Button>();
            if (button == audioButton) { ShowHover(Category.Audio); return; }
            if (button == gameplayButton) { ShowHover(Category.Gameplay); return; }
        }
        HideHover();
    }

    public void OpenCategories()
    {
        if (currentPage == Page.Audio && audioSettings != null) audioSettings.CancelChanges();
        currentPage = Page.Categories;
        if (categoriesPanel != null) categoriesPanel.SetActive(true);
        if (audioPanel != null) audioPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        HideHover();
    }

    public void SelectAudio()
    {
        ShowHover(Category.Audio);
        OpenAudio();
    }

    public void SelectGameplay()
    {
        ShowHover(Category.Gameplay);
        OpenGameplay();
    }

    public void Confirm()
    {
        if (currentPage == Page.Categories)
        {
            if (selectedCategory == Category.Audio) OpenAudio();
            else OpenGameplay();
            return;
        }
        if (currentPage == Page.Audio)
        {
            if (audioSettings != null) audioSettings.ConfirmChanges();
            ReturnToMainMenu();
            return;
        }
        OpenCategories();
    }

    public void Back()
    {
        if (currentPage == Page.Audio)
        {
            if (audioSettings != null) audioSettings.CancelChanges();
            OpenCategories();
        }
        else if (currentPage == Page.Gameplay) OpenCategories();
        else if (mainMenu != null) mainMenu.OnSettingsBackClicked();
    }

    public void CancelAudioAndReturnToCategories()
    {
        if (audioSettings != null) audioSettings.CancelChanges();
        currentPage = Page.Audio;
        OpenCategories();
    }

    public void ConfirmAudioAndReturnToMenu()
    {
        if (audioSettings != null) audioSettings.ConfirmChanges();
        ReturnToMainMenu();
    }

    private void ReturnToMainMenu()
    {
        if (audioPanel != null) audioPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (categoriesPanel != null) categoriesPanel.SetActive(false);

        if (mainMenu == null) mainMenu = FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (mainMenu != null)
        {
            mainMenu.OnSettingsBackClicked();
            return;
        }

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.gameObject.name == "Canvas_Settingpanel") canvas.gameObject.SetActive(false);
            else if (canvas.gameObject.name == "Canvas_Mainpanel") canvas.gameObject.SetActive(true);
        }
    }

    public void CancelPendingChanges()
    {
        if (currentPage == Page.Audio && audioSettings != null) audioSettings.CancelChanges();
    }

    private void ShowHover(Category category)
    {
        selectedCategory = category;
        if (audioLabel != null)
        {
            audioLabel.color = category == Category.Audio ? selectedColor : unselectedColor;
            audioLabel.text = (category == Category.Audio ? "> " : "") + audioLabelText;
        }
        if (gameplayLabel != null)
        {
            gameplayLabel.color = category == Category.Gameplay ? selectedColor : unselectedColor;
            gameplayLabel.text = (category == Category.Gameplay ? "> " : "") + gameplayLabelText;
        }
        if (audioArrow != null) audioArrow.SetActive(category == Category.Audio);
        if (gameplayArrow != null) gameplayArrow.SetActive(category == Category.Gameplay);
    }

    private void HideHover()
    {
        if (audioLabel != null)
        {
            audioLabel.text = audioLabelText;
            audioLabel.color = unselectedColor;
        }
        if (gameplayLabel != null)
        {
            gameplayLabel.text = gameplayLabelText;
            gameplayLabel.color = unselectedColor;
        }
        if (audioArrow != null) audioArrow.SetActive(false);
        if (gameplayArrow != null) gameplayArrow.SetActive(false);
    }

    private void OpenAudio()
    {
        currentPage = Page.Audio;
        if (audioSettings != null) audioSettings.BeginEdit();
        HideHover();
        if (categoriesPanel != null) categoriesPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(true);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
    }

    private void OpenGameplay()
    {
        currentPage = Page.Gameplay;
        HideHover();
        if (categoriesPanel != null) categoriesPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
    }

    private void FindOptionReferences()
    {
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (audioButton == null && button.gameObject.name.Contains("Audio")) audioButton = button;
            if (gameplayButton == null && button.gameObject.name.Contains("Gameplay")) gameplayButton = button;
        }
        if (audioButton == null && audioLabel != null) audioButton = audioLabel.GetComponentInParent<Button>();
        if (gameplayButton == null && gameplayLabel != null) gameplayButton = gameplayLabel.GetComponentInParent<Button>();
        if (audioLabel == null && audioButton != null) audioLabel = audioButton.GetComponentInChildren<TMP_Text>(true);
        if (gameplayLabel == null && gameplayButton != null) gameplayLabel = gameplayButton.GetComponentInChildren<TMP_Text>(true);
        if (audioLabel != null) audioLabelText = audioLabel.text.Replace("▶ ", "").Replace("> ", "");
        if (gameplayLabel != null) gameplayLabelText = gameplayLabel.text.Replace("▶ ", "").Replace("> ", "");
    }
}

