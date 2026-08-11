// Copyright 2026 Entex Interactive

using System.Security.Cryptography;
using System.Text;
using Parallel.Core.Utils;

namespace Parallel.Core.Security
{
    /// <summary>
    /// Provides functionality for encryption. This class cannot be inherited.
    /// </summary>
    public static class Encryption
    {
        /// <summary>
        /// Converts a string of UTF-8 characters to a base64 string.
        /// </summary>
        /// <param name="value">The string to be encoded.</param>
        /// <returns>A base64 encoded string.</returns>
        public static string Encode(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            byte[] data = Encoding.UTF8.GetBytes(value);
            string encoded = Convert.ToBase64String(data);
            return encoded;
        }

        /// <summary>
        /// Converts a base64 string to a string of UTF-8 characters.
        /// </summary>
        /// <param name="value">The base64 string to decode.</param>
        public static string Decode(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            byte[] data = Convert.FromBase64String(value);
            string decoded = Encoding.UTF8.GetString(data);
            return decoded;
        }

        /// <summary>
        /// Encrypts a <see cref="Stream"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="LocalFile"></param>
        /// <param name="masterKey"></param>
        /// <returns></returns>
        public static void EncryptStream(Stream input, Stream output, string masterKey, UnixTime timestamp, string salt, string iv)
        {
            input.Position = 0;
            byte[] derivedKey = HashGenerator.HKDF(masterKey, Encoding.ASCII.GetBytes(salt), timestamp.ToISOString(), 32);
            using (Aes aes = Aes.Create())
            {
                aes.Key = derivedKey;
                aes.IV = Encoding.UTF8.GetBytes(iv);
                aes.Mode = CipherMode.CBC;
                using (CryptoStream cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    input.CopyTo(cryptoStream);
                }
            }
        }

        /// <summary>
        /// Decrypts a <see cref="Stream"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="LocalFile"></param>
        /// <param name="masterKey"></param>
        public static void DecryptStream(Stream input, Stream output, string masterKey, UnixTime timestamp, string salt, string iv)
        {
            input.Position = 0;
            byte[] derivedKey = HashGenerator.HKDF(masterKey, Encoding.ASCII.GetBytes(salt), timestamp.ToISOString(), 32);
            using (Aes aes = Aes.Create())
            {
                aes.Key = derivedKey;
                aes.IV = Encoding.ASCII.GetBytes(iv);
                aes.Mode = CipherMode.CBC;
                using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(output);
                }
            }
        }
    }
}