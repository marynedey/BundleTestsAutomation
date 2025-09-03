using BundleTestsAutomation.Services;
using BundleTestsAutomation.Services.LogTesters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BundleTestsAutomation.Models;

public class TigrAgentLogTester : ILogTester
{
    public List<TestResult> TestLogs(string filePath)
    {
        List<TestResult> results = new List<TestResult>();

        // --- Préparation des données ---
        List<string> rawLogs = File.ReadAllLines(filePath).ToList();
        string filteredLogs = LogService.ProcessLogs(filePath);

        // --- Tests ---
        List<string> antennaErrors = TestAntennaState(rawLogs);
        results.Add(new TestResult
        {
            TestName = "Vérification de l'état de l'antenne",
            Errors = antennaErrors
        });

        List<string> batteryErrors = TestBatteryLvl(filteredLogs);
        results.Add(new TestResult
        {
            TestName = "Vérification du niveau de batterie",
            Errors = batteryErrors
        });

        List < string> motorSpeedErrors = TestMotorSpeed(filteredLogs);
        results.Add(new TestResult
        {
            TestName = "Vérification de la vitesse du moteur",
            Errors = motorSpeedErrors
        });

        return results;
    }

    private IEnumerable<JsonElement> ExtractJsonBlocks(string filteredLogs)
    {
        var dateRegex = new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", RegexOptions.Multiline);
        var matches = dateRegex.Matches(filteredLogs);

        for (int i = 0; i < matches.Count; i++)
        {
            int startIndex = matches[i].Index;
            int length = (i < matches.Count - 1) ? matches[i + 1].Index - startIndex : filteredLogs.Length - startIndex;
            string line = filteredLogs.Substring(startIndex, length).Trim();

            int braceIndex = line.IndexOf("{");
            if (braceIndex < 0) continue;

            string jsonPart = line.Substring(braceIndex);

            JsonElement? parsedElement = default;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonPart);
                parsedElement = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // ignore JSON invalide
            }

            if (parsedElement.HasValue)
            {
                yield return parsedElement.Value;
            }
        }
    }

    private List<string> TestAntennaState(List<string> logLines)
    {
        var issues = new List<string>();
        var lines = logLines.ToList();

        string[] expectedSequence = new[]
        {
            "navigation antenna info",
            "type of antenna: 2",
            "antenna state: 1",
            "ongpsantennastatus received: 1"
        };

        int startIndex = lines.FindIndex(l => l.IndexOf("antenna", StringComparison.OrdinalIgnoreCase) >= 0);

        if (startIndex == -1)
        {
            issues.Add("Aucune ligne contenant 'antenna' trouvée.");
            return issues;
        }

        for (int i = 0; i < expectedSequence.Length; i++)
        {
            int lineIndex = startIndex + i;
            if (lineIndex >= lines.Count)
            {
                issues.Add($"Ligne attendue '{expectedSequence[i]}' manquante à la fin du fichier.");
                break;
            }

            string line = lines[lineIndex];
            if (!line.Contains(expectedSequence[i], StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"Erreur à la ligne {lineIndex + 1} : attendu '{expectedSequence[i]}', trouvé '{line}'");
            }
        }

        return issues;
    }

    private List<string> TestBatteryLvl(string filteredLogs)
    {
        var socList = new List<int>();
        var issues = new List<string>();

        foreach (var json in ExtractJsonBlocks(filteredLogs))
        {
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                int vhm = evtTrk.GetProperty("VHM").GetInt32();
                if (vhm == 2 && evtTrk.TryGetProperty("SOC", out JsonElement socElem))
                {
                    socList.Add(socElem.GetInt32());
                }
            }
        }

        for (int i = 1; i < socList.Count; i++)
        {
            if (socList[i] > socList[i - 1])
            {
                issues.Add($"Erreur de SOC détectée : {socList[i - 1]}% -> {socList[i]}%");
            }
        }

        return issues;
    }

    private List<string> TestMotorSpeed(string filteredLogs)
    {
        var socList = new List<int>();
        var issues = new List<string>();
        VehicleType currentType = AppSettings.VehicleTypeSelected;
        int index = 0;

        foreach (var json in ExtractJsonBlocks(filteredLogs))
        {
            if (currentType == VehicleType.ICE)
            {
                if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
                {
                    int vhm = evtTrk.GetProperty("VHM").GetInt32();
                    if (vhm == 2 && evtTrk.TryGetProperty("RPM", out JsonElement rpmElem))
                    {
                        int rpm = rpmElem.GetInt32();
                        if (rpm == 0 || rpm == 2147483647)
                        {
                            issues.Add($"Erreur de vitesse détectée : {rpm} rpm alors que le véhicule roule (VHM = {vhm}) à l'index {index}");
                        }
                    }
                }
            }
            index++;
        }
        return issues;
    }
}