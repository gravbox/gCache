using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.GCache.Common
{
    public class EncryptionDomain
    {
        public byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            if (data == null)
                return null;

            var a = System.Security.Cryptography.Aes.Create();
            a.Key = key;
            a.IV = iv;
            var e = a.CreateEncryptor(a.Key, a.IV);
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, e, CryptoStreamMode.Write))
                {
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(data);
                    }
                }
                return ms.ToArray();
            }
        }

        public byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            if (data == null)
                return null;

            var a = System.Security.Cryptography.Aes.Create();
            a.Key = key;
            a.IV = iv;
            var e = a.CreateDecryptor(a.Key, a.IV);
            using (var ms = new MemoryStream(data))
            {
                using (var cs = new CryptoStream(ms, e, CryptoStreamMode.Read))
                {
                    return ms.ToArray();
                }
            }
        }
    }
}
