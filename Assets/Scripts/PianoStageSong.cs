using UnityEngine;

// Advances only on correct input, with no clock or BPM.
public class PianoStageSong : MonoBehaviour
{
    [SerializeField] private PianoSongData song;
    [SerializeField] private VRPianoKey[] keys;
    private int currentIndex;
    public PianoSongData Song => song;
    public bool IsComplete => song == null || currentIndex >= song.Notes.Count;

    private void Start()
    {
        foreach (VRPianoKey key in keys)
            if (key != null) key.SetSongController(this);
        RestartSong();
    }

    public void TryPlay(VRPianoKey key)
    {
        if (IsComplete || key == null || System.Array.IndexOf(keys, key) < 0)
            return;

        if (key.Note != song.Notes[currentIndex].note)
        {
            key.ShowFeedback(Color.red);
            return;
        }

        key.PlayNote();
        currentIndex++;
        RefreshColors();
        key.ShowFeedback(Color.green);
        if (IsComplete) Debug.Log("Música concluída!", this);
    }

    [ContextMenu("Restart Song")]
    public void RestartSong()
    {
        currentIndex = 0;
        foreach (VRPianoKey key in keys)
            if (key != null) key.ClearFeedback();
        RefreshColors();
    }

    private void RefreshColors()
    {
        foreach (VRPianoKey key in keys)
        {
            if (key == null) continue;
            Color? color = null;
            if (!IsComplete)
            {
                // Current takes priority when two consecutive entries use the same key.
                if (key.Note == song.Notes[currentIndex].note) color = Color.blue;
                else if (currentIndex + 1 < song.Notes.Count &&
                         key.Note == song.Notes[currentIndex + 1].note)
                    color = new Color(0.6f, 0.15f, 1f);
            }
            key.SetSequenceColor(color);
        }
    }
}
