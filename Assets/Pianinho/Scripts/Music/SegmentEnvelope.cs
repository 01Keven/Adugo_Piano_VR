using UnityEngine;

namespace Pianinho
{
    /// <summary>Entrada e saída suaves de um trecho, aplicadas amostra a amostra no áudio.</summary>
    public class SegmentEnvelope : MonoBehaviour
    {
        const double k_FadeIn = 0.006;
        const double k_FadeOut = 0.012;

        double m_Start;
        double m_End;
        int m_SampleRate;
        readonly object m_Lock = new object();

        void Awake() => m_SampleRate = AudioSettings.outputSampleRate;

        public void Set(double startDsp, double endDsp)
        {
            lock (m_Lock)
            {
                m_Start = startDsp;
                m_End = endDsp;
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            double start, end;
            lock (m_Lock)
            {
                start = m_Start;
                end = m_End;
            }
            var t = AudioSettings.dspTime;
            var dt = 1.0 / m_SampleRate;
            for (var i = 0; i < data.Length; i += channels)
            {
                var fadeIn = (t - start) / k_FadeIn;
                var fadeOut = (end - t) / k_FadeOut;
                var gain = (float)System.Math.Max(0.0, System.Math.Min(1.0, System.Math.Min(fadeIn, fadeOut)));
                for (var c = 0; c < channels; c++)
                    data[i + c] *= gain;
                t += dt;
            }
        }
    }
}
