using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gravitybox.GCache
{
    internal static class CacheItemFactory
    {
        private const int MinItems = 5000;
        private static ConcurrentBag<CacheItem> _cache = new ConcurrentBag<CacheItem>();
        private static System.Timers.Timer _timer = null;

        static CacheItemFactory()
        {
            for (var ii = 0; ii < MinItems; ii++)
                _cache.Add(new CacheItem());

            _timer = new System.Timers.Timer(2000);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();

        }

        private static void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            try
            {
                Repopulate();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        public static CacheItem Get()
        {
            CacheItem retval = null;
            if (_cache.TryTake(out retval))
                return retval;
            return new CacheItem();
        }

        private static void Repopulate()
        {
            var origCount = _cache.Count;
            var count = MinItems - origCount;
            for (var ii = 0; ii < count; ii++)
                _cache.Add(new CacheItem());
            //Logger.LogDebug("CacheItemFactory Repopulate: StartCount=" + origCount + ", Added=" + count + ", ThreadId=" + System.Threading.Thread.CurrentThread.ManagedThreadId);
        }
    }
}
