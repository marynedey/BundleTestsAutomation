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

        List<string> gpsErrors = TestGpsCoordinates(filteredLogs);
        results.Add(new TestResult
        {
            TestName = "Vérification des coordonnées GPS",
            Errors = gpsErrors
        });

        List<string> intMaxErrors = TestEvtTrkFields(filteredLogs);
        results.Add(new TestResult
        {
            TestName = "Vérification du nombre d'integer max",
            Errors = intMaxErrors
        });

        List<string> breakErrors = TestBrakeStatus(filteredLogs);
        results.Add(new TestResult
        {
            TestName = "Vérification du freinage",
            Errors = breakErrors
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
        int index = 0;

        foreach (var json in ExtractJsonBlocks(filteredLogs))
        {
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                int vhm = evtTrk.GetProperty("VHM").GetInt32();
                if (vhm == 2 && evtTrk.TryGetProperty("SOC", out JsonElement socElem))
                {
                    socList.Add(socElem.GetInt32());
                }
                index++;
            }
        }

        for (int i = 1; i < socList.Count; i++)
        {
            if (socList[i] > socList[i - 1])
            {
                issues.Add($"Erreur de SOC détectée : {socList[i - 1]}% -> {socList[i]}% à l'index {index}");
            }
        }

        return issues;
    }

    private List<string> TestMotorSpeed(string filteredLogs)
    {
        var issues = new List<string>();
        int index = 0;
        int nbErrors = 0;
        int errorRate = 0;

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

        if (index > 0)
        {
            errorRate = nbErrors * 100 / index;
            if (errorRate > 0)
            {
                issues.Add($"{errorRate}% d'erreurs"); // afin d'estimer si les integer max sont réellement à prendre en compte
            }
        }

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

    private List<string> TestGpsCoordinates(string filteredLogs)
    {
        var issues = new List<string>();
        int index = 0;
        int nbErrors = 0;
        int errorRate = 0;

        foreach (var json in ExtractJsonBlocks(filteredLogs))
        {
            if (json.TryGetProperty("GPSInfo", out JsonElement gpsInfo))
            {
                if (gpsInfo.TryGetProperty("LAT", out JsonElement latElem) &&
                    gpsInfo.TryGetProperty("LON", out JsonElement lonElem))
                {
                    try
                    {
                        int rawLat = latElem.GetInt32();
                        int rawLon = lonElem.GetInt32();

                        // Vérifie si les valeurs sont invalides
                        if (rawLat == int.MaxValue || rawLon == int.MaxValue)
                        {
                            issues.Add($"Coordonnées invalides à l'index {index} : lat={rawLat}, lon={rawLon}");
                            nbErrors++;
                        }
                        else
                        {
                            // Conversion en degrés
                            double lat = rawLat / 60000.0;
                            double lon = rawLon / 60000.0;

                            // Vérification des bornes valides
                            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                            {
                                issues.Add($"Coordonnées hors limites à l'index {index} : lat={lat}, lon={lon}");
                                nbErrors++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        issues.Add($"Erreur extraction coordonnées à l'index {index} : {ex.Message}");
                        nbErrors++;
                    }
                }
                else
                {
                    issues.Add($"gpsinfo présent mais lat/lon manquants à l'index {index}");
                    nbErrors++;
                }
            }

            index++;
        }

        if (index > 0)
        {
            errorRate = nbErrors * 100 / index;
            if (errorRate > 0)
            {
                issues.Add($"{errorRate}% d'erreurs"); // afin d'estimer si les integer max sont réellement à prendre en compte
            }
        }

        return issues;
    }

    private List<string> TestEvtTrkFields(string filteredLogs)
    {
        var issues = new List<string>();
        int index = 0;
        int nbErrors = 0;
        int totalValues = 0;

        foreach (var json in ExtractJsonBlocks(filteredLogs))
        {
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                foreach (var property in evtTrk.EnumerateObject())
                {
                    totalValues++;
                    if (property.Value.ValueKind == JsonValueKind.Number &&
                        property.Value.TryGetInt32(out int val) &&
                        val == int.MaxValue)
                    {
                        //issues.Add($"Valeur Max détectée dans EvtTRK: {property.Name} = {val} à l'index {index}");
                        nbErrors++;
                    }
                }
            }
            index++;
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
        int index = 0;
        int nbErrors = 0;

        if (currentType != VehicleType.EV)
        {
            issues.Add("Test BRKSTS ignoré (applicable uniquement aux véhicules EV)");
            return issues;
        }

        foreach (var json in ExtractJsonBlocks(filteredLogs))
        {
            if (json.TryGetProperty("EvtTRK", out JsonElement evtTrk))
            {
                if (evtTrk.TryGetProperty("BRKSTS", out JsonElement brkstsElem) &&
                    brkstsElem.ValueKind == JsonValueKind.Number)
                {
                    int value = brkstsElem.GetInt32();
                    if (value != 0 && value != 1)
                    {
                        issues.Add($"Erreur BRKSTS : valeur invalide {value} à l'index {index} (attendu 0 ou 1).");
                        nbErrors++;
                    }
                }
                else
                {
                    issues.Add($"Erreur BRKSTS : champ manquant ou invalide à l'index {index}.");
                    nbErrors++;
                }
            }
            index++;
        }

        if (index > 0)
        {
            double errorRate = (double)nbErrors * 100 / index;
            issues.Add($"{errorRate:F2}% d'erreurs BRKSTS détectées.");
        }

        return issues;
    }
}