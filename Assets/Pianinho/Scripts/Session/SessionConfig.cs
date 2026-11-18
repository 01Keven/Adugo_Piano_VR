using System;
using UnityEngine;

namespace Pianinho
{
    /// <summary>Modo de jogo definido no GDD.</summary>
    public enum GameMode
    {
        /// <summary>Tecla atual em azul e a próxima em roxo.</summary>
        Guided,
        /// <summary>Só a tecla atual acende; mede o tempo de reação.</summary>
        Reflex,
    }

    /// <summary>Qual lado do corpo está sendo exercitado.</summary>
    public enum BodySide
    {
        Both,
        Left,
        Right,
    }

    /// <summary>O que acontece quando o trecho termina e o paciente ainda não tocou a próxima tecla.</summary>
    public enum WaitRule
    {
        /// <summary>A música pausa e espera, sem pressão de tempo.</summary>
        PauseAndWait,
        /// <summary>A música também espera, mas tocar perto do fim do trecho vale mais pontos.</summary>
        ScoreRhythm,
    }

    /// <summary>
    /// Tudo o que o fisioterapeuta pode ajustar numa sessão fica aqui, com valores padrão.
    /// Novas opções entram como campos desta classe, e o resto do jogo lê daqui.
    /// </summary>
    [Serializable]
    public class SessionConfig
    {
        public const int MinKeys = 5;
        public const int MaxKeys = 12;
        public static readonly int[] AllowedBeatsPerSegment = { 1, 2, 4 };

        [Header("Paciente")]
        public string patientName = "";

        [Header("Partida")]
        public GameMode mode = GameMode.Guided;
        public BodySide side = BodySide.Both;
        public WaitRule waitRule = WaitRule.PauseAndWait;

        [Tooltip("Batidas da música tocadas a cada tecla certa (1, 2 ou 4).")]
        public int beatsPerSegment = 4;

        [Tooltip("Velocidade da música em relação ao original (1 = normal).")]
        [Range(0.5f, 1.5f)] public float tempo = 1f;

        [Header("Teclado")]
        [Range(MinKeys, MaxKeys)] public int keyCount = 8;

        [Tooltip("Quanto a tecla precisa afundar, em metros, para contar como toque.")]
        [Range(0.005f, 0.04f)] public float pressDepth = 0.015f;

        [Header("Calibração (em relação à cabeça)")]
        [Tooltip("Distância horizontal entre a cabeça e o centro do teclado, em metros.")]
        [Range(0.2f, 0.8f)] public float keyboardDistance = 0.42f;

        [Tooltip("Quanto o teclado fica abaixo da cabeça, em metros.")]
        [Range(0.1f, 0.9f)] public float keyboardBelowHead = 0.45f;

        public SessionConfig Clone() => (SessionConfig)MemberwiseClone();

        /// <summary>Corrige valores fora dos limites (por exemplo vindos de um arquivo ou do menu).</summary>
        public void Validate()
        {
            keyCount = Mathf.Clamp(keyCount, MinKeys, MaxKeys);
            if (Array.IndexOf(AllowedBeatsPerSegment, beatsPerSegment) < 0)
                beatsPerSegment = 4;
            tempo = Mathf.Clamp(tempo, 0.5f, 1.5f);
            pressDepth = Mathf.Clamp(pressDepth, 0.005f, 0.04f);
            keyboardDistance = Mathf.Clamp(keyboardDistance, 0.2f, 0.8f);
            keyboardBelowHead = Mathf.Clamp(keyboardBelowHead, 0.1f, 0.9f);
        }
    }

    /// <summary>A sessão em andamento. O menu preenche, o jogo e o prontuário leem.</summary>
    public static class Session
    {
        static SessionConfig s_Current = new SessionConfig();

        public static SessionConfig Current
        {
            get => s_Current;
            set
            {
                s_Current = value ?? new SessionConfig();
                s_Current.Validate();
                Changed?.Invoke(s_Current);
            }
        }

        /// <summary>Disparado quando a configuração é trocada ou alterada via <see cref="NotifyChanged"/>.</summary>
        public static event Action<SessionConfig> Changed;

        public static void NotifyChanged()
        {
            s_Current.Validate();
            Changed?.Invoke(s_Current);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            // Com "Enter Play Mode" sem domain reload, estáticos sobrevivem entre execuções.
            s_Current = new SessionConfig();
            Changed = null;
        }
    }
}
