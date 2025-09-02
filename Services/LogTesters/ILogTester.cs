public interface ILogTester
{
    // Retourne une liste d'erreurs détectées
    List<string> TestLogs(string filePath);
}