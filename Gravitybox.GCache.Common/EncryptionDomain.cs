using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.GCache.Common
{
    internal static class EncryptionDomain
    {
        private static byte[] IV = new byte[16] { 238, 98, 15, 77, 132, 246, 129, 65, 238, 98, 15, 77, 132, 246, 129, 65 };

        public static byte[] Encrypt(this byte[] plain, byte[] key)
        {
            byte[] encrypted;
            using (var mstream = new MemoryStream())
            {
                using (var aesProvider = new AesCryptoServiceProvider())
                {
                    using (var cryptoStream = new CryptoStream(mstream,
                        aesProvider.CreateEncryptor(key, IV), CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plain, 0, plain.Length);
                    }
                    encrypted = mstream.ToArray();
                }
            }
            return encrypted;
        }

        public static byte[] Decrypt(this byte[] encrypted, byte[] key)
        {
            byte[] plain;
            int count;
            using (var mStream = new MemoryStream(encrypted))
            {
                using (var aesProvider = new AesCryptoServiceProvider())
                {
                    aesProvider.Mode = CipherMode.CBC;
                    using (var cryptoStream = new CryptoStream(mStream,
                     aesProvider.CreateDecryptor(key, IV), CryptoStreamMode.Read))
                    {
                        plain = new byte[encrypted.Length];
                        count = cryptoStream.Read(plain, 0, plain.Length);
                    }
                }
            }
            return plain;
        }

        public static byte[] Compress(this byte[] data)
        {
            using (var memory = new MemoryStream())
            {
                using (var gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return memory.ToArray();
            }
        }

        public static byte[] Decompress(this byte[] data)
        {
            using (var stream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

    }
}
