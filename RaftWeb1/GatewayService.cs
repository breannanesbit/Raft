using RaftElection;
using System.Net.Http.Json;

namespace RaftWeb1
{
    public class GatewayService
    {
        private readonly HttpClient httpClient;

        public GatewayService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<int> GetInfor(string key)
        {
            return await httpClient.GetFromJsonAsync<int>($"gateway/strongGet/{key}");
        }

        public async Task NewValue(string username, int value)
        {
            var pair = new KeyValue
            {
                key = username,
                value = value
            };
            await httpClient.PostAsJsonAsync($"gateway/newValue", pair);

        }


    }
}
