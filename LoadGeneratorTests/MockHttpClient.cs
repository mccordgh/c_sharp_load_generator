using System.Collections.Generic;
using System.Threading.Tasks;
using LoadGenerator_Swaroop_Project;
using System.Net.Http;
using System;

namespace LoadGeneratorTests
{
    interface IMockHttpClient : IHttpClient
    {
        void PrepareGetAsyncResponse(HttpResponseMessage response);
    }

    class MockHttpClient : IHttpClient, IDisposable
    {
        private List<HttpResponseMessage> preparedResponses = new List<HttpResponseMessage>();

        public void PrepareGetAsyncResponse(HttpResponseMessage response)
        {
            preparedResponses.Insert(0, response);
        }

        public void Dispose()
        {}

        public async Task<HttpResponseMessage> GetAsync(string uri)
        {
            HttpResponseMessage response = preparedResponses[0];
            preparedResponses.RemoveAt(0);

            return await Task.Run(() => response);
        }
    }
}
