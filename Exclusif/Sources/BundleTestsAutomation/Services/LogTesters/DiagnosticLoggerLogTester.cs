using BundleTestsAutomation.Services;
using System.Text.Json;
using System.Text.RegularExpressions;
using BundleTestsAutomation.Models.Bundle;
using BundleTestsAutomation.Models.Tester;

public class DiagnosticLoggerLogTester : ITester
{
    public VehicleType currentType = AppSettings.VehicleTypeSelected;

    // Structure pour stocker timestamp + JSON
    private class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public JsonElement Json { get; set; }
    }

    public List<TestResult> Test(string filePath)
    {
        var results = new List<TestResult>();

        string rawLogs = File.ReadAllText(filePath);
        var logEntries = ExtractJsonBlocks(rawLogs).ToList();

        // --- Tests ---
        results.Add(new TestResult
        {
            TestName = "Accumulation des défauts ECUHEX pendant 15 min",
            Errors = TestEcuDefauts(logEntries)
        });

        results.Add(new TestResult
        {
            TestName = "Paramètres d'environnement pendant défauts",
            Errors = TestEnvironmentParameters(logEntries)
        });

        results.Add(new TestResult
        {
            TestName = "Événements critiques TTS détectés",
            Errors = TestCriticalTTS(logEntries)
        });

        return results;
    }

    // --- Extraction JSON + timestamp depuis logs ---
    private IEnumerable<LogEntry> ExtractJsonBlocks(string rawLogs)
    {
        var dateRegex = new Regex(@"^(?<ts>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})", RegexOptions.Multiline);
        var matches = dateRegex.Matches(rawLogs);

        for (int i = 0; i < matches.Count; i++)
        {
            int startIndex = matches[i].Index;
            int length = (i < matches.Count - 1) ? matches[i + 1].Index - startIndex : rawLogs.Length - startIndex;
            string block = rawLogs.Substring(startIndex, length).Trim();
            int braceIndex = block.IndexOf("{");
            if (braceIndex < 0) continue;

            string jsonPart = block.Substring(braceIndex);
            DateTime ts = DateTime.MinValue;
            DateTime.TryParse(matches[i].Groups["ts"].Value, out ts);

            JsonElement parsedJson;
            try
            {
                using var doc = JsonDocument.Parse(jsonPart);
                parsedJson = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                continue; // ignore JSON invalide
            }

            yield return new LogEntry
            {
                Timestamp = ts,
                Json = parsedJson
            };
        }
    }

    // --- Test accumulation défauts ECU ---
    private List<string> TestEcuDefauts(List<LogEntry> logEntries)
    {
        var issues = new List<string>();
        var firstTimestamp = logEntries.FirstOrDefault()?.Timestamp ?? DateTime.MinValue;

        foreach (var entry in logEntries)
        {
            // On ne garde que les défauts dans les 15 min
            if ((entry.Timestamp - firstTimestamp).TotalMinutes > 15)
                break;

            if (entry.Json.TryGetProperty("DM1Events", out var dm1Events) && dm1Events.ValueKind == JsonValueKind.Array)
            {
                foreach (var evt in dm1Events.EnumerateArray())
                {
                    if (evt.TryGetProperty("ECU", out var ecu))
                    {
                        int ecuVal = ecu.GetInt32();
                        issues.Add($"[{entry.Timestamp}] ECU {ecuVal} : défaut détecté");
                    }
                }
            }
        }

        return issues;
    }

    // --- Test paramètres d'environnement spécifiques ---
    private List<string> TestEnvironmentParameters(List<LogEntry> logEntries)
    {
        var issues = new List<string>();
        var parametersToTrack = new HashSet<string>
        {
            "DD_21_FuelLev1", "VEP1_F3_BatPotPwrIn1", "Telemat3vehmode", "ET1_EngCoolTemp", "VDHR_TotVehDistHr"
        };

        foreach (var entry in logEntries)
        {
            if (entry.Json.TryGetProperty("DM1Events", out var dm1Events) && dm1Events.ValueKind == JsonValueKind.Array)
            {
                foreach (var evt in dm1Events.EnumerateArray())
                {
                    if (evt.TryGetProperty("metadata", out var meta) &&
                        meta.TryGetProperty("environmentParameters", out var envParams) &&
                        envParams.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var param in envParams.EnumerateArray())
                        {
                            if (param.TryGetProperty("signalName", out var nameElem) &&
                                param.TryGetProperty("value", out var valueElem))
                            {
                                string name = nameElem.GetString();
                                if (parametersToTrack.Contains(name))
                                {
                                    string value = valueElem.ValueKind == JsonValueKind.Null ? "null" : valueElem.ToString();
                                    issues.Add($"[{entry.Timestamp}] {name} = {value}");
                                }
                            }
                        }
                    }
                }
            }
        }

        return issues;
    }

    // --- Test événements critiques TTS ---
    private List<string> TestCriticalTTS(List<LogEntry> logEntries)
    {
        var issues = new List<string>();

        foreach (var entry in logEntries)
        {
            // Exemple : chercher "Activate telltale TTS" avec state != 0
            if (entry.Json.TryGetProperty("DM1Events", out var dm1Events) && dm1Events.ValueKind == JsonValueKind.Array)
            {
                foreach (var evt in dm1Events.EnumerateArray())
                {
                    if (evt.TryGetProperty("SPN", out var spn))
                    {
                        // SPN peut être utilisé pour détecter TTS critique
                        issues.Add($"[{entry.Timestamp}] TTS critique SPN={spn.GetInt32()} détecté");
                    }
                }
            }
        }

        return issues;
    }
}