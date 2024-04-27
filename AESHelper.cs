using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Memoryboard
{
    public class AESHelper
    {
        public static byte[] Encrypt(string plainText, byte[] keySourceBytes)
        {
            byte[] keyBytes = new byte[32];

            Array.Copy(keySourceBytes, keyBytes, Math.Min(keyBytes.Length, keySourceBytes.Length));

            using Aes aes = Aes.Create();

            aes.Key = keyBytes;
            aes.IV = new byte[16];

            using MemoryStream memoryStream = new();
            using CryptoStream cryptoStream = new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            cryptoStream.FlushFinalBlock();

            return memoryStream.ToArray();
        }

        public static string Decrypt(byte[] cipherText, byte[] keySourceBytes)
        {
            byte[] keyBytes = new byte[32];

            Array.Copy(keySourceBytes, keyBytes, Math.Min(keyBytes.Length, keySourceBytes.Length));

            using Aes aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = new byte[16];
            
            using MemoryStream memoryStream = new(cipherText);
            using CryptoStream cryptoStream = new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            byte[] decryptedBytes = new byte[cipherText.Length];
            int bytesRead = cryptoStream.Read(decryptedBytes, 0, decryptedBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes, 0, bytesRead);
        }   
    }
}
