using BundleTestsAutomation.Models.Bundle;
using BundleTestsAutomation.Models.Tester;
using BundleTestsAutomation.Services;
using System.Text.RegularExpressions;

namespace BundleTestsAutomation.UI
{
    public class MainForm : Form
    {
        // --- Panels dynamiques ---
        private Panel panelMenu;
        private Panel panelContent;

        // --- Menu buttons ---
        private Button btnMenuCsv;
        private Button btnMenuLogs;
        private Button btnMenuAuto;

        // --- CSV controls ---
        private Button btnLoad;
        private Button btnCompare;
        private DataGridView gridLeft;
        private DataGridView gridRight;
        private DataGridView gridDifferences;
        private Label lblCsvDiffCount;
        private Label lblCsvCompareTitle;

        // --- Logs controls ---
        private TextBox txtLogDisplay;
        private Button btnRefreshLogs;
        private TreeView treeLogResults;
        private ComboBox cmbVehicleType;
        private Panel panelJustification;
        private TextBox txtJustification;
        private SplitContainer splitResults;
        private Dictionary<int, string> lineJustifications = new Dictionary<int, string>();

        // --- Auto controls ---
        private Button btnStartTests;
        private GroupBox grpResults;
        private TreeView treeAutoResults;
        private TextBox txtAutoJustification;
        private Dictionary<int, string> autoJustifications = new();

        // --- Dialogs ---
        private OpenFileDialog ofd;
        private FolderBrowserDialog folderBrowserDialog;

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
            btnMenuLogs = new Button { Text = "LOGS", Width = 100, Height = 40, Location = new Point(120, 5) };
            btnMenuLogs.Click += (s, e) => ShowMenuLogs();
            btnMenuAuto = new Button { Text = "AUTO", Width = 100, Height = 40, Location = new Point(230, 5) };
            btnMenuAuto.Click += (s, e) => ShowMenuAuto();

            panelMenu.Controls.Add(btnMenuCsv);
            panelMenu.Controls.Add(btnMenuLogs);
            panelMenu.Controls.Add(btnMenuAuto);

            // --- Initialiser contrôles ---
            InitializeCsvControls();
            InitializeDialogs();
            InitializeGrids();
            InitializeLogsControls();
            InitializeAutoWindow();

            // --- Afficher CSV par défaut ---
            ShowMenuLogs();
        }

