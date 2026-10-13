using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// เมนู Pause ในด่าน: กด Esc (หรือปุ่ม UI) เพื่อหยุดเกม แล้วเลือก เล่นต่อ / เริ่มด่านใหม่ / เลือกด่าน / กลับเมนู
/// วางสคริปต์นี้บน GameObject ที่ "เปิดอยู่ตลอด" (เช่น PauseManager) ห้ามวางบน pausePanel
/// เพราะ Panel ที่ถูกปิดจะไม่รับการกดปุ่ม Esc
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>สคริปต์อื่นเช่นตัวละครเช็คได้ว่าตอนนี้ pause อยู่ไหม: if (PauseMenu.IsPaused) return;</summary>
    public static bool IsPaused { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;      // Panel ที่มีปุ่มทั้งหมด (เริ่มต้นปิดไว้)

    [Header("เชื่อมกับ MenuManager (สคริปต์ MainMenu) ในด่านนี้")]
    [SerializeField] private MainMenu menuManager;       // ใช้ตอนกลับเมนู / เลือกด่าน

    [Header("ตั้งค่า")]
    [SerializeField] private bool pauseAudio = false;    // true = หยุดเสียงทั้งหมดตอน pause (ปกติให้เพลงเล่นต่อ)
    [SerializeField] private bool unlockCursor = true;   // แสดงเมาส์ตอน pause

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
        // กันกรณีเปลี่ยน Scene ตอนยัง pause อยู่ แล้วเกมค้าง
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    // ---------- ปุ่มใน Pause Panel (ผูกที่ On Click) ----------

    /// <summary>ปุ่มหยุดเกมบนหน้าจอ (สำหรับมือถือ/เมาส์) หรือเรียกจากที่อื่น</summary>
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

    /// <summary>ปุ่ม "เล่นต่อ"</summary>
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

    /// <summary>ปุ่ม "เริ่มด่านใหม่"</summary>
    public void Restart()
    {
        ResetPauseState();
        SceneTransition.Load(SceneManager.GetActiveScene().name);
    }

    /// <summary>ปุ่ม "เลือกด่าน" -> กลับเมนูแล้วเปิดหน้าเลือกด่านเลย</summary>
    public void GoToLevelSelect()
    {
        ResetPauseState();
        if (menuManager != null) menuManager.GoToLevelSelect();
        else Debug.LogWarning("PauseMenu: ยังไม่ได้ผูก Menu Manager");
    }

    /// <summary>ปุ่ม "กลับเมนูหลัก"</summary>
    public void GoToMainMenu()
    {
        ResetPauseState();
        if (menuManager != null) menuManager.GoToMenu();
        else Debug.LogWarning("PauseMenu: ยังไม่ได้ผูก Menu Manager");
    }

    // ---------- ฟังก์ชันช่วย ----------

    private void ResetPauseState()
    {
        // คืนค่า timeScale ก่อนเปลี่ยน Scene (SceneTransition ใช้เวลาแบบ unscaled อยู่แล้ว)
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