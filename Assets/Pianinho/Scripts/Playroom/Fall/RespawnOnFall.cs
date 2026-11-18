using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Playroom
{
    /// <summary>
    /// Devolve o objeto ao ponto inicial quando ele sai do salão ou após ficar solto
    /// por um tempo depois de ser pegado e largado.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Playroom/Respawn On Fall")]
    public class RespawnOnFall : MonoBehaviour
    {
        [Tooltip("Altura (mundo) abaixo da qual o objeto volta para casa.")]
        [SerializeField] float m_KillY = -2f;

        [Tooltip("Distância máxima do ponto inicial, em metros. 0 = desligado.")]
        [SerializeField, Min(0f)] float m_MaxDistanceFromHome;

        [Header("Respawn após largar")]
        [Tooltip("Tempo (em segundos) após soltar o objeto para ele voltar ao Home. 0 = desligado.")]
        [SerializeField, Min(0f)] float m_RespawnDelayAfterRelease = 5f;

        [Tooltip("Intervalo entre checagens de queda/distância, em segundos.")]
        [SerializeField, Min(0.05f)] float m_CheckInterval = 0.25f;

        [Tooltip("Ponto de retorno. Vazio = onde o objeto estava ao iniciar.")]
        [SerializeField] Transform m_Home;

        Rigidbody m_Body;
        XRGrabInteractable m_Interactable;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        float m_NextCheck;

        bool m_IsHeld;
        bool m_WaitingToRespawn;
        float m_ReleaseTimer;

        /// <summary>O objeto voltou para casa.</summary>
        public event Action Respawned;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();

            // Procura o XRGrabInteractable no próprio objeto, nos pais ou nos filhos
            m_Interactable = GetComponentInParent<XRGrabInteractable>();
            if (m_Interactable == null)
                m_Interactable = GetComponentInChildren<XRGrabInteractable>();

            if (m_Interactable == null)
                Debug.LogWarning($"[RespawnOnFall] Nenhum XRGrabInteractable encontrado em {name}!", this);

            var source = m_Home != null ? m_Home : transform;
            source.GetPositionAndRotation(out m_HomePosition, out m_HomeRotation);
        }

        void OnEnable()
        {
            if (m_Interactable != null)
            {
                m_Interactable.selectEntered.AddListener(OnGrabbed);
                m_Interactable.selectExited.AddListener(OnReleased);
            }
        }

        void OnDisable()
        {
            if (m_Interactable != null)
            {
                m_Interactable.selectEntered.RemoveListener(OnGrabbed);
                m_Interactable.selectExited.RemoveListener(OnReleased);
            }
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            m_IsHeld = true;
            m_WaitingToRespawn = false;
            m_ReleaseTimer = 0f;
        }

        void OnReleased(SelectExitEventArgs args)
        {
            // Só inicia o timer se nenhuma outra mão ainda estiver segurando o objeto
            if (m_Interactable != null && m_Interactable.isSelected)
                return;

            m_IsHeld = false;
            m_WaitingToRespawn = true;
            m_ReleaseTimer = 0f;
        }

        void Update()
        {
            if (m_IsHeld)
                return;

            // 1. Conta o tempo após o objeto ser largado
            if (m_WaitingToRespawn && m_RespawnDelayAfterRelease > 0f)
            {
                m_ReleaseTimer += Time.deltaTime;
                if (m_ReleaseTimer >= m_RespawnDelayAfterRelease)
                {
                    Respawn();
                    return;
                }
            }

            // 2. Checagem periódica de queda (KillY) ou distância máxima
            if (Time.time < m_NextCheck)
                return;
            m_NextCheck = Time.time + m_CheckInterval;

            var position = transform.position;
            var fell = position.y < m_KillY;
            var tooFar = m_MaxDistanceFromHome > 0f &&
                         (position - m_HomePosition).sqrMagnitude > m_MaxDistanceFromHome * m_MaxDistanceFromHome;

            if (fell || tooFar)
                Respawn();
        }

        /// <summary>Volta ao ponto inicial, parado, e reseta o estado de interação.</summary>
        public void Respawn()
        {
            m_IsHeld = false;
            m_WaitingToRespawn = false;
            m_ReleaseTimer = 0f;

            m_Body.linearVelocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            Respawned?.Invoke();
        }
    }
}