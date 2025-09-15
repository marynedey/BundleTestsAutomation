using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BundleTestsAutomation.Services
{
    public enum LogLevel
    {
        Unknown,
        Debug,
        Info,
        Error
    }

    public class LogMessage
    {
        public DateTime? Timestamp { get; set; }
        public LogLevel Level { get; set; } = LogLevel.Unknown;
        public string Message { get; set; } = "";

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level}: {Message}";
        }
    }

    public static class LogService
    {
        // Regex pour détecter une ligne contenant un JSON avec "Header"
        private static readonly Regex JsonLineRegex = new(@"\{""Header""\s*:\s*\{", RegexOptions.Compiled);

        // --- Combine tous les fichiers de logs (tigr.log, tigr.log.1, tigr.log.2, etc.) n respectant l’ordre chronologique (le .N est plus ancien que le .N-1) ---
        public static string CombineLogs(string logFilePath)
        {
            if (!File.Exists(logFilePath))
                throw new FileNotFoundException("Fichier log introuvable", logFilePath);

            string baseName = Path.GetFileNameWithoutExtension(logFilePath);
            string dir = Path.GetDirectoryName(logFilePath) ?? "";
            string extension = Path.GetExtension(logFilePath);

            // Récupérer tous les fichiers correspondants (ex: tigr.log, tigr.log.1, tigr.log.2, …)
            var allLogs = Directory.GetFiles(dir, $"{baseName}*{extension}")
                                   .OrderByDescending(f =>
                                   {
                                       string suffix = f.Replace($"{dir}{Path.DirectorySeparatorChar}{baseName}", "")
                                                        .Replace(extension, "");
                                       return string.IsNullOrEmpty(suffix) ? int.MaxValue : int.Parse(suffix.Trim('.'));
                                   })
                                   .ToList();

            // On lit tous les fichiers dans l’ordre du plus ancien au plus récent
            var builder = new StringBuilder();
            foreach (var file in allLogs.AsEnumerable().Reverse())
            {
                builder.AppendLine(File.ReadAllText(file));
            }

            return builder.ToString();
        }

        // --- Nettoie un contenu de logs en ne gardant que les lignes avec JSON + "Header" ---
        public static string FilterJsonLogs(string rawLogs)
        {
            if (string.IsNullOrWhiteSpace(rawLogs))
                return string.Empty;

            var lines = rawLogs.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var filtered = lines.Where(line => JsonLineRegex.IsMatch(line));
            return string.Join(Environment.NewLine, filtered);
        }

        private static readonly Regex LogLineRegex = new(
    @"^(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d+)\s+(?<level>[A-Z]+)\s+(?<msg>.*)$",
    RegexOptions.Compiled);

        private static LogLevel MapLogLevel(string level)
        {
            return level.ToUpperInvariant() switch
            {
                "DEBUG" => LogLevel.Debug,
                "INFO" => LogLevel.Info,
                "ERROR" => LogLevel.Error,
                _ => LogLevel.Unknown
            };
        }

        public static LogMessage ParseLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return new LogMessage { Level = LogLevel.Unknown, Message = "" };

            var match = LogLineRegex.Match(line);
            if (!match.Success)
                return new LogMessage { Level = LogLevel.Unknown, Message = line };

            DateTime? ts = DateTime.TryParse(match.Groups["ts"].Value, out var parsedTs) ? parsedTs : null;
            string levelStr = match.Groups["level"].Value;
            string msg = match.Groups["msg"].Value.Trim();

            return new LogMessage
            {
                Timestamp = ts,
                Level = MapLogLevel(levelStr),
                Message = msg
            };
        }

        public static List<string> GetErrorLines(string logFilePath)
        {
            if (!File.Exists(logFilePath))
                throw new FileNotFoundException("Fichier log introuvable", logFilePath);

            var allLogs = File.ReadAllText(logFilePath);
            return allLogs
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(ParseLogLine) // Parse chaque ligne en LogMessage
                .Where(l => l.Level == LogLevel.Error) // On filtre les ERROR
                .Select(l => $"[{l.Timestamp:yyyy-MM-dd HH:mm:ss}] {l.Message}") // Format lisible
                .ToList();
        }
    }
}