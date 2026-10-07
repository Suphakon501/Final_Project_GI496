using System;
using System.Collections.Generic;
using UnityEngine;

public enum NoteKey { W, A, S, D }
public enum NoteRow { Top, Middle, Bottom }

[Serializable]
public class ChartNote
{
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

    public static NoteRow DefaultRowFor(NoteKey k)
    {
        if (k == NoteKey.W) return NoteRow.Top;
        if (k == NoteKey.S) return NoteRow.Bottom;
        return NoteRow.Middle;
    }
}

[CreateAssetMenu(menuName = "Rhythm/Song Chart", fileName = "NewSongChart")]
public class SongChart : ScriptableObject
{
    [Header("Song")]
    public AudioClip music;
    public float bpm = 150f;
    public float offsetSeconds = 0f;

    [Header("Waves")]
    public float waveGapBeats = 3f;

    [Header("Notes")]
    public List<ChartNote> notes = new List<ChartNote>();

    public float BeatDuration => 60f / Mathf.Max(1f, bpm);

    [ContextMenu("Sort Notes")]
    public void SortNotes()
    {
        notes.Sort((a, b) => a.beat.CompareTo(b.beat));
    }
}
