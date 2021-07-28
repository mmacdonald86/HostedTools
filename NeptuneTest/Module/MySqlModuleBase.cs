using com.antlersoft.HostedTools.Archive.Model;
using com.antlersoft.HostedTools.Archive.Model.Configuration;
using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Interface;
using com.antlersoft.HostedTools.Serialization;
using com.antlersoft.HostedTools.Sql.Model;
using com.gt.NeptuneTest.Interface;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace com.gt.NeptuneTest.Module
{
    abstract public class MySqlModuleBase : SqlModuleBase
    {
        protected const string PID = "PID";
        protected const string PIDFILE = "PIDFILE";
        protected const string BASEDIR = "BASEDIR";
        protected const string PORT = "PORT";

        protected override string GetDefaultSkeleton(ITestConfig config)
        {
            return config.MySqlSkeleton;
        }

        protected virtual async Task<JsonHtValue> SetupDaemon(ITestInstance config, IHtValue moduleSettings)
        {
            var random = new Random();
            var skeleton = GetSkeleton(config, moduleSettings);
            int port = random.Next(15000, 30000);
            var isDocker = TestSetup.UseDocker.Value<bool>(SettingManager);
            if (isDocker)
            {
                port = 15654;
            }
            var basedir = $"/tmp/{Path.GetFileName(config.InstanceFolder)}";
            await config.RunCommandLine("/usr/bin/mkdir", $"{basedir}");
            var datapath = $"{basedir}/data";
            await config.RunCommandLine("/usr/bin/mkdir", $"{datapath}");
            var errorfile = $"{basedir}/mysqld.log";

            await config.RunCommandLine("/bin/chmod", $"0777 {basedir} {datapath}");
            var pidfile = $"{basedir}/mysql.pid";
            var mysqld_common_settings = $"--lower-case-table-names=1 --datadir {datapath} --explicit-defaults-for-timestamp=FALSE --console --log-error={errorfile} --pid-file={pidfile} --port={port} --socket={basedir}/mysql.sock";
            var initProcess = await config.RunCommandLine("/bin/su", $"mysql --shell=/bin/sh -c \"echo Initializing mysqld at {basedir}; /usr/sbin/mysqld {mysqld_common_settings} --initialize-insecure\"");
            if (initProcess == null || ! initProcess.HasExited)
            {
                throw new InvalidOperationException($"Failed to initialize mysql process: {mysqld_common_settings}");
            }
            if (initProcess.ExitCode < 0 )
            {
                throw new InvalidOperationException($"Exit code {initProcess.ExitCode} initializing mysql: {mysqld_common_settings}");
            }
            initProcess = await config.RunCommandLine("/bin/su", $"mysql --shell=/bin/sh -c \"/usr/sbin/mysqld {mysqld_common_settings}\"", true);

            // Wait until you can connect to database successfully
            for (int i = 0; i<10; i++)
            {
                await Task.Delay(2000);
                int connectResult = await RunStrings(config, port, new string[] { "\\q" });
                if (connectResult == 0)
                {
                    break;
                }
                if (i==9)
                {
                    throw new InvalidOperationException($"Failed to connect to mysld instance");
                }
            }
            var pid = initProcess.Id;

            var result = new JsonHtValue();
            result[PID] = new JsonHtValue(pid);
            result[PIDFILE] = new JsonHtValue(pidfile);
            result[BASEDIR] = new JsonHtValue(basedir);
            result[PORT] = new JsonHtValue(port);

            // Load skeleton
            int skelResult = await RunScript(config, GetSkeleton(config, moduleSettings), port);
            if (skelResult < 0)
            {
                throw new InvalidOperationException($"Failed to run skeleton mysql script {GetSkeleton(config, moduleSettings)}");
            }

            if (isDocker)
            {
                await RunStrings(config, port, new string[] {
                    "CREATE USER 'myuser'@'%' IDENTIFIED BY 'mypassword';",
                    "GRANT ALL PRIVILEGES ON *.*  TO 'myuser'@'%' WITH GRANT OPTION;",
                    "FLUSH PRIVILEGES;",
                    "\\q"
                });
            }

            return result;
        }

        protected void LoadArchive(ITestInstance config, IHtValue moduleSettings, int port)
        {
            var sqlConn = TestSetup.UseDocker.Value<bool>(SettingManager) ? new MySqlConnectionSource("127.0.0.1", port, null, "myuser", "mypassword") : new MySqlConnectionSource("localhost", port, "root");
            LoadArchive(sqlConn, config.Monitor, GetArchiveFolder(config, moduleSettings), config.ExpandValue(moduleSettings[RepoConfig].AsString), moduleSettings[ArchiveTitle].AsString); 
        }

        protected virtual async Task ShutdownDaemon(ITestInstance config, IHtValue teardown)
        {
            await config.RunCommandLine("/usr/bin/bash", $"-c \"kill $(cat {teardown[PIDFILE].AsString})\"").ConfigureAwait(false);
            await config.RunCommandLine("/usr/bin/rm", $"-rf {teardown[BASEDIR].AsString}").ConfigureAwait(false);
        }

        protected virtual async Task<int> RunScript(ITestInstance config, string path, int port)
        {
            using (var input = new FileStream(path, FileMode.Open))
            {
                var process = await config.RunCommandLine("/usr/bin/mysql", $"--host=localhost --protocol=TCP --port={port}", false, input);
                if (process == null || ! process.HasExited)
                {
                    return -1;
                }
                var result = process.ExitCode;
                if (result == -1 || (result & 127) != 0)
                {
                    return -1;
                }
                return result >> 8;
            }
        }
        protected virtual async Task<int> RunStrings(ITestInstance config, int port, IEnumerable<string> strings)
        {
            using (var input = new MemoryStream())
            using (var writer = new StreamWriter(input))
            {
                foreach (var s in strings)
                {
                    writer.WriteLine(s);
                }
                writer.Flush();
                input.Seek(0L, SeekOrigin.Begin);
                var process = await config.RunCommandLine("/usr/bin/mysql", $"--host=localhost --protocol=TCP --port={port}", false, input);
                if (process == null || !process.HasExited)
                {
                    return -1;
                }
                var result = process.ExitCode;
                if (result == -1 || (result & 127) != 0)
                {
                    return -1;
                }
                return result >> 8;
            }
        }
    }
}
