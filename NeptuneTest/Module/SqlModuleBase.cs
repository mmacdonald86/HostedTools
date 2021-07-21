using com.antlersoft.HostedTools.Archive.Model;
using com.antlersoft.HostedTools.Archive.Model.Configuration;
using com.antlersoft.HostedTools.Framework.Interface.Plugin;
using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Framework.Model;
using com.antlersoft.HostedTools.Interface;
using com.antlersoft.HostedTools.Serialization;
using com.antlersoft.HostedTools.Sql.Interface;
using com.gt.NeptuneTest.Interface;
using Newtonsoft.Json;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
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

        protected void LoadArchive(ISqlConnectionSource connectionSource, IWorkMonitor monitor, string repoFolder, string schemaConfig, string archiveTitle)
        {
            SqlRepositoryConfiguration repoConfig;
            using (var reader = new StreamReader(schemaConfig))
            using (var jsonReader = new JsonTextReader(reader))
            {
                repoConfig = new JsonFactory().GetSerializer().Deserialize<SqlRepositoryConfiguration>(jsonReader);
            }
            var repo = new SqlRepository(repoConfig, connectionSource);
            var folderArchive = new FolderRepository(repoFolder, repo.Schema);
            var spec = folderArchive.AvailableArchives().FirstOrDefault(a => a.Title == archiveTitle);
            if (spec == null)
            {
                monitor.Writer.WriteLine($"No archive with title [{archiveTitle}] found");
                return;
            }

            repo.WriteArchive(folderArchive.GetArchive(spec, monitor), monitor);

        }
    }
}
