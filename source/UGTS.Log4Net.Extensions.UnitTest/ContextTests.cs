using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NUnit.Framework;

namespace UGTS.Log4Net.Extensions.UnitTest
{
    [TestFixture]
    public class ContextTests
    {
        [Test]
        public void Returns_Zero_If_Not_Http_Context()
        {
            Assert.That(WebContext.GetRequestId, Is.EqualTo(0));
        }

        [Test]
        public void Uses_HttpContext_When_Defined()
        {
            var nonHttpResult = WebContext.GetRequestId;

            HttpContext.Current = CreateHttpContext();

            var httpResult = WebContext.GetRequestId;

            Assert.That(httpResult, Is.EqualTo(WebContext.GetRequestId));
            Assert.That(nonHttpResult, Is.Not.EqualTo(httpResult));

            HttpContext.Current = CreateHttpContext();

            var httpResult2 = WebContext.GetRequestId;

            Assert.That(httpResult2, Is.Not.EqualTo(httpResult));

            HttpContext.Current = null;

            var nonHttpResult2 = WebContext.GetRequestId;

            Assert.That(nonHttpResult2, Is.EqualTo(nonHttpResult));
        }

        private static HttpContext CreateHttpContext()
        {
            return new HttpContext(new HttpRequest("", "http://tempuri.org", ""), new HttpResponse(new StringWriter()));
        }
    }
}
