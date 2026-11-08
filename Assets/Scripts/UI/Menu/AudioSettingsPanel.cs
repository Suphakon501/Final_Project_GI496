using UnityEngine;
using UnityEngine.UI;

/// <summary>Connects existing volume sliders to the cross-scene audio runtime.</summary>
public class AudioSettingsPanel : MonoBehaviour
{
    private const string MasterKey = "Audio.Master";
    private const string MusicKey = "Audio.Music";
    private const string EffectsKey = "Audio.Effects";
    private const string NoteHitKey = "Audio.NoteHit";

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectsSlider;
    [Tooltip("สไลเดอร์เสียงกดโน้ต — เว้นว่างไว้ได้ ระบบจะก๊อปแถวของ Effects มาสร้างให้อัตโนมัติ")]
    [SerializeField] private Slider noteHitSlider;
    [SerializeField] private bool createNoteHitRowIfMissing = true;
    [Tooltip("ระยะห่างของแถวใหม่จากแถว Effects (พิกเซล)")]
    [SerializeField] private float noteHitRowSpacing = 138f;

    private float originalMaster, originalMusic, originalEffects, originalNoteHit;
    private float pendingMaster, pendingMusic, pendingEffects, pendingNoteHit;
    private bool editing;

    private void Awake()
    {
        CreateNoteHitRowIfNeeded();

        originalMaster = pendingMaster = PlayerPrefs.GetFloat(MasterKey, 1f);
        originalMusic = pendingMusic = PlayerPrefs.GetFloat(MusicKey, 1f);
        originalEffects = pendingEffects = PlayerPrefs.GetFloat(EffectsKey, 1f);
        originalNoteHit = pendingNoteHit = PlayerPrefs.GetFloat(NoteHitKey, 1f);

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
        if (noteHitSlider != null)
        {
            noteHitSlider.SetValueWithoutNotify(pendingNoteHit);
            noteHitSlider.onValueChanged.AddListener(SetNoteHitVolume);
        }
        AudioSettingsRuntime.ApplySavedSettings();
    }

    private void OnDestroy()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(SetMasterVolume);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (effectsSlider != null) effectsSlider.onValueChanged.RemoveListener(SetEffectsVolume);
        if (noteHitSlider != null) noteHitSlider.onValueChanged.RemoveListener(SetNoteHitVolume);
        if (editing) CancelChanges();
    }

    public void BeginEdit()
    {
        originalMaster = pendingMaster = PlayerPrefs.GetFloat(MasterKey, 1f);
        originalMusic = pendingMusic = PlayerPrefs.GetFloat(MusicKey, 1f);
        originalEffects = pendingEffects = PlayerPrefs.GetFloat(EffectsKey, 1f);
        originalNoteHit = pendingNoteHit = PlayerPrefs.GetFloat(NoteHitKey, 1f);
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
        originalNoteHit = pendingNoteHit;
        editing = false;
        AudioSettingsRuntime.ClearPreviewAndApplySaved();
    }

    public void CancelChanges()
    {
        if (!editing) return;
        pendingMaster = originalMaster;
        pendingMusic = originalMusic;
        pendingEffects = originalEffects;
        pendingNoteHit = originalNoteHit;
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

    /// <summary>ตัวปรับความดังเสียงกดโน้ต (แยกจาก SFX อื่น)</summary>
    public void SetNoteHitVolume(float value)
    {
        pendingNoteHit = Mathf.Clamp01(value);
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

    private void ApplyPreview() => AudioSettingsRuntime.SetPreview(pendingMaster, pendingMusic, pendingEffects, pendingNoteHit);

    private void SavePendingValues()
    {
        PlayerPrefs.SetFloat(MasterKey, pendingMaster);
        PlayerPrefs.SetFloat(MusicKey, pendingMusic);
        PlayerPrefs.SetFloat(EffectsKey, pendingEffects);
        PlayerPrefs.SetFloat(NoteHitKey, pendingNoteHit);
        PlayerPrefs.Save();
    }

    private void SyncSliders()
    {
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(pendingMaster);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(pendingMusic);
        if (effectsSlider != null) effectsSlider.SetValueWithoutNotify(pendingEffects);
        if (noteHitSlider != null) noteHitSlider.SetValueWithoutNotify(pendingNoteHit);
    }

    /// <summary>
    /// ถ้ายังไม่มีสไลเดอร์ "NOTE HIT" ในซีน ให้ก๊อปแถวของ Effects มาวางต่อด้านล่าง
    /// (ใช้สไปรท์/สไตล์เดิมทั้งหมด ไม่ต้องแก้ UI ในซีน)
    /// </summary>
    private void CreateNoteHitRowIfNeeded()
    {
        if (noteHitSlider != null || !createNoteHitRowIfMissing || effectsSlider == null) return;

        Transform parent = effectsSlider.transform.parent;
        if (parent == null) return;

        GameObject bar = Instantiate(effectsSlider.gameObject, parent);
        bar.name = "NoteHitVolumeBar";
        if (bar.transform is RectTransform barRect)
            barRect.anchoredPosition += Vector2.down * noteHitRowSpacing;

        Transform labelSource = parent.Find("EffectVolumeText");
        if (labelSource != null)
        {
            GameObject label = Instantiate(labelSource.gameObject, parent);
            label.name = "NoteHitVolumeText";
            if (label.transform is RectTransform labelRect)
                labelRect.anchoredPosition += Vector2.down * noteHitRowSpacing;

            var tmp = label.GetComponent<TMPro.TMP_Text>();
            if (tmp != null) tmp.text = "NOTE HIT";
            else
            {
                var legacy = label.GetComponent<Text>();
                if (legacy != null) legacy.text = "NOTE HIT";
            }
        }

        noteHitSlider = bar.GetComponent<Slider>();
    }
}
