using UnityEngine;
using UnityEngine.InputSystem;

public class HitZone : MonoBehaviour
{
    [Tooltip("Tecla a ser pressionada para acertar a nota")]
    public InputActionReference stepAction;

    [Tooltip("Audio Source desta tecla, com o AudioClip da nota correspondente")]
    [SerializeField] private AudioSource noteAudioSource;

    private bool canBePressed;
    private GameObject noteInZone;

    private void Awake()
    {
        if (noteAudioSource == null)
        {
            noteAudioSource = GetComponent<AudioSource>();
        }

        if (noteAudioSource != null)
        {
            noteAudioSource.playOnAwake = false;
            noteAudioSource.loop = false;
        }
    }

    private void OnEnable()
    {
        if (stepAction != null)
        {
            stepAction.action.Enable();
            stepAction.action.performed += OnStep;
            
        }
    }

    private void OnDisable()
    {
        if (stepAction != null)
        {
            stepAction.action.performed -= OnStep;
            stepAction.action.Disable();
        }
    }

    private void OnStep(InputAction.CallbackContext context)
    {
        if (canBePressed && noteInZone != null)
        {
            if (noteAudioSource != null && noteAudioSource.clip != null)
            {
                noteAudioSource.PlayOneShot(noteAudioSource.clip);
            }

            Debug.Log("Acertou a nota!");
            // Acertou a nota
            NoteBehavior note = noteInZone.GetComponentInParent<NoteBehavior>();
            if (note != null)
            {
                note.ChangeColorToGreen();
            }
            canBePressed = false;
            noteInZone = null;
        }
        else
        {
            Debug.Log("Errou a nota!");
            
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Note"))
        {
            canBePressed = true;
            noteInZone = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == noteInZone)
        {
            NoteBehavior note = noteInZone.GetComponentInParent<NoteBehavior>();
            if (note != null)
            {
                note.ChangeColorToRed();
            }
            Debug.Log("Deixou passar a nota!");
            canBePressed = false;
            noteInZone = null;
        }
    }
}
