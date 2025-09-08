using BundleTestsAutomation.Services;
using BundleTestsAutomation.Services.LogTesters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BundleTestsAutomation.Models;

public class TigrAgentLogTester : ILogTester
{
    public VehicleType currentType = AppSettings.VehicleTypeSelected;

    // Nouvelle structure pour stocker date + JSON
    public record LogEntry(DateTime Timestamp, JsonElement Json);

    public List<TestResult> TestLogs(string filePath)
    {
        List<TestResult> results = new List<TestResult>();

        // --- Préparation des données ---
        List<string> rawLogs = File.ReadAllLines(filePath).ToList();
        string filteredLogs = LogService.ProcessLogs(filePath);

        // --- Tests ---
        results.Add(new TestResult
        {
            TestName = "Vérification de l'état de l'antenne",
            Errors = TestAntennaState(rawLogs)
        });

        results.Add(new TestResult
        {
            TestName = "Vérification du niveau de batterie",
            Errors = TestBatteryLvl(filteredLogs)
        });

        results.Add(new TestResult
        {
            TestName = "Vérification de la vitesse du moteur",
            Errors = TestMotorSpeed(filteredLogs)
        });

        results.Add(new TestResult
        {
            TestName = "Vérification de la distance parcourue",
            Errors = TestDistanceTravelled(filteredLogs)
        });

        results.Add(new TestResult
        {
            TestName = "Vérification des coordonnées GPS",
            Errors = TestGpsCoordinates(filteredLogs)
        });

        results.Add(new TestResult
        {
            TestName = "Vérification du nombre d'integer max",
            Errors = TestEvtTrkFields(filteredLogs)
        });

        results.Add(new TestResult
        {
            TestName = "Vérification du freinage",
            Errors = TestBrakeStatus(filteredLogs)
        });

        return results;
    }

