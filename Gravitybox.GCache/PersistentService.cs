using System;
using System.Linq;
using System.ServiceProcess;
using System.ServiceModel;
using System.Configuration;
using Gravitybox.GCache.Common;

namespace Gravitybox.GCache
{
    public partial class PersistentService : ServiceBase
    {
        #region Class Members

        private static ISystemCore _core = null;

        #endregion

        #region Constructor

        public PersistentService()
        {
            InitializeComponent();
        }

        #endregion

        #region Service Events

        protected override void OnStart(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.Start();
        }

        protected override void OnStop()
        {
            //KillTimer();
            try
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }

            Logger.LogInfo("Services Stopped");
        }

        protected override void OnShutdown()
        {
            try
            {
                base.OnShutdown();
                Logger.LogInfo("Services ShutDown");
                //KillTimer();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        #endregion

        #region Methods

        public void Start()
        {
            try
            {
                //Do this to avoid an infinite hang if the firewall has blocked the port
                //You cannot shut down the service if blocked because it never finishes startup
                var t = new System.Threading.Thread(StartupEndpoint);
                t.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        private const int DEFAULTPORT = 7373;
        private static void StartupEndpoint()
        {
            try
            {
                //Get the port
                var portValue = ConfigurationManager.AppSettings["Port"];
                int port;
                if (!int.TryParse(portValue, out port))
                    port = DEFAULTPORT;
                if (port < 500) port = DEFAULTPORT;

                //Setup the end point
                var service = new SystemCore();
                var primaryAddress = new Uri("net.tcp://localhost:"+ port + "/__cacheCore");
                var primaryHost = new ServiceHost(service, primaryAddress);

                //Initialize the service
                var netTcpBinding = new CompressedNetTcpBinding();
                netTcpBinding.Security.Mode = SecurityMode.None;
                primaryHost.AddServiceEndpoint(typeof(ISystemCore), netTcpBinding, string.Empty);
                primaryHost.Open();

                //Create Core Listener
                var primaryEndpoint = new EndpointAddress(primaryHost.BaseAddresses.First().AbsoluteUri);
                var primaryClient = new ChannelFactory<ISystemCore>(netTcpBinding, primaryEndpoint);
                _core = primaryClient.CreateChannel();

                Logger.LogInfo("System running");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }

        #endregion

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.LogError(e.ExceptionObject as Exception);
            }
            catch (Exception)
            {
                //Do Nothing
            }
        }
    }
}
