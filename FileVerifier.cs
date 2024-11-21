using System.IO;
using System.Security.Cryptography;


namespace GNLauncher
{
    internal class FileVerifier
    {
        public string GenerateChecksum(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
            }
        }

        public bool VerifyFileIntegrity(string filePath, string originalChecksum)
        {
            string currentChecksum = GenerateChecksum(filePath);
            return currentChecksum.Equals(originalChecksum, StringComparison.OrdinalIgnoreCase);
        }
    }
}
