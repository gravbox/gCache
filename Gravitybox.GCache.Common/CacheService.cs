using System;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gravitybox.GCache.Common
{
    /// <summary>
    /// The client object used to create a strongly-typed cache interface with the server
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CacheService<T> : IDisposable
    {
        /// <summary>
        /// Creates a strongly-typed cache interface
        /// </summary>
        /// <param name="server">The cache service network name or IP address</param>
        /// <param name="port">The cache service network port</param>
        /// <param name="container">The optional container name used to group cache values groups</param>
        public CacheService(string server = "localhost", int port = 7373, string container = null)
        {
            if (container != null && container.Trim() == string.Empty)
                throw new Exception("Invalid container name!");

            this.Server = server;
            this.Port = port;
            this.Container = container;
            this.CreateConnection();
        }

        /// <summary>
        /// The key used to encrypt data before it is sent over the wire and stored.
        /// </summary>
        /// <remarks>In cache, the data is encrypted.</remarks>
        public byte[] EncryptionKey
        {
            get { return _encryptionKey; }
            set
            {
                if (value == null)
                    _encryptionKey = value;
                else if (value.Length == 16)
                    _encryptionKey = value;
                else
                    throw new Exception("Encryption key must be exactly 16 bytes.");
            }
        }
        private byte[] _encryptionKey = null;

        /// <summary>
        /// Determines if compression is used to store the values
        /// </summary>
        public bool UseCompression { get; set; } = false;

        /// <summary>
        /// The service machine's name or IP address
        /// </summary>
        public string Server { get; private set; }

        /// <summary>
        /// The optional container name used to group values into a container group
        /// </summary>
        public string Container { get; private set; }

        /// <summary>
        /// The service machine's port
        /// </summary>
        /// <remarks>The default port is 7373</remarks>
        public int Port { get; private set; }

        /// <summary>
        /// Adds the value if it does not exist or updates the value if it does exist
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddOrUpdate(string key, T value)
        {
            AddInternal(key, value, CacheExpirationMode.None, null, null);
        }

        /// <summary>
        /// Adds the value if it does not exist or updates the value if it does exist
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiresAt"></param>
        public void AddOrUpdate(string key, T value, DateTime expiresAt)
        {
            AddInternal(key, value, CacheExpirationMode.Absolute, expiresAt, null);
        }

        /// <summary>
        /// Adds the value if it does not exist or updates the value if it does exist
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiresIn"></param>
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
                    {
                        var data = value.ObjectToBin();
                        if (this.UseCompression)
                            data = data.Compress();
                        if (this.EncryptionKey != null)
                            data = EncryptionDomain.Encrypt(data, this.EncryptionKey);
                        this.DataModelService.AddOrUpdate(this.Container, key, data, expireMode, expiresAt, expiresIn);
                    }
                });
        }

        /// <summary>
        /// Uses the fetch function to add the resulting value if the key does not exist or updates the value if the key does exist
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fetchFunction"></param>
        public void AddOrUpdate(string key, Func<T> fetchFunction)
        {
            AddInternal(key, fetchFunction, CacheExpirationMode.None, null, null);
        }

        /// <summary>
        /// Uses the fetch function to add the resulting value if the key does not exist or updates the value if the key does exist
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fetchFunction"></param>
        /// <param name="expiresAt"></param>
        public void AddOrUpdate(string key, Func<T> fetchFunction, DateTime expiresAt)
        {
            AddInternal(key, fetchFunction, CacheExpirationMode.Absolute, expiresAt, null);
        }

        /// <summary>
        /// Uses the fetch function to add the resulting value if the key does not exist or updates the value if the key does exist
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fetchFunction"></param>
        /// <param name="expiresIn"></param>
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
                    {
                        var data = value.ObjectToBin();
                        if (this.UseCompression)
                            data = data.Compress();
                        if (this.EncryptionKey != null)
                            data = EncryptionDomain.Encrypt(data, this.EncryptionKey);
                        this.DataModelService.AddOrUpdate(this.Container, key, data, expireMode, expiresAt, expiresIn);
                    }
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

        /// <summary>
        /// Get a value from cache. If no value is found, null is returned.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get(string key)
        {
            byte[] results = null;
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    results = this.DataModelService.Get(this.Container, key);
                });

            T retval = default(T);
            if (results == null) retval = default(T);
            else
            {
                if (this.EncryptionKey != null)
                    results = EncryptionDomain.Decrypt(results, this.EncryptionKey);
                if (this.UseCompression)
                    results = results.Decompress();
                retval = results.BinToObject<T>();
            }

            return retval;
        }

        /// <summary>
        /// Get a value from cache if one exists. If no value is found, the fetch function is executed to generate a new value to add to cache and return
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fetchFunction"></param>
        /// <returns></returns>
        public T GetOrAdd(string key, Func<T> fetchFunction)
        {
            return GetOrAddInternal(key, fetchFunction, CacheExpirationMode.None, null, null);
        }

        /// <summary>
        /// Get a value from cache if one exists. If no value is found, the fetch function is executed to generate a new value to add to cache and return
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fetchFunction"></param>
        /// <param name="expiresAt"></param>
        /// <returns></returns>
        public T GetOrAdd(string key, Func<T> fetchFunction, DateTime expiresAt)
        {
            return GetOrAddInternal(key, fetchFunction, CacheExpirationMode.Absolute, expiresAt, null);
        }

        /// <summary>
        /// Get a value from cache if one exists. If no value is found, the fetch function is executed to generate a new value to add to cache and return
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fetchFunction"></param>
        /// <param name="expiresIn"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get a value from cache if one exists. If no value is found, the specified value is added to cache and returned
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public T GetOrAdd(string key, T value)
        {
            return GetOrAddInternal(key, value, CacheExpirationMode.None, null, null);
        }

        /// <summary>
        /// Get a value from cache if one exists. If no value is found, the specified value is added to cache and returned
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiresAt"></param>
        /// <returns></returns>
        public T GetOrAdd(string key, T value, DateTime expiresAt)
        {
            return GetOrAddInternal(key, value, CacheExpirationMode.Absolute, expiresAt, null);
        }

        /// <summary>
        /// Get a value from cache if one exists. If no value is found, the specified value is added to cache and returned
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiresIn"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Delete an item from cache based on the specified key. If using partial matching then all items that start with the specified key are matched.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="isPartial"></param>
        /// <returns></returns>
        public bool Delete(string key, bool isPartial = false)
        {
            return this.DataModelService.Delete(this.Container, key, isPartial);
        }

        /// <summary>
        /// Removes all items from the cache
        /// </summary>
        public void Clear()
        {
            RetryHelper.DefaultRetryPolicy(5)
                .Execute(() =>
                {
                    this.DataModelService.Clear(this.Container);
                });
        }

        /// <summary>
        /// Increment a counter variable by 1
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Decrement a counter variable by 1
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the current value of the specfied counter variable
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Determines if all asyncronous processes are complete
        /// </summary>
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
        void IDisposable.Dispose()
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
