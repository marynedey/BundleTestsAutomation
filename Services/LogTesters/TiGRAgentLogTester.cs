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
    public VehicleType currentType = AppSettings.VehicleTypeSelected;

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

        List<string> distanceErrors = TestDistanceTravelled(filteredLogs);
        results.Add(new TestResult
        {
            TestName = "Vérification de la distance parcourue",
            Errors = distanceErrors
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
        var issues = new List<string>();
        int index = 0;
        int nbErrors = 0;

        foreach (var json in ExtractJsonBlocks(filteredLogs))
        {
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                int vhm = evtTrk.GetProperty("VHM").GetInt32();
                if (currentType == VehicleType.ICE)
                {
                    if (vhm == 2 && evtTrk.TryGetProperty("RPM", out JsonElement rpmElem))
                    {
                        int rpm = rpmElem.GetInt32();
                        if (rpm == 0 || rpm == int.MaxValue)
                        {
                            issues.Add($"Erreur de vitesse détectée : {rpm} rpm alors que le véhicule roule (VHM = {vhm}) à l'index {index}");
                            nbErrors++;
                        }
                    }
                }
                else if (currentType == VehicleType.EV)
                {
                    if (vhm == 2 && evtTrk.TryGetProperty("RPMEV", out JsonElement rpmevElem))
                    {
                        int msp = rpmevElem.GetInt32();
                        if (msp == 0 || msp == int.MaxValue)
                        {
                            issues.Add($"Erreur de vitesse détectée : {msp} rpm alors que le véhicule roule (VHM = {vhm}) à l'index {index}");
                            nbErrors++;
                        }
                    }
                }
                index++;
            }
        }
        issues.Add($"{nbErrors*100/index}% d'erreurs"); // afin d'estimer si les integer max sont réellement à prendre en compte
        return issues;
    }

    private List<string> TestDistanceTravelled(string filteredLogs)
    {
        var dstList = new List<int>();
        var issues = new List<string>();

        foreach (var json in ExtractJsonBlocks(filteredLogs))
        {
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk) && evtTrk.TryGetProperty("DST", out JsonElement distElem))
            {
                dstList.Add(distElem.GetInt32());
            }
        }

        for (int i = 1; i < dstList.Count; i++)
        {
            if (dstList[i] < dstList[i - 1])
            {
                issues.Add($"Erreur de distance parcourue détectée : {dstList[i - 1]} -> {dstList[i]}");
            }
        }

        return issues;
    }
}