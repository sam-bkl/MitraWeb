using System;
using System.Security.Cryptography;
using System.Text;

namespace cos.Helpers
{
    public static class PasswordHelper
    {
        public static string ComputeSha256Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty", nameof(input));

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}

