using TMPro;
using UnityEngine;

// ใช้ใน scene Victory และ GameOver
// แปะบน GameObject ใดก็ได้ แล้วผูกปุ่ม: OnClick → ResultScreen.Restart / ResultScreen.BackToMenu
// ช่องแสดงผลปล่อยว่างได้ทั้งหมด ใส่เฉพาะที่หน้านั้นมี
public class ResultScreen : MonoBehaviour
{
    [Header("แสดงผลรอบล่าสุด (ปล่อยว่างได้)")]
    [SerializeField] private SpriteNumber scoreNumber;   // ตัวเลขแบบ sprite แบบเดียวกับในเกม
    [SerializeField] private SpriteNumber comboNumber;
    [SerializeField] private SpriteNumber bestNumber;
    [SerializeField] private TMP_Text scoreText;         // หรือใช้ตัวหนังสือธรรมดาก็ได้
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private GameObject newBestObject;   // เช่นป้าย NEW BEST! เปิดเฉพาะตอนทำลายสถิติ

    [Header("Scene")]
    [SerializeField] private string menuScene = "Menu";
    [Tooltip("ใช้เมื่อเปิด scene นี้ตรงๆ โดยไม่ได้มาจากด่าน (เช่นกด Play ทดสอบ)")]
    [SerializeField] private string fallbackLevelScene = "Level1";

    [Header("ปุ่มลัด")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private KeyCode menuKey = KeyCode.Escape;

    void Start()
    {
        Time.timeScale = 1f;
        var r = LevelManager.LastResult;

        if (scoreNumber != null) scoreNumber.SetValue(r.score, false);
        if (comboNumber != null) comboNumber.SetValue(r.maxCombo, false);
        if (bestNumber != null) bestNumber.SetValue(r.bestScore, false);

        if (scoreText != null) scoreText.text = r.score.ToString("N0");
        if (comboText != null) comboText.text = r.maxCombo.ToString();
        if (bestText != null) bestText.text = r.bestScore.ToString("N0");

        if (newBestObject != null) newBestObject.SetActive(r.newBest);
    }

    void Update()
    {
        if (Input.GetKeyDown(restartKey)) Restart();
        else if (Input.GetKeyDown(menuKey)) BackToMenu();
    }

    // เล่นด่านเดิมอีกรอบ
    public void Restart()
    {
        string level = LevelManager.HasResult && !string.IsNullOrEmpty(LevelManager.LastResult.levelScene)
            ? LevelManager.LastResult.levelScene
            : fallbackLevelScene;
        LevelManager.LoadSceneSafe(level);
    }

    public void BackToMenu()
    {
        LevelManager.LoadSceneSafe(menuScene);
    }
}