        #region Initialisation Controls
        private void InitializeCsvControls()
        {
            btnLoad = new Button { Text = "Charger un CSV", Height = 40, Dock = DockStyle.Top };
            btnLoad.Click += BtnLoad_Click;

            btnCompare = new Button { Text = "Comparer 2 CSV", Height = 40, Dock = DockStyle.Top };
            btnCompare.Click += BtnCompare_Click;

            lblCsvCompareTitle = new Label { Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold) };
            lblCsvDiffCount = new Label { Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular) };
        }

        private void InitializeLogsControls()
        {
            cmbVehicleType= new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Height = 30, Width = 200 };
            foreach (VehicleType type in Enum.GetValues(typeof(VehicleType)))
            {
                cmbVehicleType.Items.Add(type);
            }
            cmbVehicleType.SelectedIndex = 0;
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

        private void InitializeAutoWindow()
        {
            btnStartTests = new Button { Text = "Lancer les tests automatiques", Height = 40, Dock = DockStyle.Top };
            btnStartTests.Click += BtnStartTests_Click;

            treeAutoResults = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                Font = new Font("Consolas", 10),
                BackColor = Color.LightYellow
            };

            txtAutoJustification = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10),
                BackColor = Color.LightCyan
            };

            grpResults = new GroupBox { Text = "Résultats des tests automatiques", Dock = DockStyle.Fill };
            grpResults.Controls.Add(treeAutoResults);
        }
        #endregion

        #region Menu dynamique
        private void ShowMenuCsv()
        {
            panelContent.Controls.Clear();

            // --- Boutons CSV ---
            var panelButtons = new Panel { Dock = DockStyle.Top, Height = 150 };
            panelButtons.Controls.Add(btnCompare);
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
        private void ShowMenuLogs()
        {
            panelContent.Controls.Clear();

            // --- Panel en haut pour bouton + combobox ---
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80 // hauteur pour bouton + combobox
            };
            panelContent.Controls.Add(panelTop);

            // --- Bouton Rafraîchir ---
            btnRefreshLogs = new Button
            {
                Text = "Rafraîchir / Choisir un fichier de logs",
                Dock = DockStyle.Top,
                Height = 40
            };
            btnRefreshLogs.Click += BtnRefreshLogs_Click;
            panelTop.Controls.Add(btnRefreshLogs);

            // --- ComboBox Type de véhicule ---
            cmbVehicleType.Left = (panelContent.ClientSize.Width - cmbVehicleType.Width) / 2;
            cmbVehicleType.Top = btnRefreshLogs.Bottom + 5;
            cmbVehicleType.Anchor = AnchorStyles.Top; // reste en haut si redimensionnement
            cmbVehicleType.SelectedIndexChanged += (s, e) =>
            {
                AppSettings.VehicleTypeSelected = (VehicleType)cmbVehicleType.SelectedItem;
            };
            panelTop.Controls.Add(cmbVehicleType);

            // --- SplitContainer vertical pour logs + (résultats+justification) ---
            var splitLogs = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6
            };
            panelContent.Controls.Add(splitLogs);

            // --- GroupBox pour afficher les logs ---
            var grpLogs = new GroupBox { Text = "Logs bruts", Dock = DockStyle.Fill };
            txtLogDisplay = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 10)
            };
            grpLogs.Controls.Add(txtLogDisplay);
            splitLogs.Panel1.Controls.Add(grpLogs);

            // --- SplitContainer horizontal pour résultats + justification ---
            splitResults = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6
            };
            splitLogs.Panel2.Controls.Add(splitResults);

            // --- GroupBox pour afficher les résultats ---
            var grpResults = new GroupBox { Text = "Résultats des tests", Dock = DockStyle.Fill };
            treeLogResults = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                Font = new Font("Consolas", 10),
                BackColor = Color.LightYellow
            };
            grpResults.Controls.Add(treeLogResults);
            splitResults.Panel1.Controls.Add(grpResults);

            // --- GroupBox + TextBox pour afficher la justification ---
            var grpJustif = new GroupBox { Text = "Détails", Dock = DockStyle.Fill };
            txtJustification = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10),
                BackColor = Color.LightCyan
            };
            grpJustif.Controls.Add(txtJustification);
            splitResults.Panel2.Controls.Add(grpJustif);

            // --- Définir la largeur initiale du panel justification ---
            splitResults.SplitterDistance = splitResults.Width * 2 / 3;

            // --- Ajuster la hauteur du splitter vertical logs/resultats
            splitLogs.SplitterDistance = panelContent.Height / 2;

            btnRefreshLogs.BringToFront();
            cmbVehicleType.BringToFront();
            splitLogs.BringToFront();
            splitLogs.SplitterDistance = panelContent.Height / 2;
        }
        private void ShowMenuAuto()
        {
            panelContent.Controls.Clear();

            // --- Bouton lancer tests ---
            panelContent.Controls.Add(btnStartTests);

            // --- SplitContainer pour résultats + justification ---
            var splitAuto = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 6
            };
            panelContent.Controls.Add(splitAuto);

            // --- GroupBox résultats ---
            var grpResultsAuto = new GroupBox { Text = "Résultats des tests", Dock = DockStyle.Fill };
            grpResultsAuto.Controls.Add(treeAutoResults);
            splitAuto.Panel1.Controls.Add(grpResultsAuto);

            // --- GroupBox justification ---
            var grpJustifAuto = new GroupBox { Text = "Détails", Dock = DockStyle.Fill };
            grpJustifAuto.Controls.Add(txtAutoJustification);
            splitAuto.Panel2.Controls.Add(grpJustifAuto);

            btnStartTests.BringToFront();
            splitAuto.BringToFront();
            splitAuto.SplitterDistance = splitAuto.Width * 2 / 3;
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

            if (ofd.SafeFileName == "DTC.csv")
            {
                MessageBox.Show("Impossible de charger ce fichier, il n'est pas destiné à être lu ici. Il sert uniquement pour le contrôle des DTC.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var rows = CsvService.ReadCsv(ofd.FileName);
            CsvService.DisplayCsv(rows, gridLeft);
            Text = $"CSV Management - {Path.GetFileName(ofd.FileName)}";
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
            string allLogs = File.ReadAllText(filePath);
            txtLogDisplay.Text = allLogs;

            var tester = TesterFactory.GetTester(filePath);

            if (tester != null)
            {
                var results = tester.Test(filePath);
                DisplayTestResults(results, treeLogResults, txtJustification, lineJustifications);
            }
            else
            {
                treeLogResults.Text = "Aucun test disponible pour ce type de log.";
            }
        }
        #endregion

        #region Auto Tests Handlers
        private void BtnStartTests_Click(object? sender, EventArgs e)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
            string dataPath = Path.Combine(projectRoot, "data");

            if (!Directory.Exists(dataPath))
            {
                MessageBox.Show($"Le dossier Data est introuvable : {dataPath}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var allResults = new List<TestResult>();

            foreach (var file in Directory.GetFiles(dataPath, "*.*", SearchOption.AllDirectories))
            {
                var tester = TesterFactory.GetTester(file);

                if (tester != null)
                {
                    var results = tester.Test(file);
                    allResults.AddRange(results);
                }
            }

            if (allResults.Count == 0)
            {
                MessageBox.Show("Aucun fichier valide trouvé dans le dossier data.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                DisplayTestResults(allResults, treeAutoResults, txtAutoJustification, autoJustifications);
                MessageBox.Show($"{allResults.Count} résultats collectés depuis {dataPath}",
                        "Tests terminés", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #region Displaying results
        private void DisplayTestResults(
            List<TestResult> results,
            TreeView resultsTree,
            TextBox justificationBox,
            Dictionary<int, string> justificationMap)
        {
            resultsTree.Nodes.Clear();
            justificationBox.Clear();
            justificationMap.Clear();

            int nodeIndex = 0;
            foreach (var result in results)
            {
                var parentNode = new TreeNode($"{result.TestName}: {result.Status}")
                {
                    ForeColor = result.IsOk ? Color.Green : Color.Red
                };

                foreach (var e in result.Errors)
                {
                    string displayText = e;
                    string justification = null;

                    var match = Regex.Match(e, @"justification:\s*(.+)$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        justification = match.Groups[1].Value;
                        if (justification.Length > 50)
                            displayText = e.Replace(justification, justification.Substring(0, 50) + "...");
                    }

                    var childNode = new TreeNode(displayText)
                    {
                        Tag = justification  // Stocke la justification ici
                    };
                    parentNode.Nodes.Add(childNode);
                }

                resultsTree.Nodes.Add(parentNode);
            }

            resultsTree.AfterSelect += (s, e) =>
            {
                justificationBox.Text = e.Node.Tag as string ?? "";  // si Tag = null, vide
            };

        }
        #endregion
    }
}