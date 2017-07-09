using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using NUnit.Framework;

namespace UGTS.Log4Net.Extensions.UnitTest
{
    [TestFixture]
    public class ContextTests
    {
        [Test]
        public void Repeats_Same_Id_For_Same_Thread()
        {
            Assert.That(Context.GetOrCreateContextId, Is.EqualTo(Context.GetOrCreateContextId));
        }

        [Test]
        public void Returns_Unique_Id_For_Each_Different_Thread()
        {
            var found = new ConcurrentDictionary<long, object> {[Context.GetOrCreateContextId] = null};

            var threads = Enumerable.Repeat(0, 10).Select(i => new Thread(() =>
            {
                found[Context.GetOrCreateContextId] = null;
                found[Context.GetOrCreateContextId] = null;
            })).ToList();

            foreach (var t in threads)
            {
                t.Start();
            }

            foreach (var t in threads)
            {
                t.Join();
            }

            Assert.That(found.Count, Is.EqualTo(11));
        }

        [Test]
        public void Uses_HttpContext_When_Defined()
        {
            var nonHttpResult = Context.GetOrCreateContextId;

            HttpContext.Current = CreateHttpContext();

            var httpResult = Context.GetOrCreateContextId;

            Assert.That(httpResult, Is.EqualTo(Context.GetOrCreateContextId));
            Assert.That(nonHttpResult, Is.Not.EqualTo(httpResult));

            HttpContext.Current = CreateHttpContext();

            var httpResult2 = Context.GetOrCreateContextId;

            Assert.That(httpResult2, Is.Not.EqualTo(httpResult));

            HttpContext.Current = null;

            var nonHttpResult2 = Context.GetOrCreateContextId;

            Assert.That(nonHttpResult2, Is.EqualTo(nonHttpResult));
        }

        private static HttpContext CreateHttpContext()
        {
            return new HttpContext(new HttpRequest("", "http://tempuri.org", ""), new HttpResponse(new StringWriter()));
        }
    }
}
