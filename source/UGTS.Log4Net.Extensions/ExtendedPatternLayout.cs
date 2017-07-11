using JetBrains.Annotations;
using log4net.Layout;
using log4net.Util;

namespace UGTS.Log4Net.Extensions
{
    /// <summary>
    /// adds support for the ctxid pattern key
    /// </summary>
    [UsedImplicitly]
    public class ExtendedPatternLayout : PatternLayout
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public ExtendedPatternLayout()
        {
            AddConverter(new ConverterInfo { Name = "ctxid", Type = typeof(ContextIdPatternConverter) });
        }
    }
}