using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BundleTestsAutomation.Services.LogTesters
{
    public class TestResult
    {
        public string TestName { get; set; } = "";

        public List<string> Errors { get; set; } = new();

        // Retourne vrai si aucune erreur critique (ligne qui commence par "- Erreur")
        public bool IsOk => !Errors.Any(e => e.StartsWith("Erreur", StringComparison.OrdinalIgnoreCase)
                                          || e.StartsWith("- Erreur", StringComparison.OrdinalIgnoreCase));

        public string Status => IsOk ? "OK" : "KO";

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{TestName}: {Status}");
            foreach (var e in Errors)
            {
                sb.AppendLine($"\t{e}");
            }
            return sb.ToString();
        }
    }
}