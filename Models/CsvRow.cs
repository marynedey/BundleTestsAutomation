using System.Collections.Generic;

namespace BundleTestsAutomation.Models
{
    public class CsvRow : List<string>
    {
        public CsvRow(IEnumerable<string> row) : base(row) { }
    }
}