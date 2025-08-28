using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace BundleTestsAutomation
{
    public class MainForm : Form
    {
        private readonly Button btnLoad;
        private readonly Button btnLoadCANdata;
        private readonly Button btnCompare;
        private readonly Button btnGenerateBundleManifest;
        private readonly DataGridView gridLeft;
        private readonly DataGridView gridRight;
        private readonly OpenFileDialog ofd;

        public MainForm()
        {
            Text = "CSV Management";
            Width = 1200;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            // --- Boutons ---
            btnLoad = new Button { Text = "Charger un CSV", Dock = DockStyle.Top, Height = 42 };
            btnLoad.Click += BtnLoad_Click;

            btnLoadCANdata = new Button { Text = "Charger data CANalyzer", Dock = DockStyle.Top, Height = 42 };
            btnLoadCANdata.Click += BtnLoadCANdata_Click;

            btnCompare = new Button { Text = "Comparer 2 CSV", Dock = DockStyle.Top, Height = 42 };
            btnCompare.Click += BtnCompare_Click;

            btnGenerateBundleManifest = new Button {  Text = "Générer le Bundle Manifest", Dock = DockStyle.Top, Height=42 };
            btnGenerateBundleManifest.Click += btnGenerateBundleManifest_Click;

            // --- Split container pour afficher 2 grilles côte à côte ---
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = this.Width / 2
            };

            gridLeft = CreateGrid();
            gridRight = CreateGrid();

            split.Panel1.Controls.Add(gridLeft);
            split.Panel2.Controls.Add(gridRight);

            // --- Fenêtre d'ouverture d'un fichier CSV ---
            ofd = new OpenFileDialog
            {
                Title = "Sélectionnez un fichier CSV",
                Filter = "Fichiers CSV (*.csv)|*.csv",
                CheckFileExists = true,
                Multiselect = false
            };

            // --- Synchronisation scroll ---
            gridLeft.Scroll += grid_Scroll;
            gridRight.Scroll += grid_Scroll;

            Controls.Add(split);
            Controls.Add(btnLoad);
            Controls.Add(btnLoadCANdata);
            Controls.Add(btnCompare);
            Controls.Add(btnGenerateBundleManifest);
        }


        #region Gestion des CSV
        private DataGridView CreateGrid()
        {
            return new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AutoGenerateColumns = false
            };
        }

        // --- Charger un CSV ---
        private void BtnLoad_Click(object? sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            var rows = ReadCsv(ofd.FileName);
            DisplayCsv(rows, gridLeft);

            Text = $"CSV Management - {Path.GetFileName(ofd.FileName)}";
        }

        // --- Charger un CSV spécifique (CANalyzer) ---
        private void BtnLoadCANdata_Click(object? sender, EventArgs e)
        {
            DirectoryInfo? dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null && dir.Name != "BundleTestsAutomation")
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                MessageBox.Show(this, "Impossible de trouver le dossier BundleTestsAutomation.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string fullPath = Path.Combine(dir.FullName, "data", "data_full.csv");

            var rows = ReadCsv(fullPath)
                .Where(r => !string.Join(",", r).Contains("System", StringComparison.OrdinalIgnoreCase))
                .ToList();

            DisplayCsv(rows, gridLeft);

            Text = $"CSV Management - {Path.GetFileName(fullPath)}";
        }

        // --- Comparer 2 CSV ---
        private void BtnCompare_Click(object? sender, EventArgs e)
        {
            // Sélection du premier CSV
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            var file1 = ofd.FileName;

            // Sélection du deuxième CSV
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            var file2 = ofd.FileName;

            var rows1 = ReadCsv(file1);
            var rows2 = ReadCsv(file2);

            DisplayCsv(rows1, gridLeft);
            DisplayCsv(rows2, gridRight);

            HighlightDifferences(rows1, rows2);
        }

        // --- Affichage dans une DataGrid ---
        private void DisplayCsv(List<List<string>> rows, DataGridView targetGrid)
        {
            if (rows.Count == 0) return;

            var headers = rows[0];

            targetGrid.Columns.Clear();
            foreach (var h in headers)
            {
                targetGrid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = string.IsNullOrWhiteSpace(h) ? "" : h,
                    DataPropertyName = h,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                });
            }

            targetGrid.Rows.Clear();
            foreach (var row in rows.Skip(1))
            {
                var values = new object[targetGrid.Columns.Count];
                for (int i = 0; i < values.Length; i++)
                    values[i] = i < row.Count ? row[i] : "";

                targetGrid.Rows.Add(values);
            }
        }

        // --- Mettre en évidence les différences ---
        private void HighlightDifferences(List<List<string>> rows1, List<List<string>> rows2)
        {
            int rowCount = Math.Max(rows1.Count, rows2.Count);

            for (int i = 0; i < rowCount; i++)
            {
                var row1 = i < rows1.Count ? rows1[i] : new List<string>();
                var row2 = i < rows2.Count ? rows2[i] : new List<string>();

                int colCount = Math.Max(row1.Count, row2.Count);

                for (int j = 0; j < colCount; j++)
                {
                    string val1 = j < row1.Count ? row1[j] : "";
                    string val2 = j < row2.Count ? row2[j] : "";

                    if (val1 != val2)
                    {
                        if (i < gridLeft.Rows.Count && j < gridLeft.Columns.Count)
                            gridLeft.Rows[i - 1].Cells[j].Style.BackColor = Color.LightCoral;

                        if (i < gridRight.Rows.Count && j < gridRight.Columns.Count)
                            gridRight.Rows[i - 1].Cells[j].Style.BackColor = Color.LightCoral;
                    }
                }
            }
        }

        // --- Synchroniser les déroulements ---
        private void grid_Scroll(object? sender, ScrollEventArgs e)
        {
            if (sender is DataGridView sourceGrid)
            {
                DataGridView targetGrid = sourceGrid == gridLeft ? gridRight : gridLeft;
                targetGrid.FirstDisplayedScrollingRowIndex = sourceGrid.FirstDisplayedScrollingRowIndex;
            }
        }

        // --- Lecture CSV ---
        private List<List<string>> ReadCsv(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Le fichier {filePath} n'existe pas.");

            var lines = File.ReadAllLines(filePath).ToList();
            if (lines.Count <= 0)
                throw new Exception("Le fichier CSV ne comporte pas de données.");

            char sep = DetectSeparator(lines[0]);
            var rows = lines.Select(l => ParseCsvLine(l, sep)).ToList();

            return rows;
        }

        private static char DetectSeparator(string headerLine)
        {
            int commas = headerLine.Count(c => c == ',');
            int semis = headerLine.Count(c => c == ';');
            return semis > commas ? ';' : ',';
        }

        private static List<string> ParseCsvLine(string line, char sep)
        {
            var result = new List<string>();
            if (line == null) return result;

            bool inQuotes = false;
            var cur = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            cur.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cur.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == sep)
                    {
                        result.Add(cur.ToString());
                        cur.Clear();
                    }
                    else
                    {
                        cur.Append(c);
                    }
                }
            }

            result.Add(cur.ToString());
            return result;
        }
        #endregion


        #region Génération du Bundle Manifest
        private void btnGenerateBundleManifest_Click(object? sender, EventArgs e)
        {
            // --- Détermination du chemin ---
            string? path = BundleManifestGenerator.GetBundleManifestPath();
            if (path == null)
            {
                MessageBox.Show(this, "Impossible de trouver le dossier BundleTestsAutomation.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // --- Génération du template ---
            BundleManifestGenerator.Generate(path);
            var doc = XDocument.Load(path);

            // --- Demande des infos à l’utilisateur ---
            var infos = AskBundleInfo();

            // Mise à jour des attributs du bundle
            var bundle = doc.Root;
            if (bundle != null)
            {
                bundle.SetAttributeValue("version", infos.version);
                bundle.SetAttributeValue("sw_id", infos.swId);
                bundle.SetAttributeValue("sw_part_number", infos.swPartNumber);
            }

            // --- Mise à jour du runlevel 4 ---
            UpdateWirelessManagerArg2(doc, infos.swId);

            // --- Mise à jour de l'argument name du DSE pour les IVECOCITYBUS ---
            if (infos.swId.StartsWith("IVECOCITYBUS", StringComparison.OrdinalIgnoreCase))
            {
                UpdateDSEName(doc, infos.swId);
            }
                
            // --- Mise à jour des packages ---
            // A utiliser dans la version finie: string rootFolder = Path.Combine(dir.FullName, "data", "Bundle"); ; // le dossier racine où chercher
            string rootFolder = @"\\naslyon\PROJETS_VT\VIVERIS\2025 STAGE Automatisation Tests Bundles - Maryne DEY\Documentation IVECO\Official_bundles\IVECOCITYBUS_5803336620_v1.17.1_PROD";
            UpdatePackages(doc, rootFolder);

            doc.Save(path);

            // --- Confirmation ---
            MessageBox.Show(
                $"Le Bundle Manifest a été généré avec succès !\n\nChemin du fichier :\n{path}",
                "Fichier généré",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            // --- WARNING ---
            MessageBox.Show(
                $"Veuillez supprimer le type d'encodage en première ligne du xml.\n\nIl suffit de supprimer l'argument encoding et sa valeur associée.",
                "WARNING",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            MessageBox.Show(
                $"Veuillez entrer l'argument xmlns dans <Signature> : http://www.w3.org/2000/09/xmldsig#",
                "WARNING",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        // --- Mise à jour des packages (filename + version) ---
        private void UpdatePackages(XDocument doc, string rootFolder)
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

                // Mise à jour du filename
                string? matchedName = matchingDir != null
                    ? Path.GetFileName(matchingDir)
                    : matchingFile != null
                        ? Path.GetFileName(matchingFile)
                        : null;

                if (matchedName == null) continue;

                package.SetAttributeValue("filename", matchedName);

                // Mise à jour du numéro de version
                string version = ExtractVersion(matchedName);
                package.SetAttributeValue("version", version);

                // Mise à jour du digest
                if (matchedName != null)
                {
                    string fullPath = Path.Combine(rootFolder, matchedName);

                    package.SetAttributeValue("filename", matchedName);

                    // Calcul du SHA-256 seulement si le fichier ou le dossier existe
                    if (File.Exists(fullPath))
                    {
                        package.SetAttributeValue("digest", HashUtils.ComputeSha256HashFromFile(fullPath));
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        // Pour un dossier : concaténer le contenu des fichiers pour un hash global (optionnel)
                        var allFiles = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);
                        StringBuilder sb = new StringBuilder();
                        foreach (var file in allFiles.OrderBy(f => f))
                        {
                            sb.Append(File.ReadAllText(file));
                        }
                        package.SetAttributeValue("digest", HashUtils.ComputeSha256Hash(sb.ToString()));
                    }
                }
            }
        }

        private static string ExtractVersion(string matchedName)
        {
            // On récupère seulement le nom de fichier/dossier sans extension
            string nameWithoutExt = Path.GetFileNameWithoutExtension(matchedName);

            // Chercher la première séquence qui commence par un chiffre
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

            // Construire la version : chiffres, points, tirets autorisés
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

            // Supprimer un éventuel '.' ou '-' final parasite
            version = version.TrimEnd('.', '-');

            return version;
        }

        private (string version, string swId, string swPartNumber) AskBundleInfo()
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

            return (version, swId, swPartNumber);
        }

        private void UpdateWirelessManagerArg2(XDocument doc, string newValue)
        {
            // Trouver le package WirelessManager
            var wmPackage = doc.Root?
                .Element("packages")?
                .Elements("package")
                .FirstOrDefault(p => p.Attribute("name")?.Value == "WirelessManager");
            if (wmPackage == null) return;

            // Modifier le 2e argument par le sw_id
            var args = wmPackage.Element("execution")?.Element("args");
            var allArgs = args?.Elements("arg").ToList();
            string oldValue = allArgs?[1].Attribute("value")?.Value ?? "";
            allArgs?[1].SetAttributeValue("value", newValue);
        }

        private void UpdateDSEName(XDocument doc, string newValue)
        {
            // Trouver le package avec name="dse"
            var dsePackage = doc.Root?
                .Element("packages")?
                .Elements("package")
                .FirstOrDefault(p => p.Attribute("name")?.Value == "dse");

            if (dsePackage == null) return;

            // Changer le name du package en dsecitybus
            dsePackage.SetAttributeValue("name", "dsecitybus");
        }
        #endregion
    }
}