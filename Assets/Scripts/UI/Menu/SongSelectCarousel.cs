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
    [SerializeField] private string[] sceneNames = { "Cradle", "Level1.1", "" };

    [Header("Result labels for each song button")]
    [SerializeField] private string[] songNames = { "Sub Urban - Cradles", "Red VS White", "Coming Soon..." };
    [SerializeField] private string[] modeNames = { "NORMAL", "EASY", "NORMAL" };

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
        if (songScrollRect != null) lastScrollPosition = songScrollRect.verticalNormalizedPosition;

        initialized = true;
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
        if (songScrollRect == null && Mathf.Abs(wheel) > 0.01f && Time.unscaledTime >= nextWheelStep)
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
            songScrollRect.verticalNormalizedPosition = 1f - (float)selectedIndex / (cards.Count - 1);
            lastScrollPosition = songScrollRect.verticalNormalizedPosition;
        }
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
