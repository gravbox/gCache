using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gravitybox.GCache.Common
{
    public class CacheService<T> : IDisposable
    {
        public CacheService(string server = "localhost", int port = 7373, string container = null)
        {
            if (container != null && container.Trim() == string.Empty)
                throw new Exception("Invalid container name!");

            this.Server = server;
            this.Port = port;
            this.Container = container;
            this.CreateConnection();
        }

        public string Server { get; private set; }
        public string Container { get; private set; }
        public int Port { get; private set; }

        public void AddOrUpdate(string key, T value)
        {
            AddInternal(key, value, CacheExpirationMode.None, null, null);
        }

        public void AddOrUpdate(string key, T value, DateTime expiresAt)
        {
            AddInternal(key, value, CacheExpirationMode.Absolute, expiresAt, null);
        }

        public void AddOrUpdate(string key, T value, TimeSpan expiresIn)
        {
            AddInternal(key, value, CacheExpirationMode.Sliding, null, expiresIn);
        }

        private void AddInternal(string key, T value, CacheExpirationMode expireMode, DateTime? expiresAt, TimeSpan? expiresIn)
        {
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    if (value == null)
                        this.DataModelService.AddOrUpdate(this.Container, key, null, expireMode, expiresAt, expiresIn);
                    else
                        this.DataModelService.AddOrUpdate(this.Container, key, value.ObjectToBin(), expireMode, expiresAt, expiresIn);
                });
        }

        public void AddOrUpdate(string key, Func<T> fetchFunction)
        {
            AddInternal(key, fetchFunction, CacheExpirationMode.None, null, null);
        }

        public void AddOrUpdate(string key, Func<T> fetchFunction, DateTime expiresAt)
        {
            AddInternal(key, fetchFunction, CacheExpirationMode.Absolute, expiresAt, null);
        }

        public void AddOrUpdate(string key, Func<T> fetchFunction, TimeSpan expiresIn)
        {
            AddInternal(key, fetchFunction, CacheExpirationMode.Sliding, null, expiresIn);
        }

        private void AddInternal(string key, Func<T> fetchFunction, CacheExpirationMode expireMode, DateTime? expiresAt, TimeSpan? expiresIn)
        {
            var value = fetchFunction();
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    if (value == null)
                        this.DataModelService.AddOrUpdate(this.Container, key, null, expireMode, expiresAt, expiresIn);
                    else
                        this.DataModelService.AddOrUpdate(this.Container, key, value.ObjectToBin(), expireMode, expiresAt, expiresIn);
                });
        }

        public void AddOrUpdateAsync(string key, T value)
        {
            this.IncrementDoneCounter();
            Task.Factory.StartNew(() => AddOrUpdate(key, value))
                .ContinueWith(t => operationComplete());
        }

        public void AddOrUpdateAsync(string key, T value, DateTime expiresAt)
        {
            this.IncrementDoneCounter();
            Task.Factory.StartNew(() => AddOrUpdate(key, value, expiresAt))
                .ContinueWith(t => operationComplete());
        }

        public void AddOrUpdateAsync(string key, T value, TimeSpan expiresIn)
        {
            this.IncrementDoneCounter();
            Task.Factory.StartNew(() => AddOrUpdate(key, value, expiresIn))
                .ContinueWith(t => operationComplete());
        }

        public T Get(string key)
        {
            T retval = default(T);
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    var v = this.DataModelService.Get(this.Container, key);
                    if (v == null) retval = default(T);
                    else retval = v.BinToObject<T>();
                });
            return retval;
        }

        public T GetOrAdd(string key, Func<T> fetchFunction)
        {
            return GetOrAddInternal(key, fetchFunction, CacheExpirationMode.None, null, null);
        }

        public T GetOrAdd(string key, Func<T> fetchFunction, DateTime expiresAt)
        {
            return GetOrAddInternal(key, fetchFunction, CacheExpirationMode.Absolute, expiresAt, null);
        }

        public T GetOrAdd(string key, Func<T> fetchFunction, TimeSpan expiresIn)
        {
            return GetOrAddInternal(key, fetchFunction, CacheExpirationMode.Sliding, null, expiresIn);
        }

        private T GetOrAddInternal(string key, Func<T> fetchFunction, CacheExpirationMode expireMode, DateTime? expiresAt, TimeSpan? expiresIn)
        {
            T cachedValue = this.Get(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            var value = fetchFunction();
            this.AddInternal(key, value, expireMode, expiresAt, expiresIn);
            return value;
        }

        public T GetOrAdd(string key, T value)
        {
            return GetOrAddInternal(key, value, CacheExpirationMode.None, null, null);
        }

        public T GetOrAdd(string key, T value, DateTime expiresAt)
        {
            return GetOrAddInternal(key, value, CacheExpirationMode.Absolute, expiresAt, null);
        }

        public T GetOrAdd(string key, T value, TimeSpan expiresIn)
        {
            return GetOrAddInternal(key, value, CacheExpirationMode.Sliding, null, expiresIn);
        }

        private T GetOrAddInternal(string key, T value, CacheExpirationMode expireMode, DateTime? expiresAt, TimeSpan? expiresIn)
        {
            T cachedValue = this.Get(key);
            if (cachedValue != null)
            {
                return cachedValue;
            }

            this.AddInternal(key, value, expireMode, expiresAt, expiresIn);
            return value;
        }

        public bool Delete(string key, bool isPartial = false)
        {
            return this.DataModelService.Delete(this.Container, key, isPartial);
        }

        public void Clear()
        {
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    this.DataModelService.Clear(this.Container);
                });
        }

        public long Incr(string key)
        {
            long retval = 0;
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    retval = this.DataModelService.Incr(this.Container, key);
                });
            return retval;
        }

        public void IncrAsync(string key)
        {
            this.IncrementDoneCounter();
            Task.Factory.StartNew(() => Incr(key))
                .ContinueWith(t => operationComplete());
        }

        public long Decr(string key)
        {
            long retval = 0;
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    retval = this.DataModelService.Decr(this.Container, key);
                });
            return retval;
        }

        public void DecrAsync(string key)
        {
            this.IncrementDoneCounter();
            Task.Factory.StartNew(() => Decr(key))
                .ContinueWith(t => operationComplete());
        }

        public long GetCounter(string key)
        {
            long retval = 0;
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    retval = this.DataModelService.GetCounter(this.Container, key);
                });
            return retval;
        }

        public void ResetCounter(string key)
        {
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    this.DataModelService.ResetCounter(this.Container, key);
                });
        }

        private long _doneCounter;
        private void operationComplete()
        {
            this.DecrementDoneCounter();
        }
        private void IncrementDoneCounter() { Interlocked.Increment(ref _doneCounter); }
        private void DecrementDoneCounter() { Interlocked.Decrement(ref _doneCounter); }

        public bool IsAsyncComplete
        {
            get { return Interlocked.Read(ref _doneCounter) == 0; }
        }

        private ChannelFactory<ISystemCore> ChannelFactory { get; set; }

        protected ISystemCore DataModelService { get; private set; }

        protected virtual void CreateConnection()
        {
            this.ChannelFactory = InteractDomain.GetFactory(this.Server, this.Port);
            this.DataModelService = ChannelFactory.CreateChannel();
        }

        #region IDisposable Support
        /// <summary />
        protected virtual void Dispose(bool disposing)
        {
            if (ChannelFactory != null)
            {
                try
                {
                    if (ChannelFactory.State != CommunicationState.Faulted)
                        ChannelFactory.Close();
                }
                catch (Exception)
                {
                    //Do Nothing
                }
                finally
                {
                    if (ChannelFactory.State != CommunicationState.Closed)
                        ChannelFactory.Abort();
                    ChannelFactory = null;
                }
            }
        }

        /// <summary />
        public void Dispose()
        {
            var startTime = DateTime.Now;
            while (!this.IsAsyncComplete && DateTime.Now.Subtract(startTime).TotalSeconds < 30)
            {
                System.Threading.Thread.Sleep(25);
            }
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
