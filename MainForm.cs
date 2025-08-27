using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
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
            // --- Génération du template du fichier ---
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

            string path = Path.Combine(dir.FullName, "data", "BundleManifest.xml");
            BundleManifestGenerator.Generate(path);

            // --- Chargement du XML et ajout des données ---
            var doc = XDocument.Load(path);
            // TODO: Ajouter les données spécifiques au Bundle Manifest ici
            doc.Save(path);

            // --- Confirmation de la génération du fichier ---
            MessageBox.Show(
                $"Le Bundle Manifest a été généré avec succès !\n\nChemin du fichier :\n{path}",
                "Fichier généré",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        #endregion
    }
}