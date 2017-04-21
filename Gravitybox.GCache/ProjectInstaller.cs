using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Reflection;

namespace Gravitybox.GCache
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}