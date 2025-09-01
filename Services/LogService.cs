using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BundleTestsAutomation.Services
{
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

        // --- Combine et filtre directement un fichier de logs complet ---
        public static string ProcessLogs(string logFilePath)
        {
            var combined = CombineLogs(logFilePath);
            return FilterJsonLogs(combined);
        }
    }
}