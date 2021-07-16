using com.antlersoft.HostedTools.Interface;
using com.gt.NeptuneTest.Interface;
using System.Threading.Tasks;

namespace com.gt.NeptuneTest.Module
{
    public class XadDb : MySqlModuleBase
    {
        public override string Name => "XadDb";

        public override async Task<IHtValue> Setup(ITestInstance config, IHtValue moduleSettings)
        {
            var result = await SetupDaemon(config, moduleSettings).ConfigureAwait(false);
            config.SetConfigurationValue("XADDB_USER", "root");
            config.SetConfigurationValue("XADDB_PASSWORD", string.Empty);
            config.SetConfigurationValue("XADDB_SERVER", "localhost");
            config.SetConfigurationValue("XADDB_PORT", result[PORT].AsString);
            return result;
        }

        public override async Task Teardown(ITestInstance config, IHtValue valuesFromSetup)
        {
            await ShutdownDaemon(config, valuesFromSetup);
        }
    }
}
