using com.antlersoft.HostedTools.Interface;
using com.gt.NeptuneTest.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace com.gt.NeptuneTest.Model
{
    /// <summary>
    /// Represents the .json file with configurations for all the modules required for the test
    /// </summary>
    public class TestConfig : ITestConfig
    {
        public string MySqlSkeleton {get; set;}
        public string PostgresSkeleton { get; set; }
        public string ArchiveFolder { get; set; }
        public IHtValue Modules  { get; set; }
    }
}
