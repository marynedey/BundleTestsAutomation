using BundleTestsAutomation.Services.TesterService;

public class ISAConfigTester : ITester
{
    private Dictionary<string, string> configDict = new();

    public List<TestResult> Test(string filePath)
    {
        LoadConfig(filePath);

        var results = new List<TestResult>
        {
            TestDeadReckoningActive(),
            TestSimulationModeActive()
        };

        return results;
    }

    // --- Lit le fichier et initialise le dictionnaire clé/valeur ---
    private void LoadConfig(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Fichier de configuration introuvable", filePath);

        var lines = File.ReadAllLines(filePath)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))
                        .ToList();

        configDict = lines
            .Where(l => l.Contains("="))
            .Select(l => l.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
    }

    // --- Vérifie que DeadReckoningActive est à false ---
    private TestResult TestDeadReckoningActive()
    {
        bool ok = configDict.TryGetValue("DeadReckoningActive", out var value)
                    && value.Equals("false", StringComparison.OrdinalIgnoreCase);

        return new TestResult
        {
            TestName = "DeadReckoningActive doit être false",
            Errors = ok ? new List<string>() : new List<string> { $"DeadReckoningActive = {value ?? "non trouvé"}" },
            HasErrorLevel = !ok
        };
    }

    // --- Vérifie que SimulationMode_Active est en mode NMEA ---
    private TestResult TestSimulationModeActive()
    {
        bool ok = configDict.TryGetValue("SimulationMode_Active", out var value)
                    && value.Equals("NMEA", StringComparison.OrdinalIgnoreCase);

        return new TestResult
        {
            TestName = "SimulationMode_Active doit être NMEA",
            Errors = ok ? new List<string>() : new List<string> { $"SimulationMode_Active = {value ?? "non trouvé"}" },
            HasErrorLevel = !ok
        };
    }
}