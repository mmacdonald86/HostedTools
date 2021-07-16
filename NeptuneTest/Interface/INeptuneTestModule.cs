using com.antlersoft.HostedTools.Framework.Interface;
using com.antlersoft.HostedTools.Framework.Interface.Setting;
using com.antlersoft.HostedTools.Interface;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace com.gt.NeptuneTest.Interface
{
    public interface INeptuneTestModule : IHostedObject
    {
        /// <summary>
        /// Should be a valid identifier (or folder name) unique among modules
        /// </summary>
        string Name { get; }

        Task<IHtValue> Setup(ITestInstance config, IHtValue moduleSettings);
        Task Teardown(ITestInstance config, IHtValue valuesFromSetup);
    }
}
