using JetBrains.Annotations;
using log4net.Layout;
using log4net.Util;

namespace UGTS.Log4Net.Extensions
{
    [UsedImplicitly]
    public class ExtendedPatternLayout : PatternLayout
    {
        public ExtendedPatternLayout()
        {
            AddConverter(new ConverterInfo { Name = "ctxid", Type = typeof(ContextIdPatternConverter) });
        }
    }
}