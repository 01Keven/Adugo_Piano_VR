using System;
using UnityEngine;

namespace Pianinho
{
    /// <summary>
    /// Uma música do jogo e o que é preciso para picotá-la em trechos: o BPM e onde cai a primeira batida.
    /// </summary>
    [CreateAssetMenu(menuName = "Pianinho/Song", fileName = "Song")]
    public class SongData : ScriptableObject
    {
        public string title = "Nova música";
        public AudioClip clip;

        [Tooltip("Batidas por minuto da gravação original.")]
        public float bpm = 120f;

        [Tooltip("Segundos até a primeira batida (silêncio ou introdução antes do ritmo começar).")]
        public float firstBeatOffset;

        [Tooltip("Batidas por compasso (2 para baião, 4 para a maioria das músicas).")]
        public int beatsPerBar = 4;

        [TextArea] public string credits;

        public bool isAvailable => clip != null && bpm > 0f;

        public double beatDuration => 60.0 / Math.Max(1f, bpm);

        public double SegmentDuration(int beatsPerSegment) => beatsPerSegment * beatDuration;

        /// <summary>Quantos trechos inteiros cabem na música.</summary>
        public int SegmentCount(int beatsPerSegment)
        {
            if (!isAvailable)
                return 0;
            var usable = clip.length - firstBeatOffset;
            return Math.Max(0, (int)Math.Floor(usable / SegmentDuration(beatsPerSegment)));
        }

        /// <summary>Onde o trecho começa na gravação, em segundos.</summary>
        public double SegmentStart(int segmentIndex, int beatsPerSegment) =>
            firstBeatOffset + segmentIndex * SegmentDuration(beatsPerSegment);
    }
}
