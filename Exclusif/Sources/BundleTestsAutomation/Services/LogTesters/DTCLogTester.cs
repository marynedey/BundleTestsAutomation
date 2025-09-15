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
    public class DTCInfo
    {
        public int SPN { get; set; }
        public int FMI { get; set; }
        public int Occurrence { get; set; }
        public string RawHex { get; set; } = "";
        public TimeSpan Timestamp { get; set; }
        public int SourceAddress { get; set; } // pour savoir quel ECU a envoyé le DTC
    }

    public List<TestResult> Test(string filePath)
    {
        List<TestResult> results = new List<TestResult>();

        // --- Test DTC Presence ---
        results.Add(TestDTCPresence(filePath));

        return results;
    }

    private TestResult TestDTCPresence(string filePath)
    {
        string testName = "Vérification des DTC";
        var issues = new List<string>();

        // --- Extraction de tous les DTC du fichier log ---
        var dtcs = DTClogParser.ParseDTCFile(filePath);

        // --- Charge la liste des SPN valides depuis le fichier CSV ---
        string projectRoot = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..")
        );
        string dtcRefPath = Path.Combine(projectRoot, "data", "DTC", "DTC.csv");
        var rows = CsvService.ReadDtcCsv(dtcRefPath);

        var validSpns = rows
            .Select(r => r.SPNDLHex?.Trim())
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
                    issues.Add($"DTC trouvé : Time={dtc.Timestamp}, SPN={spnHex}, FMI={dtc.FMI}, Occurrence={dtc.Occurrence}, Hex={dtc.RawHex}, SA={dtc.SourceAddress}");
                }
            }

            if (issues.Count == 0)
                issues.Add("Aucun DTC critique trouvé (selon la référence DTC.csv)");
        }

        return new TestResult
        {
            TestName = testName,
            Errors = issues,
            HasErrorLevel = issues.Count > 0 && dtcs.Count == 0
        };
    }

    public class DTClogParser
    {
        // Extraction de toutes les lignes avec ID CAN 29 bits
        public static List<string> ExtractAllCANLines(string filePath)
        {
            return File.ReadAllLines(filePath)
                       .Where(l => Regex.IsMatch(l, @"\d+\s+[0-9A-Fa-f]{8,9}x\s+"))
                       .ToList();
        }

        private static byte[] ParseDataBytes(string dataString)
        {
            var parts = dataString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Where(p => Regex.IsMatch(p, @"^[0-9A-Fa-f]{2}$"))
                        .Select(p => byte.Parse(p, NumberStyles.HexNumber))
                        .ToArray();
        }

        // Décoder une seule ligne CAN en DTC(s)
        public static List<DTCInfo> DecodeDTCLine(string line)
        {
            var dtcList = new List<DTCInfo>();
            var match = Regex.Match(line, @"^\s*(?<ts>[0-9.]+)\s+\d+\s+(?<id>[0-9A-Fa-f]{8,9})x\s+\w+\s+d\s+\d+\s+(?<data>(?:[0-9A-Fa-f]{2}\s+)*[0-9A-Fa-f]{2})");
            if (!match.Success)
                return dtcList;

            int id = int.Parse(match.Groups["id"].Value, NumberStyles.HexNumber);

            // Extraire PGN = bits 8-25
            int sourceAddress = id & 0xFF;

            double timestampSeconds = double.Parse(match.Groups["ts"].Value, CultureInfo.InvariantCulture);
            TimeSpan ts = TimeSpan.FromSeconds(timestampSeconds);

            string dataHex = match.Groups["data"].Value;
            byte[] bytes = ParseDataBytes(dataHex);

            // --- Décoder tous les DTC dans la trame ---
            for (int i = 0; i + 3 < bytes.Length; i += 4)
            {
                byte b1 = bytes[i];
                byte b2 = bytes[i + 1];
                byte b3 = bytes[i + 2];
                byte b4 = bytes[i + 3];

                if (b1 == 0 && b2 == 0 && b3 == 0 && b4 == 0)
                    continue;

                int spn = (b3 << 16) | (b2 << 8) | b1;
                spn &= 0x7FFFF; // 19 bits SPN
                int fmi = b4 & 0x1F;
                int occurrence = (b4 >> 5) & 0x07;

                dtcList.Add(new DTCInfo
                {
                    SPN = spn,
                    FMI = fmi,
                    Occurrence = occurrence,
                    RawHex = $"{b1:X2} {b2:X2} {b3:X2} {b4:X2}",
                    Timestamp = ts,
                    SourceAddress = sourceAddress
                });
            }

            return dtcList;
        }

        // --- Parser complet du fichier log avec gestion multi-packets ---
        public static List<DTCInfo> ParseDTCFile(string filePath)
        {
            var allLines = ExtractAllCANLines(filePath);
            var allDTCs = new List<DTCInfo>();

            foreach (var line in allLines)
            {
                var dtcs = DecodeDTCLine(line);
                allDTCs.AddRange(dtcs);
            }

            return allDTCs;
        }
    }
}
