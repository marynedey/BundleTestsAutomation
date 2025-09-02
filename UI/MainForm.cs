using BundleTestsAutomation.Models;
using BundleTestsAutomation.Services;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace BundleTestsAutomation.UI
{
    public class MainForm : Form
    {
        // --- Panels dynamiques ---
        private Panel panelMenu;
        private Panel panelContent;

        // --- Menu buttons ---
        private Button btnMenuCsv;
        private Button btnMenuBundleManifest;
        private Button btnMenuLogs;

        // --- CSV controls ---
        private Button btnLoad;
        private Button btnLoadCANdata;
        private Button btnCompare;
        private DataGridView gridLeft;
        private DataGridView gridRight;
        private DataGridView gridDifferences;
        private Label lblCsvDiffCount;
        private Label lblCsvCompareTitle;

        // --- Bundle Manifest controls ---
        private Button btnGenerateBundleManifest;
        private ComboBox cmbPcmVersion;
        private Button btnValidatePcm;
        private ProgressBar progressBar;
        private Label lblProgress;
        private Button btnCancelGenerate;

        // --- Logs controls ---
        private TextBox txtLogDisplay;
        private Button btnRefreshLogs;
        private TextBox txtLogResults;

        // --- Dialogs ---
        private OpenFileDialog ofd;
        private FolderBrowserDialog folderBrowserDialog;

        // --- Cancellation ---
        private CancellationTokenSource? cancellationTokenSource;

        public MainForm()
        {
            Text = "Automatisation des tests de bundle";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            // --- Panel content dynamique ---
            panelContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            Controls.Add(panelContent);

            // --- Panel menu ---
            panelMenu = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.LightGray };
            Controls.Add(panelMenu);

            btnMenuCsv = new Button { Text = "CSV", Width = 100, Height = 40, Location = new Point(10, 5) };
            btnMenuCsv.Click += (s, e) => ShowMenuCsv();
            btnMenuBundleManifest = new Button { Text = "Bundle Manifest", Width = 150, Height = 40, Location = new Point(120, 5) };
            btnMenuBundleManifest.Click += (s, e) => ShowMenuBundleManifest();
            btnMenuLogs = new Button { Text = "Logs", Width = 100, Height = 40, Location = new Point(280, 5) };
            btnMenuLogs.Click += (s, e) => ShowMenuLogs();

            panelMenu.Controls.Add(btnMenuCsv);
            panelMenu.Controls.Add(btnMenuBundleManifest);
            panelMenu.Controls.Add(btnMenuLogs);

            // --- Initialiser contrôles ---
            InitializeCsvControls();
            InitializeBundleManifestControls();
            InitializeDialogs();
            InitializeGrids();

            // --- Afficher CSV par défaut ---
            ShowMenuCsv();

            this.Shown += MainForm_Shown;
        }

        #region Initialisation Controls
        private void InitializeCsvControls()
        {
            btnLoad = new Button { Text = "Charger un CSV", Height = 40, Dock = DockStyle.Top };
            btnLoad.Click += BtnLoad_Click;

            btnLoadCANdata = new Button { Text = "Charger data CANalyzer", Height = 40, Dock = DockStyle.Top };
            btnLoadCANdata.Click += BtnLoadCANdata_Click;

            btnCompare = new Button { Text = "Comparer 2 CSV", Height = 40, Dock = DockStyle.Top };
            btnCompare.Click += BtnCompare_Click;

            lblCsvCompareTitle = new Label { Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold) };
            lblCsvDiffCount = new Label { Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular) };
        }

        private void InitializeBundleManifestControls()
        {
            btnGenerateBundleManifest = new Button { Text = "Générer le Bundle Manifest", Height = 40, Dock = DockStyle.Top };
            btnGenerateBundleManifest.Click += BtnGenerateBundleManifest_Click;

            cmbPcmVersion = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, Height = 30 };
            for (int i = 1; i <= 10; i++) cmbPcmVersion.Items.Add(i);
            cmbPcmVersion.SelectedIndex = 2;

            btnValidatePcm = new Button { Text = "Valider la version du PCM", Dock = DockStyle.Top, Height = 30 };
            btnValidatePcm.Click += BtnValidatePcm_Click;

            progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 20, Minimum = 0, Maximum = 100, Value = 0, Visible = false };
            lblProgress = new Label { Dock = DockStyle.Top, Height = 50, AutoSize = false, Visible = false };

            btnCancelGenerate = new Button { Text = "Annuler la génération", Height = 40, Dock = DockStyle.Top, Visible = false };
            btnCancelGenerate.Click += BtnCancelGenerate_Click;
        }

        private void InitializeGrids()
        {
            gridLeft = CreateGrid();
            gridRight = CreateGrid();
            gridDifferences = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells };

            gridLeft.AllowUserToAddRows = false;
            gridRight.AllowUserToAddRows = false;
            gridDifferences.AllowUserToAddRows = false;

            gridLeft.Scroll += Grid_Scroll;
            gridRight.Scroll += Grid_Scroll;
        }

        private void InitializeDialogs()
        {
            ofd = new OpenFileDialog { Title = "Sélectionnez un fichier CSV", Filter = "Fichiers CSV (*.csv)|*.csv", CheckFileExists = true, Multiselect = false };
            folderBrowserDialog = new FolderBrowserDialog { Description = "Sélectionnez le répertoire du bundle à traiter", UseDescriptionForTitle = true, ShowNewFolderButton = false };
        }
        #endregion

        #region Menu dynamique
        private void ShowMenuCsv()
        {
            panelContent.Controls.Clear();

            // --- Boutons CSV ---
            var panelButtons = new Panel { Dock = DockStyle.Top, Height = 150 };
            panelButtons.Controls.Add(btnCompare);
            panelButtons.Controls.Add(btnLoadCANdata);
            panelButtons.Controls.Add(btnLoad);

            panelContent.Controls.Add(panelButtons);

            // --- Différences en bas ---
            var panelDiffs = new Panel { Dock = DockStyle.Bottom, Height = 200 };
            panelDiffs.Controls.Add(gridDifferences);
            panelDiffs.Controls.Add(lblCsvDiffCount);
            panelDiffs.Controls.Add(lblCsvCompareTitle);

            panelContent.Controls.Add(panelDiffs);

            // --- SplitContainer pour les grilles ---
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6
            };
            gridLeft.Dock = DockStyle.Fill;
            gridRight.Dock = DockStyle.Fill;
            split.Panel1.Controls.Add(gridLeft);
            split.Panel2.Controls.Add(gridRight);

            panelContent.Controls.Add(split);
            split.SplitterDistance = split.ClientSize.Width / 2;

            panelButtons.BringToFront();
            panelDiffs.BringToFront();
            split.BringToFront();
        }

        private void ShowMenuBundleManifest()
        {
            panelContent.Controls.Clear();

            var panelManifest = new Panel { Dock = DockStyle.Top, Height = 300 };
            panelManifest.Controls.Add(btnGenerateBundleManifest);
            panelManifest.Controls.Add(cmbPcmVersion);
            panelManifest.Controls.Add(btnValidatePcm);
            panelManifest.Controls.Add(progressBar);
            panelManifest.Controls.Add(lblProgress);
            panelManifest.Controls.Add(btnCancelGenerate);

            panelContent.Controls.Add(panelManifest);
        }

        private void ShowMenuLogs()
        {
            panelContent.Controls.Clear();

            // --- Bouton Rafraîchir ---
            btnRefreshLogs = new Button
            {
                Text = "Rafraîchir / Choisir un fichier de logs",
                Dock = DockStyle.Top,
                Height = 40
            };
            btnRefreshLogs.Click += BtnRefreshLogs_Click;
            panelContent.Controls.Add(btnRefreshLogs);

            // --- SplitContainer vertical pour logs + résultats ---
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6
            };
            panelContent.Controls.Add(split);

            // --- TextBox pour afficher les logs ---
            txtLogDisplay = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 10)
            };
            split.Panel1.Controls.Add(txtLogDisplay);

            // --- TextBox pour afficher les résultats ---
            txtLogResults = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.LightYellow
            };
            split.Panel2.Controls.Add(txtLogResults);

            btnRefreshLogs.BringToFront();
            split.BringToFront();
            split.SplitterDistance = panelContent.Height / 2;
        }
        #endregion

        #region Grid Helper
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

        private void Grid_Scroll(object? sender, ScrollEventArgs e)
        {
            if (sender is DataGridView sourceGrid)
            {
                DataGridView targetGrid = sourceGrid == gridLeft ? gridRight : gridLeft;
                CsvService.SyncGridScroll(sourceGrid, targetGrid);
            }
        }
        #endregion

        #region CSV Event Handlers
        private void BtnLoad_Click(object? sender, EventArgs e)
        {
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            gridLeft.Rows.Clear();
            gridLeft.Columns.Clear();
            gridRight.Rows.Clear();
            gridRight.Columns.Clear();

            var rows = CsvService.ReadCsv(ofd.FileName);
            CsvService.DisplayCsv(rows, gridLeft);
            Text = $"CSV Management - {Path.GetFileName(ofd.FileName)}";
        }

        private void BtnLoadCANdata_Click(object? sender, EventArgs e)
        {
            if (!File.Exists(AppSettings.DataFullCsvPath))
            {
                MessageBox.Show(this, "Le fichier data_full.csv est introuvable.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            gridLeft.Rows.Clear();
            gridLeft.Columns.Clear();
            gridRight.Rows.Clear();
            gridRight.Columns.Clear();

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

            lblCsvCompareTitle.Text = $"Comparaison : {Path.GetFileName(file1)} vs {Path.GetFileName(file2)}";

            int totalDiffs = CsvService.HighlightDifferences(rows1, rows2, gridLeft, gridRight, out var diffRows);
            lblCsvDiffCount.Text = $"Total différences : {totalDiffs}";
            CsvService.DisplayCsv(diffRows, gridDifferences);
        }
        #endregion

        #region Bundle Manifest Event Handlers
        private void BtnValidatePcm_Click(object? sender, EventArgs e)
        {
            if (cmbPcmVersion.SelectedItem == null)
            {
                MessageBox.Show(this, "Veuillez sélectionner une version de PCM.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            AppSettings.PcmVersion = (int)cmbPcmVersion.SelectedItem;
            cmbPcmVersion.Visible = false;
            btnValidatePcm.Visible = false;
            lblProgress.Visible = false;
        }

        private async void BtnGenerateBundleManifest_Click(object? sender, EventArgs e)
        {
            bool isCancelled = false;

            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                btnCancelGenerate.Visible = true;
                btnGenerateBundleManifest.Enabled = false;

                while (!AppSettings.IsValidBundleDirectory(AppSettings.BundleDirectory))
                {
                    var result = MessageBox.Show(
                        "Le dossier sélectionné n'a pas le format attendu.\nVoulez-vous choisir un nouveau dossier ?",
                        "Dossier invalide",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {
                        using var fbd = new FolderBrowserDialog
                        {
                            Description = "Sélectionnez le répertoire du bundle à traiter",
                            UseDescriptionForTitle = true,
                            ShowNewFolderButton = false
                        };
                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            AppSettings.BundleDirectory = fbd.SelectedPath;
                        }
                        else
                        {
                            btnCancelGenerate.Visible = false;
                            btnGenerateBundleManifest.Enabled = true;
                            return;
                        }
                    }
                    else
                    {
                        btnCancelGenerate.Visible = false;
                        btnGenerateBundleManifest.Enabled = true;
                        return;
                    }
                }

                ShowProgressBar(100);
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

                try
                {
                    await Task.Run(() =>
                    {
                        BundleManifestService.UpdateWirelessManagerArg2(doc, infos.SwId);
                        if (infos.SwId.StartsWith("IVECOCITYBUS", StringComparison.OrdinalIgnoreCase))
                        {
                            BundleManifestService.UpdateDSEName(doc, infos.SwId);
                        }
                        BundleManifestService.UpdatePackages(doc, rootFolder, UpdatePackageProgress, cancellationTokenSource.Token);
                    }, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    isCancelled = true;
                    HideProgressBar();
                    MessageBox.Show("La génération a été annulée.", "Annulation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnCancelGenerate.Visible = false;
                    btnGenerateBundleManifest.Enabled = true;
                    return;
                }

                // Étape 6 : Sauvegarde (95% à 100%)
                UpdateProgress(95, "Sauvegarde du BundleManifest...");
                doc.Save(path);

                // --- Corrections manuelles du XML ---
                string xmlContent = File.ReadAllText(path);
                xmlContent = Regex.Replace(xmlContent, @"encoding=[""'][^""']*[""']", "");
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
                if (!isCancelled)
                {
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
            }
            catch (Exception ex)
            {
                HideProgressBar();
                MessageBox.Show(this, $"Une erreur est survenue : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCancelGenerate.Visible = false;
                btnGenerateBundleManifest.Enabled = true;
            }
        }

        private void BtnCancelGenerate_Click(object? sender, EventArgs e)
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                cancellationTokenSource.Cancel();
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
            int stepValue;

            if (total <= 0)
            {
                stepValue = minValue;
            }
            else
            {
                stepValue = minValue + (current * (maxValue - minValue) / total);
                // S'assurer que stepValue reste dans les limites
                stepValue = Math.Max(minValue, Math.Min(maxValue, stepValue));
            }

            this.Invoke((MethodInvoker)delegate
            {
                UpdateProgress(stepValue, $"{message} ({current}/{total})");
            });
        }
        #endregion

        #region Logs Event Handlers
        private void BtnRefreshLogs_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ProcessLogs(ofd.FileName);
            }
        }
        private void ProcessLogs(string filePath)
        {
            // Afficher les logs filtrés (filtrage simple)
            string filteredLogs = LogService.ProcessLogs(filePath);
            txtLogDisplay.Text = filteredLogs;

            ILogTester? tester = null;
            string fileName = Path.GetFileName(filePath).ToLower();
            if (fileName.Contains("tigr"))
            {
                tester = new TigrAgentLogTester();
            }

            if (tester != null)
            {
                var results = tester.TestLogs(filePath);
                txtLogResults.Text = string.Join(Environment.NewLine, results);
            }
            else
            {
                txtLogResults.Text = "Aucun test disponible pour ce type de log.";
            }
        }
        #endregion

        #region Form Shown
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

                var result = MessageBox.Show(this,
                    "Vous devez sélectionner un répertoire pour continuer.\n\nVoulez-vous réessayer ?",
                    "Répertoire requis", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    this.Close();
                    return;
                }
            }
        }
        #endregion
    }
}
