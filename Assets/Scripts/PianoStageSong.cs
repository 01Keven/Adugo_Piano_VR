using UnityEngine;
using TMPro;
using System.Collections;

// Advances only on correct input, with no clock or BPM.
public class PianoStageSong : MonoBehaviour
{
    [SerializeField] private PianoSongData song;
    [SerializeField] private VRPianoKey[] keys = System.Array.Empty<VRPianoKey>();
    [SerializeField] private PianoSongData[] availableSongs;
    [SerializeField] private TMP_Text wallText;
    private GameObject songMenu;
    private bool isPlaying;
    private Coroutine completionRoutine;
    private const string Legend = "Acerto = <color=#00FF00>Verde</color>\nErro = <color=#FF0000>Vermelho</color>\nNota Atual = <color=#0088FF>Azul</color>\nPróxima Nota = <color=#AA55FF>Roxo</color>";
    private int currentIndex;
    public PianoSongData Song => song;
    public bool IsComplete => song == null || currentIndex >= song.Notes.Count;

    private void Start()
    {
        foreach (VRPianoKey key in keys)
            if (key != null) key.SetSongController(this);
        CreateSongMenu();
        ShowSongMenu();
    }

    public void TryPlay(VRPianoKey key)
    {
        if (!isPlaying || IsComplete || key == null || System.Array.IndexOf(keys, key) < 0)
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
        if (IsComplete)
        {
            isPlaying = false;
            completionRoutine = StartCoroutine(ReturnToMenu());
        }
    }

    [ContextMenu("Restart Song")]
    public void RestartSong()
    {
        if (song == null || song.Notes.Count == 0) return;
        if (completionRoutine != null)
        {
            StopCoroutine(completionRoutine);
            completionRoutine = null;
        }
        isPlaying = true;
        if (songMenu != null) songMenu.SetActive(false);
        if (wallText != null) wallText.text = Legend;
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
            if (isPlaying && !IsComplete)
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

    public void SelectSong(PianoSongData selectedSong)
    {
        if (isPlaying || selectedSong == null || selectedSong.Notes.Count == 0) return;
        song = selectedSong;
        RestartSong();
    }

    private IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(3f);
        completionRoutine = null;
        ShowSongMenu();
    }

    private void ShowSongMenu()
    {
        isPlaying = false;
        foreach (VRPianoKey key in keys)
            if (key != null) key.ClearFeedback();
        RefreshColors();
        if (wallText != null) wallText.text = "Qual música você quer tocar?";
        if (songMenu != null) songMenu.SetActive(true);
    }

    private void CreateSongMenu()
    {
        if (wallText == null)
        {
            Debug.LogError("Assign the wall text to PianoStageSong.", this);
            return;
        }
        songMenu = new GameObject("Song Selection");
        songMenu.transform.SetPositionAndRotation(wallText.transform.position, wallText.transform.rotation);
        PianoSongData[] catalog = availableSongs != null && availableSongs.Length > 0
            ? availableSongs : new[] { song };
        for (int i = 0; i < catalog.Length; i++)
        {
            PianoSongData entry = catalog[i];
            if (entry == null || entry.Notes.Count == 0) continue;
            float x = catalog.Length == 1 ? 0f : (i % 2 == 0 ? -2.2f : 2.2f);
            float y = -0.9f - (i / 2) * 0.8f;
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "Select - " + entry.SongTitle;
            button.transform.SetParent(songMenu.transform, false);
            button.transform.localPosition = new Vector3(x, y, -0.18f);
            button.transform.localScale = new Vector3(4f, 0.65f, 0.1f);
            Renderer sourceRenderer = keys.Length > 0 && keys[0] != null
                ? keys[0].GetComponent<Renderer>() : null;
            if (sourceRenderer != null)
                button.GetComponent<Renderer>().sharedMaterial = sourceRenderer.sharedMaterial;
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(0.12f, 0.16f, 0.24f));
            properties.SetColor("_EmissionColor", Color.black);
            button.GetComponent<Renderer>().SetPropertyBlock(properties);
            button.AddComponent<PianoSongButton>().Initialize(this, entry);

            GameObject labelObject = new GameObject("Title");
            labelObject.transform.SetParent(songMenu.transform, false);
            labelObject.transform.localPosition = new Vector3(x, y, -0.24f);
            TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
            label.font = wallText.font;
            label.fontSize = 4.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3.8f, 0.6f);
            label.text = entry.SongTitle;
        }
    }
}
