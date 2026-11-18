using System;
using UnityEngine;

namespace Playroom
{
    /// <summary>
    /// Devolve o objeto ao ponto inicial quando ele sai do salão (abaixo da altura mínima ou longe
    /// demais de casa). Nunca age enquanto o objeto está na mão.
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

        [Tooltip("Intervalo entre checagens, em segundos.")]
        [SerializeField, Min(0.05f)] float m_CheckInterval = 0.25f;

        [Tooltip("Ponto de retorno. Vazio = onde o objeto estava ao iniciar.")]
        [SerializeField] Transform m_Home;

        Rigidbody m_Body;
        GrabEvents m_Grab;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        float m_NextCheck;

        /// <summary>O objeto voltou para casa.</summary>
        public event Action Respawned;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            TryGetComponent(out m_Grab);

            var source = m_Home != null ? m_Home : transform;
            source.GetPositionAndRotation(out m_HomePosition, out m_HomeRotation);
        }

        void Update()
        {
            if (Time.time < m_NextCheck)
                return;
            m_NextCheck = Time.time + m_CheckInterval;

            if (m_Grab != null && m_Grab.isHeld)
                return;

            var position = transform.position;
            var fell = position.y < m_KillY;
            var tooFar = m_MaxDistanceFromHome > 0f &&
                         (position - m_HomePosition).sqrMagnitude > m_MaxDistanceFromHome * m_MaxDistanceFromHome;
            if (fell || tooFar)
                Respawn();
        }

        /// <summary>Volta ao ponto inicial, parado.</summary>
        public void Respawn()
        {
            m_Body.linearVelocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            Respawned?.Invoke();
        }
    }
}
