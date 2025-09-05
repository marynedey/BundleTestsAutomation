using BundleTestsAutomation.Models;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace BundleTestsAutomation.Services
{
    public static class AppSettings
    {
        public static string? BundleDirectory { get; set; }

        // Type de véhicule (par défaut ICE)
        public static VehicleType VehicleTypeSelected { get; set; } = VehicleType.ICE;

        public static string DataFullCsvPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "data_full.csv");
    }
}