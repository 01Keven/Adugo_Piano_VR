using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pianinho.UI
{
    
    public class MainMenu : MonoBehaviour
    {
        enum Page { Patient, Session, Calibration, Playing, Results }

        [SerializeField] PianoKeyboard m_Keyboard;
        [SerializeField] KeyboardCalibration m_Calibration;
        [SerializeField] RhythmGame m_Game;
        [SerializeField] SongLibrary m_Library;

        [SerializeField] PatientRecordExporter m_Exporter;

        [SerializeField] GameObject m_NameFieldPrefab;

        [SerializeField] bool m_ShowTestSong = true;

        [SerializeField] float m_FrontDistance = 0.65f;
        [SerializeField] float m_FrontBelowHead = 0.12f;
        [SerializeField] float m_SideGap = 0.28f;

        RectTransform m_Canvas;
        readonly Dictionary<Page, RectTransform> m_Pages = new Dictionary<Page, RectTransform>();
        Page m_Page;
        Transform m_Head;

        // Paciente
        TMP_InputField m_NameField;
        TextMeshProUGUI m_PatientHint;

        // Sessão
        TextMeshProUGUI m_SessionTitle;
        TextMeshProUGUI m_SongLabel;
        TextMeshProUGUI m_SongInfo;
        TextMeshProUGUI m_TempoLabel;
        UIFactory.Segmented m_ModeSelect;
        UIFactory.Segmented m_SideSelect;
        UIFactory.Segmented m_BeatsSelect;
        UIFactory.Segmented m_WaitSelect;
        TextMeshProUGUI m_KeysLabel;
        RectTransform m_MoreOptions;
        Button m_MoreButton;
        Button m_ContinueToCalibration;

        // Ajuste
        TextMeshProUGUI m_CalibrationHint;
        TextMeshProUGUI m_DepthLabel;

        // Partida
        TextMeshProUGUI m_PlayingTitle;
        TextMeshProUGUI m_PlayingStatus;

        // Resultado
        TextMeshProUGUI m_ResultTitle;
        TextMeshProUGUI m_ResultBody;
        TextMeshProUGUI m_ResultSaved;
        string m_SaveMessage = "";

        readonly List<SongData> m_Songs = new List<SongData>();
        int m_SongIndex;

        SongData SelectedSong => m_Songs.Count == 0 ? null : m_Songs[Mathf.Clamp(m_SongIndex, 0, m_Songs.Count - 1)];

        Transform Head
        {
            get
            {
                if (m_Head == null && Camera.main != null)
                    m_Head = Camera.main.transform;
                return m_Head;
            }
        }

        void Awake()
        {
            if (m_Keyboard == null)
                m_Keyboard = FindAnyObjectByType<PianoKeyboard>();
            if (m_Calibration == null)
                m_Calibration = FindAnyObjectByType<KeyboardCalibration>();
            if (m_Game == null)
                m_Game = FindAnyObjectByType<RhythmGame>();
            if (m_Exporter == null)
                m_Exporter = FindAnyObjectByType<PatientRecordExporter>();

            LoadSongs();
            Build();
        }

        void OnEnable()
        {
            Session.Changed += OnSessionChanged;
            if (m_Game != null)
            {
                m_Game.Pressed += OnGamePressed;
                m_Game.Finished += OnGameFinished;
            }
            if (m_Keyboard != null)
                m_Keyboard.Rebuilt += OnKeyboardRebuilt;
        }

        void OnDisable()
        {
            Session.Changed -= OnSessionChanged;
            if (m_Game != null)
            {
                m_Game.Pressed -= OnGamePressed;
                m_Game.Finished -= OnGameFinished;
            }
            if (m_Keyboard != null)
                m_Keyboard.Rebuilt -= OnKeyboardRebuilt;
        }

        System.Collections.IEnumerator Start()
        {
            // Espera o rastreamento da cabeça para posicionar o menu na frente do jogador.
            for (var i = 0; i < 30 && (Head == null || Head.localPosition == Vector3.zero); i++)
                yield return null;
            Show(Page.Patient);
        }

        void LoadSongs()
        {
            m_Songs.Clear();
            if (m_Library == null)
                return;
            foreach (var song in m_Library.All)
            {
                if (song.hideFlags.HasFlag(HideFlags.DontSave) && !m_ShowTestSong)
                    continue; // música de teste
                m_Songs.Add(song);
            }
            m_SongIndex = Mathf.Max(0, m_Songs.FindIndex(s => s.isAvailable));
        }

        // ------------------------------------------------------------------ navegação

        /// <summary>
        /// Abre o menu na primeira página, na frente do jogador. Quem chama é a área do piano
        /// (<see cref="PianoZone"/>), toda vez que o jogador entra nela.
        /// </summary>
        public void Open()
        {
            if (m_Canvas == null)
                return;
            Show(Page.Patient);
        }

        void Show(Page page)
        {
            m_Page = page;
            foreach (var pair in m_Pages)
                pair.Value.gameObject.SetActive(pair.Key == page);
            Refresh();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Canvas);

            if (page == Page.Calibration || page == Page.Playing)
                PlaceBesideKeyboard();
            else
                PlaceInFront();
        }

        void PlaceInFront()
        {
            if (Head == null)
                return;
            var forward = Head.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude < 1e-4f ? Vector3.forward : forward.normalized;
            m_Canvas.SetParent(transform, true);
            m_Canvas.SetPositionAndRotation(
                Head.position + forward * m_FrontDistance + Vector3.down * m_FrontBelowHead,
                Quaternion.LookRotation(forward, Vector3.up));
            m_Canvas.localScale = Vector3.one / UIFactory.PixelsPerMeter;
        }

        void PlaceBesideKeyboard()
        {
            if (m_Keyboard == null)
                return;
            // À esquerda do teclado, virado para o jogador e ao alcance da mão.
            m_Canvas.SetParent(m_Keyboard.transform, false);
            var halfHeight = m_Canvas.rect.height * 0.5f / UIFactory.PixelsPerMeter;
            m_Canvas.localPosition = new Vector3(-(m_Keyboard.width * 0.5f + m_SideGap), halfHeight * 0.6f, 0.02f);
            m_Canvas.localRotation = Quaternion.Euler(10f, -35f, 0f);
            m_Canvas.localScale = Vector3.one / UIFactory.PixelsPerMeter;
        }

        void OnKeyboardRebuilt()
        {
            if (m_Page == Page.Calibration || m_Page == Page.Playing)
                PlaceBesideKeyboard();
        }

        // ------------------------------------------------------------------ construção

        void Build()
        {
            m_Canvas = UIFactory.CreatePanel("Main Menu", transform, 600f);
            BuildPatientPage(CreatePage(Page.Patient));
            BuildSessionPage(CreatePage(Page.Session));
            BuildCalibrationPage(CreatePage(Page.Calibration));
            BuildPlayingPage(CreatePage(Page.Playing));
            BuildResultsPage(CreatePage(Page.Results));
            foreach (var page in m_Pages.Values)
                page.gameObject.SetActive(false);
        }

        RectTransform CreatePage(Page page)
        {
            var group = UIFactory.CreateGroup(m_Canvas, page.ToString());
            m_Pages[page] = group;
            return group;
        }

        void BuildPatientPage(RectTransform page)
        {
            UIFactory.CreateText(page, "Pianinho", 40f, FontStyles.Bold, 54f);
            UIFactory.CreateText(page, "Nome do paciente", 22f, FontStyles.Normal, 30f, color: UIFactory.MutedTextColor);
            m_NameField = CreateNameField(page);
            m_PatientHint = UIFactory.CreateText(page, "", 18f, FontStyles.Normal, 26f, color: UIFactory.MutedTextColor);
            var row = UIFactory.CreateRow(page);
            UIFactory.CreateButton(row, "Continuar", OnPatientContinue, UIFactory.PrimaryColor);
        }

        TMP_InputField CreateNameField(Transform parent)
        {
            TMP_InputField field = null;
            if (m_NameFieldPrefab != null)
            {
                // O prefab do exemplo é um canvas inteiro; aproveitamos só o campo, que já abre o teclado virtual.
                var instance = Instantiate(m_NameFieldPrefab);
                field = instance.GetComponentInChildren<TMP_InputField>(true);
                if (field != null)
                {
                    field.transform.SetParent(parent, false);
                    var element = field.GetComponent<LayoutElement>() ?? field.gameObject.AddComponent<LayoutElement>();
                    element.preferredHeight = 60f;
                    element.flexibleWidth = 1f;
                }
                Destroy(instance);
            }

            if (field == null)
            {
                Debug.LogWarning("Pianinho: prefab do campo de nome não configurado; o nome não poderá ser digitado no óculos.", this);
                return null;
            }

            field.characterLimit = 60;
            field.pointSize = 26f;
            if (field.placeholder is TextMeshProUGUI placeholder)
                placeholder.text = "Toque para digitar";
            field.onValueChanged.AddListener(value =>
            {
                Session.Current.patientName = value.Trim();
                m_PatientHint.text = "";
            });
            return field;
        }

        void BuildSessionPage(RectTransform page)
        {
            m_SessionTitle = UIFactory.CreateText(page, "", 30f, FontStyles.Bold, 44f);

            var songRow = UIFactory.CreateRow(page);
            UIFactory.CreateLabel(songRow, "Música");
            UIFactory.CreateButton(songRow, "<", () => ChangeSong(-1), width: 56f);
            m_SongLabel = UIFactory.CreateText(songRow, "", 22f, FontStyles.Bold, UIFactory.RowHeight);
            UIFactory.CreateButton(songRow, ">", () => ChangeSong(1), width: 56f);
            m_SongInfo = UIFactory.CreateText(page, "", 18f, FontStyles.Normal, 26f, color: UIFactory.MutedTextColor);

            var tempoRow = UIFactory.CreateRow(page);
            UIFactory.CreateLabel(tempoRow, "Velocidade");
            UIFactory.CreateButton(tempoRow, "-", () => ChangeTempo(-0.1f), width: 56f);
            m_TempoLabel = UIFactory.CreateText(tempoRow, "", 22f, FontStyles.Bold, UIFactory.RowHeight);
            UIFactory.CreateButton(tempoRow, "+", () => ChangeTempo(0.1f), width: 56f);

            var modeRow = UIFactory.CreateRow(page);
            UIFactory.CreateLabel(modeRow, "Modo");
            m_ModeSelect = UIFactory.CreateSegmented(modeRow, new[] { "Guiado", "Reflexo" },
                i => Change(c => c.mode = (GameMode)i));

            var sideRow = UIFactory.CreateRow(page);
            UIFactory.CreateLabel(sideRow, "Lado");
            m_SideSelect = UIFactory.CreateSegmented(sideRow, new[] { "Ambos", "Esquerdo", "Direito" },
                i => Change(c => c.side = (BodySide)i));

            m_MoreButton = UIFactory.CreateButton(UIFactory.CreateRow(page, 40f), "Mais opções", ToggleMoreOptions,
                UIFactory.UnselectedColor, height: 40f);

            m_MoreOptions = UIFactory.CreateGroup(page, "More Options");
            var beatsRow = UIFactory.CreateRow(m_MoreOptions);
            UIFactory.CreateLabel(beatsRow, "Trecho");
            m_BeatsSelect = UIFactory.CreateSegmented(beatsRow, new[] { "1 batida", "2 batidas", "4 batidas" },
                i => Change(c => c.beatsPerSegment = SessionConfig.AllowedBeatsPerSegment[i]));

            var waitRow = UIFactory.CreateRow(m_MoreOptions);
            UIFactory.CreateLabel(waitRow, "Espera");
            m_WaitSelect = UIFactory.CreateSegmented(waitRow, new[] { "Pausa e espera", "Pontua o ritmo" },
                i => Change(c => c.waitRule = (WaitRule)i));

            var keysRow = UIFactory.CreateRow(m_MoreOptions);
            UIFactory.CreateLabel(keysRow, "Teclas");
            UIFactory.CreateButton(keysRow, "-", () => Change(c => c.keyCount--), width: 56f);
            m_KeysLabel = UIFactory.CreateText(keysRow, "", 22f, FontStyles.Bold, UIFactory.RowHeight);
            UIFactory.CreateButton(keysRow, "+", () => Change(c => c.keyCount++), width: 56f);
            m_MoreOptions.gameObject.SetActive(false);

            var nav = UIFactory.CreateRow(page);
            UIFactory.CreateButton(nav, "Voltar", () => Show(Page.Patient), UIFactory.UnselectedColor, width: 140f);
            m_ContinueToCalibration = UIFactory.CreateButton(nav, "Ajustar teclado", () => Show(Page.Calibration), UIFactory.PrimaryColor);
        }

        void BuildCalibrationPage(RectTransform page)
        {
            UIFactory.CreateText(page, "Ajuste do teclado", 30f, FontStyles.Bold, 44f);
            m_CalibrationHint = UIFactory.CreateText(page,
                "Estenda as mãos onde for confortável tocar e toque em \"Pelas mãos\". Depois ajuste se precisar.",
                19f, FontStyles.Normal, 56f, color: UIFactory.MutedTextColor);

            var place = UIFactory.CreateRow(page);
            UIFactory.CreateButton(place, "Pelas mãos", () =>
            {
                m_CalibrationHint.text = m_Calibration != null && m_Calibration.CalibrateToHands()
                    ? "Teclado posicionado pelas mãos."
                    : "Nenhuma mão ou controle rastreado.";
            });
            UIFactory.CreateButton(place, "Na frente", () => m_Calibration?.PlaceInFront());

            var height = UIFactory.CreateRow(page);
            UIFactory.CreateLabel(height, "Altura");
            UIFactory.CreateButton(height, "Descer", () => m_Calibration?.NudgeHeight(-1));
            UIFactory.CreateButton(height, "Subir", () => m_Calibration?.NudgeHeight(1));

            var distance = UIFactory.CreateRow(page);
            UIFactory.CreateLabel(distance, "Distância");
            UIFactory.CreateButton(distance, "Perto", () => m_Calibration?.NudgeDistance(-1));
            UIFactory.CreateButton(distance, "Longe", () => m_Calibration?.NudgeDistance(1));

            var depth = UIFactory.CreateRow(page);
            UIFactory.CreateLabel(depth, "Toque");
            UIFactory.CreateButton(depth, "-", () => Change(c => c.pressDepth -= 0.005f), width: 56f);
            m_DepthLabel = UIFactory.CreateText(depth, "", 22f, FontStyles.Bold, UIFactory.RowHeight);
            UIFactory.CreateButton(depth, "+", () => Change(c => c.pressDepth += 0.005f), width: 56f);

            var nav = UIFactory.CreateRow(page);
            UIFactory.CreateButton(nav, "Voltar", () => Show(Page.Session), UIFactory.UnselectedColor, width: 140f);
            UIFactory.CreateButton(nav, "Começar", StartGame, UIFactory.PrimaryColor);
        }

        void BuildPlayingPage(RectTransform page)
        {
            m_PlayingTitle = UIFactory.CreateText(page, "", 26f, FontStyles.Bold, 40f);
            m_PlayingStatus = UIFactory.CreateText(page, "", 22f, FontStyles.Normal, 64f);
            var row = UIFactory.CreateRow(page, 64f);
            UIFactory.CreateButton(row, "Parar", () => m_Game?.StopGame(false), UIFactory.DangerColor, height: 64f);
        }

        void BuildResultsPage(RectTransform page)
        {
            m_ResultTitle = UIFactory.CreateText(page, "", 32f, FontStyles.Bold, 48f);
            m_ResultBody = UIFactory.CreateText(page, "", 22f, FontStyles.Normal, 220f, TextAlignmentOptions.TopLeft);
            m_ResultSaved = UIFactory.CreateText(page, "", 17f, FontStyles.Normal, 48f, TextAlignmentOptions.TopLeft, UIFactory.MutedTextColor);
            var row = UIFactory.CreateRow(page);
            UIFactory.CreateButton(row, "Menu", () => Show(Page.Session), UIFactory.UnselectedColor);
            UIFactory.CreateButton(row, "Jogar de novo", () => Show(Page.Calibration), UIFactory.PrimaryColor);
        }

        // ------------------------------------------------------------------ ações

        void OnPatientContinue()
        {
            var name = m_NameField != null ? m_NameField.text.Trim() : Session.Current.patientName;
            if (string.IsNullOrEmpty(name))
            {
                m_PatientHint.text = "Digite o nome do paciente para continuar.";
                return;
            }
            Session.Current.patientName = name;
            Show(Page.Session);
        }

        void ChangeSong(int delta)
        {
            if (m_Songs.Count == 0)
                return;
            m_SongIndex = (m_SongIndex + delta + m_Songs.Count) % m_Songs.Count;
            Refresh();
        }

        void ChangeTempo(float delta)
        {
            // Arredonda para múltiplos de 10% para não acumular erro de ponto flutuante.
            var tempo = Mathf.Round((Session.Current.tempo + delta) * 10f) / 10f;
            Change(c => c.tempo = tempo);
        }

        void ToggleMoreOptions()
        {
            m_MoreOptions.gameObject.SetActive(!m_MoreOptions.gameObject.activeSelf);
            Refresh();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_Canvas);
        }

        static void Change(Action<SessionConfig> change)
        {
            change(Session.Current);
            Session.NotifyChanged();
        }

        void StartGame()
        {
            var song = SelectedSong;
            if (song == null || !song.isAvailable)
            {
                Show(Page.Session);
                return;
            }
            if (m_Game.StartGame(song))
                Show(Page.Playing);
            else
                m_CalibrationHint.text = "Não foi possível começar com essa música.";
        }

        void OnGamePressed(PressRecord record)
        {
            if (m_Page == Page.Playing)
                Refresh();
        }

        void OnGameFinished(SessionResults results)
        {
            m_SaveMessage = "";
            if (m_Exporter != null)
            {
                var path = m_Exporter.Export(results, out var error);
                m_SaveMessage = path != null
                    ? $"Prontuário salvo em {m_Exporter.DisplayPath(System.IO.Path.GetDirectoryName(path))}"
                    : $"<color=#E8A09A>Não foi possível salvar o prontuário: {error}</color>";
            }
            Show(Page.Results);
        }

        void OnSessionChanged(SessionConfig _) => Refresh();

        // ------------------------------------------------------------------ atualização

        void Refresh()
        {
            var config = Session.Current;
            switch (m_Page)
            {
                case Page.Session:
                    RefreshSession(config);
                    break;
                case Page.Calibration:
                    m_DepthLabel.text = $"{Mathf.RoundToInt(config.pressDepth * 1000f)} mm";
                    break;
                case Page.Playing:
                    RefreshPlaying();
                    break;
                case Page.Results:
                    RefreshResults();
                    break;
            }
        }

        void RefreshSession(SessionConfig config)
        {
            m_SessionTitle.text = string.IsNullOrEmpty(config.patientName) ? "Sessão" : $"Sessão de {config.patientName}";

            var song = SelectedSong;
            m_SongLabel.text = song != null ? song.title : "(nenhuma)";
            if (song == null)
                m_SongInfo.text = "Nenhuma música cadastrada.";
            else if (!song.isAvailable)
                m_SongInfo.text = "Essa música ainda não tem áudio.";
            else
                m_SongInfo.text = $"{song.SegmentCount(config.beatsPerSegment)} toques · {FormatDuration(song.clip.length / config.tempo)}";

            var bpm = song != null ? song.bpm * config.tempo : 0f;
            m_TempoLabel.text = bpm > 0f ? $"{config.tempo:P0} · {bpm:0} BPM" : $"{config.tempo:P0}";

            m_ModeSelect.Select((int)config.mode);
            m_SideSelect.Select((int)config.side);
            m_BeatsSelect.Select(Array.IndexOf(SessionConfig.AllowedBeatsPerSegment, config.beatsPerSegment));
            m_WaitSelect.Select((int)config.waitRule);
            m_KeysLabel.text = config.keyCount.ToString();
            m_MoreButton.GetLabel().text = m_MoreOptions.gameObject.activeSelf ? "Menos opções" : "Mais opções";

            var canPlay = song != null && song.isAvailable && song.SegmentCount(config.beatsPerSegment) > 0;
            m_ContinueToCalibration.interactable = canPlay;
            m_ContinueToCalibration.SetColor(canPlay ? UIFactory.PrimaryColor : UIFactory.DisabledColor);
        }

        void RefreshPlaying()
        {
            if (m_Game == null || m_Game.song == null)
                return;
            var config = Session.Current;
            m_PlayingTitle.text = $"{m_Game.song.title} · {(config.mode == GameMode.Guided ? "Guiado" : "Reflexo")}";
            var r = m_Game.results;
            m_PlayingStatus.text = r == null ? "" :
                $"Trecho {m_Game.nextSegment} de {m_Game.totalSegments}\n{r.score} pontos · {r.correctCount} acertos · {r.errorCount} erros";
        }

        void RefreshResults()
        {
            var r = RhythmGame.lastResults;
            if (r == null)
                return;
            m_ResultTitle.text = r.completed ? "Fim da música!" : "Partida encerrada";
            var c = r.config;
            var lines = new List<string>
            {
                $"<b>{c.patientName}</b> · {r.songTitle}",
                $"{(c.mode == GameMode.Guided ? "Guiado" : "Reflexo")} · lado {SideName(c.side)} · {c.tempo:P0} da velocidade",
                "",
                $"Pontos: <b>{r.score}</b>",
                $"Acertos: {r.correctCount}   Erros: {r.errorCount}   Precisão: {r.accuracy:P0}",
                $"Trechos tocados: {CountSegments(r)} de {r.totalSegments}",
            };
            if (r.averageReaction > 0)
                lines.Add($"Tempo médio de reação: {r.averageReaction:0.00} s");
            lines.Add($"Duração: {FormatDuration((float)r.durationSeconds)}");
            m_ResultBody.text = string.Join("\n", lines);
            m_ResultSaved.text = m_SaveMessage;
        }

        static int CountSegments(SessionResults r)
        {
            var n = 0;
            foreach (var p in r.presses)
                if (p.segmentIndex >= 0)
                    n++;
            return n;
        }

        static string SideName(BodySide side) =>
            side == BodySide.Both ? "ambos" : side == BodySide.Left ? "esquerdo" : "direito";

        static string FormatDuration(float seconds)
        {
            var s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }
    }
}