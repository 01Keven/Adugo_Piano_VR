using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Pianinho
{
    /// <summary>
    /// A partida: cada toque na tecla certa toca o próximo trecho da música.
    /// <list type="bullet">
    /// <item>Modo guiado: a tecla a tocar fica azul e a seguinte roxa. A próxima já acende enquanto o
    /// trecho toca; se o paciente tocar antes do fim, o trecho seguinte sai emendado.</item>
    /// <item>Modo reflexo: só a tecla atual acende, e só quando o trecho termina. O tempo entre acender e
    /// tocar é o tempo de reação.</item>
    /// </list>
    /// Em qualquer regra de espera, a música pausa se o paciente demora; na regra "pontuar o ritmo",
    /// tocar perto do fim do trecho vale mais pontos.
    /// </summary>
    public class RhythmGame : MonoBehaviour
    {
        [SerializeField] PianoKeyboard m_Keyboard;
        [SerializeField] SegmentPlayer m_Player;
        [SerializeField] GameplayInputMode m_InputMode;

        [Tooltip("Por quanto tempo a tecla fica verde ou vermelha depois do toque, em segundos.")]
        [SerializeField] float m_FlashTime = 0.35f;

        [Header("Pontuação")]
        [SerializeField] int m_PointsPerHit = 100;
        [Tooltip("Regra 'pontuar o ritmo': diferença máxima, em segundos, para o toque contar como no tempo.")]
        [SerializeField] float m_OnTimeWindow = 0.15f;
        [SerializeField] float m_CloseWindow = 0.4f;
        [SerializeField] int m_PointsClose = 70;
        [SerializeField] int m_PointsLate = 40;

        SessionConfig m_Config;
        SongData m_Song;
        int[] m_Sequence;
        int m_Next;
        bool m_TargetLit;
        double m_LitTime;
        double m_StartTime;
        double m_PreviousEndTime = double.NaN;
        readonly Dictionary<int, (KeyState state, double until)> m_Flashes = new Dictionary<int, (KeyState, double)>();
        readonly List<int> m_Expired = new List<int>();

        public bool isRunning { get; private set; }
        public SessionResults results { get; private set; }
        public SongData song => m_Song;
        public int nextSegment => m_Next;
        public int totalSegments => m_Sequence?.Length ?? 0;

        /// <summary>Tecla que deve ser tocada agora, ou -1 se nenhuma está acesa.</summary>
        public int currentTarget => isRunning && m_TargetLit && m_Next < totalSegments ? m_Sequence[m_Next] : -1;

        /// <summary>Resultado da última partida terminada (para o prontuário).</summary>
        public static SessionResults lastResults { get; private set; }

        public event Action Started;
        public event Action<PressRecord> Pressed;
        public event Action<SessionResults> Finished;

        static double Now => Time.realtimeSinceStartupAsDouble;

        void Awake()
        {
            if (m_Keyboard == null)
                m_Keyboard = GetComponentInChildren<PianoKeyboard>();
            if (m_Player == null)
                m_Player = GetComponentInChildren<SegmentPlayer>();
            if (m_InputMode == null)
                m_InputMode = FindAnyObjectByType<GameplayInputMode>();
        }

        void OnEnable()
        {
            if (m_Keyboard == null)
                return;
            m_Keyboard.KeyPressed += OnKeyPressed;
            m_Keyboard.Rebuilt += OnKeyboardRebuilt;
        }

        void OnDisable()
        {
            if (m_Keyboard != null)
            {
                m_Keyboard.KeyPressed -= OnKeyPressed;
                m_Keyboard.Rebuilt -= OnKeyboardRebuilt;
            }
            if (isRunning)
                StopGame(false);
        }

        /// <summary>
        /// Começa uma partida com a configuração atual da sessão. Retorna falso se a música não tem áudio
        /// ou é curta demais para o tamanho de trecho escolhido.
        /// </summary>
        public bool StartGame(SongData songToPlay, int? seed = null)
        {
            if (isRunning)
                StopGame(false);
            if (songToPlay == null || !songToPlay.isAvailable)
                return false;

            m_Config = Session.Current.Clone();
            var count = songToPlay.SegmentCount(m_Config.beatsPerSegment);
            if (count == 0 || m_Keyboard.keys.Count == 0)
                return false;

            var allowed = KeySequencer.AllowedKeys(m_Keyboard.keys.Count, m_Config.side);
            var random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            m_Sequence = KeySequencer.Generate(count, allowed, random);
            m_Song = songToPlay;
            m_Next = 0;
            m_PreviousEndTime = double.NaN;
            m_Flashes.Clear();
            m_StartTime = Now;
            results = new SessionResults
            {
                config = m_Config,
                songTitle = songToPlay.title,
                startedAt = DateTime.Now,
                totalSegments = count,
            };

            isRunning = true;
            m_InputMode?.EnterGameplay();
            LightTarget();
            Started?.Invoke();
            return true;
        }

        /// <summary>Encerra a partida. <paramref name="completed"/> = a música chegou ao fim.</summary>
        public void StopGame(bool completed)
        {
            if (!isRunning)
                return;
            isRunning = false;
            if (!completed)
                m_Player.StopAll();
            results.durationSeconds = Now - m_StartTime;
            results.completed = completed;
            lastResults = results;
            m_TargetLit = false;
            m_Flashes.Clear();
            m_Keyboard.SetAll(KeyState.Idle);
            m_InputMode?.ExitGameplay();
            Finished?.Invoke(results);
        }

        void Update()
        {
            if (!isRunning)
                return;

            var audioDone = AudioSettings.dspTime >= m_Player.scheduledEndDsp;

            // Modo reflexo: a próxima tecla só acende quando o trecho termina.
            if (!m_TargetLit && m_Next < totalSegments && audioDone)
                LightTarget();

            if (m_Next >= totalSegments && audioDone)
            {
                StopGame(true);
                return;
            }

            if (m_Flashes.Count > 0)
            {
                var now = Now;
                m_Expired.Clear();
                foreach (var pair in m_Flashes)
                    if (now >= pair.Value.until)
                        m_Expired.Add(pair.Key);
                foreach (var key in m_Expired)
                    m_Flashes.Remove(key);
                if (m_Expired.Count > 0)
                    Recolor();
            }
        }

        void OnKeyPressed(PianoKey key, InteractorHandedness hand)
        {
            if (!isRunning || m_Next >= totalSegments)
                return;

            var now = Now;
            var record = new PressRecord
            {
                time = now - m_StartTime,
                keyIndex = key.index,
                targetKeyIndex = m_TargetLit ? m_Sequence[m_Next] : -1,
                hand = hand,
                timingError = double.NaN,
                segmentIndex = -1,
            };

            if (!m_TargetLit)
                record.outcome = PressOutcome.TooEarly;
            else if (key.index != m_Sequence[m_Next])
                record.outcome = PressOutcome.WrongKey;
            else if (!KeySequencer.HandAllowed(hand, m_Config.side))
                record.outcome = PressOutcome.WrongHand;
            else
                record.outcome = PressOutcome.Correct;

            if (record.outcome != PressOutcome.Correct)
            {
                Flash(key.index, KeyState.Wrong);
                Record(record);
                return;
            }

            record.reactionTime = now - m_LitTime;
            record.points = m_PointsPerHit;
            if (m_Config.waitRule == WaitRule.ScoreRhythm && !double.IsNaN(m_PreviousEndTime))
            {
                record.timingError = now - m_PreviousEndTime;
                var error = Math.Abs(record.timingError);
                record.points = error <= m_OnTimeWindow ? m_PointsPerHit : error <= m_CloseWindow ? m_PointsClose : m_PointsLate;
            }

            // Se o trecho anterior ainda está tocando, o próximo entra emendado; senão começa já.
            m_Player.Schedule(m_Song, m_Next, m_Config.beatsPerSegment, m_Config.tempo, m_Player.scheduledEndDsp);
            m_PreviousEndTime = now + (m_Player.scheduledEndDsp - AudioSettings.dspTime);

            record.segmentIndex = m_Next;
            m_Next++;
            m_TargetLit = false;
            Flash(key.index, KeyState.Correct);
            if (m_Next < totalSegments && m_Config.mode == GameMode.Guided)
                LightTarget();
            Record(record);
        }

        void Record(PressRecord record)
        {
            results.presses.Add(record);
            Pressed?.Invoke(record);
        }

        void LightTarget()
        {
            m_TargetLit = true;
            m_LitTime = Now;
            Recolor();
        }

        void Flash(int keyIndex, KeyState state)
        {
            m_Flashes[keyIndex] = (state, Now + m_FlashTime);
            Recolor();
        }

        void Recolor()
        {
            var keys = m_Keyboard.keys;
            for (var i = 0; i < keys.Count; i++)
            {
                var state = KeyState.Idle;
                if (m_TargetLit && m_Next < totalSegments)
                {
                    if (i == m_Sequence[m_Next])
                        state = KeyState.Current;
                    else if (m_Config.mode == GameMode.Guided && m_Next + 1 < totalSegments && i == m_Sequence[m_Next + 1])
                        state = KeyState.Next;
                }
                if (m_Flashes.TryGetValue(i, out var flash))
                    state = flash.state;
                keys[i].SetState(state);
            }
        }

        void OnKeyboardRebuilt()
        {
            // Mudar o número de teclas no meio da partida invalida a sequência.
            if (isRunning)
                StopGame(false);
        }
    }
}
