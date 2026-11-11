using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Adds carousel input to the song buttons already laid out in the Unity Editor.
/// This component intentionally leaves all UI art, text, colors, and positions editable.
/// </summary>
public class SongSelectCarousel : MonoBehaviour
{
    [Header("Assign buttons in display order. If empty, the default Level buttons are found by name.")]
    [SerializeField] private Button[] songButtons = new Button[3];

    [Header("Optional Scroll View. When assigned, buttons are read from its Content children.")]
    [SerializeField] private ScrollRect songScrollRect;

    [Header("Scene to load for each button. Leave blank for Coming Soon entries.")]
    [SerializeField] private string[] sceneNames = { "Cradle", "Bomfunk", "Moves Like Jagger" };

    [Header("Result labels for each song button")]
    [SerializeField] private string[] songNames = { "Sub Urban - Cradles", "Bomfunk MC's - Uprocking Beats", "Moves Like Jagger - Maroon 5" };
    [SerializeField] private string[] modeNames = { "EASY", "NORMAL", "HARD" };

    [Header("Selection animation")]
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float unselectedScale = 0.88f;
    [SerializeField] private float animationSpeed = 12f;

    private static readonly string[] DefaultButtonNames = { "Level1BT", "Level2BT", "Level3BT" };
    private readonly List<Button> cards = new List<Button>();
    private readonly List<Vector3> baseScales = new List<Vector3>();
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private MainMenu mainMenu;
    private int selectedIndex;
    private bool initialized;
    private bool scrollContentPrepared;
    private float nextWheelStep;
    private Vector2 lastPointerPosition;
    private bool hasPointerPosition;
    private float layoutScale = 1f;

    public void Initialize(MainMenu owner)
    {
        mainMenu = owner;
        if (initialized) return;

        CollectButtons();
        for (int i = 0; i < cards.Count; i++)
        {
            baseScales.Add(cards[i] != null ? cards[i].transform.localScale : Vector3.one);
        }
        if (songScrollRect != null)
        {
            // Keep all existing song buttons visible together; input changes selection only.
            songScrollRect.scrollSensitivity = 0f;
            FitExistingCardsInViewport();
        }

        initialized = true;
    }

    public void ResetSelectionToFirst()
    {
        if (!initialized || cards.Count == 0) return;
        selectedIndex = 0;
        if (songScrollRect != null && !scrollContentPrepared) FitExistingCardsInViewport();
    }

    private void CollectButtons()
    {
        cards.Clear();
        if (songScrollRect != null && songScrollRect.content != null)
        {
            cards.AddRange(songScrollRect.content.GetComponentsInChildren<Button>(true));
            // Include existing placeholder buttons so all four can be navigated.
            return;
        }

        if (songButtons != null)
            foreach (Button button in songButtons)
                if (button != null && !cards.Contains(button)) cards.Add(button);

        // Keep the original three-button hierarchy working without a Scroll View.
        if (cards.Count > 0) return;
        foreach (string buttonName in DefaultButtonNames)
        {
            Transform child = transform.Find(buttonName);
            Button button = child != null ? child.GetComponent<Button>() : null;
            if (button != null) cards.Add(button);
        }
    }

