using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DocumentGenerator
{
    public static class Encryption
    {
        /// <summary>
        /// Зашифровывает строку с помощью ключа для шифрования.
        /// </summary>
        /// <param name="source">Строка для шифрования.</param>
        /// <param name="key">Ключ для шифрования.</param>
        /// <returns>Зашифрованная строка.</returns>
        public static string Encrypt(string source, string key)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(source), key));
        }

        /// <summary>
        /// Расшифровает данные из исходной строки. Возвращает null, если прочесть данные не удалось.
        /// </summary>
        /// <param name="decryptedString">Зашифрованная строка.</param>
        /// <param name="key">Ключ для шифрования.</param>
        /// <returns>Расшфрованная строка.</returns>
        public static string Decrypt(string decryptedString, string key)
        {
            string result;
            try
            {
                using (CryptoStream cryptoStream =
                    InternalDecrypt(Convert.FromBase64String(decryptedString),
                        key))
                {
                    using (StreamReader streamReader =
                        new StreamReader(cryptoStream))
                    {
                        result = streamReader.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException)
            {
                return null;
            }

            return result;
        }

        private static byte[] Encrypt(byte[] key, string value)
        {
            using (SymmetricAlgorithm algorithm = Rijndael.Create())
            using (ICryptoTransform encryptor =
                algorithm.CreateEncryptor(
                    new PasswordDeriveBytes(value, null).GetBytes(16),
                    new byte[16]))
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs =
                new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(key, 0, key.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }

        private static CryptoStream InternalDecrypt(byte[] key, string value)
        {
            SymmetricAlgorithm sa = Rijndael.Create();
            ICryptoTransform ct = sa.CreateDecryptor((new PasswordDeriveBytes(value, null)).GetBytes(16), new byte[16]);

            MemoryStream ms = new MemoryStream(key);
            return new CryptoStream(ms, ct, CryptoStreamMode.Read);
        }
    }
}