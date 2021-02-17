using Microsoft.VisualStudio.TestTools.UnitTesting;
using LoadGenerator_Swaroop_Project;
using System.Net;
using System.Net.Http;

namespace LoadGeneratorTests
{
    [TestClass]
    public class RequestManagerTests
    {
        [TestMethod]
        public void Test_EnqueueNewRequest()
        {
            MockHttpClient mockHttp = new MockHttpClient();

            mockHttp.PrepareGetAsyncResponse(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                }
            );

            ProgramService service = new ProgramService("test/url", mockHttp);
            RequestsManager manager = new RequestsManager(service);

            manager.EnqueueNewRequest();
            Assert.AreEqual(1, manager.TotalActiveRequests());
        }
    }
}
