using System.Net.Http;
using System.Threading.Tasks;

namespace LoadGenerator_Swaroop_Project
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string requestUri);
    }

    public class ProgramHttpClient : IHttpClient
    {
        private readonly HttpClient httpClient;

        public ProgramHttpClient()
        {
            httpClient = new HttpClient();
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await httpClient.GetAsync(url);
        }
    }

    public class ProgramClient
    {
        private readonly IHttpClient Client;
        private string TestUrl;

        public ProgramClient(string url, IHttpClient client)
        {
            Client = client;
            TestUrl = url;
        }

        public async Task<HttpResponseMessage> GetTestUrlAsync()
        {
            return await Client.GetAsync(TestUrl);
        }
    }
}
