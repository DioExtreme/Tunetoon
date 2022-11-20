using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Tunetoon.BZip2;
using Tunetoon.Utilities;

namespace Tunetoon
{
    internal static class GamePatchUtils 
    {
        private static readonly uint[] ByteLookupArray = CreateByteLookupArray();

        private static uint[] CreateByteLookupArray()
        {
            var byteLookupArray = new uint[256];
            for (int i = 0; i < 256; ++i)
            {
                string byteString = i.ToString("x2");
                // Chars are in UTF-16 so 16 bits here
                char highNibble = byteString[0];
                char lowNibble = byteString[1];
                byteLookupArray[i] = highNibble + (uint)(lowNibble << 16);
            }
            return byteLookupArray;
        }

        private static string HexSHA1(byte[] bytes)
        {
            // SHA-1 has 20 bytes, therefore 40 in hex
            int hexStringLength = 40;

            var hexArray = new char[hexStringLength];
            for (int i = 0; i < bytes.Length; ++i)
            {
                uint uintValue = ByteLookupArray[bytes[i]];
                char highNibble = (char)uintValue;
                char lowNibble = (char)(uintValue >> 16);
                hexArray[2 * i] = highNibble;
                hexArray[2 * i + 1] = lowNibble;
            }
            return new string(hexArray);
        }

        public static string GetSha1FileHash(string filepath)
        {
            try
            {
                using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
                using (var sha1Cng = new SHA1Cng())
                {
                    return HexSHA1(sha1Cng.ComputeHash(fs));
                }
            }
            catch
            { 
                return null;
            }
        }

        public static string GetSha1HashString(string str)
        {
            try
            {
                using (var sha1Cng = new SHA1Cng())
                {
                    return HexSHA1(sha1Cng.ComputeHash(Encoding.UTF8.GetBytes(str)));
                }
            }
            catch
            {
                return null;
            }
        }

        public static void Decompress(string compressedFile, string localFile, string type)
        {
            using (var fs = new FileStream(compressedFile, FileMode.Open, FileAccess.Read))
            using (var fsOut = File.Create(localFile))
            {
                if (type == "bzip2")
                {
                    BZip2Decompressor.Decompress(fs, fsOut, true);
                }
                else if (type == "gzip")
                {
                    using (var GZipDecompressor = new GZipStream(fs, CompressionMode.Decompress))
                    {
                        GZipDecompressor.CopyTo(fsOut);
                    }
                }
            }
        }

        public static int Extract(string downloadedFilePath, string extractedFilePath, string type)
        {
            try
            {
                Decompress(downloadedFilePath, extractedFilePath, type);
                return 0;
            }
            catch
            {
                File.Delete(extractedFilePath);
                return -1;
            }
            finally
            {
                File.Delete(downloadedFilePath);
            }
        }

        public static bool FileIsCorrect(string filePath, string compHash)
        {
            string fileHash = GetSha1FileHash(filePath);

            if (fileHash == compHash)
            {
                return true;
            }
            return false;
        }

        public static void Patch(string patchFile, string localFile)
        {
            BsPatch.Apply(localFile, patchFile);
        }
    }
}
