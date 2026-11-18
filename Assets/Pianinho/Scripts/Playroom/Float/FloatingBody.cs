using UnityEngine;

namespace Playroom
{
    /// <summary>
    /// Simula um balão de gás hélio: empuxo contínuo para cima, desaceleração suave no teto,
    /// turbulência de vento e auto-alinhamento vertical para balançar como pêndulo invertido.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Playroom/Floating Body")]
    public class FloatingBody : MonoBehaviour
    {
        [Header("Subida (Hélio)")]
        [Tooltip("Aceleração líquida para cima, em m/s².")]
        [SerializeField, Min(0f)] float m_Lift = 2.5f;

        [Tooltip("Altura (mundo) do teto do salão.")]
        [SerializeField] float m_CeilingY = 3.2f;

        [Tooltip("Distância antes do teto em que a subida vai diminuindo.")]
        [SerializeField, Min(0.01f)] float m_SoftZone = 0.5f;

        [Header("Vento e Balanço Orgânico")]
        [Tooltip("Intensidade das rajadas laterais de ar.")]
        [SerializeField, Min(0f)] float m_SwayAcceleration = 0.8f;

        [Tooltip("Velocidade de variação do vento.")]
        [SerializeField, Min(0f)] float m_SwayFrequency = 0.7f;

        [Tooltip("Força que mantém o topo do balão apontado para cima enquanto balança.")]
        [SerializeField, Min(0f)] float m_UprightTorque = 1.5f;

        Rigidbody m_Body;
        float m_NoiseSeed;

        void Awake()
        {
            m_Body = GetComponent<Rigidbody>();
            m_Body.useGravity = false;
            m_NoiseSeed = Random.value * 100f;
        }

        void FixedUpdate()
        {
            // 1. Empuxo do hélio para cima (reduz suavemente perto do teto)
            var heightFactor = Mathf.Clamp01((m_CeilingY - m_Body.position.y) / m_SoftZone);
            var acceleration = Vector3.up * (m_Lift * heightFactor);

            // 2. Turbulência de ar em 3 eixos (Perlin Noise em camadas para não parecer robótico)
            var t = Time.time * m_SwayFrequency + m_NoiseSeed;
            var windX = (Mathf.PerlinNoise(t, 0f) - 0.5f) + (Mathf.PerlinNoise(t * 2.3f, 5f) - 0.5f) * 0.4f;
            var windZ = (Mathf.PerlinNoise(0f, t) - 0.5f) + (Mathf.PerlinNoise(5f, t * 2.3f) - 0.5f) * 0.4f;
            var windY = (Mathf.PerlinNoise(t * 1.5f, t * 1.5f) - 0.5f) * 0.3f;

            acceleration += new Vector3(windX, windY, windZ) * (m_SwayAcceleration * 2f);
            m_Body.AddForce(acceleration, ForceMode.Acceleration);

            // 3. Torque suave para o nó do balão ficar para baixo e o topo para cima (pêndulo invertido)
            var uprightAxis = Vector3.Cross(transform.up, Vector3.up);
            m_Body.AddTorque(uprightAxis * m_UprightTorque, ForceMode.Acceleration);
        }
    }
}