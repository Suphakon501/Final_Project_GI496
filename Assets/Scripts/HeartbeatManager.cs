using UnityEngine;

public class HeartbeatManager : MonoBehaviour
{
    // ทำให้เรียกใช้คลาสนี้จากสคริปต์อื่นได้ง่ายๆ (Singleton)
    public static HeartbeatManager instance;

    [Header("Heartbeat Settings")]
    public float currentBPM = 60f; // จังหวะชีพจรเริ่มต้น (60 ครั้งต่อนาที)
    public float secPerBeat;       // 1 จังหวะใช้เวลากี่วินาที
    public float heartPosition;    // เพลงเล่นมาแล้วกี่วินาที
    public float heartPositionInBeats; // เพลงเล่นมาแล้วกี่จังหวะ

    [Header("Audio")]
    private float dspSongTime; // เวลาเริ่มต้นตอนกด Play
    private AudioSource audioSource;

    void Awake()
    {
        // สร้าง Singleton
        if (instance == null) instance = this;
    }

    void Start()
    {
        // ค้นหาลำโพง (Audio Source) ที่ติดอยู่กับ GameManager
        audioSource = GetComponent<AudioSource>();

        // คำนวณความกว้างของ 1 จังหวะ (เช่น 60 BPM = 1 จังหวะ/วินาที)
        UpdateBPM(currentBPM);

        // บันทึกเวลาเริ่มต้นโดยอิงจากระบบเสียงของ Unity (เที่ยงตรงมาก)
        dspSongTime = (float)AudioSettings.dspTime;

        // สั่งให้เสียงหัวใจเต้นเริ่มเล่น
        if (audioSource.clip != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("ยังไม่ได้ใส่ไฟล์เสียง Audio Clip ใน Audio Source นะ!");
        }
    }

    void Update()
    {
        // คำนวณเวลาที่แท้จริงที่เสียงเล่นไปแล้ว
        heartPosition = (float)(AudioSettings.dspTime - dspSongTime);
        heartPositionInBeats = heartPosition / secPerBeat;
    }

    // ฟังก์ชันนี้เตรียมไว้สำหรับตอนที่ตัวละครใกล้ตาย แล้วหัวใจเต้นเร็วขึ้น
    public void UpdateBPM(float newBPM)
    {
        currentBPM = newBPM;
        secPerBeat = 60f / currentBPM;
        audioSource.pitch = currentBPM / 60f; // เร่งความเร็วเสียงให้สัมพันธ์กับ BPM
    }
}