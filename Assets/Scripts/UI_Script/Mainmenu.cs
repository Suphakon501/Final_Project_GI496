using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MenuManager: จัดการหน้าเมนูหลัก, หน้าเลือกด่าน และปุ่มกลับเมนูจากในด่าน
/// - ใน Scene "MainMenu": ผูก Main Panel / Level Panel แล้วใช้ปุ่มเล่นเกม/ออกเกม/เลือกด่าน
/// - ใน Scene ด่าน (Level1-3): วางสคริปต์นี้บน GameObject แล้วผูกปุ่มกับ GoToMenu / GoToLevelSelect
///   (ไม่ต้องใส่ Panel ก็ได้ สคริปต์จะข้ามให้เอง)
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Panels (ใช้เฉพาะใน Scene MainMenu)")]
    [SerializeField] private GameObject mainPanel;   // Panel ที่มีปุ่ม เล่นเกม / ออกเกม
    [SerializeField] private GameObject levelPanel;  // Panel ที่มีปุ่มเลือกด่าน 1-3

    [Header("ชื่อ Scene (ต้องตรงกับใน Build Settings)")]
    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private string level1Scene = "Level1";
    [SerializeField] private string level2Scene = "Level2";
    [SerializeField] private string level3Scene = "Level3";

    // จำไว้ว่าตอนโหลดเมนูกลับมา ให้เปิดหน้าเลือกด่านเลยหรือไม่
    private static bool openLevelSelectOnStart = false;

    private void Start()
    {
        // ถ้าไม่มี Panel (เช่นอยู่ใน Scene ด่าน) ก็ไม่ต้องทำอะไร
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

    // ---------- ปุ่มหน้าเมนูหลัก ----------

    /// <summary>ปุ่ม "เล่นเกม" -> เปิดหน้าเลือกด่าน</summary>
    public void OnPlayClicked()
    {
        ShowLevelPanel();
    }

    /// <summary>ปุ่ม "ออกเกม"</summary>
    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // หยุด Play Mode ใน Editor
#else
        Application.Quit();
#endif
    }

    // ---------- ปุ่มหน้าเลือกด่าน ----------

    public void OnLevel1Clicked() => LoadScene(level1Scene);
    public void OnLevel2Clicked() => LoadScene(level2Scene);
    public void OnLevel3Clicked() => LoadScene(level3Scene);

    /// <summary>ปุ่ม "ย้อนกลับ" ในหน้าเลือกด่าน -> กลับหน้าเมนูหลัก</summary>
    public void OnBackClicked()
    {
        ShowMainPanel();
    }

    // ---------- ปุ่มที่ใช้ในด่านที่กำลังเล่นอยู่ ----------

    /// <summary>กลับไป Scene เมนู แล้วแสดงหน้าเมนูหลัก</summary>
    public void GoToMenu()
    {
        openLevelSelectOnStart = false;
        LoadScene(menuScene);
    }

    /// <summary>กลับไป Scene เมนู แล้วเปิดหน้าเลือกด่านเลย</summary>
    public void GoToLevelSelect()
    {
        openLevelSelectOnStart = true;
        LoadScene(menuScene);
    }

    // ---------- ฟังก์ชันช่วย ----------

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
        SceneTransition.Load(sceneName); // fade มืด -> โหลด -> fade สว่าง
    }
}