using Microsoft.Extensions.Logging;
using System.Net.Http.Json;


namespace RaftElection
{
    public class Gateway
    {
        private readonly List<string> nodes;
        private string leadersUrl;

        public HttpClient httpClient { get; set; }
        public Gateway(List<string> nodes, ILogger<Gateway> logger)
        {
            this.httpClient = new HttpClient();
            this.nodes = nodes;
        }
        public async Task GetLeaderAsync()
        {
            foreach (var node in nodes)
            {
                var isLeader = await httpClient.GetFromJsonAsync<bool>($"{node}/leader");
                if (isLeader)
                {
                    leadersUrl = node;
                    break;
                }
            }

        }

        public async Task WriteAsync(string key, int value)
        {
            //make sure we have current leader
            await GetLeaderAsync();

            var pair = new KeyValue
            {
                key = key,
                value = value
            };

            //send the value
            var response = httpClient.PostAsJsonAsync($"{leadersUrl}/write", pair);
        }

        public async Task<(int?, int?)> EventualGetAsync(string value)
        {
            var url = nodes.FirstOrDefault();
            if (url != null)
            {
                return await httpClient.GetFromJsonAsync<(int?, int?)>($"{url}/eventalGet/{value}");
            }
            else
                return (null, null);
        }

        public async Task<(int?, int?)> StrongGetAsync(string value)
        {
            return await httpClient.GetFromJsonAsync<(int?, int?)>($"{leadersUrl}/strongGet/{value}");

        }

        public async Task<bool> CompareVersionAndSwapAsync(SwapInfo swap)
        {
            var response = await httpClient.PostAsJsonAsync($"{leadersUrl}/compareandswap", swap);
            if (response.IsSuccessStatusCode)
                return true;
            else
                return false;
        }


    }
}
