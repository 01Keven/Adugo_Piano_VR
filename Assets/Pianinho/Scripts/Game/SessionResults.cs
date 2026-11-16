using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Pianinho
{
    /// <summary>Por que um toque não contou.</summary>
    public enum PressOutcome
    {
        Correct,
        /// <summary>Tecla diferente da que estava acesa.</summary>
        WrongKey,
        /// <summary>Tecla certa, mas com a mão do lado que não está sendo exercitado.</summary>
        WrongHand,
        /// <summary>No modo reflexo, tocou antes de a tecla acender.</summary>
        TooEarly,
    }

    /// <summary>Um toque registrado durante a partida, para o prontuário.</summary>
    [Serializable]
    public struct PressRecord
    {
        /// <summary>Segundos desde o início da partida.</summary>
        public double time;
        public int keyIndex;
        /// <summary>Tecla que deveria ser tocada (-1 se nenhuma estava acesa).</summary>
        public int targetKeyIndex;
        public InteractorHandedness hand;
        public PressOutcome outcome;
        /// <summary>Segundos entre a tecla acender e o toque (só em toques certos).</summary>
        public double reactionTime;
        /// <summary>
        /// Só na regra "pontuar o ritmo": segundos entre o toque e o fim do trecho anterior.
        /// Negativo = adiantado, positivo = atrasado. NaN quando não se aplica.
        /// </summary>
        public double timingError;
        public int points;
        /// <summary>Trecho que este toque liberou (-1 em erros).</summary>
        public int segmentIndex;
    }

    /// <summary>Resultado de uma partida. É a base do prontuário.</summary>
    [Serializable]
    public class SessionResults
    {
        public SessionConfig config;
        public string songTitle;
        public DateTime startedAt;
        public double durationSeconds;
        public int totalSegments;
        public bool completed;
        public List<PressRecord> presses = new List<PressRecord>();

        public int score
        {
            get
            {
                var total = 0;
                foreach (var p in presses)
                    total += p.points;
                return total;
            }
        }

        public int correctCount => Count(p => p.outcome == PressOutcome.Correct);
        public int errorCount => Count(p => p.outcome != PressOutcome.Correct);

        /// <summary>Percentual de toques certos sobre todos os toques.</summary>
        public float accuracy => presses.Count == 0 ? 0f : (float)correctCount / presses.Count;

        /// <summary>Tempo médio de reação nos toques certos, em segundos.</summary>
        public double averageReaction
        {
            get
            {
                double sum = 0;
                var n = 0;
                foreach (var p in presses)
                {
                    if (p.outcome != PressOutcome.Correct)
                        continue;
                    sum += p.reactionTime;
                    n++;
                }
                return n == 0 ? 0 : sum / n;
            }
        }

        int Count(Predicate<PressRecord> match)
        {
            var n = 0;
            foreach (var p in presses)
                if (match(p))
                    n++;
            return n;
        }
    }
}
