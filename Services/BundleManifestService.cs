using BundleTestsAutomation.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace BundleTestsAutomation.Services
{
    public static class BundleManifestService
    {
        public static string? GetBundleManifestPath()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && dir.Name != "BundleTestsAutomation")
            {
                dir = dir.Parent;
            }
            if (dir == null)
            {
                throw new DirectoryNotFoundException("Impossible de trouver le dossier BundleTestsAutomation.");
            }
            return Path.Combine(dir.FullName, "data", "BundleManifest.xml");
        }

        public static void Generate(string filePath)
        {
            var bundle = new XElement("bundle",
                new XAttribute("version", ""),
                new XAttribute("sw_id", ""),
                new XAttribute("sw_part_number", ""),

                // --- Services ---
                new XElement("services",
                    new XElement("service",
                        new XAttribute("name", "mist_core"),
                        new XElement("configuration",
                            new XComment(" Receive DM1 from all ECU except for 0x1C and 0x4A, CAN0 "),
                            new XElement("param", new XAttribute("key", "dm1.ecu.id.can0"), new XAttribute("value", "0xA0,0x17,0x1C,0x1D,0x21,0x2A,0xF3,0x5A,0x10,0x0B,0x49,0x00,0x2F,0x40,0x38,0x03,0x2E,0x4A,0x71,0xEE,0x33,0xFC,0x27,0x80,0x9E,0x19,0x90,0x91,0x92,0x0F,0x9C,0xE8,0x3A, 0xF0")),
                            new XComment(" disable(in order):snapshot storage dtc,wifi antenna open circuit, wifi antenna short circuit to ground, Diseable DTC from TD message from Tacho timeout,Diseable DTC from TD message from Tacho timeout,Diseable DTC from TD message from Tacho timeout "),
                            new XElement("param", new XAttribute("key", "dtc.disabled"), new XAttribute("value", "0x0DF0E2,0x4AFAE5,0x4AFAE6,0x3AFEE9,0x84F3FF,0x47FAE2")),
                            new XComment(" force sleep "),
                            new XElement("param", new XAttribute("key", "pwm.shutdown.lowpower.mode"), new XAttribute("value", "sleep")),
                            new XComment(" Min uptime: 2 min "),
                            new XElement("param", new XAttribute("key", "pwm.min.uptime.interval"), new XAttribute("value", "120")),
                            new XComment(" Max postpone: 30 min (RAS for BCM update) "),
                            new XElement("param", new XAttribute("key", "pwm.max.postpone.interval"), new XAttribute("value", "1800")),
                            new XComment(" fwk rtc alarm: 12 hours (Mist wakeup interval) "),
                            new XElement("param", new XAttribute("key", "pwm.sleep.wakeup.max.interval"), new XAttribute("value", "43200")),
                            new XComment(" Sleep period: 23.55 hours (Mist check for battery safety) "),
                            new XElement("param", new XAttribute("key", "pwm.sleep.max.interval"), new XAttribute("value", "86100")),
                            new XComment(" Max wakeups count allowed in sleep then forced reboot (Mist check for system safety) "),
                            new XElement("param", new XAttribute("key", "pwm.sleep.consecutive.reboot.threshold"), new XAttribute("value", "30")),
                            new XComment(" Max RTC interval allowed = max low power period "),
                            new XElement("param", new XAttribute("key", "pwm.dsleep.wakeup.max.interval"), new XAttribute("value", "777600")),
                            new XComment(" Max wakeups count in key off (Mist check for battery safety) "),
                            new XElement("param", new XAttribute("key", "pwm.lowpower.consecutive.threshold"), new XAttribute("value", "40")),
                            new XComment(" Verify consistency period: 22 min (IHP use cases) "),
                            new XElement("param", new XAttribute("key", "pwm.max.wc.conflicts.interval"), new XAttribute("value", "1320")),
                            new XComment(" Max low power: 9 days (cabincontrol for scheduling) "),
                            new XElement("param", new XAttribute("key", "pwm.lowpower.max.interval"), new XAttribute("value", "777600"))
                        )
                    ),
                    new XElement("service",
                        new XAttribute("name", "mist_securelog"),
                        new XComment(" this config allows the securelogger to be activated without collecting and uploading data (including anomalies) to the backend "),
                        new XElement("configuration",
                            new XComment(" application needs to be activated due to cybersecurity constraints "),
                            new XElement("param", new XAttribute("key", "enabled"), new XAttribute("value", "true")),
                            new XElement("param", new XAttribute("key", "collect.anomalies"), new XAttribute("value", "false")),
                            new XElement("param", new XAttribute("key", "upload.enabled"), new XAttribute("value", "false"))
                        )
                    ),
                    new XElement("service",
                        new XAttribute("name", "mist_scavenger"),
                        new XElement("configuration",
                            new XComment(" deletes files with extensions from .2 to .10 in the indicated path when the occupancy of that path is above 92% "),
                            new XElement("param", new XAttribute("key", "cleanup"), new XAttribute("value", "/opt/appl/data/logs/apps/:92:2,3,4,5,6,7,8,9,10"))
                        )
                    ),
                    new XElement("service",
                        new XAttribute("name", "mist_monitor"),
                        new XElement("configuration",
                            new XElement("param", new XAttribute("key", "nmon.enabled"), new XAttribute("value", "False"))
                        )
                    ),
                    new XComment(" for a tuning on mqtt timings agreed with our iot architects to avoid or at least reduce the wrong overlapped reconnections of devices "),
                    new XElement("service",
                        new XAttribute("name", "mist_communication"),
                        new XElement("configuration",
                            new XElement("param", new XAttribute("key", "mqtt-keepalive"), new XAttribute("value", "30")),
                            new XElement("param", new XAttribute("key", "mqtt-ping-timeout"), new XAttribute("value", "10000")),
                            new XElement("param", new XAttribute("key", "mqtt-protocolop-timeout"), new XAttribute("value", "60000"))
                        )
                    )
                ),

                // --- Runlevels ---
                new XElement("runlevels",
                    new XAttribute("levels", "14"),
                    new XAttribute("target", "13"),
                    new XComment(" MIST / LEAP / LSW "),
                    new XElement("level", new XAttribute("name", "Level0"), new XAttribute("id", "0")),
                    new XComment(" RAS "),
                    new XElement("level", new XAttribute("name", "Level1"), new XAttribute("id", "1"), new XAttribute("start.delay", "5")),
                    new XComment(" VehicleApp "),
                    new XElement("level", new XAttribute("name", "Level2"), new XAttribute("id", "2"), new XAttribute("start.delay", "5")),
                    new XComment(" ISA "),
                    new XElement("level", new XAttribute("name", "Level3"), new XAttribute("id", "3"), new XAttribute("start.delay", "10")),
                    new XComment(" WIRELESS-MANAGER "),
                    new XElement("level", new XAttribute("name", "Level4"), new XAttribute("id", "4"), new XAttribute("start.delay", "10")),
                    new XComment(" CANLOGGER "),
                    new XElement("level", new XAttribute("name", "Level5"), new XAttribute("id", "5"), new XAttribute("start.delay", "15")),
                    new XComment(" DIAGNOSTIC-LOGGER "),
                    new XElement("level", new XAttribute("name", "Level6"), new XAttribute("id", "6"), new XAttribute("start.delay", "15")),
                    new XComment(" DSE "),
                    new XElement("level", new XAttribute("name", "Level7"), new XAttribute("id", "7"), new XAttribute("start.delay", "15")),
                    new XComment(" DATA-COLLECTOR "),
                    new XElement("level", new XAttribute("name", "Level8"), new XAttribute("id", "8"), new XAttribute("start.delay", "15")),
                    new XComment(" TIGRA "),
                    new XElement("level", new XAttribute("name", "Level9"), new XAttribute("id", "9"), new XAttribute("start.delay", "30")),
                    new XComment(" SGWLOGGER "),
                    new XElement("level", new XAttribute("name", "Level10"), new XAttribute("id", "10"), new XAttribute("start.delay", "30")),
                    new XComment(" CAID "),
                    new XElement("level", new XAttribute("name", "Level11"), new XAttribute("id", "11"), new XAttribute("start.delay", "30")),
                    new XComment(" RxWin "),
                    new XElement("level", new XAttribute("name", "Level12"), new XAttribute("id", "12"), new XAttribute("start.delay", "10")),
                    new XComment(" GPStoCAN "),
                    new XElement("level", new XAttribute("name", "Level13"), new XAttribute("id", "13"), new XAttribute("start.delay", "4"))
                ),

                // --- Packages ---
                new XElement("packages",
                    // Paquets de base
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "MiST"),
                        new XAttribute("type", "ASW"),
                        new XAttribute("version", "")
                    ),
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "BSW"),
                        new XAttribute("type", "BSW"),
                        new XAttribute("version", "")
                    ),
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "MSW"),
                        new XAttribute("type", "MSW"),
                        new XAttribute("version", "")
                    ),
                    new XComment(" Runlevel 1 "),
                    // Runlevel 1
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "ras"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "1"),
                        new XElement("execution",
                            new XElement("args",
                                new XComment(" STOP target runlevel "),
                                new XElement("arg", new XAttribute("value", "2")),
                                new XComment(" START target runlevel "),
                                new XElement("arg", new XAttribute("value", "12"))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 2 "),
                    // Runlevel 2
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "vehicleapp"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "2"),
                        new XElement("permissions", new XAttribute("type", "deny")),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "--type")),
                                new XElement("arg", new XAttribute("value", "S-WAY_MY24"))
                            )
                        ),
                        new XElement("sandbox", new XAttribute("image", "mmi"),
                            new XElement("networking", new XAttribute("network_type", "host"), "")
                        )
                    ),
                    new XComment(" Runlevel 3 "),
                    // Runlevel 3
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "eHorizonISA"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "3"),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 4 "),
                    // Runlevel 4
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "WirelessManager"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "4"),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "--type")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 5 "),
                    // Runlevel 5
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "can-logger"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "5"),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 6 "),
                    // Runlevel 6
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "diagnosticlogger"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "6"),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 7 "),
                    // Runlevel 7
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "dse"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "7"),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 8 "),
                    // Runlevel 8
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "data-collector"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "8"),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 9 "),
                    // Runlevel 9
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "tigragent"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "9"),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 10 "),
                    // Runlevel 10
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "sgwlogger"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "10"),
                        new XElement("sandbox", new XAttribute("image", "mmi"), ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "--conf")),
                                new XElement("arg", new XAttribute("value", "configuration.conf"))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "allow"),
                        new XElement("permission", new XAttribute("api", "/mist.core.DiagnosticsService/DiagnosticsClientTransceive")),
                        new XElement("permission", new XAttribute("api", "/mist.core.CanService/ReceiveDM1")),
                        new XElement("permission", new XAttribute("api", "/mist.core.SystemService/GetInfo")),
                        new XElement("permission", new XAttribute("api", "/mist.core.PowerService/PowerTransceive")),
                        new XElement("permission", new XAttribute("api", "/mist.core.NetworkService/ReceiveNadEvents")),
                        new XElement("permission", new XAttribute("api", "/mist.core.NetworkService/GetNadInfo")),
                        new XElement("permission", new XAttribute("api", "/mist.core.NetworkService/GetWifiApMode")),
                        new XElement("permission", new XAttribute("api", "/mist.core.NetworkService/GetWifiApConfiguration")),
                        new XElement("permission", new XAttribute("api", "/mist.communication.FileTransferring/UploadFile")),
                        new XElement("permission", new XAttribute("api", "/mist.communication.Messaging/SendMessages")),
                        new XElement("permission", new XAttribute("api", "/mist.communication.ConfigurationManagement/HandleConfiguration")))
                    ),
                    new XComment(" Runlevel 11 "),
                    // Runlevel 11
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "caid"),
                        new XAttribute("autostart", "false"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "11"),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 12 "),
                    // Runlevel 12
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "rxswin"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "12"),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 13 "),
                    // Runlevel 12
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", "gpstocan"),
                        new XAttribute("autostart", "true"),
                        new XAttribute("type", "PKG"),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", "13"),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    )
                ),

                // --- Signature ---
                new XElement("Signature",
                    new XAttribute("xmlns", ""),
                    new XElement("SignedInfo",
                        new XElement("CanonicalizationMethod", new XAttribute("Algorithm", "http://www.w3.org/TR/2001/REC-xml-c14n-20010315")),
                        new XElement("SignatureMethod", new XAttribute("Algorithm", "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256")),
                        new XElement("Reference",
                            new XAttribute("URI", ""),
                            new XElement("Transforms",
                                new XElement("Transform", new XAttribute("Algorithm", "http://www.w3.org/2000/09/xmldsig#enveloped-signature"))
                            ),
                            new XElement("DigestMethod", new XAttribute("Algorithm", "http://www.w3.org/2001/04/xmlenc#sha256")),
                            new XElement("DigestValue", "")
                        )
                    ),
                    new XElement("SignatureValue", ""),
                    new XElement("KeyInfo",
                        new XElement("X509Data",
                            new XElement("X509Certificate", "")
                        )
                    )
                )
            );
            var doc = new XDocument(new XDeclaration("1.0", null, null), bundle);
            doc.Save(filePath);
        }

        public static BundleInfo AskBundleInfo()
        {
            string version = Microsoft.VisualBasic.Interaction.InputBox(
                "Veuillez entrer la version du bundle :",
                "Saisie du Bundle Version",
                "1.0.0"
            );
            string swId = Microsoft.VisualBasic.Interaction.InputBox(
                "Veuillez entrer le sw_id :",
                "Saisie du sw_id",
                "IVECOINTERCITY3"
            );
            string swPartNumber = Microsoft.VisualBasic.Interaction.InputBox(
                "Veuillez entrer le sw_part_number :",
                "Saisie du sw_part_number",
                "0000000000"
            );
            return new BundleInfo { Version = version, SwId = swId, SwPartNumber = swPartNumber };
        }

        public static void UpdateWirelessManagerArg2(XDocument doc, string newValue)
        {
            var wmPackage = doc.Root?
                .Element("packages")?
                .Elements("package")
                .FirstOrDefault(p => p.Attribute("name")?.Value == "WirelessManager");
            if (wmPackage == null) return;
            var args = wmPackage.Element("execution")?.Element("args");
            var allArgs = args?.Elements("arg").ToList();
            allArgs?[1].SetAttributeValue("value", newValue);
        }

        public static void UpdateDSEName(XDocument doc, string newValue)
        {
            var dsePackage = doc.Root?
                .Element("packages")?
                .Elements("package")
                .FirstOrDefault(p => p.Attribute("name")?.Value == "dse");
            if (dsePackage == null) return;
            dsePackage.SetAttributeValue("name", "dsecitybus");
        }

        public static void UpdatePackages(XDocument doc, string rootFolder)
        {
            var specialNames = new Dictionary<string, string>
            {
                {"eHorizonISA", "ehoisa"},
                {"BSW", "leap"}
            };
            foreach (var package in doc.Root?.Element("packages")?.Elements("package") ?? Enumerable.Empty<XElement>())
            {
                string packageName = package.Attribute("name")?.Value ?? "";
                if (string.IsNullOrWhiteSpace(packageName)) continue;
                string searchName = specialNames.ContainsKey(packageName) ? specialNames[packageName] : packageName;
                var matchingDir = Directory.GetDirectories(rootFolder)
                    .FirstOrDefault(d => Path.GetFileName(d).IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0);
                var matchingFile = matchingDir == null
                    ? Directory.GetFiles(rootFolder)
                        .FirstOrDefault(f => Path.GetFileName(f).IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0)
                    : null;
                string? matchedName = matchingDir != null
                    ? Path.GetFileName(matchingDir)
                    : matchingFile != null
                        ? Path.GetFileName(matchingFile)
                        : null;
                if (matchedName == null) continue;
                package.SetAttributeValue("filename", matchedName);
                string version = ExtractVersion(matchedName);
                package.SetAttributeValue("version", version);
                string fullPath = Path.Combine(rootFolder, matchedName);
                if (File.Exists(fullPath))
                {
                    package.SetAttributeValue("digest", HashService.ComputeSha256HashFromFile(fullPath));
                }
                else if (Directory.Exists(fullPath))
                {
                    var allFiles = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);
                    StringBuilder sb = new StringBuilder();
                    foreach (var file in allFiles.OrderBy(f => f))
                    {
                        sb.Append(File.ReadAllText(file));
                    }
                    package.SetAttributeValue("digest", HashService.ComputeSha256Hash(sb.ToString()));
                }
            }
        }

        private static string ExtractVersion(string matchedName)
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(matchedName);
            int startIndex = -1;
            for (int i = 0; i < nameWithoutExt.Length; i++)
            {
                if (char.IsDigit(nameWithoutExt[i]))
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex == -1) return "";
            string version = "";
            for (int i = startIndex; i < nameWithoutExt.Length; i++)
            {
                char c = nameWithoutExt[i];
                if (char.IsDigit(c) || c == '.' || c == '-')
                {
                    version += c;
                }
                else
                {
                    break;
                }
            }
            return version.TrimEnd('.', '-');
        }
    }
}