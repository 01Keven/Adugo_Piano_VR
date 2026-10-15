using System;
using System.Collections.Generic;
using UnityEngine;

// Values match the left-to-right key order in PianoStageVR.
public enum PianoNote
{
    Do = 0, Re = 1, Mi = 2, Fa = 3, Sol = 4, La = 5, Si = 6
}

[Serializable]
public struct PianoSongNote
{
    public PianoNote note;
}

[CreateAssetMenu(fileName = "PianoSong", menuName = "Piano/Song Data")]
public class PianoSongData : ScriptableObject
{
    [SerializeField] private string songTitle = "Dó Ré Mi Fá";
    [Tooltip("Notes in playing order. Each correct selection advances one step.")]
    [SerializeField] private PianoSongNote[] notes = Array.Empty<PianoSongNote>();

    public string SongTitle => songTitle;
    public IReadOnlyList<PianoSongNote> Notes => Array.AsReadOnly(notes);

    private void OnValidate()
    {
        if (notes == null) notes = Array.Empty<PianoSongNote>();
    }
}
