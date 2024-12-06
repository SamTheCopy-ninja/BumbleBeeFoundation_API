using System.Security.Cryptography;
using System.Text;

namespace BumbleBeeFoundation_Client
{
    // Encryption helper to decrypt ID and tax numbers, so they can be added to the section 18 document
    public static class EncryptionHelper
    {
        private static readonly string dataSec = "BumbleBee123!"; 

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            using (var aes = Aes.Create())
            {
                var key = Encoding.UTF8.GetBytes(dataSec);
                Array.Resize(ref key, 32);
                aes.Key = key;
                aes.IV = new byte[16];

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
