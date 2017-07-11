using System.IO;
using log4net.Util;

namespace UGTS.Log4Net.Extensions
{
    /// <summary>
    /// Writes an auto-increment context id which is unique per http request and managed thread
    /// </summary>
    public class ContextIdPatternConverter : PatternConverter
    {
        /// <summary>
        /// Writes an auto-increment context id which is unique per http request and managed thread
        /// </summary>
        protected override void Convert(TextWriter writer, object state)
        {
            writer.Write(Context.GetOrCreateContextId);
        }
    }
}
