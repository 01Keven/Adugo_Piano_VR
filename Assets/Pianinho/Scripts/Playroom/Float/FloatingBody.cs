using UnityEngine;

namespace Playroom
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Playroom/Floating Body")]
    public class FloatingBody : MonoBehaviour
    {
        
        [SerializeField, Min(0f)] float m_Lift = 2.5f;

        [SerializeField] float m_CeilingY = 3.2f;

        [SerializeField, Min(0.01f)] float m_SoftZone = 0.5f;

        
        [SerializeField, Min(0f)] float m_SwayAcceleration = 0.8f;

        [SerializeField, Min(0f)] float m_SwayFrequency = 0.7f;

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
            // empuxo do hélio para cima reduz suavemente perto do teto
            var heightFactor = Mathf.Clamp01((m_CeilingY - m_Body.position.y) / m_SoftZone);
            var acceleration = Vector3.up * (m_Lift * heightFactor);

            // turbulencia de ar em 3 eixos 
            var t = Time.time * m_SwayFrequency + m_NoiseSeed;
            var windX = (Mathf.PerlinNoise(t, 0f) - 0.5f) + (Mathf.PerlinNoise(t * 2.3f, 5f) - 0.5f) * 0.4f;
            var windZ = (Mathf.PerlinNoise(0f, t) - 0.5f) + (Mathf.PerlinNoise(5f, t * 2.3f) - 0.5f) * 0.4f;
            var windY = (Mathf.PerlinNoise(t * 1.5f, t * 1.5f) - 0.5f) * 0.3f;

            acceleration += new Vector3(windX, windY, windZ) * (m_SwayAcceleration * 2f);
            m_Body.AddForce(acceleration, ForceMode.Acceleration);

            // torque suave para o cilindro do balao se alinhar verticalmente tipo pendulo
            var uprightAxis = Vector3.Cross(transform.up, Vector3.up);
            m_Body.AddTorque(uprightAxis * m_UprightTorque, ForceMode.Acceleration);
        }
    }
}