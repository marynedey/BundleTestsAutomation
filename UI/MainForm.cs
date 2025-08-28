using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using BundleTestsAutomation.Models;
using BundleTestsAutomation.Services;

namespace BundleTestsAutomation.UI
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

            btnLoad = new Button { Text = "Charger un CSV", Dock = DockStyle.Top, Height = 42 };
            btnLoad.Click += BtnLoad_Click;

            btnLoadCANdata = new Button { Text = "Charger data CANalyzer", Dock = DockStyle.Top, Height = 42 };
            btnLoadCANdata.Click += BtnLoadCANdata_Click;

            btnCompare = new Button { Text = "Comparer 2 CSV", Dock = DockStyle.Top, Height = 42 };
            btnCompare.Click += BtnCompare_Click;

            btnGenerateBundleManifest = new Button { Text = "Générer le Bundle Manifest", Dock = DockStyle.Top, Height = 42 };
            btnGenerateBundleManifest.Click += BtnGenerateBundleManifest_Click;

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

            ofd = new OpenFileDialog
            {
                Title = "Sélectionnez un fichier CSV",
                Filter = "Fichiers CSV (*.csv)|*.csv",
                CheckFileExists = true,
                Multiselect = false
            };

            gridLeft.Scroll += Grid_Scroll;
            gridRight.Scroll += Grid_Scroll;

            Controls.Add(split);
            Controls.Add(btnLoad);
            Controls.Add(btnLoadCANdata);
            Controls.Add(btnCompare);
            Controls.Add(btnGenerateBundleManifest);
        }

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

        private void BtnLoad_Click(object? sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            var rows = CsvService.ReadCsv(ofd.FileName);
            CsvService.DisplayCsv(rows, gridLeft);
            Text = $"CSV Management - {Path.GetFileName(ofd.FileName)}";
        }

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
            var rows = CsvService.ReadCsv(fullPath)
                .Where(r => !string.Join(",", r).Contains("System", StringComparison.OrdinalIgnoreCase))
                .ToList();
            CsvService.DisplayCsv(rows, gridLeft);
            Text = $"CSV Management - {Path.GetFileName(fullPath)}";
        }

        private void BtnCompare_Click(object? sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            var file1 = ofd.FileName;
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            var file2 = ofd.FileName;
            var rows1 = CsvService.ReadCsv(file1);
            var rows2 = CsvService.ReadCsv(file2);
            CsvService.DisplayCsv(rows1, gridLeft);
            CsvService.DisplayCsv(rows2, gridRight);
            CsvService.HighlightDifferences(rows1, rows2, gridLeft, gridRight);
        }

        private void Grid_Scroll(object? sender, ScrollEventArgs e)
        {
            if (sender is DataGridView sourceGrid)
            {
                DataGridView targetGrid = sourceGrid == gridLeft ? gridRight : gridLeft;
                CsvService.SyncGridScroll(sourceGrid, targetGrid);
            }
        }

        private void BtnGenerateBundleManifest_Click(object? sender, EventArgs e)
        {
            string? path = BundleManifestService.GetBundleManifestPath();
            if (path == null)
            {
                MessageBox.Show(this, "Impossible de trouver le dossier BundleTestsAutomation.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            BundleManifestService.Generate(path);
            var doc = XDocument.Load(path);
            var infos = BundleManifestService.AskBundleInfo();
            var bundle = doc.Root;
            if (bundle != null)
            {
                bundle.SetAttributeValue("version", infos.Version);
                bundle.SetAttributeValue("sw_id", infos.SwId);
                bundle.SetAttributeValue("sw_part_number", infos.SwPartNumber);
            }
            BundleManifestService.UpdateWirelessManagerArg2(doc, infos.SwId);
            if (infos.SwId.StartsWith("IVECOCITYBUS", StringComparison.OrdinalIgnoreCase))
            {
                BundleManifestService.UpdateDSEName(doc, infos.SwId);
            }
            string rootFolder = @"\\naslyon\PROJETS_VT\VIVERIS\2025 STAGE Automatisation Tests Bundles - Maryne DEY\Documentation IVECO\Official_bundles\IVECOCITYBUS_5803336620_v1.17.1_PROD";
            BundleManifestService.UpdatePackages(doc, rootFolder);
            doc.Save(path);
            MessageBox.Show(
                $"Le Bundle Manifest a été généré avec succès !\n\nChemin du fichier :\n{path}",
                "Fichier généré",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
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
    }
}