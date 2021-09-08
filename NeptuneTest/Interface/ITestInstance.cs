using com.antlersoft.HostedTools.Archive.Interface;
using com.antlersoft.HostedTools.Framework.Interface.Plugin;
using com.antlersoft.HostedTools.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace com.gt.NeptuneTest.Interface
{
    /// <summary>
    /// Represents the specific test environment that is being set up or torn down
    /// </summary>
    public interface ITestInstance
    {
        /// <summary>
        /// All known modules available to be configured for the instance
        /// </summary>
        IList<INeptuneTestModule> AllModules { get; }

        /// <summary>
        /// Temporary folder where instance data will reside; this folder and all contents
        /// will be removed after cleanup of all modules
        /// </summary>
        string InstanceFolder { get; }

        /// <summary>
        /// Path to neptune home (ordinarily /home/xad/neptune)
        /// </summary>
        string NeptuneHome { get; }

        /// <summary>
        /// Path to root of Neptune repository (ordinarily /neptune-src)
        /// </summary>
        string NeptuneSource { get; }

        IWorkMonitor Monitor { get; }

        ITestConfig Config { get; }

        /// <summary>
        /// Configured archive filters for this test instance, in the order they should be applied
        /// </summary>
        IEnumerable<IArchiveFilter> ArchiveFilters { get; }

        string ExpandValue(string v);

        /// <summary>
        /// Modules only call during setup; if called during teardown throws exception
        /// </summary>
        /// <param name="key">Configuration key (as it would appear in configuration file)</param>
        /// <param name="value">Value; values enclosed in {} are expanded as if portion enclosed represents key</param>
        void SetConfigurationValue(string key, string value);
        string GetExpandedConfigurationValue(string key);

        void SetConfigurationValues(string key, IEnumerable<string> value);
        IHtValue GetExpandedConfigurationValues(string key);


        Task<Process> RunCommandLine(string executable, string arguments, bool detach = false, Stream input=null);
    }
}
