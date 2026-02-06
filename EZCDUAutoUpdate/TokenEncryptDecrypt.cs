using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EZCDUAutoUpdate
{
    public static class TokenEncryptDecrypt
    {
        // Key and IV should be securely managed and stored, not hardcoded in production.
        private static readonly byte[] Key = Generate256BitsOfRandomEntropy();// 32 bytes for AES-256
        private static readonly byte[] IV = Generate256BitsOfRandomEntropy();// 16 bytes for AES

        // This constant is used to determine the keysize of the encryption algorithm in bits.
        // We divide this by 8 within the code below to get the equivalent number of bytes.
        private const int KEYSIZE = 256;

        // This constant determines the number of iterations for the password bytes generation function.
        private const int DERIVATION_ITERATIONS = 5000;

        private const string PASS_PHRASE = "CWIwbXQxNvyqydGEEhL4i8drfTIjlHGBISphSkhVfhdIF8rAQaeUXiWYNVSkBmTNVyLA49jaK" +
                                           "FnbVpuS1m2rTxQibNdQv37Hxy9blCIYWaPSJH1ew6I58TdIXrEnmciyx5dXXSzW1tJy3GwNWz" +
                                           "xtlIKjwT957HD5bshB6yNu9NQXiaG3SNBFkk8CW8QrfH5I8NE3Qkt1JSE5qWIESB4F2hnxfsD" +
                                           "pBL7b1nRJYUWFRN2DTHNiwmlFp16tQEMaBdqf3SIzgflReyGkWbDrsg6U6xX3bgTtwO4BOju4" +
                                           "yQubsTxSMwpe2em77Gl7sn9RGCg2uMZehdGEud73XEwCVKdmvY16FULyxbPJBvQs8baXjIzNc" +
                                           "NjCYyNGIBqKqjrIPX3CCqU28XVGDNDCD6NPlee2MyCpKBuRLFninCPWLnFI1nDEcsWqJTzEBD" +
                                           "vLUDHTMHaa7KQjpGkKObLND24zwaV1Pm6dmLyThx7ggcuDQlT6Df1OkrE3OQX1GpCZgf9JBYfX";

        public static string Encrypt(string plainText)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.
            var saltStringBytes = Generate256BitsOfRandomEntropy();
            var ivStringBytes = Generate256BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using var password = new Rfc2898DeriveBytes(PASS_PHRASE, saltStringBytes, DERIVATION_ITERATIONS, HashAlgorithmName.SHA1);
            var keyBytes = password.GetBytes(KEYSIZE / 8);
            var engine = new RijndaelEngine(256);
            var blockCipher = new CbcBlockCipher(engine);
            var cipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());
            var keyParam = new KeyParameter(keyBytes);
            var keyParamWithIV = new ParametersWithIV(keyParam, ivStringBytes, 0, 32);

            cipher.Init(true, keyParamWithIV);
            var comparisonBytes = new byte[cipher.GetOutputSize(plainTextBytes.Length)];
            var length = cipher.ProcessBytes(plainTextBytes, comparisonBytes, 0);

            cipher.DoFinal(comparisonBytes, length);
            // return Convert.ToBase64String(comparisonBytes);
            return Convert.ToBase64String(saltStringBytes.Concat(ivStringBytes).Concat(comparisonBytes).ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            // Get the complete stream of bytes that represent:
            // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
            var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
            // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
            var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(KEYSIZE / 8).ToArray();
            // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
            var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(KEYSIZE / 8).Take(KEYSIZE / 8).ToArray();
            // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
            var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip(KEYSIZE / 8 * 2).Take(cipherTextBytesWithSaltAndIv.Length - KEYSIZE / 8 * 2).ToArray();

            using var password = new Rfc2898DeriveBytes(PASS_PHRASE, saltStringBytes, DERIVATION_ITERATIONS, HashAlgorithmName.SHA1);
            var keyBytes = password.GetBytes(KEYSIZE / 8);
            var engine = new RijndaelEngine(256);
            var blockCipher = new CbcBlockCipher(engine);
            var cipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());
            var keyParam = new KeyParameter(keyBytes);
            var keyParamWithIV = new ParametersWithIV(keyParam, ivStringBytes, 0, 32);

            cipher.Init(false, keyParamWithIV);
            var comparisonBytes = new byte[cipher.GetOutputSize(cipherTextBytes.Length)];
            var length = cipher.ProcessBytes(cipherTextBytes, comparisonBytes, 0);

            cipher.DoFinal(comparisonBytes, length);
            //return Convert.ToBase64String(saltStringBytes.Concat(ivStringBytes).Concat(comparisonBytes).ToArray());

            var nullIndex = comparisonBytes.Length - 1;
            while (nullIndex >= 0 && comparisonBytes[nullIndex] == 0)
            {
                nullIndex--;
            }

            comparisonBytes = comparisonBytes.Take(nullIndex + 1).ToArray();

            var result = Encoding.UTF8.GetString(comparisonBytes, 0, comparisonBytes.Length);

            return result;
        }

        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using var generator = RandomNumberGenerator.Create();
            generator.GetBytes(randomBytes); // Fill the array with cryptographically secure random bytes.

            return randomBytes;
        }
    }
}
