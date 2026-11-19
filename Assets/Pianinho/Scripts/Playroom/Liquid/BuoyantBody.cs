using UnityEngine;

namespace Playroom
{
    
    [DisallowMultipleComponent]
    [AddComponentMenu("Playroom/Buoyant Body")]
    public class BuoyantBody : MonoBehaviour
    {
        [SerializeField, Min(0f)] float m_FloatFactor = 1.3f;

        [Tooltip("Profundidade, em metros, em que o empuxo chega ao máximo.")]
        [SerializeField, Min(0.01f)] float m_FullSubmersionDepth = 0.15f;

        public float floatFactor => m_FloatFactor;
        public float fullSubmersionDepth => m_FullSubmersionDepth;
    }
}
