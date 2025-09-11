using BundleTestsAutomation.Models.Tester;
using BundleTestsAutomation.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class DTCLogTester : ITester
{
    // --- Structure pour un DTC extrait ---
    public class DTCInfo
    {
        public int SPN { get; set; }
        public int FMI { get; set; }
        public int Occurrence { get; set; }
        public string RawHex { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    public class DTClogParser
    {
        // --- Extraction des lignes DM1 ---
        public static List<string> ExtractRelevantLines(string filePath)
        {
            var allLines = File.ReadAllLines(filePath);
            bool measurementStarted = false;
            var dtcLines = new List<string>();

            foreach (var line in allLines)
            {
                if (!measurementStarted && line.Contains("// Measurement", StringComparison.OrdinalIgnoreCase))
                {
                    measurementStarted = true;
                    continue;
                }

                if (measurementStarted && line.Contains("18FECA"))
                {
                    dtcLines.Add(line.Trim());
                }
            }

            return dtcLines;
        }

        // --- Conversion hex string en tableau d'octets ---
        private static byte[] ParseDataBytes(string dataString)
        {
            var parts = dataString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Where(p => Regex.IsMatch(p, @"^[0-9A-Fa-f]{2}$")).Select(p => byte.Parse(p, NumberStyles.HexNumber)).ToArray();
        }

        // --- Décodage d'une ligne DM1 en DTCs ---
        public static List<DTCInfo> DecodeDTCLine(string line)
        {
            var dtcList = new List<DTCInfo>();

            // --- Extraire timestamp + data ---
            var match = Regex.Match(line, @"^\s*(?<ts>[0-9.]+)\s+\d+\s+18FECA\w+x\s+\w+\s+d\s+\d+\s+(?<data>.+)$");
            if (!match.Success) return dtcList;

            double timestampSeconds = double.Parse(match.Groups["ts"].Value, CultureInfo.InvariantCulture);
            DateTime ts = DateTime.Today.AddSeconds(timestampSeconds);

            string dataHex = match.Groups["data"].Value;
            byte[] bytes = ParseDataBytes(dataHex);

            // --- Chaque DTC = 4 octets ---
            for (int i = 0; i + 3 < bytes.Length; i += 4)
            {
                byte b1 = bytes[i];
                byte b2 = bytes[i + 1];
                byte b3 = bytes[i + 2];
                byte b4 = bytes[i + 3];

                // --- Ignorer les DTC vides (tous les octets = 0) ---
                if (b1 == 0 && b2 == 0 && b3 == 0 && b4 == 0)
                    continue;

                // --- SPN bits 0-18 ---
                int spn = (b3 << 16) | (b2 << 8) | b1;
                spn &= 0x3FFFF; // mask 18 bits

                // --- FMI bits 0-4 du 4ème octet ---
                int fmi = b4 & 0x1F;

                // --- Occurrence bits 5-7 du 4ème octet ---
                int occurrence = (b4 >> 5) & 0x07;

                dtcList.Add(new DTCInfo
                {
                    SPN = spn,
                    FMI = fmi,
                    Occurrence = occurrence,
                    RawHex = $"{b1:X2} {b2:X2} {b3:X2} {b4:X2}",
                    Timestamp = ts
                });
            }

            return dtcList;
        }

        // --- Décodage complet d'un fichier .asc ---
        public static List<DTCInfo> ParseDTCFile(string filePath)
        {
            var relevantLines = ExtractRelevantLines(filePath);
            var allDTCs = new List<DTCInfo>();

            foreach (var line in relevantLines)
            {
                var dtcs = DecodeDTCLine(line);
                allDTCs.AddRange(dtcs);
            }

            return allDTCs;
        }
    }

    // --- Test DTC Presence ---
    private TestResult TestDTCPresence(string filePath)
    {
        string testName = "Vérification des DTC";
        var issues = new List<string>();

        // --- Extraction des DTC du fichier log ---
        var dtcs = DTClogParser.ParseDTCFile(filePath);

        // --- Charger la liste des SPN valides depuis le fichier Excel (converti en CSV) ---
        string projectRoot = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..")
        );
        string dtcRefPath = Path.Combine(projectRoot, "data", "DTC", "DTC.csv");
        var rows = CsvService.ReadCsv(dtcRefPath);

        // --- On prend la colonne 4 (index 3 car 0-based) ---
        var validSpns = rows.Skip(1) // sauter l'entête
                            .Where(r => r.Count > 3)
                            .Select(r => r[3].Trim())
                            .Where(v => !string.IsNullOrEmpty(v))
                            .ToHashSet();

        if (dtcs.Count == 0)
        {
            issues.Add("Aucun DTC trouvé dans le fichier.");
        }
        else
        {
            foreach (var dtc in dtcs)
            {
                string spnHex = $"0x{dtc.SPN:X}";
                if (validSpns.Contains(spnHex) || validSpns.Contains(dtc.SPN.ToString()))
                {
                    issues.Add($"DTC trouvé : SPN={spnHex}, FMI={dtc.FMI}, Occurrence={dtc.Occurrence}, Hex={dtc.RawHex}");
                }
            }

            if (issues.Count == 0)
                issues.Add("Aucun DTC critique (selon la référence DTC.csv) trouvé.");
        }

        return new TestResult
        {
            TestName = testName,
            Errors = issues,
            HasErrorLevel = issues.Count > 0 && dtcs.Count == 0
        };
    }

    // --- Fonction publique de test ---
    public List<TestResult> Test(string filePath)
    {
        List<TestResult> results = new List<TestResult>();

        // --- Test DTC Presence ---
        results.Add(TestDTCPresence(filePath));

        return results;
    }
}