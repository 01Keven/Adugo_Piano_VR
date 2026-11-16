using UnityEngine;

namespace Pianinho
{
    /// <summary>
    /// Gera uma música de teste simples (melodia original, 100 BPM, 4/4, 16 compassos), para testar o
    /// jogo sem depender de arquivos de áudio. Cada compasso tem uma marcação mais forte na primeira
    /// batida, então dá para ouvir se os trechos estão começando no lugar certo.
    /// </summary>
    public static class TestSongFactory
    {
        const int k_SampleRate = 44100;
        const float k_Bpm = 100f;
        const int k_BeatsPerBar = 4;
        const int k_Bars = 16;

        // Graus da escala de Dó maior, um por batida (0 = Dó). Melodia original, só para teste.
        static readonly int[] s_Melody =
        {
            0, 2, 4, 2,   4, 5, 7, 4,   5, 4, 2, 0,   2, 4, 2, -1,
            4, 5, 7, 9,   7, 5, 4, 2,   4, 2, 0, 2,   4, 2, 0, -1,
            0, 2, 4, 2,   4, 5, 7, 4,   5, 4, 2, 0,   2, 4, 2, -1,
            7, 9, 11, 12, 11, 9, 7, 5,  4, 2, 4, 2,   0, 0, 0, -1,
        };

        static readonly int[] s_ScaleSemitones = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24 };

        public static SongData Create()
        {
            var beat = 60f / k_Bpm;
            var totalBeats = k_Bars * k_BeatsPerBar;
            var samples = new float[Mathf.CeilToInt(totalBeats * beat * k_SampleRate)];
            var beatSamples = Mathf.RoundToInt(beat * k_SampleRate);

            for (var b = 0; b < totalBeats; b++)
            {
                var offset = Mathf.RoundToInt(b * beat * k_SampleRate);
                var degree = s_Melody[b % s_Melody.Length];
                var downbeat = b % k_BeatsPerBar == 0;

                // Marcação rítmica: grave no tempo forte, leve nos outros.
                AddTone(samples, offset, Mathf.RoundToInt(0.09f * k_SampleRate), downbeat ? 110f : 220f, downbeat ? 0.35f : 0.12f, 40f);

                if (degree < 0)
                    continue;
                var semis = s_ScaleSemitones[Mathf.Min(degree, s_ScaleSemitones.Length - 1)];
                var freq = 261.63f * Mathf.Pow(2f, semis / 12f);
                AddTone(samples, offset, Mathf.RoundToInt(beatSamples * 0.9f), freq, 0.28f, 4f, harmonics: true);
            }

            var clip = AudioClip.Create("Música de teste", samples.Length, 1, k_SampleRate, false);
            clip.SetData(samples, 0);

            var song = ScriptableObject.CreateInstance<SongData>();
            song.hideFlags = HideFlags.DontSave;
            song.name = "Test Song";
            song.title = "Música de teste";
            song.clip = clip;
            song.bpm = k_Bpm;
            song.beatsPerBar = k_BeatsPerBar;
            song.firstBeatOffset = 0f;
            song.credits = "Gerada pelo Pianinho para testes.";
            return song;
        }

        static void AddTone(float[] buffer, int start, int length, float freq, float amplitude, float decay, bool harmonics = false)
        {
            for (var i = 0; i < length && start + i < buffer.Length; i++)
            {
                var t = i / (float)k_SampleRate;
                var attack = Mathf.Clamp01(t / 0.005f);
                var env = attack * Mathf.Exp(-t * decay);
                var phase = 2f * Mathf.PI * freq * t;
                var s = Mathf.Sin(phase);
                if (harmonics)
                    s = 0.7f * s + 0.2f * Mathf.Sin(2f * phase) + 0.1f * Mathf.Sin(3f * phase);
                buffer[start + i] += amplitude * env * s;
            }
        }
    }
}