    private void Update()
    {
        if (!initialized) return;
        if (songScrollRect != null && !scrollContentPrepared)
            FitExistingCardsInViewport();
        ReadPointerHover();

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow)) ChangeSelection(1);
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow)) ChangeSelection(-1);

        float wheel = Input.mouseScrollDelta.y;
        RectTransform scrollViewport = songScrollRect != null
            ? (songScrollRect.viewport != null ? songScrollRect.viewport : songScrollRect.transform as RectTransform)
            : null;
        bool pointerOverScroll = scrollViewport == null ||
            RectTransformUtility.RectangleContainsScreenPoint(scrollViewport, Input.mousePosition);
        if (Mathf.Abs(wheel) > 0.01f && pointerOverScroll && Time.unscaledTime >= nextWheelStep)
        {
            ChangeSelection(wheel < 0f ? 1 : -1);
            nextWheelStep = Time.unscaledTime + 0.16f;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            LaunchSelectedSong();

        for (int i = 0; i < cards.Count && i < baseScales.Count; i++)
        {
            if (cards[i] == null) continue;
            float multiplier = i == selectedIndex ? selectedScale : unselectedScale;
            Vector3 targetScale = baseScales[i] * (layoutScale * multiplier);
            cards[i].transform.localScale = Vector3.Lerp(cards[i].transform.localScale, targetScale,
                1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime));
        }
    }

    private void ReadPointerHover()
    {
        if (EventSystem.current == null) return;
        Vector2 pointerPosition = Input.mousePosition;
        if (hasPointerPosition && (pointerPosition - lastPointerPosition).sqrMagnitude < 1f) return;
        hasPointerPosition = true;
        lastPointerPosition = pointerPosition;

        var pointer = new PointerEventData(EventSystem.current) { position = pointerPosition };
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointer, raycastResults);
        foreach (RaycastResult hit in raycastResults)
        {
            Button hitButton = hit.gameObject.GetComponentInParent<Button>();
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == hitButton) { SetSelection(i); return; }
            }
        }
    }

    private void ChangeSelection(int direction)
    {
        if (cards.Count == 0) return;
        for (int step = 1; step <= cards.Count; step++)
        {
            int candidate = (selectedIndex + direction * step + cards.Count * 2) % cards.Count;
            if (cards[candidate] != null)
            {
                SetSelection(candidate);
                return;
            }
        }
    }

    private void SetSelection(int index)
    {
        if (index < 0 || index >= cards.Count || cards[index] == null) return;
        selectedIndex = index;
        if (EventSystem.current != null && cards[index].gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(cards[index].gameObject);
    }

    private void FitExistingCardsInViewport()
    {
        if (songScrollRect == null || songScrollRect.content == null || cards.Count == 0) return;

        RectTransform content = songScrollRect.content;
        RectTransform viewport = songScrollRect.viewport != null
            ? songScrollRect.viewport
            : songScrollRect.transform as RectTransform;
        if (viewport == null || content.pivot.y < 0.99f) return;

        Canvas.ForceUpdateCanvases();
        if (viewport.rect.height <= 1f)
        {
            // Repair the existing zero-sized Viewport to fill its existing Scroll View.
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        }

        float viewportHeight = viewport.rect.height * viewport.lossyScale.y / content.lossyScale.y;
        if (viewportHeight <= 0f) return;

        // This is a static selector now, so turn off scrolling and fit the existing cards inside it.
        songScrollRect.StopMovement();
        songScrollRect.vertical = false;
        if (songScrollRect.verticalScrollbar != null)
            songScrollRect.verticalScrollbar.gameObject.SetActive(false);
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewportHeight);
        Canvas.ForceUpdateCanvases();

        float[] rowHeights = new float[cards.Count];
        float totalMaximumHeight = 0f;
        float maximumMultiplier = Mathf.Max(selectedScale, unselectedScale);
        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform rect = cards[i] != null ? cards[i].transform as RectTransform : null;
            if (rect == null) continue;
            rect.localScale = i < baseScales.Count ? baseScales[i] : Vector3.one;
            rowHeights[i] = RectTransformUtility.CalculateRelativeRectTransformBounds(content, rect).size.y;
            totalMaximumHeight += rowHeights[i] * maximumMultiplier;
        }

        const float gap = 8f;
        float gapsHeight = gap * (cards.Count + 1);
        float availableForCards = Mathf.Max(1f, viewportHeight - gapsHeight);
        layoutScale = totalMaximumHeight > availableForCards
            ? availableForCards / totalMaximumHeight
            : 1f;

        float totalSlotHeight = 0f;
        for (int i = 0; i < rowHeights.Length; i++)
            totalSlotHeight += rowHeights[i] * maximumMultiplier * layoutScale;
        float slotGap = Mathf.Max(gap, (viewportHeight - totalSlotHeight) / (cards.Count + 1));
        float cursorFromTop = slotGap;
        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform rect = cards[i] != null ? cards[i].transform as RectTransform : null;
            if (rect == null) continue;

            float slotHeight = rowHeights[i] * maximumMultiplier * layoutScale;
            Vector2 position = rect.anchoredPosition;
            position.y = viewportHeight * 0.5f - cursorFromTop - slotHeight * 0.5f;
            rect.anchoredPosition = position;
            cursorFromTop += slotHeight + slotGap;
        }

        songScrollRect.StopMovement();
        songScrollRect.Rebuild(CanvasUpdate.PostLayout);
        songScrollRect.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();
        songScrollRect.Rebuild(CanvasUpdate.PostLayout);
        scrollContentPrepared = true;
    }

    public void LaunchSelectedSong()
    {
        if (mainMenu == null || sceneNames == null || selectedIndex < 0 || selectedIndex >= sceneNames.Length) return;
        string sceneName = sceneNames[selectedIndex];
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            string songName = songNames != null && selectedIndex < songNames.Length
                ? songNames[selectedIndex]
                : sceneName;
            string modeName = modeNames != null && selectedIndex < modeNames.Length
                ? modeNames[selectedIndex]
                : "NORMAL";
            mainMenu.LoadSongScene(sceneName, songName, modeName);
        }
    }
}
