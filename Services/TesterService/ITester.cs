using BundleTestsAutomation.Services.TesterService;

public interface ITester
{
    // Retourne les résultats formatés des tests passés
    List<TestResult> Test(string filePath);
}