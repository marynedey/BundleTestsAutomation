using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BundleTestsAutomation.Models.Tester
{
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public List<string> Errors { get; set; } = new();

        // Indique si un log contient un ERROR
        public bool HasErrorLevel { get; set; } = false;

        // Considérer comme KO toutes les lignes contenant certains mots-clés ou un ERROR
        private static readonly string[] ErrorKeywords = { "Erreur", "KO", "critical", "invalid", "DTC trouvé" };

        public bool IsOk => !Errors.Any(e => ErrorKeywords.Any(k => e.Contains(k, StringComparison.OrdinalIgnoreCase)))
                            && !HasErrorLevel;

        public string Status => IsOk ? "OK" : "KO";

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{TestName}: {Status}");

            foreach (var e in Errors)
                sb.AppendLine($"\t{e}");

            if (HasErrorLevel)
                sb.AppendLine("\tLog(s) avec niveau ERROR détecté(s).");

            return sb.ToString();
        }
    }
}