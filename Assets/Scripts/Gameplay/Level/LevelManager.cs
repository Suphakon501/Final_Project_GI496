using UnityEngine;
using UnityEngine.SceneManagement;

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
        public string levelScene;
    }
    public static Result LastResult;
    public static bool HasResult;

    [Header("Level Length")]
    [SerializeField] private bool useMusicLength = true;
    [SerializeField] private float fallbackDurationSeconds = 80f;
    [SerializeField] private ProgressTracker progressTracker;

    [Header("Boss Phase Settings")]
    [SerializeField] private GameObject hitLineObject; // ลากเส้นสีเขียว (HitLine) มาใส่ช่องนี้เพื่อซ่อนตอนสู้บอส

    [Header("Result Scenes")]
    [SerializeField] private string victoryScene = "Victory";
    [SerializeField] private string gameOverScene = "GameOver";
    [SerializeField] private float delayBeforeLoad = 1.2f;

    [Header("Debug Keys (Editor / Development Build only)")]
    [SerializeField] private bool debugKeys = true;
    [SerializeField] private KeyCode debugWinKey = KeyCode.F1;
    [SerializeField] private KeyCode debugLoseKey = KeyCode.F2;
    [SerializeField] private KeyCode debugSkipKey = KeyCode.F3;

    private enum State { Playing, WaitingForLastEnemy, BossPhase, Finished }
    private State state = State.Playing;

    private AudioSource music;
    private ChartPlayer chartPlayer;
    private float duration;
    private float elapsed;
    private bool musicStarted;
    private float lastPlayingTime;
    private const float SongEndTolerance = 1f;
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

        chartPlayer = FindFirstObjectByType<ChartPlayer>();
        var beat = BeatManager.instance != null ? BeatManager.instance : FindFirstObjectByType<BeatManager>();
        music = beat != null ? beat.musicSource : null;
        if (chartPlayer != null && chartPlayer.chart != null && !chartPlayer.recordMode)
        {
            duration = chartPlayer.chart.music != null
                ? chartPlayer.chart.music.length
                : music != null && music.clip != null
                    ? music.clip.length
                    : Mathf.Max(1f, fallbackDurationSeconds);
            chartPlayer.onChartFinished.AddListener(OnChartFinished);
            return;
        }
        chartPlayer = null;

        if (useMusicLength && music != null && music.clip != null)
        {
            music.loop = false;
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
        if (state == State.Finished) return;
        if (chartPlayer != null)
        {
            if (state == State.WaitingForLastEnemy) StartBossPhase();
            return;
        }

        if (state == State.Playing && !PlayerController.isGameOver)
        {
            bool timeUp;
            if (music != null)
            {
                if (music.isPlaying)
                {
                    musicStarted = true;
                    lastPlayingTime = music.time;
                }
                timeUp = musicStarted && !music.isPlaying && lastPlayingTime >= duration - SongEndTolerance;
                elapsed = timeUp ? duration : lastPlayingTime;
            }
            else
            {
                elapsed += Time.deltaTime;
                timeUp = elapsed >= duration;
            }

            if (progressTracker != null) progressTracker.SetProgress(Mathf.Clamp01(elapsed / duration));

            // เมื่อเวลาหมด ให้หยุดสปอนแล้วเปลี่ยนไปรอดักเคลียร์โน้ตตัวสุดท้ายบนจอก่อน
            if (timeUp)
            {
                state = State.WaitingForLastEnemy;
                StopSpawners();
            }
        }
        // รอจนกว่าโน้ตตัวสุดท้ายบนจอจะหมด ค่อยเรียก StartBossPhase()
        else if (state == State.WaitingForLastEnemy)
        {
            bool enemiesLeft = LipidMovement.Active.Count > 0 || MiniLipid.Active.Count > 0;
            if (!enemiesLeft)
            {
                StartBossPhase();
            }
        }
    }

    void HandleDebugKeys()
    {
        if (Input.GetKeyDown(debugWinKey))
        {
            Debug.Log("[LevelManager] DEBUG: instant win");
            if (progressTracker != null) progressTracker.SetProgress(1f);
            Finish(true);
        }
        else if (Input.GetKeyDown(debugLoseKey))
        {
            Debug.Log("[LevelManager] DEBUG: instant lose");
            Finish(false);
        }
        else if (Input.GetKeyDown(debugSkipKey) && state == State.Playing)
        {
            float target = Mathf.Max(0f, duration - 10f);
            if (music != null && music.clip != null) music.time = Mathf.Min(target, music.clip.length - 0.1f);
            else elapsed = target;
            Debug.Log($"[LevelManager] DEBUG: skipped to {target:0}s (10s left)");
        }
    }

    // เรียกใช้งานเมื่อโน้ตตัวสุดท้ายหมดแล้ว เพื่อเข้าสู่ช่วงรัวปุ่มสู้บอส
    void StartBossPhase()
    {
        state = State.BossPhase;
        if (progressTracker != null) progressTracker.SetProgress(1f);

        // 1. ซ่อนเส้นสีเขียว (HitLine)
        if (hitLineObject != null) hitLineObject.SetActive(false);

        // 2. เริ่มต้นช่วงรัวปุ่ม (กำหนดให้รัวได้ 6 วินาที หรือปรับเวลาได้ตามต้องการ)
        if (BossMashManager.instance != null)
        {
            BossMashManager.instance.StartMash(6f);
        }
        else
        {
            Invoke(nameof(FinishBossPhase), 6f);
        }
    }

    public void FinishBossPhase()
    {
        Finish(true); // จบเกมด้วยชัยชนะ (Victory)
    }

    static void StopSpawners()
    {
        foreach (var spawner in FindObjectsByType<LipidSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
    }

    void OnChartFinished()
    {
        if (state == State.Playing)
        {
            state = State.WaitingForLastEnemy;
            StopSpawners();
        }
    }

    public void OnPlayerDied()
    {
        if (state != State.Finished) Finish(false);
    }

    void Finish(bool clear)
    {
        state = State.Finished;
        cleared = clear;
        PlayerController.isGameOver = true;
        StopSpawners();
        if (!clear && music != null) music.Stop();

        SaveResult();
        Invoke(nameof(LoadResultScene), delayBeforeLoad);
    }

    void SaveResult()
    {
        var player = PlayerController.instance;
        string level = SceneManager.GetActiveScene().name;
        int score = player != null ? player.score : 0;

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
        SceneTransition.Load(cleared ? victoryScene : gameOverScene);
    }
}
