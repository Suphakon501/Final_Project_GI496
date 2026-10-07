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

    [Header("Result Scenes")]
    [SerializeField] private string victoryScene = "Victory";
    [SerializeField] private string gameOverScene = "GameOver";
    [SerializeField] private float delayBeforeLoad = 1.2f;

    [Header("Debug Keys (Editor / Development Build only)")]
    [SerializeField] private bool debugKeys = true;
    [SerializeField] private KeyCode debugWinKey = KeyCode.F1;
    [SerializeField] private KeyCode debugLoseKey = KeyCode.F2;
    [SerializeField] private KeyCode debugSkipKey = KeyCode.F3;

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
            bool enemiesLeft = LipidMovement.Active.Count > 0 || MiniLipid.Active.Count > 0;
            if (!enemiesLeft) Finish(true);
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

    void BeginEnding()
    {
        state = State.WaitingForLastEnemy;
        if (progressTracker != null) progressTracker.SetProgress(1f);
        StopSpawners();
    }

    static void StopSpawners()
    {
        foreach (var spawner in FindObjectsByType<LipidSpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
    }

    void OnChartFinished()
    {
        if (state != State.Finished) Finish(true);
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
