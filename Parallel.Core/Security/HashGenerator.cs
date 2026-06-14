// Copyright 2026 Entex Interactive

using System.Security.Cryptography;
using System.Text;

namespace Parallel.Core.Security
{
    /// <summary>
    /// Provides functionality for generating random hashes. This class cannot be inherited.
    /// </summary>
    public static class HashGenerator
    {
        /// <summary>
        /// Generates a random series of bytes.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] RandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        public static byte[] HKDF(string masterKey, byte[] salt, string info, int length)
        {
            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(masterKey)))
            {
                byte[] prk = hmac.ComputeHash(salt);
                byte[] infoBytes = Encoding.UTF8.GetBytes(info);
                byte[] output = new byte[length];
                byte[] previous = new byte[0];
                int iterations = (int)Math.Ceiling((double)length / hmac.HashSize * 8);

                for (int i = 0; i < iterations; i++)
                {
                    byte[] input = new byte[previous.Length + infoBytes.Length + 1];
                    Buffer.BlockCopy(previous, 0, input, 0, previous.Length);
                    Buffer.BlockCopy(infoBytes, 0, input, previous.Length, infoBytes.Length);
                    input[input.Length - 1] = (byte)(i + 1);

                    previous = hmac.ComputeHash(input);
                    Buffer.BlockCopy(previous, 0, output, i * previous.Length, previous.Length);
                }

                Array.Resize(ref output, length);
                return output;
            }
        }

        /// <summary>
        /// Generates a random hash.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="lowercase"></param>
        /// <returns></returns>
        public static string GenerateHash(int length, bool lowercase = false)
        {
            return RandomNumberGenerator.GetHexString(length, lowercase);
        }

        /// <summary>
        /// Computes a SHA1 hash from a string.
        /// </summary>
        /// <param name="value">The string to hash.</param>
        /// <returns>A <see cref="SHA1"/> hash as a string.</returns>
        public static string CreateSHA1(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            return Convert.ToHexString(SHA1.HashData(Encoding.ASCII.GetBytes(value))).ToLower();
        }

        public static string CreateSHA256(Span<byte> value)
        {
            return Convert.ToHexString(SHA256.HashData(value)).ToLower();
        }

        /// <summary>
        /// Computes a SHA256 hash from bytes.
        /// </summary>
        /// <param name="value">The string to hash.</param>
        /// <returns>A <see cref="SHA256"/> hash as a string.</returns>
        public static string CreateSHA256(byte[] value)
        {
            return Convert.ToHexString(SHA256.HashData(value)).ToLower();
        }

        /// <summary>
        /// Computes a SHA256 hash from a string.
        /// </summary>
        /// <param name="value">The string to hash.</param>
        /// <returns>A <see cref="SHA256"/> hash as a string.</returns>
        public static string CreateSHA256(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLower();
        }
    }
}