using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Framework.Model;
using com.antlersoft.HostedTools.Interface;
using com.gt.NeptuneTest.Interface;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace com.gt.NeptuneTest.Module
{
    [InheritedExport(typeof(INeptuneTestModule))]
    abstract public class SqlModuleBase : HostedObjectBase, INeptuneTestModule
    {
        internal const string Skeleton = "Skeleton";
        internal const string ArchiveFolder = "ArchiveFolder";
        internal const string RepoConfig = "RepoConfig";
        internal const string ArchiveTitle = "ArchiveTitle";

        [Import]
        public ISettingManager SettingManager { get; set; }

        abstract public string Name { get; }

        abstract public Task<IHtValue> Setup(ITestInstance config, IHtValue moduleSettings);

        abstract public Task Teardown(ITestInstance config, IHtValue valuesFromSetup);

        protected abstract string GetDefaultSkeleton(ITestConfig config);

        protected virtual string GetArchiveFolder(ITestInstance config, IHtValue moduleSettings)
        {
            var archiveSetting = moduleSettings[ArchiveFolder];
            if (archiveSetting != null)
            {
                return config.ExpandValue(archiveSetting.AsString);
            }
            return config.ExpandValue(config.Config.ArchiveFolder);
        }

        protected virtual string GetSkeleton(ITestInstance config, IHtValue moduleSettings)
        {
            var setting = moduleSettings[Skeleton];
            if (setting != null)
            {
                return config.ExpandValue(setting.AsString);
            }
            return config.ExpandValue(GetDefaultSkeleton(config.Config));
        }
    }
}
