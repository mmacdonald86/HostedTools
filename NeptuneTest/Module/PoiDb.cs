using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Interface;
using com.antlersoft.HostedTools.Sql.Model;
using com.gt.NeptuneTest.Interface;
using System.Threading.Tasks;

namespace com.gt.NeptuneTest.Module
{
    public class PoiDb : PostgresModuleBase
    {
        public override string Name => "PoiDb";

        public async override Task<IHtValue> Setup(ITestInstance config, IHtValue moduleSettings)
        {
            var result = await SetupSkeletonDatabase(config, moduleSettings);

            string dockerHost = TestSetup.PoiDbNetworkAddress.Value<string>(SettingManager);

            // If not UseDocker, we must be *in* docker, so need host to load archive to to be docker poidb
            string host = TestSetup.UseDocker.Value<bool>(SettingManager) ? "localhost" : dockerHost;

            var connection = new PostgreSqlConnectionSource($"Host={host};Port=5432;Username=admin;Password=admin;Database={result[DB_NAME].AsString}");

            LoadArchive(connection, config.Monitor, GetArchiveFolder(config, moduleSettings), config.ExpandValue(moduleSettings[RepoConfig].AsString), config.ExpandValue(moduleSettings[ArchiveTitle].AsString));

            config.SetConfigurationValue("POIDBCONFIG_SERVER", $"\"{dockerHost}:5432\"");
            config.SetConfigurationValue("POIDBCONFIG_USER", "\"admin\"");
            config.SetConfigurationValue("POIDBCONFIG_PWD", "\"admin\"");
            return result;
        }

        public async override Task Teardown(ITestInstance config, IHtValue valuesFromSetup)
        {
            await DropDatabase(config, valuesFromSetup);
        }
    }
}
