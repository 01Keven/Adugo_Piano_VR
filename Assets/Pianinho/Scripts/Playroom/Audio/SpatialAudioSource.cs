using UnityEngine;

namespace Playroom
{
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Playroom/Spatial Audio Source")]
    public class SpatialAudioSource : MonoBehaviour
    {
        [Header("Som")]
        [SerializeField] AudioClip m_Clip;
        [SerializeField] bool m_Loop = true;
        [SerializeField] bool m_PlayOnEnable = true;
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.8f;

        [Header("Espacialização")]
        [SerializeField, Min(0.1f)] float m_MinDistance = 1f;
        [SerializeField, Min(0.2f)] float m_MaxDistance = 12f;

        [Tooltip("0 = ponto único; valores maiores deixam o som mais largo.")]
        [SerializeField, Range(0f, 360f)] float m_Spread;

        [Tooltip("Deixe em 0 em VR: o efeito Doppler costuma causar desconforto.")]
        [SerializeField, Range(0f, 5f)] float m_DopplerLevel;

        AudioSource m_Audio;

        public bool isPlaying => m_Audio != null && m_Audio.isPlaying;

        void Awake()
        {
            m_Audio = GetComponent<AudioSource>();
            Apply();
        }

        void OnEnable()
        {
            if (m_PlayOnEnable)
                Play();
        }

        void OnValidate()
        {
            m_MaxDistance = Mathf.Max(m_MaxDistance, m_MinDistance + 0.1f);
            if (TryGetComponent(out m_Audio))
                Apply();
        }

        public void Play()
        {
            if (m_Audio != null && m_Clip != null && !m_Audio.isPlaying)
                m_Audio.Play();
        }

        public void Stop()
        {
            if (m_Audio != null)
                m_Audio.Stop();
        }

        void Apply()
        {
            m_Audio.clip = m_Clip;
            m_Audio.loop = m_Loop;
            m_Audio.playOnAwake = false;
            m_Audio.volume = m_Volume;

            m_Audio.spatialBlend = 1f;
            m_Audio.spatialize = true;
            m_Audio.rolloffMode = AudioRolloffMode.Logarithmic;
            m_Audio.minDistance = m_MinDistance;
            m_Audio.maxDistance = m_MaxDistance;
            m_Audio.spread = m_Spread;
            m_Audio.dopplerLevel = m_DopplerLevel;
        }
    }
}
