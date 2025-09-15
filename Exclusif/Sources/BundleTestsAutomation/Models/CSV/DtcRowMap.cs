using CsvHelper.Configuration;

namespace BundleTestsAutomation.Models.CSV
{
    public sealed class DtcRowMap : ClassMap<DtcRow>
    {
        public DtcRowMap()
        {
            Map(m => m.DTCName).Name("DTC name");
            Map(m => m.DTCExtract).Name("DTC Extract");
            Map(m => m.DTCCodeHex).Name("DTC code [hex]");
            Map(m => m.SPNDLHex).Name("SPN DL [hex]");
            Map(m => m.ToBeIgnored).Name("To be ignored");
            Map(m => m.Justification).Name("Justification");
        }
    }
}