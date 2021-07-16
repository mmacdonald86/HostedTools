using com.antlersoft.HostedTools.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace com.gt.NeptuneTest.Interface
{
    public interface ITestConfig
    {
        string MySqlSkeleton { get; }
        string PostgresSkeleton { get; }
        string ArchiveFolder { get; }
        IHtValue Modules { get; }

    }
}
