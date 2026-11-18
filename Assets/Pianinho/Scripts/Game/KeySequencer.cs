using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Pianinho
{
    /// <summary>Decide quais teclas podem acender e qual mão conta, conforme o lado exercitado.</summary>
    public static class KeySequencer
    {
        /// <summary>
        /// Teclas do lado escolhido. Lado esquerdo = metade esquerda do teclado; direito = metade direita.
        /// Com número ímpar de teclas, a do meio entra nos dois lados.
        /// </summary>
        public static List<int> AllowedKeys(int keyCount, BodySide side)
        {
            var keys = new List<int>();
            var half = keyCount / 2;
            var from = side == BodySide.Right ? half : 0;
            var to = side == BodySide.Left ? keyCount - half : keyCount;
            for (var i = from; i < to; i++)
                keys.Add(i);
            return keys;
        }

        /// <summary>Se a mão pode tocar no lado escolhido. Mão desconhecida (None) sempre conta.</summary>
        public static bool HandAllowed(InteractorHandedness hand, BodySide side)
        {
            if (side == BodySide.Both || hand == InteractorHandedness.None)
                return true;
            return side == BodySide.Left ? hand == InteractorHandedness.Left : hand == InteractorHandedness.Right;
        }

        /// <summary>Sorteia uma tecla por trecho, sem repetir a anterior.</summary>
        public static int[] Generate(int segmentCount, IReadOnlyList<int> allowed, Random random)
        {
            if (allowed == null || allowed.Count == 0)
                throw new ArgumentException("Nenhuma tecla permitida.", nameof(allowed));

            var sequence = new int[segmentCount];
            var previous = -1;
            for (var i = 0; i < segmentCount; i++)
            {
                int pick;
                if (allowed.Count == 1)
                {
                    pick = allowed[0];
                }
                else
                {
                    do
                        pick = allowed[random.Next(allowed.Count)];
                    while (pick == previous);
                }
                sequence[i] = pick;
                previous = pick;
            }
            return sequence;
        }
    }
}
