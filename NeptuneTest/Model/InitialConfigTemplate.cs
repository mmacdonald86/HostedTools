using com.antlersoft.HostedTools.Interface;
using System.Collections.Generic;

namespace com.gt.NeptuneTest.Model
{
    public class InitialConfigTemplate
    {
        public List<string> ConfigFiles { get; set; }
        public IHtValue RegExpReplace { get; set; }
        public IHtValue ConfigValues { get; set; }
    }
}
