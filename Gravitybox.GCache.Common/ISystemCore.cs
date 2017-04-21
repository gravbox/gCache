using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace Gravitybox.GCache.Common
{
    public enum CacheExpirationMode
    {
        None,
        Absolute,
        Sliding,
    }

    [ServiceContract]
    public interface ISystemCore
    {
        [OperationContract]
        void AddOrUpdate(string container, string key, byte[] value, CacheExpirationMode expireMode, DateTime? expires, TimeSpan? expiresIn);

        [OperationContract]
        byte[] Get(string container, string key);

        [OperationContract]
        bool Delete(string container, string key, bool isPartial = false);

        [OperationContract]
        void Clear(string container);

        [OperationContract]
        long Incr(string container, string key);

        [OperationContract]
        long Decr(string container, string key);

        [OperationContract]
        long GetCounter(string container, string key);

        [OperationContract]
        void ResetCounter(string container, string key);

    }

}
