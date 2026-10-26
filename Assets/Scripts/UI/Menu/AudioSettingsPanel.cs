using UnityEngine;
using UnityEngine.UI;

/// <summary>Connects existing volume sliders to the cross-scene audio runtime. Creates no UI.</summary>
public class AudioSettingsPanel : MonoBehaviour
{
    private const string MasterKey = "Audio.Master";
    private const string MusicKey = "Audio.Music";
    private const string EffectsKey = "Audio.Effects";

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectsSlider;

    private float originalMaster, originalMusic, originalEffects;
    private float pendingMaster, pendingMusic, pendingEffects;
    private bool editing;

    private void Awake()
    {
        originalMaster = pendingMaster = PlayerPrefs.GetFloat(MasterKey, 1f);
        originalMusic = pendingMusic = PlayerPrefs.GetFloat(MusicKey, 1f);
        originalEffects = pendingEffects = PlayerPrefs.GetFloat(EffectsKey, 1f);

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(pendingMaster);
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(pendingMusic);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (effectsSlider != null)
        {
            effectsSlider.SetValueWithoutNotify(pendingEffects);
            effectsSlider.onValueChanged.AddListener(SetEffectsVolume);
        }
        AudioSettingsRuntime.ApplySavedSettings();
    }

    private void OnDestroy()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (effectsSlider != null) effectsSlider.onValueChanged.RemoveListener(SetEffectsVolume);
        if (editing) CancelChanges();
    }

    public void BeginEdit()
    {
        originalMaster = pendingMaster = PlayerPrefs.GetFloat(MasterKey, 1f);
        originalMusic = pendingMusic = PlayerPrefs.GetFloat(MusicKey, 1f);
        originalEffects = pendingEffects = PlayerPrefs.GetFloat(EffectsKey, 1f);
        editing = true;
        SyncSliders();
        ApplyPreview();
    }

    public void ConfirmChanges()
    {
        if (!editing) return;
        SavePendingValues();
        originalMaster = pendingMaster;
        originalMusic = pendingMusic;
        originalEffects = pendingEffects;
        editing = false;
        AudioSettingsRuntime.ClearPreviewAndApplySaved();
    }

    public void CancelChanges()
    {
        if (!editing) return;
        pendingMaster = originalMaster;
        pendingMusic = originalMusic;
        pendingEffects = originalEffects;
        editing = false;
        SyncSliders();
        AudioSettingsRuntime.ClearPreviewAndApplySaved();
    }

    public void SetMasterVolume(float value)
    {
        pendingMaster = Mathf.Clamp01(value);
        OnValueChanged();
    }

    public void SetMusicVolume(float value)
    {
        pendingMusic = Mathf.Clamp01(value);
        OnValueChanged();
    }

    public void SetEffectsVolume(float value)
    {
        pendingEffects = Mathf.Clamp01(value);
        OnValueChanged();
    }

    private void OnValueChanged()
    {
        if (editing) ApplyPreview();
        else
        {
            SavePendingValues();
            AudioSettingsRuntime.ApplySavedSettings();
        }
    }

    private void ApplyPreview() => AudioSettingsRuntime.SetPreview(pendingMaster, pendingMusic, pendingEffects);

    private void SavePendingValues()
    {
        PlayerPrefs.SetFloat(MasterKey, pendingMaster);
        PlayerPrefs.SetFloat(MusicKey, pendingMusic);
        PlayerPrefs.SetFloat(EffectsKey, pendingEffects);
        PlayerPrefs.Save();
    }

    private void SyncSliders()
    {
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(pendingMaster);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(pendingMusic);
        if (effectsSlider != null) effectsSlider.SetValueWithoutNotify(pendingEffects);
    }
}
