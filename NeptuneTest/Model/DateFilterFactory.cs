using com.antlersoft.HostedTools.Archive.Interface;
using com.antlersoft.HostedTools.Archive.Model;
using com.antlersoft.HostedTools.Framework.Model;
using com.antlersoft.HostedTools.Interface;
using System;
using System.ComponentModel.Composition;

namespace com.gt.NeptuneTest.Model
{
    [Export(typeof(IArchiveFilterFactory))]
    public class DateFilterFactory : HostedObjectBase, IArchiveFilterFactory
    {
        public string Name => "DateFilter";

        public IArchiveFilter ConfigureFilter(IHtValue configuration)
        {
            var baseDate = configuration["BaseDate"];
            var result = new DateArchiveFilter();
            result.Offset = TimeSpan.FromTicks(0L);
            if (baseDate != null)
            {
                if (baseDate.IsDictionary)
                {
                    result.Offset = DateTime.UtcNow - DateTime.FromFileTimeUtc(baseDate["Ticks"].AsLong);
                }
                else
                {
                    DateTime parsed;
                    if (DateTime.TryParse(baseDate.AsString, null, System.Globalization.DateTimeStyles.AssumeUniversal, out parsed))
                    {
                        result.Offset = DateTime.UtcNow - parsed;
                    }
                }
            }
            return result;
        }
    }
}
