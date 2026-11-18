using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PianoSongButton : XRSimpleInteractable
{
    private PianoStageSong stage;
    private PianoSongData song;

    public void Initialize(PianoStageSong owner, PianoSongData selection)
    {
        stage = owner;
        song = selection;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (stage != null) stage.SelectSong(song);
    }
}
