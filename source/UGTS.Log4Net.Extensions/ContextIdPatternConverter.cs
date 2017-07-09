using System.IO;
using log4net.Util;

namespace UGTS.Log4Net.Extensions
{
    public class ContextIdPatternConverter : PatternConverter
    {
        protected override void Convert(TextWriter writer, object state)
        {
            writer.Write(Context.GetOrCreateContextId);
        }
    }
}
