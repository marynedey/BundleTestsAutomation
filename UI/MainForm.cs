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
        private Button btnMenuTiGR;

        // --- CSV controls ---
        private Button btnLoad;
        private Button btnLoadCANdata;
        private Button btnCompare;
        private DataGridView gridLeft;
        private DataGridView gridRight;
        private DataGridView gridDifferences;
        private Label lblCsvDiffCount;
        private Label lblCsvCompareTitle;

        // --- Logs controls ---
        private TextBox txtLogDisplay;
        private Button btnRefreshLogs;
        private TextBox txtLogResults;
        private ComboBox cmbVehicleType;

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
            btnMenuTiGR = new Button { Text = "TiGR", Width = 100, Height = 40, Location = new Point(120, 5) };
            btnMenuTiGR.Click += (s, e) => ShowMenuTiGR();

            panelMenu.Controls.Add(btnMenuCsv);
            panelMenu.Controls.Add(btnMenuTiGR);

            // --- Initialiser contrôles ---
            InitializeCsvControls();
            InitializeDialogs();
            InitializeGrids();
            InitializeTiGRControls();

            // --- Afficher CSV par défaut ---
            ShowMenuCsv();
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

        private void InitializeTiGRControls()
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
        private void ShowMenuTiGR()
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
            cmbVehicleType.BringToFront();
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
    }
}