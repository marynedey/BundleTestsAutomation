using BundleTestsAutomation.Models.CSV;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BundleTestsAutomation.Services
{
    public static class CsvService
    {
        // --- Lecture générique et tolérante pour n'importe quel CSV ---
        public static List<CsvRow> ReadCsv(string filePath, string delimiter = ";")
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = false,
                BadDataFound = null,
                MissingFieldFound = null,
                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                DetectDelimiter = true
            });

            var rows = new List<CsvRow>();
            while (csv.Read())
            {
                var row = new List<string>();
                for (int i = 0; csv.TryGetField<string>(i, out var field); i++)
                    row.Add(field ?? "");
                rows.Add(new CsvRow(row));
            }
            return rows;
        }

        // --- Lecture typée pour les DTC ---
        public static List<DtcRow> ReadDtcCsv(string filePath, string delimiter = ";")
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = true,
                BadDataFound = null,
                MissingFieldFound = null,
                TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                DetectDelimiter = true
            });

            csv.Context.RegisterClassMap<DtcRowMap>();
            return csv.GetRecords<DtcRow>().ToList();
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
    }
}