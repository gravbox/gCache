using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravitybox.GCache.Common;
using System.Configuration;

namespace Gravitybox.GCache.Tests
{
    class Program
    {
        private const string SERVER = "localhost";
        private static Random _rnd = new Random();
        private static string _server = "";

        static void Main(string[] args)
        {
            try
            {
                _server = ConfigurationManager.AppSettings["server"];
                if (string.IsNullOrEmpty(_server))
                    _server = "localhost";

                Console.WriteLine("Server=" + _server);

                System.Threading.Thread.Sleep(2000);

                Test1();
                //Test2();
                //Test3();
                //TestParallel();
                //TestCounter1();
                //TestCounter2();
                //TestLarge();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("Press <ENTER> to end...");
            Console.ReadLine();
        }

        private static void Test1()
        {
            var theItem = new TestItem();
            for (var ii = 0; ii < 500; ii++)
                theItem.B.Add("Blah " + ii);

            using (var cache = new CacheService<TestItem>(server: _server))
            {
                var timer = Stopwatch.StartNew();
                for (var ii = 0; ii < 1000; ii++)
                {
                    var key = (ii % 100).ToString();
                    cache.AddOrUpdate(key, theItem);
                    var v = cache.Get(key);
                };
                timer.Stop();
                Console.WriteLine("Elapsed= " + timer.ElapsedMilliseconds);
            }
        }

        private static void TestParallel()
        {
            var timer = Stopwatch.StartNew();

            var theItem = new TestItem();
            for (var ii = 0; ii < 500; ii++)
                theItem.B.Add("Blah " + ii);

            Parallel.For(0, 10000, (ii) =>
            {
                using (var cache = new CacheService<TestItem>())
                {
                    var key = (ii % 100).ToString();
                    cache.AddOrUpdate(key, theItem, new DateTime(2000, 1, 1));
                    var v = cache.Get(key);
                }
            });

            timer.Stop();
            Console.WriteLine("Elapsed= " + timer.ElapsedMilliseconds);
        }

        private static void Test2()
        {
            var key = "QQ";
            using (var cache = new CacheService<string>(container: "Z1"))
            {
                cache.AddOrUpdate(key, "Hello");
            }
            using (var cache = new CacheService<string>(container: "Z1"))
            {
                var q = cache.Get(key);
            }
            using (var cache = new CacheService<string>(container: "Z2"))
            {
                var q = cache.Get(key);
            }
        }

        private static void Test3()
        {
            var theItem = new TestItem();
            for (var ii = 0; ii < 500; ii++)
                theItem.B.Add("Blah " + ii);

            using (var cache = new CacheService<TestItem>())
            {
                cache.UseCompression = true;
                cache.EncryptionKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8 };
                var key = "zz";
                cache.AddOrUpdate(key, theItem);
                var v = cache.Get(key);
                Console.WriteLine("Test3: Complete");
            }
        }

        private static void TestCounter1()
        {
            var key = "s82js7";
            var timer = Stopwatch.StartNew();
            Parallel.For(0, 10000, (ii) =>
            {
                using (var cache = new CacheService<string>())
                {
                    var v = cache.Incr(key);
                }
            });
            timer.Stop();

            using (var cache = new CacheService<string>())
            {
                var v1 = cache.GetCounter(key);
                cache.ResetCounter(key);
                var v2 = cache.GetCounter(key);
                Console.WriteLine("Counter=" + v1 + ", Elapsed=" + timer.ElapsedMilliseconds);
            }

        }

        private static void TestCounter2()
        {
            var key = "s82js7sd";
            var timer = Stopwatch.StartNew();

            using (var cache = new CacheService<string>())
            {
                cache.ResetCounter(key);
                for (var ii = 0; ii < 10000; ii++)
                {
                    cache.IncrAsync(key);
                }
                Console.WriteLine("Async Load: Elapsed=" + timer.ElapsedMilliseconds);
            }
            timer.Stop();
            Console.WriteLine("Async Complete: Elapsed=" + timer.ElapsedMilliseconds);

            using (var cache = new CacheService<string>())
            {
                long lastValue = 0;
                long v = -1;
                do
                {
                    lastValue = v;
                    System.Threading.Thread.Sleep(50);
                    v = cache.GetCounter(key);
                    Console.WriteLine("Counter=" + v);
                } while (v != lastValue);

            }

        }

        private static void TestLarge()
        {
            var container = "Large";

            var batchCount = 900;
            var itemsPerBatch = 1000;
            var timer = Stopwatch.StartNew();

            using (var cache = new CacheService<string>(server: _server, container: container))
            {
                var timer2 = Stopwatch.StartNew();
                var value = RandomString(500);
                for (var ii = 0; ii < batchCount * itemsPerBatch; ii++)
                {

                    var key = "Key-" + ii;
                    cache.AddOrUpdateAsync(key, value);
                    if (ii % 1000 == 0)
                        Console.WriteLine("Added " + ii + ", Elapsed=" + timer2.ElapsedMilliseconds);
                }
                while (!cache.IsAsyncComplete) { System.Threading.Thread.Sleep(25); }
            }

            timer.Stop();
            Console.WriteLine("Loaded: Count=" + (batchCount * itemsPerBatch) + ", Elapsed=" + timer.ElapsedMilliseconds);

            var testCount = 20000;
            timer = Stopwatch.StartNew();
            var hitCount = 0;
            using (var cache = new CacheService<string>(server: _server, container: container))
            {
                for (var ii = 0; ii < testCount; ii++)
                {
                    var key = "Key-" + (_rnd.Next(1, 1000000));
                    var value = cache.Get(key);
                    if (value != null)
                        hitCount++;
                }
            }
            timer.Stop();
            Console.WriteLine("Access Complete " + testCount + ", HitCount=" + hitCount + ", Elapsed=" + timer.ElapsedMilliseconds);
        }

        public static string RandomString(int length)
        {
            if (length < 1) length = 1;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            for (int i = 0; i < stringChars.Length; i++)
                stringChars[i] = chars[_rnd.Next(chars.Length)];
            return new String(stringChars);
        }

    }

    [Serializable]
    public class TestItem
    {
        public string A { get; set; }
        public List<string> B { get; set; } = new List<string>();
        public SubItem C { get; set; } = new SubItem();
    }

    [Serializable]
    public class SubItem
    {
        public string Z { get; set; }
    }

}
