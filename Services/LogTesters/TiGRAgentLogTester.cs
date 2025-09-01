using System;
using System.Collections.Generic;
using System.Linq;

public class TigrAgentLogTester : ILogTester
{
    public List<string> TestLogs(IEnumerable<string> logLines)
    {
        var issues = new List<string>();
        var lines = logLines.ToList();

        string[] expectedSequence = new[]
        {
            "navigation antenna ifo",
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
}