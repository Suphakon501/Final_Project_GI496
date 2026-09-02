using UnityEngine;

public class HeartbeatManager : MonoBehaviour
{
    public static HeartbeatManager instance;

    [Header("Heartbeat Settings")]
    public float currentBPM = 60f; 
    public float secPerBeat;      
    public float heartPosition;    
    public float heartPositionInBeats; 

    [Header("Audio")]
    private float dspSongTime; 
    private AudioSource audioSource;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        UpdateBPM(currentBPM);

        dspSongTime = (float)AudioSettings.dspTime;

        if (audioSource.clip != null)
        {
            audioSource.Play();
        }
        else
        {
            //Debug.LogWarning("ยังไม่ได้ใส่ไฟล์เสียง Audio Clip ใน Audio Source");
        }
    }

    void Update()
    {
        heartPosition = (float)(AudioSettings.dspTime - dspSongTime);
        heartPositionInBeats = heartPosition / secPerBeat;
    }

    public void UpdateBPM(float newBPM)
    {
        currentBPM = newBPM;
        secPerBeat = 60f / currentBPM;
        audioSource.pitch = currentBPM / 60f; 
    }
}