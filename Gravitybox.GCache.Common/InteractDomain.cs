using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace Gravitybox.GCache.Common
{
    /// <summary />
    internal static class InteractDomain
    {
        /// <summary />
        public static ChannelFactory<ISystemCore> GetFactory(string serverName)
        {
            return GetFactory(serverName, 1973);
        }

        /// <summary />
        public static ChannelFactory<ISystemCore> GetFactory(string serverName, int port)
        {
            var myBinding = new CompressedNetTcpBinding() { MaxBufferSize = 10 * 1024 * 1024, MaxReceivedMessageSize = 10 * 1024 * 1024, MaxBufferPoolSize = 10 * 1024 * 1024 };
            myBinding.ReaderQuotas.MaxStringContentLength = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxBytesPerRead = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxArrayLength = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxDepth = 10 * 1024 * 1024;
            myBinding.ReaderQuotas.MaxNameTableCharCount = 10 * 1024 * 1024;
            myBinding.Security.Mode = SecurityMode.None;
            var myEndpoint = new EndpointAddress("net.tcp://" + serverName + ":" + port + "/__cacheCore");
            return new ChannelFactory<ISystemCore>(myBinding, myEndpoint);
        }

        public static ISystemCore GetCache(string serverName, int port)
        {
            var factory = InteractDomain.GetFactory(serverName, port);
            return factory.CreateChannel();
        }

    }
}