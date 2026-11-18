using UnityEngine;

namespace Playroom
{
    /// <summary>
    /// Toca um som de impacto 3D proporcional à velocidade da colisão, com variação de tom.
    /// Funciona em qualquer objeto com Rigidbody (bola, bloco, pelúcia).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Playroom/Impact Sound")]
    public class ImpactSound : MonoBehaviour
    {
        [SerializeField] AudioClip[] m_Clips;

        [Header("Velocidade do impacto (m/s)")]
        [SerializeField, Min(0f)] float m_MinSpeed = 0.8f;
        [SerializeField, Min(0.01f)] float m_MaxSpeed = 6f;

        [Header("Variação")]
        [SerializeField] Vector2 m_PitchRange = new Vector2(0.92f, 1.08f);

        [Tooltip("Tempo mínimo entre dois sons, para não virar metralhadora.")]
        [SerializeField, Min(0f)] float m_Cooldown = 0.08f;

        AudioSource m_Audio;
        float m_LastPlayTime = float.NegativeInfinity;

        void Awake()
        {
            m_Audio = GetComponent<AudioSource>();
            m_Audio.playOnAwake = false;
            m_Audio.spatialBlend = 1f;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (m_Clips == null || m_Clips.Length == 0 || Time.time - m_LastPlayTime < m_Cooldown)
                return;

            var speed = collision.relativeVelocity.magnitude;
            if (speed < m_MinSpeed)
                return;

            m_LastPlayTime = Time.time;
            var loudness = Mathf.InverseLerp(m_MinSpeed, m_MaxSpeed, speed);
            m_Audio.pitch = Random.Range(m_PitchRange.x, m_PitchRange.y);
            m_Audio.PlayOneShot(m_Clips[Random.Range(0, m_Clips.Length)], Mathf.Lerp(0.2f, 1f, loudness));
        }
    }
}
