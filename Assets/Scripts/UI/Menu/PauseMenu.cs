using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// ���� Pause 㹴�ҹ: �� Esc (���ͻ��� UI) ������ش�� �������͡ ��蹵�� / �������ҹ���� / ���͡��ҹ / ��Ѻ����
/// �ҧʤ�Ի���麹 GameObject ��� "�Դ�����ʹ" (�� PauseManager) �����ҧ�� pausePanel
/// ���� Panel ���١�Դ������Ѻ��á����� Esc
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>ʤ�Ի������蹵���Ф�������ҵ͹��� pause �������: if (PauseMenu.IsPaused) return;</summary>
    public static bool IsPaused { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;      // Panel ����ջ��������� (������鹻Դ���)

    [Header("������Ѻ MenuManager (ʤ�Ի�� MainMenu) 㹴�ҹ���")]
    [SerializeField] private MainMenu menuManager;       // ��͹��Ѻ���� / ���͡��ҹ

    [Header("��駤��")]
    [SerializeField] private bool pauseAudio = false;    // true = ��ش���§�������͹ pause (��������ŧ��蹵��)
    [SerializeField] private bool unlockCursor = true;   // �ʴ������͹ pause

    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    private void Start()
    {
        IsPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (PausePressed())
            TogglePause();
    }

    private void OnDestroy()
    {
        // �ѹ�ó�����¹ Scene �͹�ѧ pause ���� ��������ҧ
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    // ---------- ����� Pause Panel (�١��� On Click) ----------

    /// <summary>������ش����˹�Ҩ� (����Ѻ��Ͷ��/�����) �������¡�ҡ������</summary>
    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        Time.timeScale = 0f;
        if (pauseAudio) AudioListener.pause = true;
        if (pausePanel != null) pausePanel.SetActive(true);

        if (unlockCursor)
        {
            previousLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>���� "��蹵��"</summary>
    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pausePanel != null) pausePanel.SetActive(false);


        if (unlockCursor)
        {
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    /// <summary>���� "�������ҹ����"</summary>
    public void Restart()
    {
        ResetPauseState();
        SceneTransition.Load(SceneManager.GetActiveScene().name);
    }

    /// <summary>���� "���͡��ҹ" -> ��Ѻ���������Դ˹�����͡��ҹ���</summary>
    public void GoToLevelSelect()
    {
        ResetPauseState();
        if (menuManager != null) menuManager.GoToLevelSelect();
        else Debug.LogWarning("PauseMenu: �ѧ�����١ Menu Manager");
    }

    /// <summary>���� "��Ѻ������ѡ"</summary>
    public void GoToMainMenu()
    {
        ResetPauseState();
        if (menuManager != null) menuManager.GoToMenu();
        else Debug.LogWarning("PauseMenu: �ѧ�����١ Menu Manager");
    }

    // ---------- �ѧ��ѹ���� ----------

    private void ResetPauseState()
    {
        // �׹��� timeScale ��͹����¹ Scene (SceneTransition ������Ẻ unscaled ��������)
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pausePanel != null) pausePanel.SetActive(false);

    }

    private static bool PausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}




