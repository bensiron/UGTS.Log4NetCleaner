using System;
using System.Threading;
using System.Web;
#pragma warning disable 1591

namespace UGTS.Log4Net.Extensions
{
    public static class Context
    {
        private const string IDKey = "UgtsThreadContextID";
        private static long nextID;

        [ThreadStatic]
        private static long threadID;

        private static long NextID()
        {
            return Interlocked.Increment(ref nextID);
        }

        public static long GetOrCreateContextId
        {
            get
            {
                var c = HttpContext.Current;
                if (c != null) return GetOrCreateHttpContextId(c);

                // otherwise assign it to the thread
                // (don't simply use the ManagedThreadId because it will conflict with the NextID() values)
                if (threadID == 0) threadID = NextID();
                return threadID;
            }
        }

        private static long GetOrCreateHttpContextId(HttpContext c)
        {
            var o = c.Items[IDKey];
            if (o is long) return (long) o;
            var n = NextID();
            c.Items[IDKey] = n;
            return n;
        }
    }
}
