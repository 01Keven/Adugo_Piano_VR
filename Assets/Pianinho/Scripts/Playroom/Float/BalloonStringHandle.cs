using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Playroom
{
    /// <summary>
    /// Ponta da cordinha do balão: começa presa a um ponto do cenário e se solta
    /// permanentemente no primeiro grab, ficando leve para subir junto com o balão.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
    [AddComponentMenu("Playroom/Balloon String Handle")]
    public class BalloonStringHandle : MonoBehaviour
    {
        [Header("Ancoragem Inicial")]
        [Tooltip("Se marcado, a cordinha começa presa e só solta quando o jogador segurar.")]
        [SerializeField] bool m_StartAnchored = true;

        [Tooltip("Onde a cordinha está amarrada. Se vazio, usa a posição inicial.")]
        [SerializeField] Transform m_AnchorPoint;

        [Header("Visual do Fio (Opcional)")]
        [Tooltip("Transform da base (nó) do balão para desenhar a linha da cordinha.")]
        [SerializeField] Transform m_BalloonKnot;

        Rigidbody m_Body;
        XRGrabInteractable m_Interactable;
        LineRenderer m_Line;
        Vector3 m_AnchorWorldPos;
        bool m_IsDetached;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_Interactable = GetComponent<XRGrabInteractable>();
            TryGetComponent(out m_Line);

            // Cordinha quase sem peso e sem gravidade para não ancorar o balão ao soltar
            m_Body.mass = 0.002f;
            m_Body.useGravity = false;

            m_AnchorWorldPos = m_AnchorPoint != null ? m_AnchorPoint.position : transform.position;

            if (m_StartAnchored)
            {
                m_Body.isKinematic = true;
                transform.position = m_AnchorWorldPos;
                m_IsDetached = false;
            }
            else
            {
                m_Body.isKinematic = false;
                m_IsDetached = true;
            }
        }

        void OnEnable()
        {
            m_Interactable.selectEntered.AddListener(OnGrabbed);
            m_Interactable.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            m_Interactable.selectEntered.RemoveListener(OnGrabbed);
            m_Interactable.selectExited.RemoveListener(OnReleased);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            m_IsDetached = true;
            m_Body.isKinematic = false;
            m_Body.useGravity = false;
        }

        void OnReleased(SelectExitEventArgs args)
        {
            // Garante que o XRGrabInteractable não devolva a cordinha para Kinematic ou com gravidade
            if (m_IsDetached)
            {
                m_Body.isKinematic = false;
                m_Body.useGravity = false;
            }
        }

        void LateUpdate()
        {
            if (m_Line != null && m_BalloonKnot != null)
            {
                m_Line.SetPosition(0, transform.position);
                m_Line.SetPosition(1, m_BalloonKnot.position);
            }
        }
    }
}