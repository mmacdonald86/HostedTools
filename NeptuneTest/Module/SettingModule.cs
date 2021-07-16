using com.antlersoft.HostedTools.Framework.Model;
using com.antlersoft.HostedTools.Interface;
using com.gt.NeptuneTest.Interface;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace com.gt.NeptuneTest.Module
{
    [Export(typeof(INeptuneTestModule))]
    public class SettingModule : HostedObjectBase, INeptuneTestModule
    {
        public string Name => "Setting";

        public Task<IHtValue> Setup(ITestInstance config, IHtValue moduleSettings)
        {
            if (moduleSettings.IsDictionary)
            {
                foreach (var f in moduleSettings.AsDictionaryElements)
                {
                    config.SetConfigurationValue(f.Key, f.Value.AsString);
                }
            }
            return Task.FromResult(moduleSettings);
        }

        public Task Teardown(ITestInstance config, IHtValue valuesFromSetup)
        {
            return Task.CompletedTask;
        }
    }
}
