using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Interaction for the stationary piano keys in PianoStageVR.
public class VRPianoKey : XRSimpleInteractable
{
    [SerializeField] private AudioClip noteClip;
    [SerializeField, ColorUsage(false, true)] private Color glowColor = new Color(3f, 2.4f, 1.5f);
    [SerializeField, Min(0.01f)] private float glowDuration = 0.25f;

    private AudioSource noteAudio;
    private Renderer keyRenderer;
    private MaterialPropertyBlock properties;
    private Material glowMaterial;
    private float feedbackUntil;
    private bool showingFeedback;

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
        if (noteClip != null)
            noteAudio.PlayOneShot(noteClip);

        if (keyRenderer != null)
        {
            keyRenderer.GetPropertyBlock(properties);
            properties.SetColor("_EmissionColor", glowColor);
            keyRenderer.SetPropertyBlock(properties);
            feedbackUntil = Time.time + glowDuration;
            showingFeedback = true;
        }
    }

    private void Update()
    {
        if (showingFeedback && Time.time >= feedbackUntil)
        {
            keyRenderer.SetPropertyBlock(null);
            showingFeedback = false;
        }
    }

    protected override void OnDestroy()
    {
        if (glowMaterial != null)
            Destroy(glowMaterial);
        base.OnDestroy();
    }
}
