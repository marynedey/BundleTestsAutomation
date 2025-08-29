using BundleTestsAutomation.Models;
using BundleTestsAutomation.Services;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

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
        private readonly ProgressBar progressBar;
        private readonly Label lblProgress;
        private readonly FolderBrowserDialog folderBrowserDialog;
        private readonly ComboBox cmbPcmVersion;
        private readonly Button btnValidatePcm;

        public MainForm()
        {
            Text = "Automatisation des tests de bundle";
            Width = 1400;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;

            // --- Boutons CSV ---
            btnLoad = new Button { Text = "Charger un CSV", Height = 40, Dock = DockStyle.Top };
            btnLoad.Click += BtnLoad_Click;

            btnLoadCANdata = new Button { Text = "Charger data CANalyzer", Height = 40, Dock = DockStyle.Top };
            btnLoadCANdata.Click += BtnLoadCANdata_Click;

            btnCompare = new Button { Text = "Comparer 2 CSV", Height = 40, Dock = DockStyle.Top };
            btnCompare.Click += BtnCompare_Click;

            var groupCsv = new GroupBox
            {
                Text = "Gestion CSV",
                Dock = DockStyle.Top,
                Height = 160,
                Padding = new Padding(10)
            };
            groupCsv.Controls.Add(btnCompare);
            groupCsv.Controls.Add(btnLoadCANdata);
            groupCsv.Controls.Add(btnLoad);

            // --- Boutons Manifest + PCM + Progression ---
            btnGenerateBundleManifest = new Button { Text = "Générer le Bundle Manifest", Height = 40, Dock = DockStyle.Top };
            btnGenerateBundleManifest.Click += BtnGenerateBundleManifest_Click;

            cmbPcmVersion = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = 30,
                Visible = true
            };
            for (int i = 1; i <= 10; i++)
                cmbPcmVersion.Items.Add(i);
            cmbPcmVersion.SelectedIndex = 2;

            btnValidatePcm = new Button
            {
                Text = "Valider la version du PCM",
                Dock = DockStyle.Top,
                Height = 30,
                Visible = true
            };
            btnValidatePcm.Click += BtnValidatePcm_Click;

            lblProgress = new Label
            {
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 50,
                Text = "Prêt",
                Visible = false,
                AutoSize = false
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 20,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Visible = false
            };

            var groupManifest = new GroupBox
            {
                Text = "Bundle Manifest",
                Dock = DockStyle.Top,
                Height = 220,
                Padding = new Padding(10)
            };
            groupManifest.Controls.Add(progressBar);
            groupManifest.Controls.Add(lblProgress);
            groupManifest.Controls.Add(btnValidatePcm);
            groupManifest.Controls.Add(cmbPcmVersion);
            groupManifest.Controls.Add(btnGenerateBundleManifest);

            // --- Panneau gauche ---
            var panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                Padding = new Padding(10)
            };
            panelLeft.Controls.Add(groupManifest);
            panelLeft.Controls.Add(groupCsv);

            // --- SplitContainer pour les grilles ---
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = (Width - panelLeft.Width) / 2
            };

            gridLeft = CreateGrid();
            gridRight = CreateGrid();
            split.Panel1.Controls.Add(gridLeft);
            split.Panel2.Controls.Add(gridRight);

            // --- OpenFileDialog ---
            ofd = new OpenFileDialog
            {
                Title = "Sélectionnez un fichier CSV",
                Filter = "Fichiers CSV (*.csv)|*.csv",
                CheckFileExists = true,
                Multiselect = false
            };

            // Scroll synchronisé
            gridLeft.Scroll += Grid_Scroll;
            gridRight.Scroll += Grid_Scroll;

            // Ajout des contrôles
            Controls.Add(split);
            Controls.Add(panelLeft);

            // Sélection du répertoire au lancement
            folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Sélectionnez le répertoire du bundle à traiter",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            this.Shown += MainForm_Shown;

            AppSettings.BundleDirectory = folderBrowserDialog.SelectedPath;
        }

        private void MainForm_Shown(object? sender, EventArgs e)
        {
            while (true)
            {
                if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK &&
                    !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    AppSettings.BundleDirectory = folderBrowserDialog.SelectedPath;
                    break;
                }

                var result = MessageBox.Show(
                    this,
                    "Vous devez sélectionner un répertoire pour continuer.\n\nVoulez-vous réessayer ?",
                    "Répertoire requis",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    this.Close();
                    return;
                }
            }
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
            if (!File.Exists(AppSettings.DataFullCsvPath))
            {
                MessageBox.Show(this, "Le fichier data_full.csv est introuvable.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var rows = CsvService.ReadCsv(AppSettings.DataFullCsvPath)
                .Where(r => !string.Join(",", r).Contains("System", StringComparison.OrdinalIgnoreCase))
                .ToList();
            CsvService.DisplayCsv(rows, gridLeft);
            Text = $"CSV Management - {Path.GetFileName(AppSettings.DataFullCsvPath)}";
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

        private void BtnValidatePcm_Click(object? sender, EventArgs e)
        {
            if (cmbPcmVersion.SelectedItem == null)
            {
                MessageBox.Show(this, "Veuillez sélectionner une version de PCM.", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AppSettings.PcmVersion = (int)cmbPcmVersion.SelectedItem;
            cmbPcmVersion.Visible = false;
            btnValidatePcm.Visible = false;
            lblProgress.Visible = false;

            MessageBox.Show(this,
                $"Version du PCM définie sur {AppSettings.PcmVersion}.\n" +
                $"Le sw_id sera : {AppSettings.ExtractBundleInfoFromDirectoryWithoutPcm().SwId}{AppSettings.PcmVersion}",
                "Version du PCM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private async void BtnGenerateBundleManifest_Click(object? sender, EventArgs e)
        {
            try
            {
                ShowProgressBar(100);

                if (string.IsNullOrEmpty(AppSettings.BundleDirectory))
                {
                    MessageBox.Show(this, "Aucun répertoire de bundle sélectionné.", "Erreur",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    HideProgressBar();
                    return;
                }

                string path = AppSettings.BundleManifestPath;

                // Étape 1 : Génération du template (5%)
                UpdateProgress(5, "Génération du template XML...");
                BundleManifestService.Generate(path);

                // Étape 2 : Chargement du document XML (10%)
                UpdateProgress(10, "Chargement du document XML...");
                var doc = XDocument.Load(path);

                // Étape 3 : Extraction des infos (15%)
                UpdateProgress(15, "Extraction des informations du Bundle...");
                var infos = AppSettings.ExtractBundleInfoFromDirectory();

                // Étape 4 : Mise à jour des attributs du bundle (20%)
                UpdateProgress(20, "Mise à jour des attributs du Bundle...");
                var bundle = doc.Root;
                if (bundle != null)
                {
                    bundle.SetAttributeValue("version", infos.Version);
                    bundle.SetAttributeValue("sw_id", infos.SwId);
                    bundle.SetAttributeValue("sw_part_number", infos.SwPartNumber);
                }

                // Étape 5 : Mise à jour des packages (20% à 90%)
                UpdateProgress(20, "Préparation de la mise à jour des packages...");
                string rootFolder = AppSettings.BundleDirectory ?? string.Empty;

                await Task.Run(() =>
                {
                    BundleManifestService.UpdateWirelessManagerArg2(doc, infos.SwId);
                    if (infos.SwId.StartsWith("IVECOCITYBUS", StringComparison.OrdinalIgnoreCase))
                    {
                        BundleManifestService.UpdateDSEName(doc, infos.SwId);
                    }
                    BundleManifestService.UpdatePackages(doc, rootFolder, UpdatePackageProgress);
                });

                // Étape 6 : Sauvegarde (95% à 100%)
                UpdateProgress(95, "Sauvegarde du BundleManifest...");
                doc.Save(path);

                // --- Corrections manuelles du XML (pas top mais j'ai pas trouvé mieux pour le moment) ---
                // Supprime manuellement l'attribut encoding
                string xmlContent = File.ReadAllText(path);
                xmlContent = Regex.Replace(xmlContent, @"encoding=[""'][^""']*[""']", "");

                // Supprime les attributs xmlns vides et l'ajoute correctement
                xmlContent = xmlContent.Replace(" xmlns=\"\"", "");
                int index = xmlContent.IndexOf("<Signature");
                if (index >= 0)
                {
                    int endIndex = xmlContent.IndexOf(">", index);
                    if (endIndex >= 0 && !xmlContent.Substring(index, endIndex - index).Contains("xmlns=\"http://www.w3.org/2000/09/xmldsig#\""))
                    {
                        xmlContent = xmlContent.Insert(endIndex, " xmlns=\"http://www.w3.org/2000/09/xmldsig#\"");
                    }
                }
                File.WriteAllText(path, xmlContent);

                UpdateProgress(100, "Finalisation...");

                HideProgressBar();

                // Message de confirmation
                MessageBox.Show(
                    $"Le Bundle Manifest a été généré avec succès !\n\n" +
                    $"Version: {infos.Version}\n" +
                    $"sw_id: {infos.SwId}\n" +
                    $"sw_part_number: {infos.SwPartNumber}\n\n" +
                    $"Chemin du fichier :\n{path}",
                    "Fichier généré",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                HideProgressBar();
                MessageBox.Show(this, $"Une erreur est survenue : {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowProgressBar(int maxValue)
        {
            progressBar.Value = 0;
            progressBar.Maximum = maxValue;
            progressBar.Visible = true;
            lblProgress.Visible = true;
            lblProgress.Text = "Début de la génération...";
            Application.DoEvents();
        }

        private void UpdateProgress(int value, string message)
        {
            if (value > progressBar.Maximum) value = progressBar.Maximum;
            progressBar.Value = value;
            lblProgress.Text = $"{message} ({value}%)";
            Application.DoEvents();
        }

        private void HideProgressBar()
        {
            progressBar.Value = 0;
            progressBar.Visible = false;
            lblProgress.Visible = false;
            lblProgress.Text = "Prêt";
        }

        private void UpdatePackageProgress(int current, int total, string message)
        {
            int minValue = 20;
            int maxValue = 90;
            int stepValue = minValue + (current * (maxValue - minValue) / total);
            this.Invoke((MethodInvoker)delegate
            {
                UpdateProgress(stepValue, $"{message} ({current}/{total})");
            });
        }
    }
}