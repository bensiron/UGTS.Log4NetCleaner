using System.Threading;
using System.Web;
#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions
{
    public static class WebContext
    {
        private const string IDKey = "UgtsThreadContextID";
        private static long nextID;

        private static long NextID()
        {
            return Interlocked.Increment(ref nextID);
        }

        public static long GetRequestId
        {
            get
            {
                var c = HttpContext.Current;
                if (c == null) return 0;

                var o = c.Items[IDKey];
                if (o is long) return (long)o;
                var n = NextID();
                c.Items[IDKey] = n;
                return n;
            }
        }
    }
}
