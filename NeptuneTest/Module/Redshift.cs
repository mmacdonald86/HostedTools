

using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Interface;
using com.antlersoft.HostedTools.Sql.Model;
using com.gt.NeptuneTest.Interface;

namespace com.gt.NeptuneTest.Module
{
    public class Redshift : PoiDb
    {
        public override string Name => "Redshift";

        public async override System.Threading.Tasks.Task<IHtValue> Setup(ITestInstance config, IHtValue moduleSettings)
        {
            var result = await SetupSkeletonDatabase(config, moduleSettings);

            string dockerHost = TestSetup.PoiDbNetworkAddress.Value<string>(SettingManager);

            // If not UseDocker, we must be *in* docker, so need host to load archive to to be docker poidb
            string host = TestSetup.UseDocker.Value<bool>(SettingManager) ? "localhost" : dockerHost;

            var connection = new PostgreSqlConnectionSource($"Host={host};Port=5432;Username=admin;Password=admin;Database={result[DB_NAME].AsString}");

            LoadArchive(config, connection, config.Monitor, GetArchiveFolder(config, moduleSettings), config.ExpandValue(moduleSettings[RepoConfig].AsString), config.ExpandValue(moduleSettings[ArchiveTitle].AsString));

            config.SetConfigurationValue("REDSHIFT_HOST", $"\"{dockerHost}\"");
            config.SetConfigurationValue("REDSHIFT_DB", $"\"{result[DB_NAME].AsString}\"");
            config.SetConfigurationValue("REDSHIFT_USER", "\"admin\"");
            config.SetConfigurationValue("REDSHIFT_PWD", "\"admin\"");
            config.SetConfigurationValue("REDSHIFT_PORT", "\"5432\"");
            return result;
        }
    }
}
