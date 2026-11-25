using UnityEngine;

namespace Playroom
{
    /// <summary>
    /// Faz o ragdoll se comportar como uma pelúcia: movimento lento e "pesado de enchimento".
    /// Fica no osso raiz (o do tronco), o mesmo que tem o XR Grab Interactable.
    /// Freia os membros (amortecimento linear e angular), limita a velocidade de giro,
    /// fecha um pouco as juntas e deixa o limite delas "macio" para absorver o balanço.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Playroom/Plush Ragdoll")]
    public class PlushRagdoll : MonoBehaviour
    {
        [Header("Estabilidade")]
        [SerializeField, Range(4, 30)] int m_SolverIterations = 14;
        [SerializeField, Range(1, 10)] int m_SolverVelocityIterations = 6;
        [SerializeField, Min(0.1f)] float m_MaxDepenetrationVelocity = 1.5f;

        [Header("Sensação de pelúcia (maior = mais lento e mole)")]
        [SerializeField, Min(0f)] float m_LinearDamping = 1.2f;
        [SerializeField, Min(0f)] float m_RestingAngularDamping = 6f;
        [SerializeField, Min(0f)] float m_HeldAngularDamping = 10f;
        [SerializeField, Min(0.5f)] float m_MaxAngularVelocity = 4f;

        [Header("Juntas")]
        [Tooltip("1 = limites do wizard. Menor fecha mais as juntas, menos balanço.")]
        [SerializeField, Range(0.2f, 1f)] float m_JointLimitScale = 0.6f;
        [SerializeField, Min(0f)] float m_LimitSpring = 40f;
        [SerializeField, Min(0f)] float m_LimitDamper = 8f;

        [Header("Massa")]
        [Tooltip("Massa total da pelúcia em kg. Pelúcia de verdade é leve.")]
        [SerializeField, Min(0.1f)] float m_TotalMass = 1.5f;

        Rigidbody[] m_Limbs;
        GrabEvents m_Grab;

        void Awake()
        {
            m_Limbs = GetComponentsInChildren<Rigidbody>(true);
            TryGetComponent(out m_Grab);

            ApplyMass();

            foreach (var limb in m_Limbs)
            {
                limb.solverIterations = m_SolverIterations;
                limb.solverVelocityIterations = m_SolverVelocityIterations;
                limb.maxDepenetrationVelocity = m_MaxDepenetrationVelocity;
                limb.maxAngularVelocity = m_MaxAngularVelocity;
                limb.linearDamping = m_LinearDamping;
                limb.interpolation = RigidbodyInterpolation.Interpolate;
                limb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                if (limb.TryGetComponent(out CharacterJoint joint))
                    SoftenJoint(joint);
            }
            SetAngularDamping(m_RestingAngularDamping);
        }

        void OnEnable()
        {
            if (m_Grab == null)
                return;
            m_Grab.Grabbed += OnGrabbed;
            m_Grab.Released += OnReleased;
        }

        void OnDisable()
        {
            if (m_Grab == null)
                return;
            m_Grab.Grabbed -= OnGrabbed;
            m_Grab.Released -= OnReleased;
        }

        void OnGrabbed() => SetAngularDamping(m_HeldAngularDamping);

        void OnReleased() => SetAngularDamping(m_RestingAngularDamping);

        void ApplyMass()
        {
            float current = 0f;
            foreach (var limb in m_Limbs)
                current += limb.mass;
            if (current <= 0f)
                return;

            float factor = m_TotalMass / current;
            foreach (var limb in m_Limbs)
                limb.mass *= factor;
        }

        void SoftenJoint(CharacterJoint joint)
        {
            joint.enableProjection = true;
            joint.enablePreprocessing = false;

            joint.lowTwistLimit = Scale(joint.lowTwistLimit);
            joint.highTwistLimit = Scale(joint.highTwistLimit);
            joint.swing1Limit = Scale(joint.swing1Limit);
            joint.swing2Limit = Scale(joint.swing2Limit);

            var spring = new SoftJointLimitSpring
            {
                spring = m_LimitSpring,
                damper = m_LimitDamper
            };
            joint.twistLimitSpring = spring;
            joint.swingLimitSpring = spring;
        }

        SoftJointLimit Scale(SoftJointLimit limit)
        {
            limit.limit *= m_JointLimitScale;
            limit.bounciness = 0f;
            return limit;
        }

        void SetAngularDamping(float value)
        {
            foreach (var limb in m_Limbs)
                limb.angularDamping = value;
        }
    }
}