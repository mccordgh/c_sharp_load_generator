using System.Net.Http;
using System.Threading.Tasks;

namespace LoadGenerator_Swaroop_Project
{
    public class ProgramClient
    {
        private HttpClient Client = new HttpClient();
        private string TestUrl;

        public ProgramClient(string url)
        {
            TestUrl = url;
        }

        public async Task<HttpResponseMessage> GetTestUrlAsync()
        {
            return await Client.GetAsync(TestUrl);
        }
    }
}
