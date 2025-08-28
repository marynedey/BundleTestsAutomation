using BundleTestsAutomation.Models;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BundleTestsAutomation.Services
{
    public static class AppSettings
    {
        public static string? BundleDirectory { get; set; }

        // Version du PCM (1 à 10, par défaut 3)
        public static int PcmVersion { get; set; } = 3;

        public static string BundleManifestPath =>
            Path.Combine(BundleDirectory ?? string.Empty, "BundleManifest.xml");

        // Chemin du fichier data_full.csv (relatif à l'exécutable)
        public static string DataFullCsvPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "data_full.csv");

        // Extraire les infos du bundle
        public static BundleInfo ExtractBundleInfoFromDirectoryWithoutPcm()
        {
            if (string.IsNullOrEmpty(BundleDirectory))
                throw new InvalidOperationException("Aucun répertoire de bundle sélectionné.");

            string dirName = new DirectoryInfo(BundleDirectory).Name;
            var match = Regex.Match(dirName, @"^(?<swId>[A-Z]+)_(?<swPartNumber>\d+)_v(?<version>\d+\.\d+\.\d+)_.+$");

            if (!match.Success)
                throw new FormatException("Le nom du dossier doit être au format : NOM_NUMERO_vX.X.X_... (ex: IVECOCITYBUS_5803336620_v1.17.1_PROD).");

            return new BundleInfo
            {
                SwId = match.Groups["swId"].Value,
                SwPartNumber = match.Groups["swPartNumber"].Value,
                Version = match.Groups["version"].Value
            };
        }

        public static BundleInfo ExtractBundleInfoFromDirectory()
        {
            var info = ExtractBundleInfoFromDirectoryWithoutPcm();
            info.SwId = $"{info.SwId}{PcmVersion}";
            return info;
        }
    }
}