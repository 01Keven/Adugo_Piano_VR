using UnityEngine;

namespace Playroom
{
   
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Playroom/Plush Ragdoll")]
    public class PlushRagdoll : MonoBehaviour
    {
        [SerializeField, Range(4, 30)] int m_SolverIterations = 12;
        [SerializeField, Range(1, 10)] int m_SolverVelocityIterations = 4;
        [SerializeField, Min(0.1f)] float m_MaxDepenetrationVelocity = 2f;

        [SerializeField, Min(0f)] float m_RestingDamping = 1.5f;
        [SerializeField, Min(0f)] float m_HeldDamping = 4f;

        Rigidbody[] m_Limbs;
        GrabEvents m_Grab;

        void Awake()
        {
            m_Limbs = GetComponentsInChildren<Rigidbody>(true);
            TryGetComponent(out m_Grab);

            foreach (var limb in m_Limbs)
            {
                limb.solverIterations = m_SolverIterations;
                limb.solverVelocityIterations = m_SolverVelocityIterations;
                limb.maxDepenetrationVelocity = m_MaxDepenetrationVelocity;
                limb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            SetDamping(m_RestingDamping);
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

        void OnGrabbed() => SetDamping(m_HeldDamping);

        void OnReleased() => SetDamping(m_RestingDamping);

        void SetDamping(float value)
        {
            foreach (var limb in m_Limbs)
                limb.angularDamping = value;
        }
    }
}
