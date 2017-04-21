using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravitybox.GCache.Common;

namespace Gravitybox.GCache
{
    /// <summary>
    /// The is the wrapper for all cached items that keeps up with all related metadata
    /// </summary>
    internal class CacheItem
    {
        /// <summary>
        /// The actual cache value
        /// </summary>
        public byte[] Value { get; set; }
        /// <summary>
        /// The time that the item was originally added
        /// </summary>
        public DateTime Added { get; set; } = DateTime.Now;
        /// <summary>
        /// The amount of time from last access that an object should stay in cache
        /// </summary>
        public TimeSpan? ExpiresIn { get; set; }
        /// <summary>
        /// The absolute date/time that an object should be purged from cache
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }
        /// <summary>
        /// The last time that this cache object was accessed
        /// </summary>
        public DateTime LastAccess { get; set; } = DateTime.Now;
        /// <summary>
        /// The expiration mode: None, Absolute, or Sliding
        /// </summary>
        public CacheExpirationMode ExpirationMode { get; set; } = CacheExpirationMode.None;

        /// <summary>
        /// Determines if this object should be purged from cache
        /// </summary>
        /// <returns></returns>
        public bool IsExpired()
        {
            switch (this.ExpirationMode)
            {
                case CacheExpirationMode.Absolute:
                    return (DateTime.Now > this.ExpiresAt.Value);
                case CacheExpirationMode.None:
                    return false;
                case CacheExpirationMode.Sliding:
                    return DateTime.Now > this.LastAccess.Add(this.ExpiresIn.Value);
            }
            throw new Exception("Unknown expiration mode");
        }

    }
}
