using UnityEngine;

namespace Playroom
{
    /// <summary>
    /// Faz um Rigidbody flutuar (balões): sem gravidade, sobe devagar até um teto macio e balança
    /// de leve. Só aplica força quando não está na mão.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Playroom/Floating Body")]
    public class FloatingBody : MonoBehaviour
    {
        [Header("Subida")]
        [Tooltip("Aceleração líquida para cima, em m/s².")]
        [SerializeField, Min(0f)] float m_Lift = 0.6f;

        [Tooltip("Altura (mundo) do teto do salão.")]
        [SerializeField] float m_CeilingY = 3.2f;

        [Tooltip("Distância antes do teto em que a subida vai diminuindo.")]
        [SerializeField, Min(0.01f)] float m_SoftZone = 0.5f;

        [Header("Balanço")]
        [SerializeField, Min(0f)] float m_SwayAcceleration = 0.15f;
        [SerializeField, Min(0f)] float m_SwayFrequency = 0.6f;

        Rigidbody m_Body;
        GrabEvents m_Grab;
        float m_NoiseSeed;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_Body.useGravity = false;
            TryGetComponent(out m_Grab);
            m_NoiseSeed = Random.value * 100f;
        }

        void FixedUpdate()
        {
            if (m_Grab != null && m_Grab.isHeld)
                return;

            var heightFactor = Mathf.Clamp01((m_CeilingY - m_Body.position.y) / m_SoftZone);
            var acceleration = Vector3.up * (m_Lift * heightFactor);

            // Ruído suave, diferente para cada balão, para o movimento parecer orgânico.
            var t = Time.time * m_SwayFrequency + m_NoiseSeed;
            acceleration += new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, 0f, Mathf.PerlinNoise(0f, t) - 0.5f)
                            * (m_SwayAcceleration * 2f);

            m_Body.AddForce(acceleration, ForceMode.Acceleration);
        }
    }
}
