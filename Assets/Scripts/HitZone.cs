using UnityEngine;
using UnityEngine.InputSystem;

public class HitZone : MonoBehaviour
{
    [Tooltip("Tecla a ser pressionada para acertar a nota")]
    public InputActionReference stepAction;

    private bool canBePressed;
    private GameObject noteInZone;

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
            Debug.Log("Acertou a nota!");
            // Acertou a nota
            Destroy(noteInZone);
            canBePressed = false;
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
            Debug.Log("deixou passar");
        }
    }

}