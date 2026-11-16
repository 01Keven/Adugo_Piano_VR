using UnityEngine;

namespace Pianinho
{
    /// <summary>Estado visual de uma tecla.</summary>
    public enum KeyState
    {
        Idle,
        /// <summary>Tecla a ser tocada agora (azul).</summary>
        Current,
        /// <summary>Próxima tecla no modo guiado (roxo).</summary>
        Next,
        /// <summary>Tocada no momento certo (verde).</summary>
        Correct,
        /// <summary>Tocada no momento errado (vermelho).</summary>
        Wrong,
    }

    /// <summary>
    /// Cores do teclado. O GDD pede baixa saturação para o conforto dos pacientes,
    /// então os tons são suaves, mas ainda bem distintos entre si.
    /// </summary>
    [CreateAssetMenu(menuName = "Pianinho/Key Palette", fileName = "KeyPalette")]
    public class KeyPalette : ScriptableObject
    {
        public Color idle = new Color(0.94f, 0.93f, 0.90f);
        public Color current = new Color(0.42f, 0.62f, 0.86f);
        public Color next = new Color(0.64f, 0.54f, 0.82f);
        public Color correct = new Color(0.50f, 0.77f, 0.57f);
        public Color wrong = new Color(0.88f, 0.50f, 0.47f);
        public Color blackKey = new Color(0.24f, 0.24f, 0.27f);
        public Color body = new Color(0.36f, 0.42f, 0.52f);

        [Tooltip("Quanto a cor da tecla escurece enquanto ela está afundada.")]
        [Range(0f, 0.5f)] public float pressedDarken = 0.15f;

        public Color Get(KeyState state)
        {
            switch (state)
            {
                case KeyState.Current: return current;
                case KeyState.Next: return next;
                case KeyState.Correct: return correct;
                case KeyState.Wrong: return wrong;
                default: return idle;
            }
        }

        static KeyPalette s_Default;

        /// <summary>Paleta padrão, usada quando nenhuma for atribuída.</summary>
        public static KeyPalette Default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = CreateInstance<KeyPalette>();
                    s_Default.hideFlags = HideFlags.DontSave;
                }
                return s_Default;
            }
        }
    }
}
