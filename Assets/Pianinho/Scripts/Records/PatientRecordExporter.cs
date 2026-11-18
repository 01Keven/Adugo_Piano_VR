using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Pianinho
{
    /// <summary>
    /// Salva o prontuário de cada partida em arquivos no próprio aparelho, para o fisioterapeuta copiar
    /// ligando o óculos no computador pelo cabo USB. Nada é enviado pela internet.
    /// <list type="bullet">
    /// <item><c>Pianinho/sessoes.csv</c>: uma linha por partida, de todos os pacientes (evolução).</item>
    /// <item><c>Pianinho/&lt;paciente&gt;/&lt;data&gt;_&lt;música&gt;_toques.csv</c>: cada toque da partida.</item>
    /// <item><c>Pianinho/&lt;paciente&gt;/&lt;data&gt;_&lt;música&gt;_resumo.txt</c>: resumo legível.</item>
    /// </list>
    /// Os CSV usam ponto e vírgula e vírgula decimal, para abrir direto no Excel em português.
    /// </summary>
    public class PatientRecordExporter : MonoBehaviour
    {
        const string k_RootFolder = "Pianinho";
        const string k_Separator = ";";
        static readonly CultureInfo s_Culture = CultureInfo.GetCultureInfo("pt-BR");
        static readonly UTF8Encoding s_Utf8Bom = new UTF8Encoding(true);

        [Tooltip("Pasta base no computador quando roda no Editor ou no Windows. Vazio = Documentos do usuário.")]
        [SerializeField] string m_DesktopRootOverride = "";

        /// <summary>Pasta raiz onde o último prontuário foi salvo.</summary>
        public string rootFolder { get; private set; }

        /// <summary>Caminho do último resumo salvo, ou nulo.</summary>
        public string lastSummaryPath { get; private set; }

        /// <summary>
        /// Salva a partida. Retorna o caminho do resumo, ou nulo se não foi possível salvar
        /// (o motivo vai para o console e para <paramref name="error"/>).
        /// </summary>
        public string Export(SessionResults results, out string error)
        {
            error = null;
            if (results == null)
            {
                error = "Nenhuma partida para salvar.";
                return null;
            }

            try
            {
                rootFolder = ResolveRoot();
                var patientFolder = Path.Combine(rootFolder, SafeName(results.config.patientName, "Sem nome"));
                Directory.CreateDirectory(patientFolder);

                var stamp = results.startedAt.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
                var baseName = $"{stamp}_{SafeName(results.songTitle, "musica")}";
                var pressesPath = Path.Combine(patientFolder, baseName + "_toques.csv");
                var summaryPath = Path.Combine(patientFolder, baseName + "_resumo.txt");
                var sessionsPath = Path.Combine(rootFolder, "sessoes.csv");

                File.WriteAllText(pressesPath, BuildPressesCsv(results), s_Utf8Bom);
                File.WriteAllText(summaryPath, BuildSummary(results), s_Utf8Bom);
                AppendSession(sessionsPath, results, Path.GetFileName(pressesPath));

                MakeVisibleOverUsb(pressesPath, summaryPath, sessionsPath);
                lastSummaryPath = summaryPath;
                Debug.Log($"Pianinho: prontuário salvo em {patientFolder}");
                return summaryPath;
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogError($"Pianinho: não foi possível salvar o prontuário. {e}");
                return null;
            }
        }

        /// <summary>Caminho curto para mostrar na tela, a partir da pasta Pianinho.</summary>
        public string DisplayPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(rootFolder))
                return fullPath;
            var parent = Path.GetDirectoryName(rootFolder);
            var relative = fullPath.StartsWith(parent ?? "") ? fullPath.Substring(parent.Length).TrimStart('/', '\\') : fullPath;
            var location = Application.platform == RuntimePlatform.Android ? StorageLabel(parent) : parent;
            return Path.Combine(location ?? "", relative).Replace('\\', '/');
        }

        // ------------------------------------------------------------------ pastas

        string ResolveRoot()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                // Pasta pública Documents: aparece no computador pelo cabo USB (transferência de arquivos).
                var documents = "/storage/emulated/0/Documents";
                var shared = Path.Combine(documents, k_RootFolder);
                if (CanWrite(shared))
                    return shared;
                // Em versões antigas do Android sem acesso à pasta pública, usa a pasta do app,
                // que também aparece pelo cabo em Android/data/<pacote>/files.
                return Path.Combine(Application.persistentDataPath, k_RootFolder);
            }

            var root = string.IsNullOrEmpty(m_DesktopRootOverride)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : m_DesktopRootOverride;
            if (string.IsNullOrEmpty(root))
                root = Application.persistentDataPath;
            return Path.Combine(root, k_RootFolder);
        }

        static bool CanWrite(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                var probe = Path.Combine(folder, ".teste");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static string StorageLabel(string parent)
        {
            if (parent == null)
                return "";
            const string storage = "/storage/emulated/0/";
            return parent.StartsWith(storage) ? parent.Substring(storage.Length) : parent;
        }

        /// <summary>
        /// No Android, o computador só enxerga arquivos novos pelo cabo depois que o sistema os indexa.
        /// </summary>
        static void MakeVisibleOverUsb(params string[] paths)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var scanner = new AndroidJavaClass("android.media.MediaScannerConnection");
                var mimeTypes = new string[paths.Length];
                for (var i = 0; i < paths.Length; i++)
                    mimeTypes[i] = paths[i].EndsWith(".csv") ? "text/csv" : "text/plain";
                scanner.CallStatic("scanFile", activity, paths, mimeTypes, new ScanCompletedListener());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Pianinho: arquivos salvos, mas o aviso ao sistema falhou ({e.Message}). Eles podem demorar para aparecer no computador.");
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>O Android exige um objeto de retorno; não há nada a fazer quando a indexação termina.</summary>
        class ScanCompletedListener : AndroidJavaProxy
        {
            public ScanCompletedListener() : base("android.media.MediaScannerConnection$OnScanCompletedListener") { }

            // Chamado pelo Android (nome e assinatura vêm da interface Java).
            void onScanCompleted(string path, AndroidJavaObject uri) { }
        }
#endif

        static string SafeName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var c in value.Trim())
                sb.Append(Array.IndexOf(invalid, c) >= 0 || c == ';' ? '_' : c);
            var name = sb.ToString().Trim('.', ' ');
            return name.Length == 0 ? fallback : name.Length > 60 ? name.Substring(0, 60) : name;
        }

        // ------------------------------------------------------------------ conteúdo

        static string BuildPressesCsv(SessionResults r)
        {
            var sb = new StringBuilder();
            Row(sb, "toque", "tempo_s", "tecla", "tecla_acesa", "mao", "resultado", "reacao_s", "diferenca_ritmo_s", "pontos", "trecho");
            for (var i = 0; i < r.presses.Count; i++)
            {
                var p = r.presses[i];
                var correct = p.outcome == PressOutcome.Correct;
                Row(sb,
                    (i + 1).ToString(s_Culture),
                    Num(p.time, "0.000"),
                    (p.keyIndex + 1).ToString(s_Culture),
                    p.targetKeyIndex >= 0 ? (p.targetKeyIndex + 1).ToString(s_Culture) : "",
                    HandName(p.hand),
                    OutcomeName(p.outcome),
                    correct ? Num(p.reactionTime, "0.000") : "",
                    double.IsNaN(p.timingError) ? "" : Num(p.timingError, "0.000"),
                    p.points.ToString(s_Culture),
                    p.segmentIndex >= 0 ? (p.segmentIndex + 1).ToString(s_Culture) : "");
            }
            return sb.ToString();
        }

        static readonly string[] s_SessionHeader =
        {
            "data", "hora", "paciente", "musica", "modo", "lado", "velocidade_pct", "batidas_por_trecho", "espera",
            "teclas", "concluida", "duracao_s", "trechos_tocados", "total_trechos", "pontos", "acertos", "erros",
            "precisao_pct", "reacao_media_s", "erros_tecla", "erros_mao", "adiantados", "arquivo_toques",
        };

        static void AppendSession(string path, SessionResults r, string pressesFile)
        {
            var sb = new StringBuilder();
            var isNew = !File.Exists(path);
            if (isNew)
                Row(sb, s_SessionHeader);

            var c = r.config;
            Row(sb,
                r.startedAt.ToString("dd/MM/yyyy", s_Culture),
                r.startedAt.ToString("HH:mm:ss", s_Culture),
                c.patientName,
                r.songTitle,
                ModeName(c.mode),
                SideName(c.side),
                Num(c.tempo * 100, "0"),
                c.beatsPerSegment.ToString(s_Culture),
                WaitName(c.waitRule),
                c.keyCount.ToString(s_Culture),
                r.completed ? "sim" : "não",
                Num(r.durationSeconds, "0.0"),
                CountSegments(r).ToString(s_Culture),
                r.totalSegments.ToString(s_Culture),
                r.score.ToString(s_Culture),
                r.correctCount.ToString(s_Culture),
                r.errorCount.ToString(s_Culture),
                Num(r.accuracy * 100, "0.0"),
                r.averageReaction > 0 ? Num(r.averageReaction, "0.000") : "",
                Count(r, PressOutcome.WrongKey).ToString(s_Culture),
                Count(r, PressOutcome.WrongHand).ToString(s_Culture),
                Count(r, PressOutcome.TooEarly).ToString(s_Culture),
                pressesFile);

            if (isNew)
                File.WriteAllText(path, sb.ToString(), s_Utf8Bom);
            else
                File.AppendAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        static string BuildSummary(SessionResults r)
        {
            var c = r.config;
            var sb = new StringBuilder();
            sb.AppendLine("PIANINHO - PRONTUÁRIO DA SESSÃO");
            sb.AppendLine(new string('=', 40));
            sb.AppendLine($"Paciente:        {c.patientName}");
            sb.AppendLine($"Data:            {r.startedAt.ToString("dd/MM/yyyy HH:mm", s_Culture)}");
            sb.AppendLine($"Música:          {r.songTitle}");
            sb.AppendLine($"Modo:            {ModeName(c.mode)}");
            sb.AppendLine($"Lado exercitado: {SideName(c.side)}");
            sb.AppendLine($"Velocidade:      {Num(c.tempo * 100, "0")}%");
            sb.AppendLine($"Trecho:          {c.beatsPerSegment} batida(s) por toque");
            sb.AppendLine($"Espera:          {WaitName(c.waitRule)}");
            sb.AppendLine($"Teclas:          {c.keyCount}");
            sb.AppendLine();
            sb.AppendLine("RESULTADO");
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Partida:         {(r.completed ? "música completa" : "encerrada antes do fim")}");
            sb.AppendLine($"Duração:         {FormatDuration(r.durationSeconds)}");
            sb.AppendLine($"Trechos tocados: {CountSegments(r)} de {r.totalSegments}");
            sb.AppendLine($"Pontos:          {r.score}");
            sb.AppendLine($"Acertos:         {r.correctCount}");
            sb.AppendLine($"Erros:           {r.errorCount} (tecla errada {Count(r, PressOutcome.WrongKey)}, mão errada {Count(r, PressOutcome.WrongHand)}, adiantado {Count(r, PressOutcome.TooEarly)})");
            sb.AppendLine($"Precisão:        {Num(r.accuracy * 100, "0.0")}%");
            if (r.averageReaction > 0)
                sb.AppendLine($"Reação média:    {Num(r.averageReaction, "0.00")} s");
            AppendHandBreakdown(sb, r);
            sb.AppendLine();
            sb.AppendLine("Cada toque está detalhado no arquivo _toques.csv desta partida.");
            return sb.ToString();
        }

        static void AppendHandBreakdown(StringBuilder sb, SessionResults r)
        {
            foreach (var hand in new[] { InteractorHandedness.Left, InteractorHandedness.Right })
            {
                int hits = 0, misses = 0;
                double reaction = 0;
                foreach (var p in r.presses)
                {
                    if (p.hand != hand)
                        continue;
                    if (p.outcome == PressOutcome.Correct)
                    {
                        hits++;
                        reaction += p.reactionTime;
                    }
                    else
                    {
                        misses++;
                    }
                }
                if (hits + misses == 0)
                    continue;
                var avg = hits > 0 ? $", reação média {Num(reaction / hits, "0.00")} s" : "";
                sb.AppendLine($"Mão {HandName(hand),-9}    {Plural(hits, "acerto")}, {Plural(misses, "erro")}{avg}");
            }
        }

        // ------------------------------------------------------------------ formatação

        static void Row(StringBuilder sb, params string[] fields)
        {
            for (var i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                    sb.Append(k_Separator);
                sb.Append(Escape(fields[i]));
            }
            sb.Append("\r\n");
        }

        static string Escape(string value)
        {
            value ??= "";
            if (value.IndexOfAny(new[] { ';', '"', '\n', '\r' }) < 0)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        static string Num(double value, string format) => value.ToString(format, s_Culture);

        static string Plural(int count, string word) => $"{count} {word}{(count == 1 ? "" : "s")}";

        static int Count(SessionResults r, PressOutcome outcome)
        {
            var n = 0;
            foreach (var p in r.presses)
                if (p.outcome == outcome)
                    n++;
            return n;
        }

        static int CountSegments(SessionResults r)
        {
            var n = 0;
            foreach (var p in r.presses)
                if (p.segmentIndex >= 0)
                    n++;
            return n;
        }

        static string FormatDuration(double seconds)
        {
            var s = Math.Max(0, (int)Math.Round(seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        public static string HandName(InteractorHandedness hand) =>
            hand == InteractorHandedness.Left ? "esquerda" : hand == InteractorHandedness.Right ? "direita" : "-";

        public static string OutcomeName(PressOutcome outcome)
        {
            switch (outcome)
            {
                case PressOutcome.Correct: return "acerto";
                case PressOutcome.WrongKey: return "tecla errada";
                case PressOutcome.WrongHand: return "mão errada";
                case PressOutcome.TooEarly: return "adiantado";
                default: return outcome.ToString();
            }
        }

        public static string ModeName(GameMode mode) => mode == GameMode.Guided ? "guiado" : "reflexo";

        public static string SideName(BodySide side) =>
            side == BodySide.Both ? "ambos" : side == BodySide.Left ? "esquerdo" : "direito";

        public static string WaitName(WaitRule rule) => rule == WaitRule.PauseAndWait ? "pausa e espera" : "pontua o ritmo";
    }
}
