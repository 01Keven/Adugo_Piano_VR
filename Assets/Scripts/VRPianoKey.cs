using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Interaction for the stationary piano keys in PianoStageVR.
public class VRPianoKey : XRSimpleInteractable
{
    [SerializeField] private AudioClip noteClip;
    [SerializeField] private PianoNote note;
    [SerializeField, Min(0.01f)] private float glowDuration = 0.25f;

    private AudioSource noteAudio;
    private Renderer keyRenderer;
    private MaterialPropertyBlock properties;
    private Material glowMaterial;
    private float feedbackUntil;
    private bool showingFeedback;
    private PianoStageSong songController;
    private Color? sequenceColor;
    public PianoNote Note => note;

    public void SetSongController(PianoStageSong controller) => songController = controller;

    protected override void Awake()
    {
        base.Awake();
        keyRenderer = GetComponent<Renderer>();
        properties = new MaterialPropertyBlock();
        if (keyRenderer != null && keyRenderer.sharedMaterial != null)
        {
            glowMaterial = new Material(keyRenderer.sharedMaterial);
            glowMaterial.EnableKeyword("_EMISSION");
            keyRenderer.sharedMaterial = glowMaterial;
        }
        noteAudio = gameObject.AddComponent<AudioSource>();
        noteAudio.playOnAwake = false;
        noteAudio.spatialBlend = 0f;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (songController != null) songController.TryPlay(this);
    }

    public void PlayNote()
    {
        if (noteClip != null)
            noteAudio.PlayOneShot(noteClip);
    }

    public void SetSequenceColor(Color? color)
    {
        sequenceColor = color;
        if (!showingFeedback) ApplyColor(sequenceColor);
    }

    public void ShowFeedback(Color color)
    {
        ApplyColor(color);
        feedbackUntil = Time.time + glowDuration;
        showingFeedback = true;
    }

    public void ClearFeedback()
    {
        showingFeedback = false;
        ApplyColor(sequenceColor);
    }

    private void ApplyColor(Color? color)
    {
        if (keyRenderer != null)
        {
            properties.Clear();
            if (color.HasValue)
            {
                properties.SetColor("_BaseColor", color.Value);
                properties.SetColor("_Color", color.Value);
                properties.SetColor("_EmissionColor", color.Value * 0.5f);
            }
            keyRenderer.SetPropertyBlock(color.HasValue ? properties : null);
        }
    }

    private void Update()
    {
        if (showingFeedback && Time.time >= feedbackUntil)
        {
            showingFeedback = false;
            ApplyColor(sequenceColor);
        }
    }

    protected override void OnDestroy()
    {
        if (glowMaterial != null)
            Destroy(glowMaterial);
        base.OnDestroy();
    }
}
