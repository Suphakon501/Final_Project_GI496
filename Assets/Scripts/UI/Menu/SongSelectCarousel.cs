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
    private float lastScrollPosition = -1f;

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
            // Route wheel input through the same cyclic selection as the keyboard.
            songScrollRect.scrollSensitivity = 0f;
            PrepareScrollContentForCentering();
            lastScrollPosition = songScrollRect.verticalNormalizedPosition;
        }

        initialized = true;
    }

    public void ResetSelectionToFirst()
    {
        if (!initialized || cards.Count == 0) return;
        selectedIndex = 0;
        if (songScrollRect == null) return;

        if (!scrollContentPrepared) PrepareScrollContentForCentering();
        if (scrollContentPrepared) CenterSelectedCard();
        lastScrollPosition = songScrollRect.verticalNormalizedPosition;
    }

    private void CollectButtons()
    {
        cards.Clear();
        if (songScrollRect != null && songScrollRect.content != null)
        {
            cards.AddRange(songScrollRect.content.GetComponentsInChildren<Button>(true));
            // The Scroll View handles wheel and scrollbar dragging. Keyboard selection keeps it in sync.
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
            PrepareScrollContentForCentering();
        ReadPointerHover();

        if (songScrollRect != null && cards.Count > 1)
        {
            float position = songScrollRect.verticalNormalizedPosition;
            if (Mathf.Abs(position - lastScrollPosition) > 0.001f)
            {
                int index = Mathf.Clamp(Mathf.RoundToInt((1f - position) * (cards.Count - 1)), 0, cards.Count - 1);
                SetSelection(index, false);
                lastScrollPosition = position;
            }
        }

        if (Input.GetKeyDown(KeyCode.DownArrow)) ChangeSelection(1);
        else if (Input.GetKeyDown(KeyCode.UpArrow)) ChangeSelection(-1);

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
            Vector3 targetScale = baseScales[i] * multiplier;
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
                if (cards[i] == hitButton) { SetSelection(i, false); return; }
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
                SetSelection(candidate, true);
                return;
            }
        }
    }

    private void SetSelection(int index, bool scrollToSelection)
    {
        if (index < 0 || index >= cards.Count || cards[index] == null) return;
        selectedIndex = index;
        if (EventSystem.current != null && cards[index].gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(cards[index].gameObject);

        if (scrollToSelection && songScrollRect != null && cards.Count > 1)
        {
            CenterSelectedCard();
            lastScrollPosition = songScrollRect.verticalNormalizedPosition;
        }
    }

    private void PrepareScrollContentForCentering()
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
            // Repair an un-sized existing Viewport so ScrollRect can measure and center its cards.
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        }

        float viewportHeight = viewport.rect.height * viewport.lossyScale.y / content.lossyScale.y;
        if (viewportHeight <= 0f) return;

        RectTransform firstCard = cards[0].transform as RectTransform;
        if (firstCard == null) return;

        Vector3 firstCenter = content.InverseTransformPoint(firstCard.TransformPoint(firstCard.rect.center));
        float topPadding = -viewportHeight * 0.5f - firstCenter.y;
        foreach (Button card in cards)
        {
            RectTransform rect = card != null ? card.transform as RectTransform : null;
            if (rect != null) rect.anchoredPosition += Vector2.up * topPadding;
        }

        Bounds cardBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, cards[0].transform);
        for (int i = 1; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, cards[i].transform);
            cardBounds.Encapsulate(bounds.min);
            cardBounds.Encapsulate(bounds.max);
        }

        float requiredHeight = -cardBounds.min.y + viewportHeight * 0.5f;
        if (requiredHeight > content.rect.height)
        {
            float[] savedCardCenterY = new float[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                RectTransform rect = cards[i].transform as RectTransform;
                savedCardCenterY[i] = content.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
            }

            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
            Canvas.ForceUpdateCanvases();

            // Preserve each card's content-local position when resizing moves its anchor reference.
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null) continue;
                RectTransform rect = cards[i].transform as RectTransform;
                float currentCenterY = content.InverseTransformPoint(rect.TransformPoint(rect.rect.center)).y;
                rect.anchoredPosition += Vector2.up * (savedCardCenterY[i] - currentCenterY);
            }
        }

        songScrollRect.StopMovement();
        songScrollRect.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();
        scrollContentPrepared = true;
    }

    private void CenterSelectedCard()
    {
        if (songScrollRect == null || songScrollRect.content == null || selectedIndex < 0 || selectedIndex >= cards.Count)
            return;

        if (!scrollContentPrepared)
        {
            PrepareScrollContentForCentering();
            if (!scrollContentPrepared) return;
        }

        RectTransform content = songScrollRect.content;
        RectTransform viewport = songScrollRect.viewport != null
            ? songScrollRect.viewport
            : songScrollRect.transform as RectTransform;
        RectTransform card = cards[selectedIndex] != null ? cards[selectedIndex].transform as RectTransform : null;
        if (viewport == null || card == null || content.pivot.y < 0.99f) return;

        Canvas.ForceUpdateCanvases();
        float viewportHeight = viewport.rect.height * viewport.lossyScale.y / content.lossyScale.y;
        float maxOffset = Mathf.Max(0f, content.rect.height - viewportHeight);
        Vector3 cardCenter = content.InverseTransformPoint(card.TransformPoint(card.rect.center));
        float desiredOffset = Mathf.Clamp(-cardCenter.y - viewportHeight * 0.5f, 0f, maxOffset);

        songScrollRect.StopMovement();
        songScrollRect.verticalNormalizedPosition = maxOffset > 0f ? 1f - desiredOffset / maxOffset : 1f;
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
