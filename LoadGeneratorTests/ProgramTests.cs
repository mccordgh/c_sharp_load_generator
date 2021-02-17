using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using LoadGenerator_Swaroop_Project;

namespace LoadGeneratorTests
{
    [TestClass]
    public class ProgramTests
    {
        [DataRow(123, 3435)]
        [DataRow(2100, 300)]
        [DataRow(34, 546)]
        [DataTestMethod]
        public void Test_BuildOutputForEachRequestStatus(int numberStatusOk, int numberStatusBadRequest)
        {
            ConcurrentDictionary<HttpStatusCode, int> completedRequests = new ConcurrentDictionary<HttpStatusCode, int>();
            completedRequests.TryAdd(HttpStatusCode.OK, numberStatusOk);
            completedRequests.TryAdd(HttpStatusCode.BadRequest, numberStatusBadRequest);

            int totalRequestsCompleted;
            List<string> output = Program.BuildOutputForEachRequestStatus(completedRequests, out totalRequestsCompleted);

            // can't rely on ConcurrentDictionary to preserve order of keys so we don't know which index will have which formatted output line.
            string okMatch = output.Find(item => item.Contains($"Status Code {HttpStatusCode.OK}"));
            string badReqMatch = output.Find(item => item.Contains($"Status Code {HttpStatusCode.BadRequest}"));

            Assert.IsNotNull(okMatch);
            Assert.IsNotNull(badReqMatch);
        }

        [DataRow(1, 3)]
        [DataRow(22, 2)]
        [DataRow(333, 1)]
        [DataRow(4444, 0)]
        [DataRow(55555, 0)]
        [DataTestMethod]
        public void Test_GetSpacerString(int number, int expectedSpaces)
        {
            string spaces = Program.GetSpacerString(number);

            Assert.AreEqual(new string(' ', expectedSpaces), spaces);
        }
    }
}
