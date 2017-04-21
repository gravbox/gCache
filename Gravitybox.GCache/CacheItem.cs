using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gravitybox.GCache.Common;

namespace Gravitybox.GCache
{
    internal class CacheItem
    {
        public byte[] Value { get; set; }
        public DateTime Added { get; set; } = DateTime.Now;
        public TimeSpan? ExpiresIn { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTime LastAccess { get; set; } = DateTime.Now;
        public CacheExpirationMode ExpirationMode { get; set; } = CacheExpirationMode.None;

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
            throw new Exception("Unknown Error");
        }

    }
}
