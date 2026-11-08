using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Applies saved and preview audio levels across every loaded scene. Creates no UI.</summary>
[DefaultExecutionOrder(-200)]
public class AudioSettingsRuntime : MonoBehaviour
{
    private const string MasterKey = "Audio.Master";
    private const string MusicKey = "Audio.Music";
    private const string EffectsKey = "Audio.Effects";
    private const string NoteHitKey = "Audio.NoteHit";

    private static AudioSettingsRuntime instance;
    private static bool previewActive;
    private static float previewMaster = 1f;
    private static float previewMusic = 1f;
    private static float previewEffects = 1f;
    private static float previewNoteHit = 1f;

    private readonly Dictionary<AudioSource, float> baseVolumes = new Dictionary<AudioSource, float>();
    private readonly List<AudioSource> destroyedSources = new List<AudioSource>();
    private AudioSource effectsSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        previewActive = false;
        previewMaster = previewMusic = previewEffects = previewNoteHit = 1f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var runtimeObject = new GameObject("AudioSettingsRuntime");
        DontDestroyOnLoad(runtimeObject);
        var createdEffectsSource = runtimeObject.AddComponent<AudioSource>();
        createdEffectsSource.playOnAwake = false;
        createdEffectsSource.loop = false;
        createdEffectsSource.spatialBlend = 0f;
        instance = runtimeObject.AddComponent<AudioSettingsRuntime>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        effectsSource = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyVolumes();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyVolumes();
    private void Update() => ApplyVolumes();

    public static void SetPreview(float master, float music, float effects, float noteHit = 1f)
    {
        previewActive = true;
        previewMaster = Mathf.Clamp01(master);
        previewMusic = Mathf.Clamp01(music);
        previewEffects = Mathf.Clamp01(effects);
        previewNoteHit = Mathf.Clamp01(noteHit);
        if (instance != null) instance.ApplyVolumes();
    }

    /// <summary>ความดังเสียงกดโน้ต (0-1) — คูณซ้อนจากสไลเดอร์ Effects อีกชั้น</summary>
    public static float NoteHitVolume => previewActive ? previewNoteHit : PlayerPrefs.GetFloat(NoteHitKey, 1f);

    public static void ClearPreviewAndApplySaved()
    {
        previewActive = false;
        if (instance != null) instance.ApplyVolumes();
    }

    public static void ApplySavedSettings()
    {
        if (instance != null) instance.ApplyVolumes();
    }

    public static void PlayEffect(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || instance == null || instance.effectsSource == null) return;
        instance.ApplyVolumes();
        instance.effectsSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    private void ApplyVolumes()
    {
        float master = previewActive ? previewMaster : PlayerPrefs.GetFloat(MasterKey, 1f);
        float music = previewActive ? previewMusic : PlayerPrefs.GetFloat(MusicKey, 1f);
        float effects = previewActive ? previewEffects : PlayerPrefs.GetFloat(EffectsKey, 1f);
        AudioListener.volume = master;

        destroyedSources.Clear();
        foreach (var pair in baseVolumes)
            if (pair.Key == null) destroyedSources.Add(pair.Key);
        foreach (var source in destroyedSources) baseVolumes.Remove(source);

        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (source == null) continue;
            if (!baseVolumes.ContainsKey(source)) baseVolumes[source] = source.volume;

            string sourceName = source.gameObject.name.ToLowerInvariant();
            bool isMusic = (BeatManager.instance != null && source == BeatManager.instance.musicSource)
                || sourceName.Contains("music")
                || sourceName.Contains("bgm");
            source.volume = baseVolumes[source] * (isMusic ? music : effects);
        }
    }
}
