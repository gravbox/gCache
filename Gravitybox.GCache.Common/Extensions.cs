using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.GCache.Common
{
    internal static class Extensions
    {
        /// <summary />
        public static byte[] ObjectToBin(this object obj)
        {
            if (obj == null) throw new Exception("Object cannot be null");
            try
            {
                //Open stream and move to end for writing
                using (var stream = new MemoryStream())
                {
                    stream.Seek(0, SeekOrigin.End);
                    var formatter = new BinaryFormatter();
                    formatter.TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.TypesWhenNeeded;
                    formatter.Serialize(stream, obj);
                    stream.Close();
                    var v = stream.ToArray().ZipBytes();
                    return v;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary />
        public static T BinToObject<T>(this byte[] data)
        {
            try
            {
                var formatter = new BinaryFormatter();
                formatter.TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.TypesWhenNeeded;
                using (var stream = new MemoryStream(data.UnzipBytes()))
                {
                    return (T)formatter.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary />
        public static byte[] ZipBytes(this byte[] byteArray, bool fast = true)
        {
            if (byteArray == null) return null;
            try
            {
                //Prepare for compress
                using (var ms = new System.IO.MemoryStream())
                {
                    var level = System.IO.Compression.CompressionLevel.Fastest;
                    if (!fast) level = System.IO.Compression.CompressionLevel.Optimal;
                    using (var sw = new System.IO.Compression.GZipStream(ms, level))
                    {
                        //Compress
                        sw.Write(byteArray, 0, byteArray.Length);
                        sw.Close();

                        //Transform byte[] zip data to string
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary />
        public static byte[] UnzipBytes(this byte[] byteArray)
        {
            //If null stream return null string
            if (byteArray == null) return null;

            try
            {
                using (var memoryStream = new MemoryStream(byteArray))
                {
                    using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        using (var writerStream = new MemoryStream())
                        {
                            gZipStream.CopyTo(writerStream);
                            gZipStream.Close();
                            memoryStream.Close();
                            return writerStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
