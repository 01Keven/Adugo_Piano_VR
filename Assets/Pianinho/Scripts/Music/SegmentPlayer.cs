using UnityEngine;

namespace Pianinho
{
    /// <summary>
    /// Toca trechos de uma música com precisão de amostra. Dois canais se alternam, então um trecho
    /// agendado para começar quando o anterior termina sai emendado, sem buraco. Cada trecho tem
    /// entrada e saída suaves para não estalar no corte.
    /// </summary>
    public class SegmentPlayer : MonoBehaviour
    {
        [Tooltip("O GDD pede a música ao fundo, em volume baixo.")]
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.6f;

        [Tooltip("Antecedência mínima para agendar um trecho, em segundos.")]
        [SerializeField] float m_ScheduleLead = 0.04f;

        AudioSource[] m_Sources;
        SegmentEnvelope[] m_Envelopes;
        int m_Next;

        /// <summary>Quando (no relógio de áudio) termina o último trecho agendado.</summary>
        public double scheduledEndDsp { get; private set; }

        public bool isPlaying => AudioSettings.dspTime < scheduledEndDsp;

        public float volume
        {
            get => m_Volume;
            set
            {
                m_Volume = Mathf.Clamp01(value);
                if (m_Sources != null)
                    foreach (var source in m_Sources)
                        source.volume = m_Volume;
            }
        }

        void Awake()
        {
            m_Sources = new AudioSource[2];
            m_Envelopes = new SegmentEnvelope[2];
            for (var i = 0; i < 2; i++)
            {
                var go = new GameObject($"Segment Channel {i + 1}");
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = m_Volume;
                m_Sources[i] = source;
                m_Envelopes[i] = go.AddComponent<SegmentEnvelope>();
            }
        }

        /// <summary>
        /// Agenda um trecho. Começa em <paramref name="startDsp"/> ou assim que possível, o que for
        /// mais tarde. <paramref name="tempo"/> acelera ou desacelera (por enquanto muda também o tom).
        /// Retorna quando o trecho começa, no relógio de áudio.
        /// </summary>
        public double Schedule(SongData song, int segmentIndex, int beatsPerSegment, float tempo, double startDsp)
        {
            if (song == null || !song.isAvailable)
                return AudioSettings.dspTime;

            var now = AudioSettings.dspTime;
            var start = System.Math.Max(startDsp, now + m_ScheduleLead);
            var length = song.SegmentDuration(beatsPerSegment) / tempo;
            var clipStart = song.SegmentStart(segmentIndex, beatsPerSegment);

            var source = m_Sources[m_Next];
            var envelope = m_Envelopes[m_Next];
            m_Next = (m_Next + 1) % m_Sources.Length;

            source.Stop();
            source.clip = song.clip;
            source.pitch = tempo;
            source.timeSamples = Mathf.Clamp((int)(clipStart * song.clip.frequency), 0, song.clip.samples - 1);
            envelope.Set(start, start + length);
            source.PlayScheduled(start);
            source.SetScheduledEndTime(start + length);

            scheduledEndDsp = start + length;
            return start;
        }

        public void StopAll()
        {
            if (m_Sources == null)
                return;
            foreach (var source in m_Sources)
                source.Stop();
            scheduledEndDsp = 0;
        }
    }
}
