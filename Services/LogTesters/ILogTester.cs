using BundleTestsAutomation.Services.LogTesters;

public interface ILogTester
{
    // Retourne une liste d'erreurs détectées
    List<TestResult> TestLogs(string filePath);
}