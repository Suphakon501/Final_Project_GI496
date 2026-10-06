using System;
using System.Collections.Generic;
using UnityEngine;

public enum NoteKey { W, A, S, D }
public enum NoteRow { Top, Middle, Bottom }

[Serializable]
public class ChartNote
{
    [Tooltip("บีทที่โน้ตต้องถึงเส้น (นับจากบีทแรกของเพลง, 0.5 = ครึ่งบีท)")]
    public float beat;
    public NoteKey key;
    public NoteRow row = NoteRow.Middle;

    public KeyCode KeyCode
    {
        get
        {
            switch (key)
            {
                case NoteKey.W: return KeyCode.W;
                case NoteKey.A: return KeyCode.A;
                case NoteKey.S: return KeyCode.S;
                default: return KeyCode.D;
            }
        }
    }

    // ตอนอัด: กำหนดแถวจากปุ่มให้ก่อน (W บน, S ล่าง, A/D กลาง) แก้ทีหลังได้
    public static NoteRow DefaultRowFor(NoteKey k)
    {
        if (k == NoteKey.W) return NoteRow.Top;
        if (k == NoteKey.S) return NoteRow.Bottom;
        return NoteRow.Middle;
    }
}

// ชาร์ต 1 เพลง: สร้างจาก Project > Create > Rhythm > Song Chart
// โน้ตเก็บเป็น "บีท" ไม่ใช่วินาที เลยปรับ BPM / offset ทีหลังได้โดยโน้ตยังลงจังหวะเหมือนเดิม
[CreateAssetMenu(menuName = "Rhythm/Song Chart", fileName = "NewSongChart")]
public class SongChart : ScriptableObject
{
    [Header("เพลง")]
    public AudioClip music;
    public float bpm = 150f;
    [Tooltip("ใช้แทนค่า Song Offset Seconds ของ BeatManager ตอนเล่นชาร์ตนี้")]
    public float offsetSeconds = 0f;

    [Header("ไขมันตัวใหญ่")]
    [Tooltip("โน้ตห่างกันเกินกี่บีท ถึงจะนับเป็นไขมันตัวใหม่")]
    public float waveGapBeats = 3f;

    [Header("โน้ต (เรียงตามบีทให้อัตโนมัติตอนเริ่มเล่น / คลิกขวาที่ชื่อ component > Sort Notes)")]
    public List<ChartNote> notes = new List<ChartNote>();

    public float BeatDuration => 60f / Mathf.Max(1f, bpm);

    [ContextMenu("Sort Notes")]
    public void SortNotes()
    {
        notes.Sort((a, b) => a.beat.CompareTo(b.beat));
    }
}
