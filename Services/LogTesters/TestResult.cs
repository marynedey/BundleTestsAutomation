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
        public List<string> Errors { get; set; } = new List<string>();
        public bool HasErrors => Errors.Count > 0;
        public override string ToString()
        {
            if (!HasErrors) return $"{TestName}: OK";
            return $"{TestName}: KO\r\n- {string.Join("\r\n- ", Errors)}";
        }
    }
}
