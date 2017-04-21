using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Configuration;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace Gravitybox.GCache
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.LogInfo("Initializing Service.....");
#if DEBUG
            Logger.LogInfo("(Debug Build)");
#endif
            if (args.Any(x => x == "-console" || x == "/console"))
            {
                try
                {
                    var service = new PersistentService();
                    service.Start();
                    Console.WriteLine("Press <ENTER> to stop...");
                    Console.ReadLine();
                    service.Stop();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to start service from console.");
                    throw;
                }
            }
            else
            {
                try
                {
                    var servicesToRun = new ServiceBase[]
                                        {
                                            new PersistentService()
                                        };
                    ServiceBase.Run(servicesToRun);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to start service.");
                }
            }
        }
    }
}