    private IEnumerable<LogEntry> ExtractJsonBlocks(string filteredLogs)
    {
        var dateRegex = new Regex(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", RegexOptions.Multiline);
        var matches = dateRegex.Matches(filteredLogs);

        for (int i = 0; i < matches.Count; i++)
        {
            int startIndex = matches[i].Index;
            int length = (i < matches.Count - 1)
                ? matches[i + 1].Index - startIndex
                : filteredLogs.Length - startIndex;
            string line = filteredLogs.Substring(startIndex, length).Trim();

            if (!DateTime.TryParse(matches[i].Value, out var timestamp))
                continue;

            int braceIndex = line.IndexOf("{");
            if (braceIndex < 0) continue;

            string jsonPart = line.Substring(braceIndex);

            JsonElement? parsedJson = null;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonPart);
                parsedJson = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // JSON invalide → on ignore
            }

            if (parsedJson.HasValue)
            {
                yield return new LogEntry(timestamp, parsedJson.Value);
            }
        }
    }

    private List<string> TestAntennaState(List<string> logLines)
    {
        var issues = new List<string>();
        string[] expectedSequence =
        {
            "navigation antenna info",
            "type of antenna: 2",
            "antenna state: 1",
            "ongpsantennastatus received: 1"
        };

        int startIndex = logLines.FindIndex(l => l.IndexOf("antenna", StringComparison.OrdinalIgnoreCase) >= 0);
        if (startIndex == -1)
        {
            issues.Add("Aucune ligne contenant 'antenna' trouvée.");
            return issues;
        }

        for (int i = 0; i < expectedSequence.Length; i++)
        {
            int lineIndex = startIndex + i;
            if (lineIndex >= logLines.Count)
            {
                issues.Add($"Ligne attendue '{expectedSequence[i]}' manquante à la fin du fichier.");
                break;
            }

            string line = logLines[lineIndex];
            if (!line.Contains(expectedSequence[i], StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"Erreur : attendu '{expectedSequence[i]}', trouvé '{line}'");
            }
        }

        return issues;
    }

    private List<string> TestBatteryLvl(string filteredLogs)
    {
        var socList = new List<(DateTime Timestamp, int Value)>();
        var issues = new List<string>();

        foreach (var entry in ExtractJsonBlocks(filteredLogs))
        {
            var json = entry.Json;
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                int vhm = evtTrk.GetProperty("VHM").GetInt32();
                if (vhm == 2 && evtTrk.TryGetProperty("SOC", out JsonElement socElem))
                {
                    socList.Add((entry.Timestamp, socElem.GetInt32()));
                }
            }
        }

        for (int i = 1; i < socList.Count; i++)
        {
            if (socList[i].Value > socList[i - 1].Value)
            {
                issues.Add($"[{socList[i].Timestamp}] Erreur de SOC : {socList[i - 1].Value}% -> {socList[i].Value}%");
            }
        }

        return issues;
    }

    private List<string> TestMotorSpeed(string filteredLogs)
    {
        var issues = new List<string>();
        int nbErrors = 0;
        int total = 0;

        foreach (var entry in ExtractJsonBlocks(filteredLogs))
        {
            var json = entry.Json;
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
                            issues.Add($"[{entry.Timestamp}] Erreur RPM ICE : {rpm} alors que le véhicule roule");
                            nbErrors++;
                        }
                    }
                }
                else if (currentType == VehicleType.EV)
                {
                    if (vhm == 2 && evtTrk.TryGetProperty("RPMEV", out JsonElement rpmevElem))
                    {
                        int rpmEv = rpmevElem.GetInt32();
                        if (rpmEv == 0 || rpmEv == int.MaxValue)
                        {
                            issues.Add($"[{entry.Timestamp}] Erreur RPM EV : {rpmEv} alors que le véhicule roule");
                            nbErrors++;
                        }
                    }
                }

                total++;
            }
        }

        if (total > 0)
        {
            int errorRate = nbErrors * 100 / total;
            if (errorRate > 0)
            {
                issues.Add($"{errorRate}% d'erreurs de vitesse moteur");
            }
        }

        return issues;
    }

    private List<string> TestDistanceTravelled(string filteredLogs)
    {
        var dstList = new List<(DateTime Timestamp, int Value)>();
        var issues = new List<string>();

        foreach (var entry in ExtractJsonBlocks(filteredLogs))
        {
            var json = entry.Json;
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk) &&
                evtTrk.TryGetProperty("DST", out JsonElement distElem))
            {
                dstList.Add((entry.Timestamp, distElem.GetInt32()));
            }
        }

        for (int i = 1; i < dstList.Count; i++)
        {
            if (dstList[i].Value < dstList[i - 1].Value)
            {
                issues.Add($"[{dstList[i].Timestamp}] Erreur distance : {dstList[i - 1].Value} -> {dstList[i].Value}");
            }
        }

        return issues;
    }

    private List<string> TestGpsCoordinates(string filteredLogs)
    {
        var issues = new List<string>();
        int nbErrors = 0;
        int total = 0;

        foreach (var entry in ExtractJsonBlocks(filteredLogs))
        {
            var json = entry.Json;
            if (json.TryGetProperty("GPSInfo", out JsonElement gpsInfo))
            {
                total++;
                if (gpsInfo.TryGetProperty("LAT", out JsonElement latElem) &&
                    gpsInfo.TryGetProperty("LON", out JsonElement lonElem))
                {
                    int rawLat = latElem.GetInt32();
                    int rawLon = lonElem.GetInt32();

                    if (rawLat == int.MaxValue || rawLon == int.MaxValue)
                    {
                        issues.Add($"[{entry.Timestamp}] Coordonnées invalides : lat={rawLat}, lon={rawLon}");
                        nbErrors++;
                    }
                    else
                    {
                        double lat = rawLat / 60000.0;
                        double lon = rawLon / 60000.0;

                        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                        {
                            issues.Add($"[{entry.Timestamp}] Coordonnées hors limites : lat={lat}, lon={lon}");
                            nbErrors++;
                        }
                    }
                }
                else
                {
                    issues.Add($"[{entry.Timestamp}] gpsinfo présent mais lat/lon manquants");
                    nbErrors++;
                }
            }
        }

        if (total > 0)
        {
            int errorRate = nbErrors * 100 / total;
            if (errorRate > 0)
            {
                issues.Add($"{errorRate}% d'erreurs GPS");
            }
        }

        return issues;
    }

    private List<string> TestEvtTrkFields(string filteredLogs)
    {
        var issues = new List<string>();
        int nbErrors = 0;
        int totalValues = 0;

        foreach (var entry in ExtractJsonBlocks(filteredLogs))
        {
            var json = entry.Json;
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                foreach (var property in evtTrk.EnumerateObject())
                {
                    totalValues++;
                    if (property.Value.ValueKind == JsonValueKind.Number &&
                        property.Value.TryGetInt32(out int val) &&
                        val == int.MaxValue)
                    {
                        nbErrors++;
                    }
                }
            }
        }

        if (totalValues > 0)
        {
            double errorRate = (double)nbErrors * 100 / totalValues;
            issues.Add($"{errorRate:F2}% des valeurs de EvtTRK sont à int.MaxValue");
        }
        else
        {
            issues.Add("Aucune valeur trouvée dans EvtTRK.");
        }

        return issues;
    }

    private List<string> TestBrakeStatus(string filteredLogs)
    {
        var issues = new List<string>();
        int nbErrors = 0;
        int total = 0;

        if (currentType != VehicleType.EV)
        {
            issues.Add("Test BRKSTS ignoré (uniquement véhicules EV)");
            return issues;
        }

        foreach (var entry in ExtractJsonBlocks(filteredLogs))
        {
            var json = entry.Json;
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                total++;
                if (evtTrk.TryGetProperty("BRKSTS", out JsonElement brkstsElem) &&
                    brkstsElem.ValueKind == JsonValueKind.Number)
                {
                    int value = brkstsElem.GetInt32();
                    if (value != 0 && value != 1)
                    {
                        issues.Add($"[{entry.Timestamp}] Erreur BRKSTS : valeur {value} (attendu 0 ou 1)");
                        nbErrors++;
                    }
                }
                else
                {
                    issues.Add($"[{entry.Timestamp}] Erreur BRKSTS : champ manquant ou invalide");
                    nbErrors++;
                }
            }
        }

        if (total > 0)
        {
            double errorRate = (double)nbErrors * 100 / total;
            issues.Add($"{errorRate:F2}% d'erreurs BRKSTS");
        }

        return issues;
    }
}