using BundleTestsAutomation.Models;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace BundleTestsAutomation.Services
{
    public static class AppSettings
    {
        public static string? BundleDirectory { get; set; }

        // Version du PCM (par défaut 3)
        public static int PcmVersion { get; set; } = 3;

        public static string BundleManifestPath =>
            Path.Combine(BundleDirectory ?? string.Empty, "BundleManifest.xml");

        public static string DataFullCsvPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "data_full.csv");

        // Expression régulière pour extraire les infos du dossier
        private static readonly Regex BundleDirRegex =
            new(@"^(?<swId>[A-Z]+)_(?<swPartNumber>\d+)_v(?<version>\d+\.\d+\.\d+)_.+$", RegexOptions.Compiled);

        // Vérifie si le dossier a le format attendu
        public static bool IsValidBundleDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return false;

            string dirName = new DirectoryInfo(path).Name;
            return BundleDirRegex.IsMatch(dirName);
        }

        // Extrait les informations du dossier sans ajouter la version PCM
        public static BundleInfo ExtractBundleInfoFromDirectoryWithoutPcm(string? path = null)
        {
            path ??= BundleDirectory;

            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException("Aucun répertoire de bundle sélectionné.");

            string dirName = new DirectoryInfo(path).Name;
            var match = BundleDirRegex.Match(dirName);

            if (!match.Success)
                throw new FormatException("Le nom du dossier doit être au format : NOM_NUMERO_vX.X.X_... (ex: IVECOCITYBUS_5803336620_v1.17.1_PROD).");

            return new BundleInfo
            {
                SwId = match.Groups["swId"].Value,
                SwPartNumber = match.Groups["swPartNumber"].Value,
                Version = match.Groups["version"].Value
            };
        }

        // Extrait les informations du dossier et ajoute la version PCM
        public static BundleInfo ExtractBundleInfoFromDirectory()
        {
            var info = ExtractBundleInfoFromDirectoryWithoutPcm();
            info.SwId = $"{info.SwId}{PcmVersion}";
            return info;
        }
    }
}