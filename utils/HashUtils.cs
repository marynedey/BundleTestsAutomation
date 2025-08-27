using System.Security.Cryptography;
using System.Text;

namespace BundleTestsAutomation
{
    public static class HashUtils
    {
        // SHA-256 d'une chaîne
        public static string ComputeSha256Hash(string text)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                byte[] hashBytes = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
            }
        }

        // SHA-256 d'un fichier
        public static string ComputeSha256HashFromFile(string filePath)
        {
            using (SHA256 sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
            }
        }
    }
}