using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using Gravitybox.GCache.Common;
using System.Threading.Tasks;

namespace Gravitybox.GCache
{
    [Serializable()]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    internal class SystemCore : ISystemCore
    {
        private ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>(4, 552527);
        private List<string> _markedDeletion = new List<string>();
        private System.Timers.Timer _timer = null;

        public SystemCore()
        {
            try
            {
#if DEBUG
                const int TimeInterval = 5000;
#else
                const int TimeInterval = 120000;
#endif
                var q = CacheItemFactory.Get();
                //Cull cache every minute
                _timer = new System.Timers.Timer(TimeInterval);
                _timer.Elapsed += _timer_Elapsed;
                _timer.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        /// <summary>
        /// Add or update a object into cache
        /// </summary>
        /// <param name="container"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expireMode"></param>
        /// <param name="expiresAt"></param>
        /// <param name="expiresIn"></param>
        public void AddOrUpdate(string container, string key, byte[] value, CacheExpirationMode expireMode, DateTime? expiresAt, TimeSpan? expiresIn)
        {
            if (string.IsNullOrEmpty(key))
                throw new Exception("Key not set!");
            try
            {
                key = MakeKey(container, key);

                var newCache = CacheItemFactory.Get();
                newCache.Value = value;
                newCache.ExpirationMode = expireMode;
                newCache.ExpiresAt = expiresAt;
                newCache.ExpiresIn = expiresIn;
                newCache.LastAccess = DateTime.Now;
                //_cache.AddOrUpdate(key, newCache, (a, b) => newCache);
                _cache.GetOrAdd(key, newCache);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Gets an object from cache and returns null if none exists
        /// </summary>
        /// <param name="container"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] Get(string container, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new Exception("Key not set!");
            try
            {
                key = MakeKey(container, key);

                CacheItem v;
                if (_cache.TryGetValue(key, out v))
                {
                    if (!v.IsExpired())
                    {
                        v.LastAccess = DateTime.Now;
                        return v.Value;
                    }
                    else
                        _markedDeletion.Add(key); //Expired so mark for next delete interaction
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Delete an object from cache and returns true if successful
        /// </summary>
        /// <param name="container"></param>
        /// <param name="key"></param>
        /// <param name="isPartial"></param>
        /// <returns></returns>
        public bool Delete(string container, string key, bool isPartial = false)
        {
            if (string.IsNullOrEmpty(key))
                throw new Exception("Key not set!");

            try
            {
                key = MakeKey(container, key);

                CacheItem v;
                if (isPartial)
                {
                    var count = 0;
                    foreach (var k in _cache.Keys)
                    {
                        if (k.StartsWith(key) && _cache.TryRemove(k, out v))
                            count++;
                    }
                    return (count > 0);
                }
                else
                {
                    return _cache.TryRemove(key, out v);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        /// <summary>
        /// Clears all objects from the cache
        /// </summary>
        /// <param name="container"></param>
        public void Clear(string container)
        {
            try
            {
                if (!string.IsNullOrEmpty(container))
                {
                    var key = MakeKey(container, string.Empty);
                    var count = 0;
                    CacheItem v;
                    foreach (var k in _cache.Keys)
                    {
                        if (k.StartsWith(key) && _cache.TryRemove(k, out v))
                            count++;
                    }
                }
                else
                {
                    _cache.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        private object _locker = new object();

        /// <summary>
        /// Increment a counter variable by 1
        /// </summary>
        /// <param name="container"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public long Incr(string container, string key)
        {
            lock (_locker)
            {
                long retval = 0;
                var v = this.Get(container, key);
                if (v != null)
                    retval = BitConverter.ToInt64(v, 0);
                retval++;
                this.AddOrUpdate(container, key, BitConverter.GetBytes(retval), CacheExpirationMode.None, null, null);
                return retval;
            }
        }

        /// <summary>
        /// Decrement a counter variable by 1
        /// </summary>
        /// <param name="container"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public long Decr(string container, string key)
        {
            lock (_locker)
            {
                long retval = 0;
                var v = this.Get(container, key);
                if (v != null)
                    retval = BitConverter.ToInt64(v, 0);
                retval--;
                this.AddOrUpdate(container, key, BitConverter.GetBytes(retval), CacheExpirationMode.None, null, null);
                return retval;
            }
        }

        /// <summary>
        /// Get the current counter value
        /// </summary>
        /// <param name="container"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public long GetCounter(string container, string key)
        {
            lock (_locker)
            {
                long retval = 0;
                var v = this.Get(container, key);
                if (v != null)
                    retval = BitConverter.ToInt64(v, 0);
                return retval;
            }
        }

        /// <summary>
        /// Reset the counter value to zero
        /// </summary>
        /// <param name="container"></param>
        /// <param name="key"></param>
        public void ResetCounter(string container, string key)
        {
            lock (_locker)
            {
                this.Delete(container, key);
            }
        }

        private string MakeKey(string container, string key)
        {
            if (string.IsNullOrEmpty(container))
                container = "default";
            return "!!" + container + "|" + key;
        }

        private int _lastCount = 0;
        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Enabled = false;
            try
            {
                //Find all items that need to be removed and do so
                CacheItem v;
                var count = 0;
                var timer = Stopwatch.StartNew();
                foreach (var key in _cache.Keys)
                {
                    if (_cache.TryGetValue(key, out v))
                    {
                        if (v.IsExpired())
                        {
                            _cache.TryRemove(key, out v);
                            count++;
                        }
                    }
                }
                timer.Stop();

                if (count > 0)
                    Logger.LogInfo("Cache Purge: Count=" + count + ", Elapsed=" + timer.ElapsedMilliseconds);

                Logger.LogInfo("Stats: Count=" + _cache.Count + ", LastCount=" + _lastCount + ", Increase=" + (_cache.Count - _lastCount) + ", ThreadId=" + System.Threading.Thread.CurrentThread.ManagedThreadId);
                _lastCount = _cache.Count;
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

    }
}
