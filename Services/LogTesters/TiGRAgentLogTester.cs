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

    // --- Structure pour conserver le JSON + timestamp associé ---
    private class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public JsonElement Json { get; set; }
    }

    public List<TestResult> TestLogs(string filePath)
    {
        List<TestResult> results = new List<TestResult>();

        // --- Préparation des données ---
        List<string> rawLogs = File.ReadAllLines(filePath).ToList();
        string allLogs = File.ReadAllText(filePath);

        // --- Vérification globale des logs ERROR ---
        var errorLines = LogService.GetErrorLines(filePath);

        results.Add(new TestResult
        {
            TestName = "Logs contenant ERROR",
            Errors = errorLines,
            HasErrorLevel = errorLines.Any()
        });

        // --- Tests ---
        results.Add(TestAntennaState(rawLogs));
        results.Add(TestBatteryLvl(allLogs));
        results.Add(TestMotorSpeed(allLogs));
        results.Add(TestDistanceTravelled(allLogs));
        results.Add(TestGpsCoordinates(allLogs));
        results.Add(TestEvtTrkFields(allLogs));
        results.Add(TestBrakeStatus(allLogs));

        return results;
    }

    // --- Extraction JSON + timestamp ---
    private IEnumerable<LogEntry> ExtractJsonBlocks(string allLogs)
    {
        var dateRegex = new Regex(@"^(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})", RegexOptions.Multiline);
        var matches = dateRegex.Matches(allLogs);

        for (int i = 0; i < matches.Count; i++)
        {
            int startIndex = matches[i].Index;
            int length = (i < matches.Count - 1)
                ? matches[i + 1].Index - startIndex
                : allLogs.Length - startIndex;

            string block = allLogs.Substring(startIndex, length).Trim();
            int braceIndex = block.IndexOf("{");
            if (braceIndex < 0) continue;

            string jsonPart = block.Substring(braceIndex);

            DateTime ts = DateTime.MinValue;
            if (DateTime.TryParse(matches[i].Groups["ts"].Value, out var parsedTs))
                ts = parsedTs;

            JsonElement? parsed = null;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonPart);
                parsed = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // JSON invalide → ignoré
            }

            if (parsed.HasValue)
            {
                yield return new LogEntry
                {
                    Timestamp = ts,
                    Json = parsed.Value
                };
            }
        }
    }

    // --- Test antenne (pas de timestamp disponible) ---
    private TestResult TestAntennaState(List<string> logLines)
    {
        string testName = "Vérification de l'état de l'antenne";
        var issues = new List<string>();
        string[] expectedSequence = new[]
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
            return new TestResult
            {
                TestName = testName,
                Errors = issues
            };
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
                issues.Add($"Erreur à la ligne {lineIndex + 1} : attendu '{expectedSequence[i]}', trouvé '{line}'");
            }
        }

        return new TestResult
        {
            TestName = testName,
            Errors = issues
        };
    }

    // --- Test niveau de batterie ---
    private TestResult TestBatteryLvl(string allLogs)
    {
        string testName = "Vérification du niveau de batterie";
        var issues = new List<string>();
        int prevSoc = -1;

        foreach (var entry in ExtractJsonBlocks(allLogs))
        {
            var json = entry.Json;

            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                int vhm = evtTrk.GetProperty("VHM").GetInt32();
                if (vhm == 2 && evtTrk.TryGetProperty("SOC", out JsonElement socElem))
                {
                    int soc = socElem.GetInt32();
                    if (prevSoc != -1 && soc > prevSoc)
                    {
                        issues.Add($"[{entry.Timestamp}] Erreur de SOC : {prevSoc}% -> {soc}%");
                    }
                    prevSoc = soc;
                }
            }
        }

        return new TestResult
        {
            TestName = testName,
            Errors = issues
        };
    }

    // --- Test vitesse moteur ---
    private TestResult TestMotorSpeed(string allLogs)
    {
        string testName = "Vérification de la vitesse du moteur";
        var issues = new List<string>();
        int total = 0;
        int nbErrors = 0;

        foreach (var entry in ExtractJsonBlocks(allLogs))
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
                        int msp = rpmevElem.GetInt32();
                        if (msp == 0 || msp == int.MaxValue)
                        {
                            issues.Add($"[{entry.Timestamp}] Erreur RPM EV : {msp} alors que le véhicule roule");
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
                issues.Add($"{errorRate}% d'erreurs de vitesse moteur");
        }

        return new TestResult
        {
            TestName = testName,
            Errors = issues
        };
    }

    // --- Test distance parcourue ---
    private TestResult TestDistanceTravelled(string allLogs)
    {
        string testName = "Vérification de la distance parcourue";
        var issues = new List<string>();
        int prevDist = -1;

        foreach (var entry in ExtractJsonBlocks(allLogs))
        {
            var json = entry.Json;

            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk) &&
                evtTrk.TryGetProperty("DST", out JsonElement distElem))
            {
                int dist = distElem.GetInt32();
                if (prevDist != -1 && dist < prevDist)
                {
                    issues.Add($"[{entry.Timestamp}] Erreur de distance : {prevDist} -> {dist}");
                }
                prevDist = dist;
            }
        }

        return new TestResult
        {
            TestName = testName,
            Errors = issues
        };
    }

    // --- Test coordonnées GPS ---
    private TestResult TestGpsCoordinates(string allLogs)
    {
        string testName = "Vérification des coordonnées GPS";
        var issues = new List<string>();
        int total = 0, nbErrors = 0;

        foreach (var entry in ExtractJsonBlocks(allLogs))
        {
            var json = entry.Json;

            if (json.TryGetProperty("GPSInfo", out JsonElement gpsInfo))
            {
                if (gpsInfo.TryGetProperty("LAT", out JsonElement latElem) &&
                    gpsInfo.TryGetProperty("LON", out JsonElement lonElem))
                {
                    try
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
                    catch (Exception ex)
                    {
                        issues.Add($"[{entry.Timestamp}] Erreur extraction coordonnées : {ex.Message}");
                        nbErrors++;
                    }
                }
                else
                {
                    issues.Add($"[{entry.Timestamp}] gpsinfo présent mais lat/lon manquants");
                    nbErrors++;
                }
            }
            total++;
        }

        if (total > 0)
        {
            int errorRate = nbErrors * 100 / total;
            if (errorRate > 0)
                issues.Add($"{errorRate}% d'erreurs GPS");
        }

        return new TestResult
        {
            TestName = testName,
            Errors = issues
        };
    }

    // --- Test champs intergers max dans EvtTRK ---
    private TestResult TestEvtTrkFields(string allLogs)
    {
        string testName = "Vérification du nombre d'integer max";
        var issues = new List<string>();
        int nbErrors = 0, totalValues = 0;

        foreach (var entry in ExtractJsonBlocks(allLogs))
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

        return new TestResult
        {
            TestName = testName,
            Errors = issues
        };
    }

    // --- Test freinage ---
    private TestResult TestBrakeStatus(string allLogs)
    {
        string testName = "Vérification du freinage";
        var issues = new List<string>();
        int total = 0, nbErrors = 0;

        if (currentType != VehicleType.EV)
        {
            issues.Add("Test BRKSTS ignoré (applicable uniquement aux véhicules EV)");
            return new TestResult
            {
                TestName = testName,
                Errors = issues
            };
        }

        foreach (var entry in ExtractJsonBlocks(allLogs))
        {
            var json = entry.Json;

            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                if (evtTrk.TryGetProperty("BRKSTS", out JsonElement brkstsElem) &&
                    brkstsElem.ValueKind == JsonValueKind.Number)
                {
                    int value = brkstsElem.GetInt32();
                    if (value != 0 && value != 1)
                    {
                        issues.Add($"[{entry.Timestamp}] Erreur BRKSTS : {value} (attendu 0 ou 1)");
                        nbErrors++;
                    }
                }
                else
                {
                    issues.Add($"[{entry.Timestamp}] Champ BRKSTS manquant ou invalide");
                    nbErrors++;
                }
                total++;
            }
        }

        if (total > 0)
        {
            double errorRate = (double)nbErrors * 100 / total;
            if (errorRate > 0)
                issues.Add($"{errorRate:F2}% d'erreurs BRKSTS détectées");
        }

        return new TestResult
        {
            TestName = testName,
            Errors = issues
        };
    }
}