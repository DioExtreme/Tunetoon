using System;
using System.IO;
using System.Security.Cryptography;

namespace Tunetoon.Encrypt
{
    internal static class DataProtection
    {
        private static Rfc2898DeriveBytes PBKDF2;
        private static Aes AES = Aes.Create();
        public static void CreatePBKDF2(string password)
        {
            PBKDF2 = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256);

            AES.Mode = CipherMode.CBC;
            AES.Padding = PaddingMode.PKCS7;
            AES.IV = PBKDF2.GetBytes(AES.BlockSize / 8);
            AES.Key = PBKDF2.GetBytes(AES.KeySize / 8);
        }

        public static void LoadPBKDF2(string password, byte[] salt)
        {
            PBKDF2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);

            AES.Mode = CipherMode.CBC;
            AES.Padding = PaddingMode.PKCS7;
            AES.IV = PBKDF2.GetBytes(AES.BlockSize / 8);
            AES.Key = PBKDF2.GetBytes(AES.KeySize / 8);
        }

        public static void WriteJsonToEncryptedFile(byte[] jsonBytes, string fileName)
        {
            string base64encrypted;

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(PBKDF2.Salt, 0, 16);
                using (var cs = new CryptoStream(memoryStream, AES.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(jsonBytes);
                }
                byte[] memoryStreamBytes = memoryStream.ToArray();
                base64encrypted = Convert.ToBase64String(memoryStreamBytes);
            }

            using (var sw = new StreamWriter(fileName))
            {
                sw.Write(base64encrypted);
            }
        }

        public static string ReadJsonFromEncryptedFile(string masterPassword, string fileName)
        {
            byte[] encryptedBytes;

            using (var sr = new StreamReader(fileName))
            {
                encryptedBytes = Convert.FromBase64String(sr.ReadToEnd());
            }

            using (var memoryStream = new MemoryStream(encryptedBytes))
            {
                byte[] salt = new byte[16];
                memoryStream.Read(salt, 0, 16);
                LoadPBKDF2(masterPassword, salt);

                using (var cryptoStream = new CryptoStream(memoryStream, AES.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cryptoStream))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
