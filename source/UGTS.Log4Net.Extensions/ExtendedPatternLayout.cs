using JetBrains.Annotations;
using log4net.Layout;
using log4net.Util;

namespace UGTS.Log4Net.Extensions
{
    /// <summary>
    /// adds support for the reqid pattern key, which evaluates to a unique incrementing id
    /// for each distinct http context (web request), or 0 if not in the context of a web request.
    /// </summary>
    [UsedImplicitly]
    public class ExtendedPatternLayout : PatternLayout
    {
        /// <summary>
        /// default constructor
        /// </summary>
        public ExtendedPatternLayout()
        {
            AddConverter(new ConverterInfo { Name = "reqid", Type = typeof(WebRequestIdPatternConverter) });
        }
    }
}