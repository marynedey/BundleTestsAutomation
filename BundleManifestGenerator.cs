using System.Xml.Linq;

namespace BundleTestsAutomation
{
    public static class BundleManifestGenerator
    {
        public static void Generate(string filePath)
        {
            var bundle = new XElement("bundle",
                new XAttribute("version", "1.0"),
                new XAttribute("sw_id", "12345"),
                new XAttribute("sw_part_number", "12345"),

                // --- Services ---
                new XElement("services",
                    new XElement("service",
                        new XAttribute("name", ""),
                        new XElement("configuration",
                            new XComment(" Receive DM1 from all ECU except for 0x1C and 0x4A, CAN0 "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" disable(in order):snapshot storage dtc,wifi antenna open circuit, wifi antenna short circuit to ground, Diseable DTC from TD message from Tacho timeout,Diseable DTC from TD message from Tacho timeout,Diseable DTC from TD message from Tacho timeout "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" force sleep "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" Min uptime: 2 min "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" Max postpone: 30 min (RAS for BCM update) "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" fwk rtc alarm: 12 hours (Mist wakeup interval) "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" Sleep period: 23.55 hours (Mist check for battery safety) "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" Max wakeups count allowed in sleep then forced reboot (Mist check for system safety) "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" Max RTC interval allowed = max low power period "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" Max wakeups count in key off (Mist check for battery safety) "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" Verify consistency period: 22 min (IHP use cases) "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XComment(" Max low power: 9 days (cabincontrol for scheduling) "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", ""))
                        )
                    ),
                    new XElement("service",
                        new XAttribute("name", ""),
                        new XComment(" this config allows the securelogger to be activated without collecting and uploading data (including anomalies) to the backend "),
                        new XElement("configuration",
                            new XComment(" application needs to be activated due to cybersecurity constraints "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", ""))
                        )
                    ),
                    new XElement("service",
                        new XAttribute("name", ""),
                        new XElement("configuration",
                            new XComment(" deletes files with extensions from .2 to .10 in the indicated path when the occupancy of that path is above 92% "),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", ""))
                        )
                    ),
                    new XElement("service",
                        new XAttribute("name", ""),
                        new XElement("configuration",
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", ""))
                        )
                    ),
                    new XComment(" for a tuning on mqtt timings agreed with our iot architects to avoid or at least reduce the wrong overlapped reconnections of devices "),
                    new XElement("service",
                        new XAttribute("name", ""),
                        new XElement("configuration",
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", "")),
                            new XElement("param", new XAttribute("key", ""), new XAttribute("value", ""))
                        )
                    )
                ),

                // --- Runlevels ---
                new XElement("runlevels",
                    new XAttribute("levels", ""),
                    new XAttribute("target", ""),
                    new XComment(" MIST / LEAP / LSW "),
                    new XElement("level", new XAttribute("name", "Level0"), new XAttribute("id","0")),
                    new XComment(" RAS "),
                    new XElement("level", new XAttribute("name", "Level1"), new XAttribute("id","1"), new XAttribute("start.delay", "")),
                    new XComment(" VehicleApp "),
                    new XElement("level", new XAttribute("name", "Level2"), new XAttribute("id","2"), new XAttribute("start.delay", "")),
                    new XComment(" ISA "),
                    new XElement("level", new XAttribute("name", "Level3"), new XAttribute("id","3"), new XAttribute("start.delay", "")),
                    new XComment(" WIRELESS-MANAGER "),
                    new XElement("level", new XAttribute("name", "Level4"), new XAttribute("id","4"), new XAttribute("start.delay", "")),
                    new XComment(" CANLOGGER "),
                    new XElement("level", new XAttribute("name", "Level5"), new XAttribute("id","5"), new XAttribute("start.delay", "")),
                    new XComment(" DIAGNOSTIC-LOGGER "),
                    new XElement("level", new XAttribute("name", "Level6"), new XAttribute("id","6"), new XAttribute("start.delay", "")),
                    new XComment(" DSE "),
                    new XElement("level", new XAttribute("name", "Level7"), new XAttribute("id","7"), new XAttribute("start.delay", "")),
                    new XComment(" DATA-COLLECTOR "),
                    new XElement("level", new XAttribute("name", "Level8"), new XAttribute("id","8"), new XAttribute("start.delay", "")),
                    new XComment(" TIGRA "),
                    new XElement("level", new XAttribute("name", "Level9"), new XAttribute("id","9"), new XAttribute("start.delay", "")),
                    new XComment(" SGWLOGGER "),
                    new XElement("level", new XAttribute("name", "Level10"), new XAttribute("id", "10"), new XAttribute("start.delay", "")),
                    new XComment(" CAID "),
                    new XElement("level", new XAttribute("name", "Level11"), new XAttribute("id", "11"), new XAttribute("start.delay", "")),
                    new XComment(" RxWin "),
                    new XElement("level", new XAttribute("name", "Level12"), new XAttribute("id", "12"), new XAttribute("start.delay", ""))
                ),

                // --- Packages ---
                new XElement("packages",
                    // Paquets de base
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", "")
                    ),
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", "")
                    ),
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", "")
                    ),
                    new XComment(" Runlevel 1 "),
                    // Runlevel 1
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XComment(" STOP target runlevel "),
                                new XElement("arg", new XAttribute("value", "")),
                                new XComment(" START target runlevel "),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 2 "),
                    // Runlevel 2
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 3 "),
                    // Runlevel 3
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 4 "),
                    // Runlevel 4
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
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
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 6 "),
                    // Runlevel 6
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 7 "),
                    // Runlevel 7
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 8 "),
                    // Runlevel 8
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 9 "),
                    // Runlevel 9
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 10 "),
                    // Runlevel 10
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 11 "),
                    // Runlevel 11
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    ),
                    new XComment(" Runlevel 12 "),
                    // Runlevel 12
                    new XElement("package",
                        new XAttribute("digest", ""),
                        new XAttribute("filename", ""),
                        new XAttribute("name", ""),
                        new XAttribute("autostart", ""),
                        new XAttribute("type", ""),
                        new XAttribute("version", ""),
                        new XAttribute("runlevel", ""),
                        new XElement("execution",
                            new XElement("args",
                                new XElement("arg", new XAttribute("value", "")),
                                new XElement("arg", new XAttribute("value", ""))
                            )
                        ),
                        new XElement("permissions", new XAttribute("type", "deny"))
                    )
                ),

                // --- Signature ---
                new XElement("Signature",
                    new XAttribute("xmlns", ""),
                    new XElement("SignedInfo",
                        new XElement("CanonicalizationMethod", new XAttribute("Algorithm", "")),
                        new XElement("SignatureMethod", new XAttribute("Algorithm", "")),
                        new XElement("Reference",
                            new XAttribute("URI", ""),
                            new XElement("Transforms",
                                new XElement("Transform", new XAttribute("Algorithm", ""))
                            ),
                            new XElement("DigestMethod", new XAttribute("Algorithm", "")),
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

            // Création du document XML (avec l'encodage par défaut TODO: le supprimer)
            var doc = new XDocument(new XDeclaration("1.0", null, null), bundle);
            doc.Save(filePath);
        }
    }
}