using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Interface;
using com.antlersoft.HostedTools.Serialization;
using com.gt.NeptuneTest.Interface;
using System;
using System.Threading.Tasks;

namespace com.gt.NeptuneTest.Module
{
    public abstract class PostgresModuleBase : SqlModuleBase
    {
        protected const string DB_NAME = "DB_NAME";
        protected override string GetDefaultSkeleton(ITestConfig config)
        {
            return config.PostgresSkeleton;
        }

        protected virtual string GetConnectionParams()
        {
            return $"--host={TestSetup.PoiDbNetworkAddress.Value<string>(SettingManager)} --port=5432 --username=admin";
        }

        protected virtual async Task<JsonHtValue> SetupSkeletonDatabase(ITestInstance config, IHtValue moduleSettings)
        {
            // Setup .pgpass for Postgres
            await config.RunCommandLine("/bin/sh", "-c \"echo '*:5432:*:admin:admin' > $HOME/.pgpass\"");
            await config.RunCommandLine("/bin/sh", "-c \"chmod 0600 $HOME/.pgpass\"");

            var dbname = config.ExpandValue(moduleSettings[DB_NAME].AsString);

            var connectionParams = GetConnectionParams();

            var dropProcess = await config.RunCommandLine("/usr/bin/dropdb", $"--if-exists {connectionParams} {dbname}", false);
            dropProcess.WaitForExit();

            var process = await config.RunCommandLine("/usr/bin/createdb", $"{connectionParams} {dbname}");
            if (process == null || !process.HasExited || process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to create postgres db {dbname}");
            }

            var skeleton = GetSkeleton(config, moduleSettings);
            process = await config.RunCommandLine("/usr/bin/psql", $"{connectionParams} -f {skeleton} {dbname}");
            if (process == null || !process.HasExited || process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to load skeleton {skeleton} into Postgres db {dbname}");
            }

            var result = new JsonHtValue();
            result[DB_NAME] = new JsonHtValue(dbname);

            return result;
        }

        protected virtual async Task DropDatabase(ITestInstance config, IHtValue teardownSettings)
        {
            var connectionParams = GetConnectionParams();
            var dbname = teardownSettings[DB_NAME].AsString;

            var process = await config.RunCommandLine("/usr/bin/dropdb", $"{connectionParams} {dbname}");
            if (process == null || !process.HasExited || process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to drop postgres db {dbname}");
            }
        }
    }
}
