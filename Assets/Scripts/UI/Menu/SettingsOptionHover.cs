using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Highlights one existing settings option when the pointer enters it.</summary>
public class SettingsOptionHover : MonoBehaviour, IPointerEnterHandler
{
    public enum Option { Audio, Gameplay }

    [SerializeField] private SettingsMenuController settingsMenu;
    [SerializeField] private Option option;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (settingsMenu == null) return;
        if (option == Option.Audio) settingsMenu.SelectAudio();
        else settingsMenu.SelectGameplay();
    }
}
