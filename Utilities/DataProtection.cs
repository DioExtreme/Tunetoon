using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Tunetoon.Utilities {
    internal static class DataProtection {
        private static byte[] _usernameEntropy = new byte[32];
        private static byte[] _passwordEntropy = new byte[32];
        public static void MakeEntropy()
        {   
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(_usernameEntropy);
                rng.GetBytes(_passwordEntropy);
            }

            var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\DioExtreme\Tunetoon");
            key.SetValue("Candy", _usernameEntropy);
            key.SetValue("AnotherCandy", _passwordEntropy);
            key.Close();
        }

        public static void LoadEntropy()
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\DioExtreme\Tunetoon");
  
            if (key != null)
            {
                _usernameEntropy = (byte[])key.GetValue("Candy");
                _passwordEntropy = (byte[])key.GetValue("AnotherCandy");
                key.Close();
            }
        }
        public static string Encrypt(byte[] plaintext, bool isPass)
        {
            byte[] entropy;

            if (isPass)
            {
                entropy = _passwordEntropy;
            }
            else
            {
                entropy = _usernameEntropy;
            }

            return Convert.ToBase64String(ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser));
        }

        public static string Decrypt(string ciphertext, bool isPass) {
            try
            {
                byte[] entropy;

                if (isPass)
                {
                    entropy = _passwordEntropy;
                }
                else
                {
                    entropy = _usernameEntropy;
                }

                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(ciphertext), entropy,
                    DataProtectionScope.CurrentUser));
            }
            catch
            {
                // Unfortunate, usually anti-viruses or another user/computer
                // When it comes to DPAPI, this usually means data loss
                return null;
            }
        }
    }
}
