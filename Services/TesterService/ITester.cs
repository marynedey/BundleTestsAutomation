using BundleTestsAutomation.Services.LogTesters;

public interface ITester
{
    // Retourne les résultats formatés des tests passés
    List<TestResult> Test(string filePath);
}