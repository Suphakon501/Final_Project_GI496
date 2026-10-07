using UnityEngine;
using UnityEngine.SceneManagement;

// คุมด่าน 1 รอบ: เริ่ม → หลอด progress วิ่งตามความยาวด่าน → หมดเวลา (หยุดปล่อยไขมัน รอตัวสุดท้ายเคลียร์) → ไป scene Victory
// เลือดหมดกลางทาง → ไป scene GameOver
// ผลของรอบล่าสุด (คะแนน / คอมโบ / ด่านที่เล่น) เก็บไว้ใน LevelManager.LastResult ให้ scene Victory / GameOver อ่าน
//
// ใช้: Create Empty ในด่าน → Add Component → Level Manager
[DefaultExecutionOrder(-150)]
public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    public struct Result
    {
        public bool cleared;
        public int score;
        public int maxCombo;
        public int bestScore;
        public bool newBest;
        public string levelScene; // ไว้ให้ปุ่ม Restart โหลดด่านเดิม
    }
    public static Result LastResult;
    public static bool HasResult;

    [Header("ความยาวด่าน")]
    [Tooltip("มีเพลงใน BeatManager = ด่านยาวเท่าเพลง (เพลงจะไม่วนลูป)")]
    [SerializeField] private bool useMusicLength = true;
    [Tooltip("ใช้เมื่อไม่มีเพลง หรือปิด Use Music Length (วินาที)")]
    [SerializeField] private float fallbackDurationSeconds = 80f;
    [SerializeField] private ProgressTracker progressTracker; // ว่าง = หาในฉากให้เอง

    [Header("Scene ปลายทาง (ชื่อต้องตรงกับใน Build Settings)")]
    [SerializeField] private string victoryScene = "Victory";
    [SerializeField] private string gameOverScene = "GameOver";
    [SerializeField] private float delayBeforeLoad = 1.2f; // รอให้เห็นท่าสุดท้าย / ตายก่อนเปลี่ยน scene

    [Header("ปุ่มลัดทดสอบ (ใช้ได้เฉพาะใน Editor / Development Build)")]
    [SerializeField] private bool debugKeys = true;
    [SerializeField] private KeyCode debugWinKey = KeyCode.F1;   // ชนะทันที → Victory
    [SerializeField] private KeyCode debugLoseKey = KeyCode.F2;  // แพ้ทันที → GameOver
    [SerializeField] private KeyCode debugSkipKey = KeyCode.F3;  // ข้ามไปช่วงท้ายด่าน (เหลือ 10 วิ)

    private enum State { Playing, WaitingForLastEnemy, Finished }
    private State state = State.Playing;

    private AudioSource music;
    private ChartPlayer chartPlayer;
    private float duration;
    private float elapsed;
    private bool musicStarted;
    private bool cleared;

    public bool IsFinished => state == State.Finished;

    void Awake()
    {
        instance = this;
        Time.timeScale = 1f;
        LipidMovement.ResetDifficultyTimer();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        if (progressTracker == null) progressTracker = FindFirstObjectByType<ProgressTracker>();
        if (progressTracker != null) progressTracker.SetExternalControl();

        // โหมดชาร์ต: ChartPlayer คุมเพลง/หลอดเอง เราแค่รอสัญญาณเพลงจบ
        chartPlayer = FindFirstObjectByType<ChartPlayer>();
        if (chartPlayer != null && chartPlayer.chart != null && !chartPlayer.recordMode)
        {
            chartPlayer.onChartFinished.AddListener(OnChartFinished);
            return;
        }
        chartPlayer = null;

        var beat = BeatManager.instance != null ? BeatManager.instance : FindFirstObjectByType<BeatManager>();
        music = beat != null ? beat.musicSource : null;

        if (useMusicLength && music != null && music.clip != null)
        {
            music.loop = false; // เพลงจบ = หมดเวลาด่าน
            duration = music.clip.length;
        }
        else
        {
            music = null;
            duration = Mathf.Max(1f, fallbackDurationSeconds);
        }
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (debugKeys && state != State.Finished) HandleDebugKeys();
#endif
        if (state == State.Finished || chartPlayer != null) return;

        if (state == State.Playing && !PlayerController.isGameOver)
        {
            bool timeUp;
            if (music != null)
            {
                if (music.isPlaying) musicStarted = true;
                elapsed = music.isPlaying ? music.time : (musicStarted ? duration : 0f);
                timeUp = musicStarted && !music.isPlaying;
            }
            else
            {
                elapsed += Time.deltaTime;
                timeUp = elapsed >= duration;
            }

            if (progressTracker != null) progressTracker.SetProgress(Mathf.Clamp01(elapsed / duration));
            if (timeUp) BeginEnding();
        }
        else if (state == State.WaitingForLastEnemy)
        {
            // หยุดปล่อยไขมันแล้ว รอตัวที่ยังอยู่บนจอสู้จนจบ
            bool enemiesLeft = FindObjectsByType<LipidMovement>(FindObjectsSortMode.None).Length > 0
                            || FindObjectsByType<MiniLipid>(FindObjectsSortMode.None).Length > 0;
            if (!enemiesLeft) Finish(true);
        }
    }

    void HandleDebugKeys()
    {
        if (Input.GetKeyDown(debugWinKey))
        {
            Debug.Log("[LevelManager] DEBUG: ชนะทันที");
            if (progressTracker != null) progressTracker.SetProgress(1f);
            Finish(true);
        }
        else if (Input.GetKeyDown(debugLoseKey))
        {
            Debug.Log("[LevelManager] DEBUG: แพ้ทันที");
            Finish(false);
        }
        else if (Input.GetKeyDown(debugSkipKey) && state == State.Playing)
        {
            float target = Mathf.Max(0f, duration - 10f);
            if (music != null && music.clip != null) music.time = Mathf.Min(target, music.clip.length - 0.1f);
            else elapsed = target;
            Debug.Log($"[LevelManager] DEBUG: ข้ามไปวินาทีที่ {target:0} (เหลือ 10 วิ)");
        }
    }

    // หมดเวลาด่าน (หลอดวิ่งสุด): ไม่ปล่อยไขมันตัวใหม่แล้ว
    void BeginEnding()
    {
        state = State.WaitingForLastEnemy;
        if (progressTracker != null) progressTracker.SetProgress(1f);
        foreach (var spawner in FindObjectsByType<LipidSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
    }

    void OnChartFinished()
    {
        if (state != State.Finished) Finish(true);
    }

    // PlayerController เรียกตอนเลือดหมด
    public void OnPlayerDied()
    {
        if (state != State.Finished) Finish(false);
    }

    void Finish(bool clear)
    {
        state = State.Finished;
        cleared = clear;
        PlayerController.isGameOver = true; // หยุดรับปุ่ม / หยุดปล่อยของทุกอย่าง
        foreach (var spawner in FindObjectsByType<LipidSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
        if (!clear && music != null) music.Stop();

        SaveResult();
        Invoke(nameof(LoadResultScene), delayBeforeLoad);
    }

    void SaveResult()
    {
        var player = PlayerController.instance;
        string level = SceneManager.GetActiveScene().name;
        int score = player != null ? player.score : 0;

        // คะแนนสูงสุดแยกตามด่าน (นับเฉพาะรอบที่ผ่านด่าน)
        string key = "HighScore_" + level;
        int best = PlayerPrefs.GetInt(key, 0);
        bool newBest = cleared && score > best;
        if (newBest)
        {
            best = score;
            PlayerPrefs.SetInt(key, best);
            PlayerPrefs.Save();
        }

        LastResult = new Result
        {
            cleared = cleared,
            score = score,
            maxCombo = player != null ? player.maxCombo : 0,
            bestScore = best,
            newBest = newBest,
            levelScene = level,
        };
        HasResult = true;
    }

    void LoadResultScene()
    {
        LoadSceneSafe(cleared ? victoryScene : gameOverScene);
    }

    // เช็คก่อนว่า scene อยู่ใน Build Profiles แล้ว ไม่งั้น SceneTransition จะค้างที่หน้าโหลด
    public static bool LoadSceneSafe(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[LevelManager] โหลด scene '{sceneName}' ไม่ได้: ยังไม่ได้เพิ่มใน File > Build Profiles > Scene List (หรือชื่อไม่ตรง)");
            return false;
        }
        SceneTransition.Load(sceneName);
        return true;
    }
}
