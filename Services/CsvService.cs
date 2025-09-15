using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BundleTestsAutomation.Models.CSV;

namespace BundleTestsAutomation.Services
{
    public static class CsvService
    {
        public static List<CsvRow> ReadCsv(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Le fichier {filePath} n'existe pas.");
            var lines = File.ReadAllLines(filePath).ToList();
            if (lines.Count <= 0)
                throw new Exception("Le fichier CSV ne comporte pas de données.");
            char sep = DetectSeparator(lines[0]);
            return lines.Select(l => new CsvRow(ParseCsvLine(l, sep))).ToList();
        }

        public static void DisplayCsv(List<CsvRow> rows, DataGridView targetGrid)
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

        public static int HighlightDifferences(
            List<CsvRow> rows1,
            List<CsvRow> rows2,
            DataGridView gridLeft,
            DataGridView gridRight,
            out List<CsvRow> diffRows)
        {
            diffRows = new List<CsvRow>();
            int totalDifferences = 0;
            int rowCount = Math.Max(rows1.Count, rows2.Count);

            // Détermine le nombre maximum de colonnes pour toutes les lignes
            int colCount = 0;
            for (int i = 0; i < rowCount; i++)
            {
                var row1 = i < rows1.Count ? rows1[i] : new CsvRow(new List<string>());
                var row2 = i < rows2.Count ? rows2[i] : new CsvRow(new List<string>());
                colCount = Math.Max(colCount, Math.Max(row1.Count, row2.Count));
            }

            // Désactive le repainting
            gridLeft.SuspendLayout();
            gridRight.SuspendLayout();

            // --- Colonnes ---
            while (gridLeft.Columns.Count < colCount)
                gridLeft.Columns.Add($"Col{gridLeft.Columns.Count}", $"Col{gridLeft.Columns.Count}");
            while (gridRight.Columns.Count < colCount)
                gridRight.Columns.Add($"Col{gridRight.Columns.Count}", $"Col{gridRight.Columns.Count}");

            // --- Lignes ---
            gridLeft.Rows.Clear();
            gridRight.Rows.Clear();
            for (int i = 0; i < rowCount; i++)
            {
                gridLeft.Rows.Add();
                gridRight.Rows.Add();
            }

            // --- Comparaison cellule par cellule ---
            for (int i = 0; i < rowCount; i++)
            {
                var row1 = i < rows1.Count ? rows1[i] : new CsvRow(new List<string>());
                var row2 = i < rows2.Count ? rows2[i] : new CsvRow(new List<string>());

                var diffRow = new List<string>();
                bool rowHasDiff = false;

                for (int j = 0; j < colCount; j++)
                {
                    string val1 = j < row1.Count ? row1[j] : "";
                    string val2 = j < row2.Count ? row2[j] : "";

                    gridLeft.Rows[i].Cells[j].Value = val1;
                    gridRight.Rows[i].Cells[j].Value = val2;

                    if (val1 != val2)
                    {
                        gridLeft.Rows[i].Cells[j].Style.BackColor = System.Drawing.Color.LightCoral;
                        gridRight.Rows[i].Cells[j].Style.BackColor = System.Drawing.Color.LightCoral;
                        totalDifferences++;
                        rowHasDiff = true;
                        diffRow.Add($"{val1} → {val2}");
                    }
                    else
                    {
                        diffRow.Add("");
                    }
                }

                if (rowHasDiff)
                    diffRows.Add(new CsvRow(diffRow));
            }

            // Réactive le repainting
            gridLeft.ResumeLayout();
            gridRight.ResumeLayout();

            return totalDifferences;
        }

        public static void SyncGridScroll(DataGridView sourceGrid, DataGridView targetGrid)
        {
            try
            {
                if (sourceGrid.Rows.Count > 0 && targetGrid.Rows.Count > 0)
                    targetGrid.FirstDisplayedScrollingRowIndex = sourceGrid.FirstDisplayedScrollingRowIndex;
            }
            catch (InvalidOperationException)
            {
                // ignoré car la grille cible est vide ou non scrollable
            }
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
    }
}